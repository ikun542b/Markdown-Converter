using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using MarkdownDocumentConverter.Core.Exceptions;
using MarkdownDocumentConverter.Core.Models;

namespace MarkdownDocumentConverter.Core.Formatting;

public sealed class DocxPostProcessor
{
    private const string HeaderRelationshipType =
        "http://schemas.openxmlformats.org/officeDocument/2006/relationships/header";

    private const string FooterRelationshipType =
        "http://schemas.openxmlformats.org/officeDocument/2006/relationships/footer";

    private const string HeaderContentType =
        "application/vnd.openxmlformats-officedocument.wordprocessingml.header+xml";

    private const string FooterContentType =
        "application/vnd.openxmlformats-officedocument.wordprocessingml.footer+xml";

    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace ContentTypes = "http://schemas.openxmlformats.org/package/2006/content-types";
    private static readonly XNamespace Dc = "http://purl.org/dc/elements/1.1/";

    public void Apply(
        string docxPath,
        string inputPath,
        DocumentStyle style,
        PageSize pageSize,
        string? fontFamily,
        double fontScale)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(docxPath);
        ArgumentNullException.ThrowIfNull(style);

        if (!File.Exists(docxPath))
        {
            throw new ConversionException($"找不到需要后处理的 DOCX：{docxPath}");
        }

        using var archive = ZipFile.Open(docxPath, ZipArchiveMode.Update);

        var styles = LoadXml(archive, "word/styles.xml");
        ApplyTypography(styles, style, fontFamily, fontScale);
        SaveXml(archive, "word/styles.xml", styles);

        var document = LoadXml(archive, "word/document.xml");
        var relationships = LoadXml(archive, "word/_rels/document.xml.rels");
        var contentTypes = LoadXml(archive, "[Content_Types].xml");
        var (title, author) = ReadCoreProperties(archive);
        var sectPr = GetOrCreateSectionProperties(document);

        sectPr.Elements(W + "headerReference").Remove();
        sectPr.Elements(W + "footerReference").Remove();
        sectPr.Elements(W + "pgSz").Remove();
        sectPr.Elements(W + "pgMar").Remove();
        sectPr.Elements(W + "pgNumType").Remove();

        var references = new List<XElement>();
        var fontSize = FooterFontSize(style, fontScale);

        if (style.HeaderEnabled)
        {
            var relationshipId = EnsureRelationship(
                relationships,
                HeaderRelationshipType,
                "header1.xml");

            references.Add(new XElement(
                W + "headerReference",
                new XAttribute(W + "type", "default"),
                new XAttribute(R + "id", relationshipId)));

            SaveXml(
                archive,
                "word/header1.xml",
                CreateHeader(title, author, inputPath, fontFamily, fontSize));

            EnsureContentType(contentTypes, "/word/header1.xml", HeaderContentType);
        }

        if (style.FooterEnabled || style.PageNumberEnabled)
        {
            var relationshipId = EnsureRelationship(
                relationships,
                FooterRelationshipType,
                "footer1.xml");

            references.Add(new XElement(
                W + "footerReference",
                new XAttribute(W + "type", "default"),
                new XAttribute(R + "id", relationshipId)));

            SaveXml(
                archive,
                "word/footer1.xml",
                CreateFooter(style, fontFamily, fontSize));

            EnsureContentType(contentTypes, "/word/footer1.xml", FooterContentType);
        }

        if (references.Count > 0)
        {
            sectPr.AddFirst(references);
        }

        AddPageProperties(sectPr, style, pageSize);

        var settings = LoadXml(archive, "word/settings.xml");
        EnsureUpdateFields(settings);

        SaveXml(archive, "word/document.xml", document);
        SaveXml(archive, "word/_rels/document.xml.rels", relationships);
        SaveXml(archive, "[Content_Types].xml", contentTypes);
        SaveXml(archive, "word/settings.xml", settings);
    }

    private static void ApplyTypography(
        XDocument styles,
        DocumentStyle style,
        string? fontFamily,
        double fontScale)
    {
        var root = styles.Root
            ?? throw new ConversionException("styles.xml 缺少根元素。");

        var bodyFactor = style.BaseFontSizePt / 12.0 * Math.Max(fontScale, 0.1);
        var headingFactor = bodyFactor * (style.HeadingScale / 1.25);

        var defaultRunProperties = root
            .Element(W + "docDefaults")
            ?.Element(W + "rPrDefault")
            ?.Element(W + "rPr");

        if (defaultRunProperties is null)
        {
            throw new ConversionException("styles.xml 缺少 docDefaults。");
        }

        ApplyFont(defaultRunProperties, fontFamily);
        ScaleRunSizes(defaultRunProperties, bodyFactor);

        foreach (var styleElement in root.Elements(W + "style"))
        {
            var styleId = (string?)styleElement.Attribute(W + "styleId") ?? string.Empty;
            var runProperties = styleElement.Element(W + "rPr");

            if (runProperties is null)
            {
                continue;
            }

            if (!IsMonospaceStyle(styleId))
            {
                ApplyFont(runProperties, fontFamily);
            }

            ScaleRunSizes(
                runProperties,
                IsHeadingStyle(styleId) ? headingFactor : bodyFactor);
        }

        ApplyBodySpacing(root, style);
    }

    private static void ApplyBodySpacing(XElement stylesRoot, DocumentStyle style)
    {
        var defaultParagraphProperties = stylesRoot
            .Element(W + "docDefaults")
            ?.Element(W + "pPrDefault")
            ?.Element(W + "pPr");

        if (defaultParagraphProperties is not null)
        {
            SetSpacing(defaultParagraphProperties, style.ParagraphSpacingPt, style.ParagraphSpacingPt);
        }

        var bodyText = FindStyle(stylesRoot, "BodyText");
        if (bodyText is not null)
        {
            SetStyleSpacing(bodyText, style.ParagraphSpacingPt, style.ParagraphSpacingPt, style.LineSpacing);
        }

        var compact = FindStyle(stylesRoot, "Compact");
        if (compact is not null)
        {
            var compactSpacing = Math.Max(1, style.ParagraphSpacingPt * 0.3);
            SetStyleSpacing(compact, compactSpacing, compactSpacing);
        }

        var blockText = FindStyle(stylesRoot, "BlockText");
        if (blockText is not null)
        {
            SetStyleSpacing(blockText, style.ParagraphSpacingPt, style.ParagraphSpacingPt);
        }
    }

    private static void SetStyleSpacing(
        XElement style,
        double beforePt,
        double afterPt,
        double? lineSpacing = null)
    {
        var paragraphProperties = style.Element(W + "pPr");

        if (paragraphProperties is null)
        {
            paragraphProperties = new XElement(W + "pPr");
            style.Add(paragraphProperties);
        }

        SetSpacing(paragraphProperties, beforePt, afterPt, lineSpacing);
    }

    private static void ApplyFont(XElement runProperties, string? fontFamily)
    {
        if (string.IsNullOrWhiteSpace(fontFamily))
        {
            return;
        }

        var fonts = runProperties.Element(W + "rFonts");

        if (fonts is null)
        {
            fonts = new XElement(W + "rFonts");
            runProperties.AddFirst(fonts);
        }

        fonts.SetAttributeValue(W + "ascii", fontFamily);
        fonts.SetAttributeValue(W + "hAnsi", fontFamily);
        fonts.SetAttributeValue(W + "eastAsia", fontFamily);
        fonts.SetAttributeValue(W + "cs", fontFamily);
        fonts.SetAttributeValue(W + "asciiTheme", null);
        fonts.SetAttributeValue(W + "hAnsiTheme", null);
        fonts.SetAttributeValue(W + "eastAsiaTheme", null);
        fonts.SetAttributeValue(W + "cstheme", null);
    }

    private static void ScaleRunSizes(XElement runProperties, double factor)
    {
        foreach (var sizeElement in runProperties.Elements()
                     .Where(element => element.Name == W + "sz" || element.Name == W + "szCs"))
        {
            var rawValue = (string?)sizeElement.Attribute(W + "val");

            if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                continue;
            }

            var scaled = Math.Max(8, (int)Math.Round(value * factor, MidpointRounding.AwayFromZero));
            sizeElement.SetAttributeValue(W + "val", scaled);
        }
    }

    private static void SetSpacing(
        XElement paragraphProperties,
        double beforePt,
        double afterPt,
        double? lineSpacing = null)
    {
        var spacing = paragraphProperties.Element(W + "spacing");

        if (spacing is null)
        {
            spacing = new XElement(W + "spacing");
            paragraphProperties.Add(spacing);
        }

        spacing.SetAttributeValue(W + "before", ToTwips(beforePt));
        spacing.SetAttributeValue(W + "after", ToTwips(afterPt));

        if (lineSpacing is not null)
        {
            spacing.SetAttributeValue(
                W + "line",
                (int)Math.Round(lineSpacing.Value * 240, MidpointRounding.AwayFromZero));
            spacing.SetAttributeValue(W + "lineRule", "auto");
        }
    }

    private static bool IsHeadingStyle(string styleId)
    {
        return styleId == "TOCHeading"
            || (styleId.StartsWith("Heading", StringComparison.Ordinal)
                && styleId.Length == 8
                && char.IsAsciiDigit(styleId[7]));
    }

    private static bool IsMonospaceStyle(string styleId)
    {
        return styleId is "VerbatimChar" or "SourceCode";
    }

    private static XElement? FindStyle(XElement root, string styleId)
    {
        return root.Elements(W + "style")
            .FirstOrDefault(style => (string?)style.Attribute(W + "styleId") == styleId);
    }

    private static XElement GetOrCreateSectionProperties(XDocument document)
    {
        var body = document.Root?.Element(W + "body")
            ?? throw new ConversionException("document.xml 缺少 body。");

        var sectPr = body.Element(W + "sectPr");

        if (sectPr is null)
        {
            sectPr = new XElement(W + "sectPr");
            body.Add(sectPr);
        }

        return sectPr;
    }

    private static void AddPageProperties(XElement sectPr, DocumentStyle style, PageSize pageSize)
    {
        var (width, height) = pageSize switch
        {
            PageSize.A4 => (11906, 16838),
            PageSize.A5 => (8391, 11906),
            _ => (11906, 16838),
        };

        var pageSizeElement = new XElement(
            W + "pgSz",
            new XAttribute(W + "w", width),
            new XAttribute(W + "h", height),
            new XAttribute(W + "orient", "portrait"));

        var margins = style.PageMargins;
        var pageMarginElement = new XElement(
            W + "pgMar",
            new XAttribute(W + "top", MmToTwips(margins.TopMm)),
            new XAttribute(W + "right", MmToTwips(margins.RightMm)),
            new XAttribute(W + "bottom", MmToTwips(margins.BottomMm)),
            new XAttribute(W + "left", MmToTwips(margins.LeftMm)),
            new XAttribute(W + "header", 720),
            new XAttribute(W + "footer", 720),
            new XAttribute(W + "gutter", 0));

        var pageNumbering = new XElement(
            W + "pgNumType",
            new XAttribute(W + "start", 1));

        var insertAfter = sectPr.Elements()
            .LastOrDefault(element =>
                element.Name == W + "headerReference"
                || element.Name == W + "footerReference"
                || element.Name == W + "footnotePr");

        if (insertAfter is not null)
        {
            insertAfter.AddAfterSelf(pageSizeElement, pageMarginElement, pageNumbering);
        }
        else
        {
            sectPr.AddFirst(pageSizeElement, pageMarginElement, pageNumbering);
        }
    }

    private static XDocument CreateHeader(
        string? title,
        string? author,
        string inputPath,
        string? fontFamily,
        int fontSize)
    {
        var parts = new[] { title, author }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .ToArray();

        var text = parts.Length > 0
            ? string.Join("  ·  ", parts)
            : Path.GetFileNameWithoutExtension(inputPath);

        var paragraph = new XElement(
            W + "p",
            new XElement(
                W + "pPr",
                new XElement(
                    W + "pBdr",
                    new XElement(
                        W + "bottom",
                        new XAttribute(W + "val", "single"),
                        new XAttribute(W + "sz", "4"),
                        new XAttribute(W + "space", "4"),
                        new XAttribute(W + "color", "BFBFBF"))),
                new XElement(W + "spacing", new XAttribute(W + "after", "80")),
                new XElement(W + "jc", new XAttribute(W + "val", "center"))),
            new XElement(
                W + "r",
                CreateRunProperties(fontSize, fontFamily),
                new XElement(
                    W + "t",
                    new XAttribute(XNamespace.Xml + "space", "preserve"),
                    text)));

        return new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(W + "hdr", paragraph));
    }

    private static XDocument CreateFooter(DocumentStyle style, string? fontFamily, int fontSize)
    {
        var runs = new List<XElement>();

        if (style.PageNumberEnabled)
        {
            if (style.Id == "minimal")
            {
                runs.AddRange(CreateField("PAGE", "1", fontSize, fontFamily));
                runs.Add(CreateTextRun(" / ", fontSize, fontFamily));
                runs.AddRange(CreateField("NUMPAGES", "1", fontSize, fontFamily));
            }
            else
            {
                runs.Add(CreateTextRun("第 ", fontSize, fontFamily));
                runs.AddRange(CreateField("PAGE", "1", fontSize, fontFamily));
                runs.Add(CreateTextRun(" 页 / 共 ", fontSize, fontFamily));
                runs.AddRange(CreateField("NUMPAGES", "1", fontSize, fontFamily));
                runs.Add(CreateTextRun(" 页", fontSize, fontFamily));
            }
        }

        var paragraph = new XElement(
            W + "p",
            new XElement(
                W + "pPr",
                new XElement(W + "spacing", new XAttribute(W + "before", "80")),
                new XElement(W + "jc", new XAttribute(W + "val", "center"))),
            runs);

        return new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(W + "ftr", paragraph));
    }

    private static IEnumerable<XElement> CreateField(
        string instruction,
        string placeholder,
        int fontSize,
        string? fontFamily)
    {
        yield return new XElement(
            W + "r",
            CreateRunProperties(fontSize, fontFamily),
            new XElement(W + "fldChar", new XAttribute(W + "fldCharType", "begin")));

        yield return new XElement(
            W + "r",
            CreateRunProperties(fontSize, fontFamily),
            new XElement(
                W + "instrText",
                new XAttribute(XNamespace.Xml + "space", "preserve"),
                $" {instruction} "));

        yield return new XElement(
            W + "r",
            CreateRunProperties(fontSize, fontFamily),
            new XElement(W + "fldChar", new XAttribute(W + "fldCharType", "separate")));

        yield return new XElement(
            W + "r",
            CreateRunProperties(fontSize, fontFamily),
            new XElement(W + "t", placeholder));

        yield return new XElement(
            W + "r",
            CreateRunProperties(fontSize, fontFamily),
            new XElement(W + "fldChar", new XAttribute(W + "fldCharType", "end")));
    }

    private static XElement CreateTextRun(string text, int fontSize, string? fontFamily)
    {
        return new XElement(
            W + "r",
            CreateRunProperties(fontSize, fontFamily),
            new XElement(W + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), text));
    }

    private static XElement CreateRunProperties(int fontSize, string? fontFamily)
    {
        var properties = new XElement(
            W + "rPr",
            new XElement(W + "sz", new XAttribute(W + "val", fontSize)),
            new XElement(W + "szCs", new XAttribute(W + "val", fontSize)));

        if (!string.IsNullOrWhiteSpace(fontFamily))
        {
            var fonts = new XElement(
                W + "rFonts",
                new XAttribute(W + "ascii", fontFamily),
                new XAttribute(W + "hAnsi", fontFamily),
                new XAttribute(W + "eastAsia", fontFamily),
                new XAttribute(W + "cs", fontFamily));

            properties.AddFirst(fonts);
        }

        return properties;
    }

    private static int FooterFontSize(DocumentStyle style, double fontScale)
    {
        return Math.Max(
            14,
            (int)Math.Round(style.BaseFontSizePt * 0.8 * fontScale * 2, MidpointRounding.AwayFromZero));
    }

    private static int MmToTwips(double millimeters)
    {
        return (int)Math.Round(millimeters / 25.4 * 1440, MidpointRounding.AwayFromZero);
    }

    private static int ToTwips(double points)
    {
        return (int)Math.Round(points * 20, MidpointRounding.AwayFromZero);
    }

    private static (string? Title, string? Author) ReadCoreProperties(ZipArchive archive)
    {
        var entry = archive.GetEntry("docProps/core.xml");

        if (entry is null)
        {
            return (null, null);
        }

        using var stream = entry.Open();
        var document = XDocument.Load(stream);

        return (
            NullIfWhiteSpace(document.Root?.Element(Dc + "title")?.Value),
            NullIfWhiteSpace(document.Root?.Element(Dc + "creator")?.Value));
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string EnsureRelationship(
        XDocument relationships,
        string type,
        string target)
    {
        var root = relationships.Root
            ?? throw new ConversionException("document.xml.rels 缺少根元素。");

        var existing = root.Elements(PackageRelationships + "Relationship")
            .FirstOrDefault(element =>
                (string?)element.Attribute("Type") == type
                && (string?)element.Attribute("Target") == target);

        if (existing is not null)
        {
            return (string?)existing.Attribute("Id")
                ?? throw new ConversionException("关系缺少 Id。");
        }

        var id = NextRelationshipId(root);
        root.Add(new XElement(
            PackageRelationships + "Relationship",
            new XAttribute("Id", id),
            new XAttribute("Type", type),
            new XAttribute("Target", target)));

        return id;
    }

    private static string NextRelationshipId(XElement relationshipsRoot)
    {
        var next = 1;

        foreach (var relationship in relationshipsRoot.Elements(PackageRelationships + "Relationship"))
        {
            var id = (string?)relationship.Attribute("Id");

            if (id is null || !id.StartsWith("rId", StringComparison.Ordinal))
            {
                continue;
            }

            if (int.TryParse(id[3..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
            {
                next = Math.Max(next, number + 1);
            }
        }

        return "rId" + next.ToString(CultureInfo.InvariantCulture);
    }

    private static void EnsureContentType(
        XDocument contentTypes,
        string partName,
        string contentType)
    {
        var root = contentTypes.Root
            ?? throw new ConversionException("[Content_Types].xml 缺少根元素。");

        var namespaceName = root.Name.Namespace;
        var exists = root.Elements(namespaceName + "Override")
            .Any(element => (string?)element.Attribute("PartName") == partName);

        if (exists)
        {
            return;
        }

        root.Add(new XElement(
            namespaceName + "Override",
            new XAttribute("PartName", partName),
            new XAttribute("ContentType", contentType)));
    }

    private static void EnsureUpdateFields(XDocument settings)
    {
        var root = settings.Root
            ?? throw new ConversionException("settings.xml 缺少根元素。");

        var updateFields = root.Element(W + "updateFields");

        if (updateFields is null)
        {
            updateFields = new XElement(W + "updateFields");

            var followingNames = new[]
            {
                "hdrShapeDefaults",
                "footnotePr",
                "endnotePr",
                "compat",
                "docVars",
                "rsids",
                "mathPr",
            };

            var insertBefore = root.Elements()
                .FirstOrDefault(element => followingNames.Contains(element.Name.LocalName));

            if (insertBefore is not null)
            {
                insertBefore.AddBeforeSelf(updateFields);
            }
            else
            {
                root.Add(updateFields);
            }
        }

        updateFields.SetAttributeValue(W + "val", "true");
    }

    private static XDocument LoadXml(ZipArchive archive, string entryName)
    {
        var entry = archive.GetEntry(entryName)
            ?? throw new ConversionException($"DOCX 缺少部件：{entryName}");

        using var stream = entry.Open();
        return XDocument.Load(stream, LoadOptions.PreserveWhitespace);
    }

    private static void SaveXml(ZipArchive archive, string entryName, XDocument document)
    {
        archive.GetEntry(entryName)?.Delete();
        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);

        using var stream = entry.Open();
        document.Save(stream, SaveOptions.DisableFormatting);
    }
}

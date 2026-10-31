using System.Diagnostics;
using System.IO.Compression;
using System.Xml.Linq;

const string W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
XNamespace w = W;

var repoRoot = Directory.GetCurrentDirectory();
var pandoc = Path.Combine(repoRoot, "resources", "engines", "win-x64", "pandoc.exe");
var outputDirectory = Path.Combine(repoRoot, "resources", "docx");
Directory.CreateDirectory(outputDirectory);

var tempDirectory = Path.Combine(
    Path.GetTempPath(),
    "MDC",
    "reference-docs-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(tempDirectory);

try
{
    var markdownPath = Path.Combine(tempDirectory, "reference.md");
    await File.WriteAllTextAsync(
        markdownPath,
        "# Reference\n\nBody text with **bold**, *italic*, `code`, and [link](https://example.com).\n\n## Heading 2\n\n| A | B |\n|---|---|\n| 1 | 2 |\n");

    var baseDocx = Path.Combine(tempDirectory, "base.docx");
    Run(pandoc, new[] { markdownPath, "-o", baseDocx, "--standalone" });

    CreateVariant(
        baseDocx,
        outputDirectory,
        "modern",
        latinFont: "Calibri",
        eastAsianFont: "Microsoft YaHei",
        headingColor: "1F4E79",
        secondaryHeadingColor: "2E74B5",
        tableFill: "D9E2F3",
        tableBorderColor: "BFBFBF",
        tableBorders: "horizontal");

    CreateVariant(
        baseDocx,
        outputDirectory,
        "academic",
        latinFont: "Times New Roman",
        eastAsianFont: "SimSun",
        headingColor: "000000",
        secondaryHeadingColor: "000000",
        tableFill: "E7E6E6",
        tableBorderColor: "000000",
        tableBorders: "all");

    CreateVariant(
        baseDocx,
        outputDirectory,
        "minimal",
        latinFont: "Calibri",
        eastAsianFont: "Microsoft YaHei",
        headingColor: "000000",
        secondaryHeadingColor: "000000",
        tableFill: "F2F2F2",
        tableBorderColor: "A6A6A6",
        tableBorders: "horizontal");

    Console.WriteLine($"Reference documents written to {outputDirectory}");
}
finally
{
    try
    {
        Directory.Delete(tempDirectory, recursive: true);
    }
    catch (IOException)
    {
    }
}

void Run(string fileName, string[] arguments)
{
    var startInfo = new ProcessStartInfo
    {
        FileName = fileName,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true,
    };

    foreach (var argument in arguments)
    {
        startInfo.ArgumentList.Add(argument);
    }

    using var process = Process.Start(startInfo)
        ?? throw new InvalidOperationException("无法启动 Pandoc。");
    var stdout = process.StandardOutput.ReadToEnd();
    var stderr = process.StandardError.ReadToEnd();
    process.WaitForExit();

    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException(
            $"Pandoc 失败（{process.ExitCode}）：{stdout}{Environment.NewLine}{stderr}");
    }
}

void CreateVariant(
    string baseDocx,
    string outputDirectory,
    string id,
    string latinFont,
    string eastAsianFont,
    string headingColor,
    string secondaryHeadingColor,
    string tableFill,
    string tableBorderColor,
    string tableBorders)
{
    var variantPath = Path.Combine(outputDirectory, id + ".docx");
    File.Copy(baseDocx, variantPath, overwrite: true);

    using var archive = ZipFile.Open(variantPath, ZipArchiveMode.Update);
    var stylesEntry = archive.GetEntry("word/styles.xml")
        ?? throw new InvalidOperationException("基础 DOCX 缺少 word/styles.xml。");

    XDocument styles;
    using (var stream = stylesEntry.Open())
    {
        styles = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
    }

    var root = styles.Root ?? throw new InvalidOperationException("styles.xml 无根元素。");

    var docDefaultsRunProperties = root
        .Element(w + "docDefaults")
        ?.Element(w + "rPrDefault")
        ?.Element(w + "rPr");

    if (docDefaultsRunProperties is null)
    {
        throw new InvalidOperationException("styles.xml 缺少 docDefaults/rPrDefault/rPr。");
    }

    SetFonts(docDefaultsRunProperties, latinFont, eastAsianFont);

    foreach (var style in root.Elements(w + "style"))
    {
        var styleId = (string?)style.Attribute(w + "styleId") ?? string.Empty;
        var runProperties = style.Element(w + "rPr");

        if (runProperties is not null && !IsMonospaceStyle(styleId))
        {
            SetFonts(runProperties, latinFont, eastAsianFont);
        }

        if (IsHeading(styleId))
        {
            EnsureBold(runProperties);
            SetColor(
                runProperties,
                styleId == "Heading1" || styleId == "TOCHeading"
                    ? headingColor
                    : secondaryHeadingColor);
        }
    }

    var bodyText = FindStyle(root, "BodyText");
    if (bodyText is not null)
    {
        SetSpacing(bodyText, before: 180, after: 180, line: 360);
    }

    var hyperlink = FindStyle(root, "Hyperlink");
    SetColor(hyperlink?.Element(w + "rPr"), "0563C1");

    var table = FindStyle(root, "Table");
    if (table is not null)
    {
        ApplyTableStyle(table, tableFill, tableBorderColor, tableBorders);
    }

    var sourceCode = FindStyle(root, "SourceCode");
    if (sourceCode is not null)
    {
        ApplyCodeStyle(sourceCode);
    }

    stylesEntry.Delete();
    var replacement = archive.CreateEntry("word/styles.xml", CompressionLevel.Optimal);
    using var output = replacement.Open();
    styles.Save(output, SaveOptions.DisableFormatting);
}

bool IsMonospaceStyle(string styleId)
{
    return styleId is "VerbatimChar" or "SourceCode";
}

bool IsHeading(string styleId)
{
    return styleId.StartsWith("Heading", StringComparison.Ordinal)
        && styleId.Length == 8
        && char.IsAsciiDigit(styleId[7]);
}

XElement? FindStyle(XElement root, string styleId)
{
    XNamespace w = W;
    return root.Elements(w + "style")
        .FirstOrDefault(style => (string?)style.Attribute(w + "styleId") == styleId);
}

void SetFonts(XElement runProperties, string latinFont, string eastAsianFont)
{
    XNamespace w = W;
    var fonts = runProperties.Element(w + "rFonts");

    if (fonts is null)
    {
        fonts = new XElement(w + "rFonts");
        runProperties.AddFirst(fonts);
    }

    fonts.SetAttributeValue(w + "ascii", latinFont);
    fonts.SetAttributeValue(w + "hAnsi", latinFont);
    fonts.SetAttributeValue(w + "eastAsia", eastAsianFont);
    fonts.SetAttributeValue(w + "cs", latinFont);
    fonts.SetAttributeValue(w + "asciiTheme", null);
    fonts.SetAttributeValue(w + "hAnsiTheme", null);
    fonts.SetAttributeValue(w + "eastAsiaTheme", null);
    fonts.SetAttributeValue(w + "cstheme", null);
}

void EnsureBold(XElement? runProperties)
{
    if (runProperties is null)
    {
        return;
    }

    XNamespace w = W;

    if (runProperties.Element(w + "b") is not null)
    {
        return;
    }

    var bold = new XElement(w + "b");
    var boldComplexScript = new XElement(w + "bCs");
    var fonts = runProperties.Element(w + "rFonts");

    if (fonts is not null)
    {
        fonts.AddAfterSelf(bold, boldComplexScript);
    }
    else
    {
        runProperties.AddFirst(bold, boldComplexScript);
    }
}

void SetColor(XElement? runProperties, string color)
{
    if (runProperties is null)
    {
        return;
    }

    XNamespace w = W;
    var existing = runProperties.Element(w + "color");

    if (existing is null)
    {
        var colorElement = new XElement(w + "color", new XAttribute(w + "val", color));
        var sizeElement = runProperties.Element(w + "sz");

        if (sizeElement is not null)
        {
            sizeElement.AddBeforeSelf(colorElement);
        }
        else
        {
            runProperties.Add(colorElement);
        }

        return;
    }

    existing.SetAttributeValue(w + "val", color);
    existing.SetAttributeValue(w + "themeColor", null);
    existing.SetAttributeValue(w + "themeShade", null);
    existing.SetAttributeValue(w + "themeTint", null);
}

void SetSpacing(XElement style, int before, int after, int line)
{
    XNamespace w = W;
    var paragraphProperties = style.Element(w + "pPr");

    if (paragraphProperties is null)
    {
        paragraphProperties = new XElement(w + "pPr");
        style.Add(paragraphProperties);
    }

    var spacing = paragraphProperties.Element(w + "spacing");

    if (spacing is null)
    {
        spacing = new XElement(w + "spacing");
        paragraphProperties.Add(spacing);
    }

    spacing.SetAttributeValue(w + "before", before);
    spacing.SetAttributeValue(w + "after", after);
    spacing.SetAttributeValue(w + "line", line);
    spacing.SetAttributeValue(w + "lineRule", "auto");
}

void ApplyTableStyle(
    XElement tableStyle,
    string headerFill,
    string borderColor,
    string borderMode)
{
    XNamespace w = W;
    var tableProperties = tableStyle.Element(w + "tblPr");

    if (tableProperties is null)
    {
        tableProperties = new XElement(w + "tblPr");
        tableStyle.AddFirst(tableProperties);
    }

    tableProperties.Element(w + "tblBorders")?.Remove();

    var borders = new XElement(w + "tblBorders");
    AddBorder(borders, "top", borderColor, borderMode is "all" or "outer-horizontal" or "horizontal");
    AddBorder(borders, "left", borderColor, borderMode is "all");
    AddBorder(borders, "bottom", borderColor, borderMode is "all" or "outer-horizontal" or "horizontal");
    AddBorder(borders, "right", borderColor, borderMode is "all");
    AddBorder(borders, "insideH", borderColor, borderMode is "all" or "horizontal");
    AddBorder(borders, "insideV", borderColor, borderMode is "all");

    var cellMargin = tableProperties.Element(w + "tblCellMar");
    var tableLook = tableProperties.Element(w + "tblLook");

    if (cellMargin is not null)
    {
        cellMargin.AddBeforeSelf(borders);
    }
    else if (tableLook is not null)
    {
        tableLook.AddBeforeSelf(borders);
    }
    else
    {
        tableProperties.Add(borders);
    }

    var firstRow = tableStyle.Elements(w + "tblStylePr")
        .FirstOrDefault(element => (string?)element.Attribute(w + "type") == "firstRow");

    if (firstRow is null)
    {
        firstRow = new XElement(w + "tblStylePr", new XAttribute(w + "type", "firstRow"));
        tableStyle.Add(firstRow);
    }

    var cellProperties = firstRow.Element(w + "tcPr");

    if (cellProperties is null)
    {
        cellProperties = new XElement(w + "tcPr");
        firstRow.Add(cellProperties);
    }

    cellProperties.Elements(w + "shd").Remove();
    var shading = new XElement(
        w + "shd",
        new XAttribute(w + "val", "clear"),
        new XAttribute(w + "color", "auto"),
        new XAttribute(w + "fill", headerFill));

    var verticalAlignment = cellProperties.Element(w + "vAlign");

    if (verticalAlignment is not null)
    {
        verticalAlignment.AddBeforeSelf(shading);
    }
    else
    {
        cellProperties.Add(shading);
    }

    var rowRunProperties = firstRow.Element(w + "rPr");

    if (rowRunProperties is null)
    {
        rowRunProperties = new XElement(w + "rPr");
        cellProperties.AddBeforeSelf(rowRunProperties);
    }

    EnsureBold(rowRunProperties);
}

void AddBorder(XElement borders, string name, string color, bool enabled)
{
    XNamespace w = W;
    borders.Add(new XElement(
        w + name,
        new XAttribute(w + "val", enabled ? "single" : "none"),
        new XAttribute(w + "sz", enabled ? "4" : "0"),
        new XAttribute(w + "space", "0"),
        new XAttribute(w + "color", enabled ? color : "auto")));
}

void ApplyCodeStyle(XElement sourceCode)
{
    XNamespace w = W;
    var paragraphProperties = sourceCode.Element(w + "pPr");

    if (paragraphProperties is null)
    {
        paragraphProperties = new XElement(w + "pPr");
        sourceCode.Add(paragraphProperties);
    }

    paragraphProperties.Elements(w + "shd").Remove();
    paragraphProperties.Elements(w + "pBdr").Remove();

    var border = new XElement(
        w + "pBdr",
        new XElement(
            w + "top",
            new XAttribute(w + "val", "single"),
            new XAttribute(w + "sz", "4"),
            new XAttribute(w + "space", "4"),
            new XAttribute(w + "color", "D9D9D9")),
        new XElement(
            w + "bottom",
            new XAttribute(w + "val", "single"),
            new XAttribute(w + "sz", "4"),
            new XAttribute(w + "space", "4"),
            new XAttribute(w + "color", "D9D9D9")));

    var shading = new XElement(
        w + "shd",
        new XAttribute(w + "val", "clear"),
        new XAttribute(w + "color", "auto"),
        new XAttribute(w + "fill", "F5F5F5"));

    var insertionPoint = paragraphProperties.Element(w + "wordWrap")
        ?? paragraphProperties.Element(w + "spacing")
        ?? paragraphProperties.Element(w + "ind");

    if (insertionPoint is not null)
    {
        insertionPoint.AddBeforeSelf(border, shading);
    }
    else
    {
        paragraphProperties.Add(border, shading);
    }

    var runProperties = sourceCode.Element(w + "rPr");

    if (runProperties is null)
    {
        runProperties = new XElement(w + "rPr");
        sourceCode.Add(runProperties);
    }

    var fonts = runProperties.Element(w + "rFonts");

    if (fonts is null)
    {
        fonts = new XElement(w + "rFonts");
        runProperties.AddFirst(fonts);
    }

    fonts.SetAttributeValue(w + "ascii", "Consolas");
    fonts.SetAttributeValue(w + "hAnsi", "Consolas");
    fonts.SetAttributeValue(w + "eastAsia", "Microsoft YaHei");
    fonts.SetAttributeValue(w + "asciiTheme", null);
    fonts.SetAttributeValue(w + "hAnsiTheme", null);
    fonts.SetAttributeValue(w + "eastAsiaTheme", null);
    fonts.SetAttributeValue(w + "cstheme", null);
}

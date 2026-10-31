using System.IO.Compression;
using System.Xml.Linq;
using MarkdownDocumentConverter.Core.Models;
using MarkdownDocumentConverter.Tests.Infrastructure;

namespace MarkdownDocumentConverter.Tests.Integration;

public sealed class WordPipelineTests
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    [Fact]
    public async Task Modern_style_applies_a4_layout_header_footer_toc_and_update_fields()
    {
        using var output = new TestOutputDirectory();
        var services = new TestServices();
        var request = new ConversionRequest
        {
            InputPath = TestPaths.Fixture("basic.md"),
            OutputDirectory = output.Path,
            StyleId = "modern",
            PageSize = PageSize.A4,
        };

        var result = await services.WordPipeline.ConvertAsync(request, output.File("modern.docx"));

        var document = LoadPart(result.OutputPath, "word/document.xml");
        var body = document.Root!.Element(W + "body")!;
        var section = body.Element(W + "sectPr")!;

        Assert.Equal("11906", (string?)section.Element(W + "pgSz")?.Attribute(W + "w"));
        Assert.Equal("16838", (string?)section.Element(W + "pgSz")?.Attribute(W + "h"));
        Assert.Equal(MmToTwips(25), (int?)section.Element(W + "pgMar")?.Attribute(W + "top"));
        Assert.Equal(MmToTwips(24), (int?)section.Element(W + "pgMar")?.Attribute(W + "left"));
        Assert.NotNull(section.Element(W + "headerReference"));
        Assert.NotNull(section.Element(W + "footerReference"));

        var header = LoadPart(result.OutputPath, "word/header1.xml");
        Assert.Contains("Phase 0 测试文档", header.Root!.Value, StringComparison.Ordinal);

        var footer = LoadPart(result.OutputPath, "word/footer1.xml");
        var footerInstructions = footer.Descendants(W + "instrText").Select(element => element.Value).ToArray();
        Assert.Contains(footerInstructions, instruction => instruction.Contains("PAGE", StringComparison.Ordinal));
        Assert.Contains(footerInstructions, instruction => instruction.Contains("NUMPAGES", StringComparison.Ordinal));

        var documentText = document.Root.Value;
        Assert.Contains("目录", documentText, StringComparison.Ordinal);
        Assert.Contains(
            document.Descendants(W + "instrText"),
            element => element.Value.Contains("TOC", StringComparison.Ordinal));

        Assert.True(document.Descendants(W + "tbl").Any());
        Assert.Contains(
            document.Descendants(W + "pStyle"),
            element => (string?)element.Attribute(W + "val") == "SourceCode");
        Assert.Contains("<m:oMath", document.ToString(SaveOptions.DisableFormatting), StringComparison.Ordinal);

        var footnotes = LoadPart(result.OutputPath, "word/footnotes.xml");
        Assert.Contains("脚注内容", footnotes.Root!.Value, StringComparison.Ordinal);

        var settings = LoadPart(result.OutputPath, "word/settings.xml");
        Assert.Equal(
            "true",
            (string?)settings.Root!.Element(W + "updateFields")?.Attribute(W + "val"));

        var styles = LoadPart(result.OutputPath, "word/styles.xml");
        var heading1 = FindStyle(styles, "Heading1")!;
        Assert.Equal(
            "0",
            (string?)heading1
                .Element(W + "pPr")
                ?.Element(W + "outlineLvl")
                ?.Attribute(W + "val"));
    }

    [Fact]
    public async Task Academic_style_a5_font_and_scale_are_applied_consistently()
    {
        using var output = new TestOutputDirectory();
        var services = new TestServices();
        var request = new ConversionRequest
        {
            InputPath = TestPaths.Fixture("basic.md"),
            OutputDirectory = output.Path,
            StyleId = "academic",
            PageSize = PageSize.A5,
            FontFamily = "Arial",
            FontScale = 1.2,
        };

        var result = await services.WordPipeline.ConvertAsync(request, output.File("academic.docx"));

        var document = LoadPart(result.OutputPath, "word/document.xml");
        var section = document.Root!.Element(W + "body")!.Element(W + "sectPr")!;
        Assert.Equal("8391", (string?)section.Element(W + "pgSz")?.Attribute(W + "w"));
        Assert.Equal("11906", (string?)section.Element(W + "pgSz")?.Attribute(W + "h"));
        Assert.Equal(MmToTwips(30), (int?)section.Element(W + "pgMar")?.Attribute(W + "top"));

        var styles = LoadPart(result.OutputPath, "word/styles.xml");
        var defaultRunProperties = styles.Root!
            .Element(W + "docDefaults")!
            .Element(W + "rPrDefault")!
            .Element(W + "rPr")!;

        var defaultFonts = defaultRunProperties.Element(W + "rFonts")!;
        Assert.Equal("Arial", (string?)defaultFonts.Attribute(W + "ascii"));
        Assert.Equal("Arial", (string?)defaultFonts.Attribute(W + "eastAsia"));

        Assert.Equal(
            "29",
            (string?)defaultRunProperties.Element(W + "sz")?.Attribute(W + "val"));

        var bodyText = FindStyle(styles, "BodyText")!;
        var spacing = bodyText.Element(W + "pPr")!.Element(W + "spacing")!;
        Assert.Equal("120", (string?)spacing.Attribute(W + "before"));
        Assert.Equal("360", (string?)spacing.Attribute(W + "line"));

        var heading1 = FindStyle(styles, "Heading1")!;
        var headingFonts = heading1.Element(W + "rPr")!.Element(W + "rFonts")!;
        Assert.Equal("Arial", (string?)headingFonts.Attribute(W + "ascii"));

        var verbatim = FindStyle(styles, "VerbatimChar")!;
        var verbatimFonts = verbatim.Element(W + "rPr")!.Element(W + "rFonts")!;
        Assert.Equal("Consolas", (string?)verbatimFonts.Attribute(W + "ascii"));
    }

    [Fact]
    public async Task Minimal_style_omits_header_and_toc_but_keeps_page_numbers()
    {
        using var output = new TestOutputDirectory();
        var services = new TestServices();
        var request = new ConversionRequest
        {
            InputPath = TestPaths.Fixture("basic.md"),
            OutputDirectory = output.Path,
            StyleId = "minimal",
            PageSize = PageSize.A4,
        };

        var result = await services.WordPipeline.ConvertAsync(request, output.File("minimal.docx"));

        using var archive = ZipFile.OpenRead(result.OutputPath);
        Assert.Null(archive.GetEntry("word/header1.xml"));
        Assert.NotNull(archive.GetEntry("word/footer1.xml"));

        var document = LoadPart(result.OutputPath, "word/document.xml");
        var section = document.Root!.Element(W + "body")!.Element(W + "sectPr")!;
        Assert.Null(section.Element(W + "headerReference"));
        Assert.NotNull(section.Element(W + "footerReference"));
        Assert.DoesNotContain(
            document.Descendants(W + "instrText"),
            element => element.Value.Contains("TOC", StringComparison.Ordinal));

        var footer = LoadPart(result.OutputPath, "word/footer1.xml");
        Assert.Contains(
            footer.Descendants(W + "instrText"),
            element => element.Value.Contains("PAGE", StringComparison.Ordinal));

        var styles = LoadPart(result.OutputPath, "word/styles.xml");
        var defaultRunProperties = styles.Root!
            .Element(W + "docDefaults")!
            .Element(W + "rPrDefault")!
            .Element(W + "rPr")!;

        Assert.Equal(
            "21",
            (string?)defaultRunProperties.Element(W + "sz")?.Attribute(W + "val"));
    }

    private static XDocument LoadPart(string docxPath, string partName)
    {
        using var archive = ZipFile.OpenRead(docxPath);
        var entry = archive.GetEntry(partName)
            ?? throw new InvalidOperationException($"DOCX 缺少部件：{partName}");

        using var stream = entry.Open();
        return XDocument.Load(stream);
    }

    private static XElement? FindStyle(XDocument styles, string styleId)
    {
        return styles.Root!
            .Elements(W + "style")
            .FirstOrDefault(style => (string?)style.Attribute(W + "styleId") == styleId);
    }

    private static int MmToTwips(double millimeters)
    {
        return (int)Math.Round(millimeters / 25.4 * 1440, MidpointRounding.AwayFromZero);
    }
}

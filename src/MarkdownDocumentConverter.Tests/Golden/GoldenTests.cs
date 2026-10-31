using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MarkdownDocumentConverter.Core.Models;
using MarkdownDocumentConverter.Tests.Infrastructure;
using UglyToad.PdfPig;

namespace MarkdownDocumentConverter.Tests.Golden;

public sealed class GoldenTests
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    public static IEnumerable<object[]> Cases { get; } = new[]
    {
        new object[] { "Basic", "modern", PageSize.A4, false, false, "GoldenBasic", 1 },
        new object[] { "Academic", "academic", PageSize.A5, false, true, "学术标题", 1 },
        new object[] { "Math", "modern", PageSize.A4, true, false, "数学公式", 1 },
        new object[] { "Chinese", "academic", PageSize.A4, false, false, "中文长文", 1 },
        new object[] { "Tables", "academic", PageSize.A4, false, true, "表格", 1 },
        new object[] { "LongDocument", "minimal", PageSize.A4, false, false, "第60节", 3 },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Golden_fixture_produces_expected_docx_and_pdf_structure(
        string folder,
        string styleId,
        PageSize pageSize,
        bool expectsMath,
        bool expectsTable,
        string expectedText,
        int minimumPdfPages)
    {
        using var output = new TestOutputDirectory();
        var services = new TestServices();
        var fixture = Path.Combine(
            TestPaths.RepositoryRoot,
            "tests",
            "Golden",
            folder,
            "document.md");

        var request = new ConversionRequest
        {
            InputPath = fixture,
            OutputDirectory = output.Path,
            StyleId = styleId,
            PageSize = pageSize,
            Formats = OutputFormat.All,
        };

        var docx = await services.WordPipeline.ConvertAsync(
            request,
            output.File($"{folder}-{styleId}.docx"));
        var pdf = await services.PdfPipeline.ConvertAsync(
            request,
            output.File($"{folder}-{styleId}.pdf"));

        var document = LoadPart(docx.OutputPath, "word/document.xml");
        var documentXml = document.ToString(SaveOptions.DisableFormatting);
        var documentText = NormalizeWhitespace(string.Concat(
            document.Descendants(W + "t").Select(element => element.Value)));
        Assert.Contains(expectedText, documentText, StringComparison.Ordinal);

        if (expectsMath)
        {
            Assert.Contains("<m:oMath", documentXml, StringComparison.Ordinal);
        }

        if (expectsTable)
        {
            Assert.NotEmpty(document.Descendants(W + "tbl"));
        }

        using var pdfDocument = PdfDocument.Open(pdf.OutputPath);
        var pageTexts = pdfDocument.GetPages()
            .Select(page => NormalizeWhitespace(page.Text))
            .ToList();

        Assert.True(
            pageTexts.Count >= minimumPdfPages,
            $"{folder} 期望至少 {minimumPdfPages} 页，实际 {pageTexts.Count} 页。");
        Assert.All(pageTexts, page => Assert.False(string.IsNullOrWhiteSpace(page)));
        Assert.Contains(expectedText, string.Join("\n", pageTexts), StringComparison.Ordinal);
        Assert.True(pdfDocument.TryGetBookmarks(out var bookmarks));
        Assert.NotEmpty(bookmarks.GetNodes());
    }

    private static XDocument LoadPart(string docxPath, string partName)
    {
        using var archive = ZipFile.OpenRead(docxPath);
        var entry = archive.GetEntry(partName)
            ?? throw new InvalidOperationException($"DOCX 缺少部件：{partName}");

        using var stream = entry.Open();
        return XDocument.Load(stream);
    }

    private static string NormalizeWhitespace(string text)
    {
        return Regex.Replace(text, @"\s+", string.Empty);
    }
}

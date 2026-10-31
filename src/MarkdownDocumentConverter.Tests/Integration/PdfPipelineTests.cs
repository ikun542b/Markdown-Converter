using System.Text;
using System.Text.RegularExpressions;
using MarkdownDocumentConverter.Core.Models;
using MarkdownDocumentConverter.Tests.Infrastructure;
using UglyToad.PdfPig;

namespace MarkdownDocumentConverter.Tests.Integration;

public sealed class PdfPipelineTests
{
    [Fact]
    public async Task Modern_pdf_has_chinese_text_toc_metadata_page_numbers_and_bookmarks()
    {
        using var output = new TestOutputDirectory();
        var services = new TestServices();
        var request = new ConversionRequest
        {
            InputPath = TestPaths.Fixture("basic.md"),
            OutputDirectory = output.Path,
            StyleId = "modern",
            PageSize = PageSize.A4,
            FontFamily = "Arial",
        };

        var result = await services.PdfPipeline.ConvertAsync(request, output.File("modern.pdf"));

        using var pdf = PdfDocument.Open(result.OutputPath);
        var pageTexts = pdf.GetPages()
            .Select(page => NormalizeWhitespace(page.Text))
            .ToList();
        var text = string.Join("\n", pageTexts);

        Assert.True(pdf.NumberOfPages >= 3);
        Assert.Contains("一级标题", text, StringComparison.Ordinal);
        Assert.Contains("目录", text, StringComparison.Ordinal);
        Assert.Contains("第1页", text, StringComparison.Ordinal);
        Assert.DoesNotContain("第1页", pageTexts[0], StringComparison.Ordinal);

        var firstBodyPage = pageTexts.FindIndex(page => page.Contains("第1页", StringComparison.Ordinal));
        Assert.True(firstBodyPage >= 2, "页码应从正文页开始，而不是标题页或目录页。");
        Assert.Contains("Phase0测试文档", pageTexts[firstBodyPage], StringComparison.Ordinal);

        Assert.Contains("脚注内容", text, StringComparison.Ordinal);
        Assert.Contains("varanswer=42", text, StringComparison.Ordinal);
        // Typst 数学字体导出为数学斜体字符（U+1D45A/U+1D450），而不是 ASCII "mc"。
        Assert.Contains("𝑚𝑐2", text, StringComparison.Ordinal);
        Assert.Contains("DOCX", text, StringComparison.Ordinal);

        var hyperlinks = pdf.GetPages()
            .SelectMany(page => page.GetHyperlinks())
            .ToList();
        Assert.Contains(
            hyperlinks,
            hyperlink => hyperlink.Uri.Contains("example.com", StringComparison.OrdinalIgnoreCase));

        Assert.True(pdf.TryGetBookmarks(out var bookmarks));
        Assert.NotEmpty(bookmarks.GetNodes());

        Assert.Equal("Phase 0 测试文档", pdf.Information.Title);
        Assert.Equal("测试作者", pdf.Information.Author);

        var firstPage = pdf.GetPage(1);
        Assert.InRange(firstPage.Width, 594, 597);
        Assert.InRange(firstPage.Height, 840, 843);

        var bytes = await File.ReadAllBytesAsync(result.OutputPath);
        Assert.Contains("/Outlines", Encoding.ASCII.GetString(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Academic_pdf_uses_a5_page_size()
    {
        using var output = new TestOutputDirectory();
        var services = new TestServices();
        var request = new ConversionRequest
        {
            InputPath = TestPaths.Fixture("basic.md"),
            OutputDirectory = output.Path,
            StyleId = "academic",
            PageSize = PageSize.A5,
            FontFamily = "SimSun",
            FontScale = 1.1,
        };

        var result = await services.PdfPipeline.ConvertAsync(request, output.File("academic.pdf"));

        using var pdf = PdfDocument.Open(result.OutputPath);
        var firstPage = pdf.GetPage(1);
        Assert.InRange(firstPage.Width, 418, 421);
        Assert.InRange(firstPage.Height, 594, 597);
    }

    [Fact]
    public async Task Minimal_pdf_omits_toc_and_uses_compact_page_number_format()
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

        var result = await services.PdfPipeline.ConvertAsync(request, output.File("minimal.pdf"));

        using var pdf = PdfDocument.Open(result.OutputPath);
        var text = NormalizeWhitespace(string.Join("\n", pdf.GetPages().Select(page => page.Text)));

        Assert.DoesNotContain("目录", text, StringComparison.Ordinal);
        Assert.Contains("1", text, StringComparison.Ordinal);
        Assert.DoesNotContain("第1页", text, StringComparison.Ordinal);
    }

    private static string NormalizeWhitespace(string text)
    {
        return Regex.Replace(text, @"\s+", string.Empty);
    }
}

using System.IO.Compression;
using System.Text;
using MarkdownDocumentConverter.Tests.Infrastructure;

namespace MarkdownDocumentConverter.Tests.Integration;

public sealed class EngineIntegrationTests
{
    [Fact]
    public async Task Pandoc_reports_expected_version()
    {
        var services = new TestServices();

        var version = await services.Pandoc.GetVersionAsync();

        Assert.StartsWith("pandoc 3.13", version, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Typst_reports_expected_version()
    {
        var services = new TestServices();

        var version = await services.Typst.GetVersionAsync();

        Assert.StartsWith("typst 0.15.1", version, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Word_pipeline_generates_docx_with_native_math()
    {
        using var output = new TestOutputDirectory();
        var services = new TestServices();

        var result = await services.WordPipeline.ConvertAsync(
            TestPaths.Fixture("basic.md"),
            output.File("basic.docx"));

        Assert.True(result.SizeBytes > 0);
        Assert.EndsWith(".docx", result.OutputPath, StringComparison.OrdinalIgnoreCase);

        using var archive = ZipFile.OpenRead(result.OutputPath);
        var documentEntry = archive.GetEntry("word/document.xml");
        Assert.NotNull(documentEntry);

        using var reader = new StreamReader(documentEntry!.Open(), Encoding.UTF8);
        var documentXml = await reader.ReadToEndAsync();

        Assert.Contains("<m:oMath", documentXml, StringComparison.Ordinal);
        Assert.DoesNotContain("<w:drawing", documentXml, StringComparison.Ordinal);
        Assert.Contains("一级标题", documentXml, StringComparison.Ordinal);
        Assert.Contains("测试作者", documentXml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Pdf_pipeline_generates_valid_pdf()
    {
        using var output = new TestOutputDirectory();
        var services = new TestServices();

        var result = await services.PdfPipeline.ConvertAsync(
            TestPaths.Fixture("basic.md"),
            output.File("basic.pdf"));

        Assert.True(result.SizeBytes > 10_000);
        Assert.EndsWith(".pdf", result.OutputPath, StringComparison.OrdinalIgnoreCase);

        var bytes = await File.ReadAllBytesAsync(result.OutputPath);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
    }
}

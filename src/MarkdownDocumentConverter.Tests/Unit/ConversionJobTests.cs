using MarkdownDocumentConverter.Core.Models;

namespace MarkdownDocumentConverter.Tests.Unit;

public sealed class ConversionJobTests
{
    [Fact]
    public void Unrequested_format_starts_as_skipped()
    {
        var job = new ConversionJob(@"C:\docs\input.md", @"C:\docs", OutputFormat.Docx);

        Assert.Equal(ConversionStatus.Waiting, job.WordStatus);
        Assert.Equal(ConversionStatus.Skipped, job.PdfStatus);
        Assert.Equal(ConversionStatus.Waiting, job.Status);
        Assert.False(job.IsFinished);
    }

    [Fact]
    public void Overall_status_follows_both_requested_formats()
    {
        var job = new ConversionJob(@"C:\docs\input.md", @"C:\docs", OutputFormat.All);

        job.MarkRunning(OutputFormat.Docx);
        Assert.Equal(ConversionStatus.Running, job.WordStatus);
        Assert.Equal(ConversionStatus.Running, job.Status);

        job.MarkSucceeded(OutputFormat.Docx, @"C:\docs\input.docx", TimeSpan.FromSeconds(1));
        Assert.Equal(ConversionStatus.Running, job.Status);
        Assert.Equal(ConversionStatus.Waiting, job.PdfStatus);

        job.MarkRunning(OutputFormat.Pdf);
        job.MarkSucceeded(OutputFormat.Pdf, @"C:\docs\input.pdf", TimeSpan.FromSeconds(2));

        Assert.Equal(ConversionStatus.Success, job.Status);
        Assert.Equal(ConversionStatus.Success, job.WordStatus);
        Assert.Equal(ConversionStatus.Success, job.PdfStatus);
        Assert.Equal(TimeSpan.FromSeconds(3), job.Duration);
        Assert.True(job.IsFinished);
    }

    [Fact]
    public void Failure_of_one_format_does_not_erase_other_output()
    {
        var job = new ConversionJob(@"C:\docs\input.md", @"C:\docs", OutputFormat.All);
        job.MarkSucceeded(OutputFormat.Docx, @"C:\docs\input.docx", TimeSpan.FromSeconds(1));

        job.MarkFailed(OutputFormat.Pdf, "PDF 编译失败。");

        Assert.Equal(ConversionStatus.Failed, job.Status);
        Assert.Equal(ConversionStatus.Success, job.WordStatus);
        Assert.Equal(ConversionStatus.Failed, job.PdfStatus);
        Assert.NotNull(job.DocxPath);
        Assert.Null(job.PdfPath);
        Assert.Equal("PDF 编译失败。", job.ErrorMessage);

        var result = job.ToResult();
        Assert.Equal(job.Id, result.JobId);
        Assert.Equal(ConversionStatus.Failed, result.Status);
        Assert.Equal(job.DocxPath, result.DocxPath);
    }

    [Fact]
    public void Unrequested_format_cannot_be_updated()
    {
        var job = new ConversionJob(@"C:\docs\input.md", @"C:\docs", OutputFormat.Docx);

        Assert.Throws<InvalidOperationException>(() =>
            job.MarkSucceeded(OutputFormat.Pdf, @"C:\docs\input.pdf", TimeSpan.Zero));
    }

    [Fact]
    public void Job_requires_at_least_one_format()
    {
        Assert.Throws<ArgumentException>(() =>
            new ConversionJob(@"C:\docs\input.md", @"C:\docs", OutputFormat.None));
    }
}

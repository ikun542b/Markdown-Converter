using MarkdownDocumentConverter.Core.Models;
using MarkdownDocumentConverter.Tests.Infrastructure;

namespace MarkdownDocumentConverter.Tests.Integration;

public sealed class ConversionCoordinatorTests
{
    [Fact]
    public async Task Converts_selected_format_and_reports_success()
    {
        using var output = new TestOutputDirectory();
        var services = new TestServices();
        var request = new ConversionRequest
        {
            InputPath = TestPaths.Fixture("basic.md"),
            OutputDirectory = output.Path,
            Formats = OutputFormat.Docx,
            StyleId = "modern",
        };

        var progress = new RecordingProgress<ConversionProgress>();
        var result = await services.Coordinator.ConvertAsync(request, progress);

        Assert.Equal(ConversionStatus.Success, result.Status);
        Assert.NotNull(result.DocxPath);
        Assert.Null(result.PdfPath);
        Assert.True(File.Exists(result.DocxPath));
        Assert.Contains(
            progress.Items,
            item => item.Stage == ConversionStage.Completed && item.Status == ConversionStatus.Success);
    }

    [Fact]
    public async Task Missing_input_is_reported_as_failure_without_throwing()
    {
        using var output = new TestOutputDirectory();
        var services = new TestServices();
        var request = new ConversionRequest
        {
            InputPath = output.File("missing.md"),
            OutputDirectory = output.Path,
            Formats = OutputFormat.Docx,
        };

        var result = await services.Coordinator.ConvertAsync(request);

        Assert.Equal(ConversionStatus.Failed, result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
        Assert.Null(result.DocxPath);
    }

    [Fact]
    public async Task Skip_policy_leaves_existing_output_untouched()
    {
        using var output = new TestOutputDirectory();
        var services = new TestServices();
        var existingPath = output.File("basic.docx");
        await File.WriteAllTextAsync(existingPath, "keep me");

        var request = new ConversionRequest
        {
            InputPath = TestPaths.Fixture("basic.md"),
            OutputDirectory = output.Path,
            Formats = OutputFormat.Docx,
            OverwritePolicy = OverwritePolicy.Skip,
        };

        var result = await services.Coordinator.ConvertAsync(request);

        Assert.Equal(ConversionStatus.Success, result.Status);
        Assert.Null(result.DocxPath);
        Assert.Equal("keep me", await File.ReadAllTextAsync(existingPath));
    }

    private sealed class RecordingProgress<T> : IProgress<T>
    {
        public List<T> Items { get; } = new();

        public void Report(T value)
        {
            Items.Add(value);
        }
    }
}

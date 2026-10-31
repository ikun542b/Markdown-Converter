using MarkdownDocumentConverter.Core.Models;
using MarkdownDocumentConverter.Tests.Infrastructure;

namespace MarkdownDocumentConverter.Tests.Integration;

public sealed class BatchServiceTests
{
    [Fact]
    public async Task Hundred_files_complete_with_parallel_batch()
    {
        using var output = new TestOutputDirectory();
        var services = new TestServices();
        var requests = new List<ConversionRequest>();

        for (var index = 0; index < 100; index++)
        {
            var inputPath = output.File($"file-{index:000}.md");
            await File.WriteAllTextAsync(
                inputPath,
                $"# 文档 {index}\n\n这是第 {index} 个批量转换测试。\n");

            requests.Add(new ConversionRequest
            {
                InputPath = inputPath,
                OutputDirectory = output.File($"out-{index:000}"),
                Formats = OutputFormat.Docx,
                StyleId = "minimal",
            });
        }

        var results = await services.Batch.ConvertAsync(requests, maxParallelJobs: 4);

        Assert.Equal(100, results.Count);
        Assert.All(results, result =>
        {
            Assert.Equal(ConversionStatus.Success, result.Status);
            Assert.NotNull(result.DocxPath);
            Assert.True(File.Exists(result.DocxPath));
        });
    }

    [Fact]
    public async Task One_failed_file_does_not_stop_other_jobs()
    {
        using var output = new TestOutputDirectory();
        var services = new TestServices();
        var validA = output.File("a.md");
        var validB = output.File("b.md");
        await File.WriteAllTextAsync(validA, "# A\n\n内容 A。\n");
        await File.WriteAllTextAsync(validB, "# B\n\n内容 B。\n");

        var requests = new[]
        {
            new ConversionRequest
            {
                InputPath = validA,
                OutputDirectory = output.File("out-a"),
                Formats = OutputFormat.Docx,
            },
            new ConversionRequest
            {
                InputPath = output.File("missing.md"),
                OutputDirectory = output.File("out-missing"),
                Formats = OutputFormat.Docx,
            },
            new ConversionRequest
            {
                InputPath = validB,
                OutputDirectory = output.File("out-b"),
                Formats = OutputFormat.Docx,
            },
        };

        var results = await services.Batch.ConvertAsync(requests, maxParallelJobs: 3);

        Assert.Equal(3, results.Count);
        Assert.Equal(2, results.Count(result => result.Status == ConversionStatus.Success));
        Assert.Single(results, result => result.Status == ConversionStatus.Failed);
    }
}

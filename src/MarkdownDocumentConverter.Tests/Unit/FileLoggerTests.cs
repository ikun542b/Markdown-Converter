using MarkdownDocumentConverter.Core.Logging;
using MarkdownDocumentConverter.Tests.Infrastructure;

namespace MarkdownDocumentConverter.Tests.Unit;

public sealed class FileLoggerTests
{
    [Fact]
    public void Logger_rotates_and_keeps_retention_limit()
    {
        using var directory = new TestOutputDirectory();
        using var logger = new FileLogger(
            directory.Path,
            retentionCount: 2,
            maxFileSizeBytes: 1024);

        for (var index = 0; index < 100; index++)
        {
            logger.Info(new string('x', 100) + index);
        }

        Assert.True(File.Exists(logger.LogFilePath));
        Assert.Contains("INFO", File.ReadAllText(logger.LogFilePath), StringComparison.Ordinal);

        var rotatedFiles = Directory.GetFiles(directory.Path, "app-*.log");
        Assert.NotEmpty(rotatedFiles);
        Assert.True(rotatedFiles.Length <= 3, $"轮转文件过多：{rotatedFiles.Length}");
    }
}

using MarkdownDocumentConverter.Core.Engines;
using MarkdownDocumentConverter.Core.Exceptions;

namespace MarkdownDocumentConverter.Tests.Unit;

public sealed class ProcessRunnerTests
{
    [Fact]
    public async Task Missing_executable_produces_clear_error()
    {
        var runner = new ProcessRunner();

        var exception = await Assert.ThrowsAsync<ProcessExecutionException>(() =>
            runner.RunAsync(
                @"Z:\missing\converter.exe",
                Array.Empty<string>(),
                workingDirectory: null,
                TimeSpan.FromSeconds(5)));

        Assert.Contains("找不到可执行文件", exception.Message, StringComparison.Ordinal);
    }
}

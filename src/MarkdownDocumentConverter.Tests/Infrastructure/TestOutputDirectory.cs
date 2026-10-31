namespace MarkdownDocumentConverter.Tests.Infrastructure;

internal sealed class TestOutputDirectory : IDisposable
{
    public TestOutputDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "MDC",
            "tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string File(string fileName)
    {
        return System.IO.Path.Combine(Path, fileName);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch (IOException)
        {
            // 测试清理失败不应掩盖测试结果。
        }
        catch (UnauthorizedAccessException)
        {
            // 测试清理失败不应掩盖测试结果。
        }
    }
}

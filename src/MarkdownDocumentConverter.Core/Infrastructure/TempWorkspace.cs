namespace MarkdownDocumentConverter.Core.Infrastructure;

public sealed class TempWorkspace : IDisposable
{
    private readonly bool _retain;

    private TempWorkspace(string jobDirectory, bool retain)
    {
        JobDirectory = jobDirectory;
        _retain = retain;

        InputDirectory = Path.Combine(JobDirectory, "input");
        IntermediateDirectory = Path.Combine(JobDirectory, "intermediate");
        OutputDirectory = Path.Combine(JobDirectory, "output");
        LogDirectory = Path.Combine(JobDirectory, "logs");

        Directory.CreateDirectory(InputDirectory);
        Directory.CreateDirectory(IntermediateDirectory);
        Directory.CreateDirectory(OutputDirectory);
        Directory.CreateDirectory(LogDirectory);
    }

    public string JobDirectory { get; }

    public string InputDirectory { get; }

    public string IntermediateDirectory { get; }

    public string OutputDirectory { get; }

    public string LogDirectory { get; }

    public static TempWorkspace Create(bool retain = false)
    {
        var jobId = Guid.NewGuid().ToString("N");
        var jobDirectory = Path.Combine(Path.GetTempPath(), "MDC", jobId);
        return new TempWorkspace(jobDirectory, retain);
    }

    public void Dispose()
    {
        if (_retain)
        {
            return;
        }

        try
        {
            if (Directory.Exists(JobDirectory))
            {
                Directory.Delete(JobDirectory, recursive: true);
            }
        }
        catch (IOException)
        {
            // 临时文件可能仍被引擎占用，交给系统清理。
        }
        catch (UnauthorizedAccessException)
        {
            // 临时文件可能仍被引擎占用，交给系统清理。
        }
    }
}

using System.Text;
using MarkdownDocumentConverter.Core.Abstractions;

namespace MarkdownDocumentConverter.Core.Logging;

public sealed class FileLogger : IAppLogger, IDisposable
{
    private readonly object _sync = new();
    private readonly string _directory;
    private readonly string _currentPath;
    private readonly int _retentionCount;
    private readonly long _maxFileSizeBytes;

    public FileLogger(
        string? directory = null,
        int retentionCount = 10,
        long maxFileSizeBytes = 2 * 1024 * 1024)
    {
        _directory = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MarkdownDocumentConverter",
            "logs");

        _retentionCount = Math.Max(1, retentionCount);
        _maxFileSizeBytes = Math.Max(1024, maxFileSizeBytes);
        _currentPath = Path.Combine(_directory, "app.log");

        Directory.CreateDirectory(_directory);
    }

    public string LogFilePath => _currentPath;

    public void Info(string message)
    {
        Write("INFO", message, exception: null);
    }

    public void Error(string message, Exception? exception = null)
    {
        Write("ERROR", message, exception);
    }

    public void Dispose()
    {
        // 文件以追加方式打开，无需持有句柄。
    }

    private void Write(string level, string message, Exception? exception)
    {
        lock (_sync)
        {
            try
            {
                if (File.Exists(_currentPath)
                    && new FileInfo(_currentPath).Length >= _maxFileSizeBytes)
                {
                    Rotate();
                }

                var builder = new StringBuilder();
                builder.Append(DateTimeOffset.Now.ToString("O"));
                builder.Append(" [");
                builder.Append(level);
                builder.Append("] ");
                builder.AppendLine(message);

                if (exception is not null)
                {
                    builder.AppendLine(exception.ToString());
                }

                File.AppendAllText(_currentPath, builder.ToString(), new UTF8Encoding(false));
            }
            catch (IOException)
            {
                // 日志失败不能影响转换主流程。
            }
            catch (UnauthorizedAccessException)
            {
                // 日志失败不能影响转换主流程。
            }
        }
    }

    private void Rotate()
    {
        var rotatedPath = Path.Combine(
            _directory,
            $"app-{DateTime.Now:yyyyMMdd-HHmmssfff}.log");

        File.Move(_currentPath, rotatedPath, overwrite: true);

        var oldFiles = Directory.GetFiles(_directory, "app-*.log")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .Skip(_retentionCount)
            .ToArray();

        foreach (var oldFile in oldFiles)
        {
            try
            {
                File.Delete(oldFile);
            }
            catch (IOException)
            {
                // 忽略并发删除失败。
            }
        }
    }
}

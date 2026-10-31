namespace MarkdownDocumentConverter.Core.Exceptions;

public sealed class ProcessExecutionException : ConversionException
{
    public ProcessExecutionException(string message, string fileName, Exception? innerException = null)
        : base(message, innerException!)
    {
        FileName = fileName;
        StandardOutput = string.Empty;
        StandardError = string.Empty;
    }

    public ProcessExecutionException(
        string message,
        string fileName,
        string standardOutput,
        string standardError,
        int? exitCode,
        bool timedOut,
        TimeSpan duration,
        Exception? innerException = null)
        : base(message, innerException!)
    {
        FileName = fileName;
        StandardOutput = standardOutput;
        StandardError = standardError;
        ExitCode = exitCode;
        TimedOut = timedOut;
        Duration = duration;
    }

    public string FileName { get; }

    public string StandardOutput { get; }

    public string StandardError { get; }

    public int? ExitCode { get; }

    public bool TimedOut { get; }

    public TimeSpan Duration { get; }
}

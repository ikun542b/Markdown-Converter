namespace MarkdownDocumentConverter.Core.Models;

public sealed class ConversionJob
{
    public ConversionJob(string inputPath, string outputDirectory, OutputFormat formats)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        if (formats == OutputFormat.None)
        {
            throw new ArgumentException("至少需要选择一种输出格式。", nameof(formats));
        }

        Id = Guid.NewGuid();
        InputPath = Path.GetFullPath(inputPath);
        OutputDirectory = Path.GetFullPath(outputDirectory);
        Formats = formats;

        WordStatus = formats.HasFlag(OutputFormat.Docx)
            ? ConversionStatus.Waiting
            : ConversionStatus.Skipped;
        PdfStatus = formats.HasFlag(OutputFormat.Pdf)
            ? ConversionStatus.Waiting
            : ConversionStatus.Skipped;
    }

    public Guid Id { get; }

    public string InputPath { get; }

    public string OutputDirectory { get; }

    public OutputFormat Formats { get; }

    public ConversionStatus Status { get; private set; } = ConversionStatus.Waiting;

    public ConversionStatus WordStatus { get; private set; }

    public ConversionStatus PdfStatus { get; private set; }

    public string? DocxPath { get; private set; }

    public string? PdfPath { get; private set; }

    public string? ErrorMessage { get; private set; }

    public TimeSpan Duration { get; private set; }

    public bool IsFinished => Status is
        ConversionStatus.Success or
        ConversionStatus.Failed or
        ConversionStatus.Cancelled;

    private bool HasStarted { get; set; }

    public void MarkRunning(OutputFormat format)
    {
        EnsureRequested(format);
        HasStarted = true;
        SetFormatStatus(format, ConversionStatus.Running);
        Status = ConversionStatus.Running;
    }

    public void MarkSucceeded(OutputFormat format, string outputPath, TimeSpan duration)
    {
        EnsureRequested(format);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        HasStarted = true;

        if (format == OutputFormat.Docx)
        {
            DocxPath = Path.GetFullPath(outputPath);
        }
        else
        {
            PdfPath = Path.GetFullPath(outputPath);
        }

        Duration += duration;
        SetFormatStatus(format, ConversionStatus.Success);
        RefreshStatus();
    }

    public void MarkFailed(OutputFormat format, string errorMessage)
    {
        EnsureRequested(format);
        HasStarted = true;
        ErrorMessage = errorMessage;
        SetFormatStatus(format, ConversionStatus.Failed);
        RefreshStatus();
    }

    public void MarkCancelled(OutputFormat format)
    {
        EnsureRequested(format);
        HasStarted = true;
        SetFormatStatus(format, ConversionStatus.Cancelled);
        RefreshStatus();
    }

    public void MarkSkipped(OutputFormat format, string? reason = null)
    {
        EnsureRequested(format);
        HasStarted = true;
        ErrorMessage ??= reason;
        SetFormatStatus(format, ConversionStatus.Skipped);
        RefreshStatus();
    }

    public ConversionResult ToResult()
    {
        return new ConversionResult(
            Id,
            InputPath,
            DocxPath,
            PdfPath,
            Status,
            ErrorMessage,
            Duration);
    }

    private void RefreshStatus()
    {
        var requestedStatuses = new List<ConversionStatus>(2);

        if (Formats.HasFlag(OutputFormat.Docx))
        {
            requestedStatuses.Add(WordStatus);
        }

        if (Formats.HasFlag(OutputFormat.Pdf))
        {
            requestedStatuses.Add(PdfStatus);
        }

        if (requestedStatuses.Any(status => status == ConversionStatus.Failed))
        {
            Status = ConversionStatus.Failed;
        }
        else if (requestedStatuses.Any(status => status == ConversionStatus.Cancelled))
        {
            Status = ConversionStatus.Cancelled;
        }
        else if (requestedStatuses.All(status => status is
                     ConversionStatus.Success or
                     ConversionStatus.Skipped))
        {
            Status = ConversionStatus.Success;
        }
        else if (HasStarted || requestedStatuses.Any(status => status == ConversionStatus.Running))
        {
            Status = ConversionStatus.Running;
        }
        else
        {
            Status = ConversionStatus.Waiting;
        }
    }

    private void SetFormatStatus(OutputFormat format, ConversionStatus status)
    {
        if (format == OutputFormat.Docx)
        {
            WordStatus = status;
        }
        else
        {
            PdfStatus = status;
        }
    }

    private void EnsureRequested(OutputFormat format)
    {
        if (format is not (OutputFormat.Docx or OutputFormat.Pdf))
        {
            throw new ArgumentException("格式必须是单一的输出格式。", nameof(format));
        }

        if (!Formats.HasFlag(format))
        {
            throw new InvalidOperationException($"任务未请求输出格式：{format}。");
        }
    }
}

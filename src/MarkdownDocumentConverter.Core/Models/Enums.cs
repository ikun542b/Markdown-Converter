namespace MarkdownDocumentConverter.Core.Models;

public enum PageSize
{
    A4,
    A5,
}

[Flags]
public enum OutputFormat
{
    None = 0,
    Docx = 1,
    Pdf = 2,
    All = Docx | Pdf,
}

public enum OverwritePolicy
{
    Rename,
    Overwrite,
    Skip,
}

public enum ConversionStatus
{
    Waiting,
    Running,
    Success,
    Failed,
    Cancelled,
    Skipped,
}

public enum ConversionStage
{
    Waiting,
    Preprocessing,
    Word,
    Pdf,
    Completed,
    Failed,
    Cancelled,
}

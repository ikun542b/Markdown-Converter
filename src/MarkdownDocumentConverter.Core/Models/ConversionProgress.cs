namespace MarkdownDocumentConverter.Core.Models;

public sealed record ConversionProgress(
    Guid JobId,
    string InputPath,
    ConversionStage Stage,
    OutputFormat? Format,
    int CompletedCount,
    int TotalCount,
    ConversionStatus Status,
    string Message);

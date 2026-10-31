namespace MarkdownDocumentConverter.Core.Models;

public sealed record ConversionResult(
    Guid JobId,
    string InputPath,
    string? DocxPath,
    string? PdfPath,
    ConversionStatus Status,
    string? ErrorMessage,
    TimeSpan Duration);

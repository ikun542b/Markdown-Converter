namespace MarkdownDocumentConverter.Core.Models;

public sealed record ProcessResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    TimeSpan Duration);

namespace MarkdownDocumentConverter.Core.Models;

public sealed record ConversionArtifact(
    string OutputPath,
    long SizeBytes,
    TimeSpan Duration,
    string StandardError);

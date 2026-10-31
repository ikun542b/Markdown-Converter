namespace MarkdownDocumentConverter.Core.Models;

public sealed record ConversionRequest
{
    public required string InputPath { get; init; }

    public required string OutputDirectory { get; init; }

    public OutputFormat Formats { get; init; } = OutputFormat.All;

    public PageSize PageSize { get; init; } = PageSize.A4;

    public string StyleId { get; init; } = "modern";

    public string? FontFamily { get; init; }

    public double FontScale { get; init; } = 1.0;

    public OverwritePolicy OverwritePolicy { get; init; } = OverwritePolicy.Rename;

    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(2);
}

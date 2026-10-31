namespace MarkdownDocumentConverter.Core.Models;

public sealed record DocumentMetadata
{
    public string? Title { get; init; }

    public string? Author { get; init; }

    public DateOnly? Date { get; init; }

    public string? Subject { get; init; }

    public IReadOnlyList<string> Keywords { get; init; } = Array.Empty<string>();

    public string? Language { get; init; }

    public IReadOnlyDictionary<string, string> AdditionalFields { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

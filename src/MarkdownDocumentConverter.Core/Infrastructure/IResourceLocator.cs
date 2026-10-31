namespace MarkdownDocumentConverter.Core.Infrastructure;

public interface IResourceLocator
{
    string? ResourceRoot { get; }

    string GetEnginePath(string fileName);

    bool TryGetEnginePath(string fileName, out string path);

    string GetResourcePath(params string[] relativeParts);
}

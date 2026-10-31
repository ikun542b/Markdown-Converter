namespace MarkdownDocumentConverter.Core.Infrastructure;

public interface IResourceLocator
{
    string? ResourceRoot { get; }

    string GetEnginePath(string fileName);

    bool TryGetEnginePath(string fileName, out string path);

    string GetDocxTemplatePath(string styleId);

    string GetTypstTemplatePath(string styleId);

    string GetResourcePath(params string[] relativeParts);
}

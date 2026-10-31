using MarkdownDocumentConverter.Core.Exceptions;

namespace MarkdownDocumentConverter.Core.Infrastructure;

public sealed class ResourceLocator : IResourceLocator
{
    private const string EngineFolderName = "engines";
    private const string EnginePlatformFolderName = "win-x64";

    private readonly string? _resourceRoot;

    public ResourceLocator(string? startDirectory = null)
    {
        _resourceRoot = FindResourceRoot(startDirectory ?? AppContext.BaseDirectory);
    }

    public string? ResourceRoot => _resourceRoot;

    public string GetEnginePath(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        if (_resourceRoot is null)
        {
            throw new ResourceNotFoundException(
                "未找到 resources 目录。请确认应用目录中包含 resources/engines/win-x64。");
        }

        var path = Path.Combine(
            _resourceRoot,
            EngineFolderName,
            EnginePlatformFolderName,
            fileName);

        if (!File.Exists(path))
        {
            throw new ResourceNotFoundException($"找不到转换引擎：{path}");
        }

        return path;
    }

    public bool TryGetEnginePath(string fileName, out string path)
    {
        path = string.Empty;

        if (_resourceRoot is null || string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var candidate = Path.Combine(
            _resourceRoot,
            EngineFolderName,
            EnginePlatformFolderName,
            fileName);

        if (!File.Exists(candidate))
        {
            return false;
        }

        path = candidate;
        return true;
    }

    public string GetDocxTemplatePath(string styleId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(styleId);

        var fileName = styleId + ".docx";

        if (!string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal))
        {
            throw new ResourceNotFoundException($"无效的样式标识：{styleId}");
        }

        if (_resourceRoot is null)
        {
            throw new ResourceNotFoundException(
                "未找到 resources 目录。请确认应用目录中包含 resources/docx。");
        }

        var path = Path.Combine(_resourceRoot, "docx", fileName);

        if (!File.Exists(path))
        {
            throw new ResourceNotFoundException($"找不到 Word 样式模板：{path}");
        }

        return path;
    }

    public string GetResourcePath(params string[] relativeParts)
    {
        ArgumentNullException.ThrowIfNull(relativeParts);

        if (_resourceRoot is null)
        {
            throw new ResourceNotFoundException(
                "未找到 resources 目录。请确认应用目录中包含 resources/engines/win-x64。");
        }

        var parts = new string[relativeParts.Length + 1];
        parts[0] = _resourceRoot;
        Array.Copy(relativeParts, 0, parts, 1, relativeParts.Length);

        var path = Path.Combine(parts);

        if (!File.Exists(path) && !Directory.Exists(path))
        {
            throw new ResourceNotFoundException($"找不到资源：{path}");
        }

        return path;
    }

    private static string? FindResourceRoot(string startDirectory)
    {
        var overrideRoot = Environment.GetEnvironmentVariable("MDC_RESOURCE_ROOT");
        if (!string.IsNullOrWhiteSpace(overrideRoot))
        {
            var fullOverridePath = Path.GetFullPath(overrideRoot);
            if (LooksLikeResourceRoot(fullOverridePath))
            {
                return fullOverridePath;
            }
        }

        var current = new DirectoryInfo(Path.GetFullPath(startDirectory));

        for (var depth = 0; current is not null && depth < 12; depth++)
        {
            var candidate = Path.Combine(current.FullName, "resources");
            if (LooksLikeResourceRoot(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }

    private static bool LooksLikeResourceRoot(string path)
    {
        return File.Exists(Path.Combine(
            path,
            EngineFolderName,
            EnginePlatformFolderName,
            "pandoc.exe"));
    }
}

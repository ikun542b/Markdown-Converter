using MarkdownDocumentConverter.Core.Models;

namespace MarkdownDocumentConverter.Core.Services;

public sealed class OutputPathService
{
    public string Resolve(
        string inputPath,
        string outputDirectory,
        string extension,
        OverwritePolicy policy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);

        Directory.CreateDirectory(outputDirectory);

        var baseName = Path.GetFileNameWithoutExtension(inputPath);

        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "document";
        }

        var candidate = Path.Combine(outputDirectory, baseName + extension);

        if (!File.Exists(candidate))
        {
            return candidate;
        }

        if (policy == OverwritePolicy.Overwrite)
        {
            return candidate;
        }

        if (policy == OverwritePolicy.Skip)
        {
            return candidate;
        }

        for (var index = 1; index < 10_000; index++)
        {
            var numbered = Path.Combine(
                outputDirectory,
                $"{baseName} ({index}){extension}");

            if (!File.Exists(numbered))
            {
                return numbered;
            }
        }

        throw new InvalidOperationException($"无法为输出文件生成唯一名称：{candidate}");
    }
}

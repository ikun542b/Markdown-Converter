using MarkdownDocumentConverter.Core.Exceptions;
using MarkdownDocumentConverter.Core.Infrastructure;
using MarkdownDocumentConverter.Core.Models;

namespace MarkdownDocumentConverter.Core.Engines;

public sealed class PandocEngine
{
    private static readonly TimeSpan VersionTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan ConversionTimeout = TimeSpan.FromMinutes(2);

    private readonly IProcessRunner _processRunner;
    private readonly IResourceLocator _resourceLocator;

    public PandocEngine(IProcessRunner processRunner, IResourceLocator resourceLocator)
    {
        _processRunner = processRunner;
        _resourceLocator = resourceLocator;
    }

    public string ExecutablePath => _resourceLocator.GetEnginePath("pandoc.exe");

    public async Task<string> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        var result = await _processRunner.RunAsync(
            ExecutablePath,
            new[] { "--version" },
            workingDirectory: null,
            VersionTimeout,
            cancellationToken).ConfigureAwait(false);

        var firstLine = result.StandardOutput
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(firstLine))
        {
            throw new ConversionException("Pandoc 未返回版本信息。");
        }

        return firstLine;
    }

    public Task<ProcessResult> ConvertToDocxAsync(
        string inputPath,
        string outputPath,
        string? referenceDocPath = null,
        bool includeTableOfContents = false,
        string? tableOfContentsTitle = null,
        CancellationToken cancellationToken = default)
    {
        var input = ValidateInput(inputPath);
        var output = PrepareOutput(outputPath);

        var arguments = new List<string>
        {
            input,
            "-o",
            output,
            "--standalone",
        };

        if (!string.IsNullOrWhiteSpace(referenceDocPath))
        {
            arguments.Add("--reference-doc");
            arguments.Add(Path.GetFullPath(referenceDocPath));
        }

        if (includeTableOfContents)
        {
            arguments.Add("--toc");

            if (!string.IsNullOrWhiteSpace(tableOfContentsTitle))
            {
                arguments.Add("--metadata");
                arguments.Add($"toc-title={tableOfContentsTitle}");
            }
        }

        return _processRunner.RunAsync(
            ExecutablePath,
            arguments,
            Path.GetDirectoryName(input),
            ConversionTimeout,
            cancellationToken);
    }

    public Task<ProcessResult> ConvertToTypstAsync(
        string inputPath,
        string outputPath,
        string? templatePath = null,
        bool includeTableOfContents = false,
        int tableOfContentsDepth = 3,
        CancellationToken cancellationToken = default)
    {
        var input = ValidateInput(inputPath);
        var output = PrepareOutput(outputPath);

        var arguments = new List<string>
        {
            input,
            "-t",
            "typst",
            "-o",
            output,
            "--standalone",
        };

        if (!string.IsNullOrWhiteSpace(templatePath))
        {
            arguments.Add("--template");
            arguments.Add(Path.GetFullPath(templatePath));
        }

        if (includeTableOfContents)
        {
            arguments.Add("--toc");
            arguments.Add("--toc-depth");
            arguments.Add(tableOfContentsDepth.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        return _processRunner.RunAsync(
            ExecutablePath,
            arguments,
            Path.GetDirectoryName(input),
            ConversionTimeout,
            cancellationToken);
    }

    private static string ValidateInput(string inputPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);

        var fullPath = Path.GetFullPath(inputPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"找不到 Markdown 文件：{fullPath}", fullPath);
        }

        return fullPath;
    }

    private static string PrepareOutput(string outputPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var fullPath = Path.GetFullPath(outputPath);
        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return fullPath;
    }
}

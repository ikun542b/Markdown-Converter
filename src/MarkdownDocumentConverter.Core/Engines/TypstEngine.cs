using MarkdownDocumentConverter.Core.Exceptions;
using MarkdownDocumentConverter.Core.Infrastructure;
using MarkdownDocumentConverter.Core.Models;

namespace MarkdownDocumentConverter.Core.Engines;

public sealed class TypstEngine
{
    private static readonly TimeSpan VersionTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan CompileTimeout = TimeSpan.FromMinutes(2);

    private readonly IProcessRunner _processRunner;
    private readonly IResourceLocator _resourceLocator;

    public TypstEngine(IProcessRunner processRunner, IResourceLocator resourceLocator)
    {
        _processRunner = processRunner;
        _resourceLocator = resourceLocator;
    }

    public string ExecutablePath => _resourceLocator.GetEnginePath("typst.exe");

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
            throw new ConversionException("Typst 未返回版本信息。");
        }

        return firstLine;
    }

    public Task<ProcessResult> CompileAsync(
        string inputPath,
        string outputPath,
        string? fontPath = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);

        var input = Path.GetFullPath(inputPath);
        if (!File.Exists(input))
        {
            throw new FileNotFoundException($"找不到 Typst 源文件：{input}", input);
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var output = Path.GetFullPath(outputPath);
        var directory = Path.GetDirectoryName(output);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var arguments = new List<string>
        {
            "compile",
            input,
            output,
        };

        if (!string.IsNullOrWhiteSpace(fontPath))
        {
            arguments.Add("--font-path");
            arguments.Add(Path.GetFullPath(fontPath));
        }

        return _processRunner.RunAsync(
            ExecutablePath,
            arguments,
            Path.GetDirectoryName(input),
            CompileTimeout,
            cancellationToken);
    }
}

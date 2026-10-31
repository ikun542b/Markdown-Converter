using MarkdownDocumentConverter.Core.Engines;
using MarkdownDocumentConverter.Core.Models;

namespace MarkdownDocumentConverter.Core.Pipelines;

public sealed class WordPipeline
{
    private readonly PandocEngine _pandoc;

    public WordPipeline(PandocEngine pandoc)
    {
        _pandoc = pandoc;
    }

    public async Task<ConversionArtifact> ConvertAsync(
        string inputPath,
        string outputPath,
        string? referenceDocPath = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _pandoc.ConvertToDocxAsync(
            inputPath,
            outputPath,
            referenceDocPath,
            cancellationToken).ConfigureAwait(false);

        var fullOutputPath = Path.GetFullPath(outputPath);
        OutputValidator.EnsureNonEmpty(fullOutputPath, "Word");
        OutputValidator.EnsureDocx(fullOutputPath);

        return new ConversionArtifact(
            fullOutputPath,
            new FileInfo(fullOutputPath).Length,
            result.Duration,
            result.StandardError);
    }
}

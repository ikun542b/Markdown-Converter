using MarkdownDocumentConverter.Core.Engines;
using MarkdownDocumentConverter.Core.Formatting;
using MarkdownDocumentConverter.Core.Infrastructure;
using MarkdownDocumentConverter.Core.Models;

namespace MarkdownDocumentConverter.Core.Pipelines;

public sealed class WordPipeline
{
    private readonly PandocEngine _pandoc;
    private readonly IResourceLocator _resourceLocator;
    private readonly DocxPostProcessor _postProcessor;

    public WordPipeline(
        PandocEngine pandoc,
        IResourceLocator resourceLocator,
        DocxPostProcessor? postProcessor = null)
    {
        _pandoc = pandoc;
        _resourceLocator = resourceLocator;
        _postProcessor = postProcessor ?? new DocxPostProcessor();
    }

    public Task<ConversionArtifact> ConvertAsync(
        string inputPath,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPath))
            ?? Path.GetTempPath();

        var request = new ConversionRequest
        {
            InputPath = inputPath,
            OutputDirectory = outputDirectory,
        };

        return ConvertAsync(request, outputPath, cancellationToken);
    }

    public async Task<ConversionArtifact> ConvertAsync(
        ConversionRequest request,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var style = StyleCatalog.GetById(request.StyleId);
        var referenceDocPath = _resourceLocator.GetDocxTemplatePath(style.Id);
        var fullOutputPath = Path.GetFullPath(outputPath);
        var outputDirectory = Path.GetDirectoryName(fullOutputPath);

        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        try
        {
            var pandocResult = await _pandoc.ConvertToDocxAsync(
                request.InputPath,
                fullOutputPath,
                referenceDocPath,
                includeTableOfContents: style.TocEnabled,
                tableOfContentsTitle: "目录",
                cancellationToken).ConfigureAwait(false);

            _postProcessor.Apply(
                fullOutputPath,
                request.InputPath,
                style,
                request.PageSize,
                request.FontFamily,
                Math.Clamp(request.FontScale, 0.5, 2.0));

            OutputValidator.EnsureNonEmpty(fullOutputPath, "Word");
            OutputValidator.EnsureDocx(fullOutputPath);

            return new ConversionArtifact(
                fullOutputPath,
                new FileInfo(fullOutputPath).Length,
                pandocResult.Duration,
                pandocResult.StandardError);
        }
        catch
        {
            TryDelete(fullOutputPath);
            throw;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // 清理失败不应掩盖原始异常。
        }
        catch (UnauthorizedAccessException)
        {
            // 清理失败不应掩盖原始异常。
        }
    }
}

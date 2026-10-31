using MarkdownDocumentConverter.Core.Engines;
using MarkdownDocumentConverter.Core.Formatting;
using MarkdownDocumentConverter.Core.Infrastructure;
using MarkdownDocumentConverter.Core.Models;

namespace MarkdownDocumentConverter.Core.Pipelines;

public sealed class PdfPipeline
{
    private readonly PandocEngine _pandoc;
    private readonly TypstEngine _typst;
    private readonly IResourceLocator _resourceLocator;
    private readonly TypstTemplateRenderer _templateRenderer;

    public PdfPipeline(
        PandocEngine pandoc,
        TypstEngine typst,
        IResourceLocator resourceLocator,
        TypstTemplateRenderer? templateRenderer = null)
    {
        _pandoc = pandoc;
        _typst = typst;
        _resourceLocator = resourceLocator;
        _templateRenderer = templateRenderer ?? new TypstTemplateRenderer();
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
        var templatePath = _resourceLocator.GetTypstTemplatePath(style.Id);
        var fullOutputPath = Path.GetFullPath(outputPath);
        var outputDirectory = Path.GetDirectoryName(fullOutputPath);

        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        using var workspace = TempWorkspace.Create();
        var renderedTemplatePath = Path.Combine(workspace.IntermediateDirectory, "template.typ");
        var typstSourcePath = Path.Combine(workspace.IntermediateDirectory, "document.typ");

        _templateRenderer.Render(templatePath, renderedTemplatePath, style, request);

        try
        {
            var pandocResult = await _pandoc.ConvertToTypstAsync(
                request.InputPath,
                typstSourcePath,
                renderedTemplatePath,
                includeTableOfContents: style.TocEnabled,
                tableOfContentsDepth: 3,
                cancellationToken).ConfigureAwait(false);

            var typstResult = await _typst.CompileAsync(
                typstSourcePath,
                fullOutputPath,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            OutputValidator.EnsureNonEmpty(fullOutputPath, "PDF");
            OutputValidator.EnsurePdf(fullOutputPath);

            return new ConversionArtifact(
                fullOutputPath,
                new FileInfo(fullOutputPath).Length,
                pandocResult.Duration + typstResult.Duration,
                typstResult.StandardError);
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

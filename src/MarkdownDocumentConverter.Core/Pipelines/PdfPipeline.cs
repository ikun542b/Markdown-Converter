using MarkdownDocumentConverter.Core.Engines;
using MarkdownDocumentConverter.Core.Infrastructure;
using MarkdownDocumentConverter.Core.Models;

namespace MarkdownDocumentConverter.Core.Pipelines;

public sealed class PdfPipeline
{
    private readonly PandocEngine _pandoc;
    private readonly TypstEngine _typst;

    public PdfPipeline(PandocEngine pandoc, TypstEngine typst)
    {
        _pandoc = pandoc;
        _typst = typst;
    }

    public async Task<ConversionArtifact> ConvertAsync(
        string inputPath,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var fullOutputPath = Path.GetFullPath(outputPath);
        var outputDirectory = Path.GetDirectoryName(fullOutputPath);

        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        using var workspace = TempWorkspace.Create();
        var typstSourcePath = Path.Combine(workspace.IntermediateDirectory, "document.typ");

        var pandocResult = await _pandoc.ConvertToTypstAsync(
            inputPath,
            typstSourcePath,
            cancellationToken).ConfigureAwait(false);

        if (!File.Exists(typstSourcePath))
        {
            throw new Exceptions.ConversionException($"Pandoc 未生成 Typst 源文件：{typstSourcePath}");
        }

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
}

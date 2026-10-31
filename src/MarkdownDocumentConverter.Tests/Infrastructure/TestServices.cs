using MarkdownDocumentConverter.Core.Engines;
using MarkdownDocumentConverter.Core.Infrastructure;
using MarkdownDocumentConverter.Core.Pipelines;
using MarkdownDocumentConverter.Core.Services;

namespace MarkdownDocumentConverter.Tests.Infrastructure;

internal sealed class TestServices
{
    public TestServices()
    {
        var processRunner = new ProcessRunner();
        Locator = new ResourceLocator();
        Pandoc = new PandocEngine(processRunner, Locator);
        Typst = new TypstEngine(processRunner, Locator);
        WordPipeline = new WordPipeline(Pandoc, Locator);
        PdfPipeline = new PdfPipeline(Pandoc, Typst, Locator);
        OutputPathService = new OutputPathService();
        Coordinator = new ConversionCoordinator(WordPipeline, PdfPipeline, OutputPathService);
    }

    public ResourceLocator Locator { get; }

    public PandocEngine Pandoc { get; }

    public TypstEngine Typst { get; }

    public WordPipeline WordPipeline { get; }

    public PdfPipeline PdfPipeline { get; }

    public OutputPathService OutputPathService { get; }

    public ConversionCoordinator Coordinator { get; }
}

using MarkdownDocumentConverter.App.Services;
using MarkdownDocumentConverter.Core.Engines;
using MarkdownDocumentConverter.Core.Infrastructure;
using MarkdownDocumentConverter.Core.Logging;
using MarkdownDocumentConverter.Core.Models;
using MarkdownDocumentConverter.Core.Pipelines;
using MarkdownDocumentConverter.Core.Services;

namespace MarkdownDocumentConverter.App;

public sealed class AppServices
{
    public AppServices()
    {
        var processRunner = new ProcessRunner();
        Locator = new ResourceLocator();
        Pandoc = new PandocEngine(processRunner, Locator);
        Typst = new TypstEngine(processRunner, Locator);
        WordPipeline = new WordPipeline(Pandoc, Locator);
        PdfPipeline = new PdfPipeline(Pandoc, Typst, Locator);
        Logger = new FileLogger();
        Coordinator = new ConversionCoordinator(WordPipeline, PdfPipeline, logger: Logger);
        Batch = new BatchService(Coordinator);
        SettingsService = new SettingsService(new JsonSettingsStore());
        FontService = new SystemFontService();
    }

    public ResourceLocator Locator { get; }

    public PandocEngine Pandoc { get; }

    public TypstEngine Typst { get; }

    public WordPipeline WordPipeline { get; }

    public PdfPipeline PdfPipeline { get; }

    public ConversionCoordinator Coordinator { get; }

    public BatchService Batch { get; }

    public FileLogger Logger { get; }

    public SettingsService SettingsService { get; }

    public SystemFontService FontService { get; }

    public AppSettings Settings { get; private set; } = new();

    public event EventHandler? SettingsChanged;

    public async Task LoadSettingsAsync(CancellationToken cancellationToken = default)
    {
        Settings = await SettingsService.LoadAsync(cancellationToken).ConfigureAwait(true);
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        Settings = AppSettingsMigration.Normalize(settings);
        await SettingsService.SaveAsync(Settings, cancellationToken).ConfigureAwait(true);
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }
}

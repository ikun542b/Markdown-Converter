using MarkdownDocumentConverter.Core.Abstractions;
using MarkdownDocumentConverter.Core.Models;

namespace MarkdownDocumentConverter.Core.Services;

public sealed class SettingsService
{
    private readonly ISettingsStore _store;

    public SettingsService(ISettingsStore store)
    {
        _store = store;
    }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
        return AppSettingsMigration.Normalize(settings);
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        var normalized = AppSettingsMigration.Normalize(settings);
        await _store.SaveAsync(normalized, cancellationToken).ConfigureAwait(false);
    }
}

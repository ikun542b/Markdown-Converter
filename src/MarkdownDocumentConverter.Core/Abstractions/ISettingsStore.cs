using MarkdownDocumentConverter.Core.Models;

namespace MarkdownDocumentConverter.Core.Abstractions;

public interface ISettingsStore
{
    Task<AppSettings?> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}

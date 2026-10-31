using System.Collections.Concurrent;
using MarkdownDocumentConverter.Core.Models;

namespace MarkdownDocumentConverter.Core.Services;

public sealed class BatchService
{
    private readonly ConversionCoordinator _coordinator;

    public BatchService(ConversionCoordinator coordinator)
    {
        _coordinator = coordinator;
    }

    public async Task<IReadOnlyList<ConversionResult>> ConvertAsync(
        IReadOnlyList<ConversionRequest> requests,
        int maxParallelJobs,
        IProgress<ConversionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requests);

        if (requests.Count == 0)
        {
            return Array.Empty<ConversionResult>();
        }

        var results = new ConcurrentDictionary<string, ConversionResult>(StringComparer.OrdinalIgnoreCase);
        using var throttler = new SemaphoreSlim(
            Math.Clamp(maxParallelJobs, 1, 8),
            Math.Clamp(maxParallelJobs, 1, 8));

        var tasks = requests.Select(async request =>
        {
            await throttler.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var result = await _coordinator
                    .ConvertAsync(request, progress, cancellationToken)
                    .ConfigureAwait(false);

                results[request.InputPath] = result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                results[request.InputPath] = new ConversionResult(
                    Guid.NewGuid(),
                    request.InputPath,
                    null,
                    null,
                    ConversionStatus.Failed,
                    ErrorMapper.ToUserError(ex).UserMessage,
                    TimeSpan.Zero);
            }
            finally
            {
                throttler.Release();
            }
        }).ToArray();

        await Task.WhenAll(tasks).ConfigureAwait(false);

        return requests
            .Select(request => results.TryGetValue(request.InputPath, out var result)
                ? result
                : new ConversionResult(
                    Guid.NewGuid(),
                    request.InputPath,
                    null,
                    null,
                    ConversionStatus.Cancelled,
                    "转换未完成。",
                    TimeSpan.Zero))
            .ToArray();
    }
}

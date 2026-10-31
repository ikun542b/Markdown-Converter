using MarkdownDocumentConverter.Core.Models;

namespace MarkdownDocumentConverter.Core.Engines;

public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string? workingDirectory,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}

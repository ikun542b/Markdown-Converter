using MarkdownDocumentConverter.Core.Models;

namespace MarkdownDocumentConverter.Core.Abstractions;

public interface IAppLogger
{
    void Info(string message);

    void Error(string message, Exception? exception = null);
}

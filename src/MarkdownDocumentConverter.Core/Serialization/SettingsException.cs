using MarkdownDocumentConverter.Core.Exceptions;

namespace MarkdownDocumentConverter.Core.Serialization;

public sealed class SettingsException : ConversionException
{
    public SettingsException(string message)
        : base(message)
    {
    }

    public SettingsException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

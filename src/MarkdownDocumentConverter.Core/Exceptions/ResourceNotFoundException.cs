namespace MarkdownDocumentConverter.Core.Exceptions;

public sealed class ResourceNotFoundException : ConversionException
{
    public ResourceNotFoundException(string message)
        : base(message)
    {
    }
}

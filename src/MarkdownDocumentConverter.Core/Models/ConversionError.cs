namespace MarkdownDocumentConverter.Core.Models;

public enum ConversionErrorCode
{
    Unknown,
    InputNotFound,
    InputAccessDenied,
    UnsupportedFormat,
    EngineMissing,
    EngineFailed,
    EngineTimeout,
    OutputInvalid,
    SettingsInvalid,
    Cancelled,
    OutputConflict,
}

public sealed record ConversionError(
    ConversionErrorCode Code,
    string UserMessage,
    string? TechnicalDetails = null);

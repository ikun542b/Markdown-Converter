using MarkdownDocumentConverter.Core.Exceptions;
using MarkdownDocumentConverter.Core.Models;
using MarkdownDocumentConverter.Core.Serialization;
using MarkdownDocumentConverter.Core.Services;

namespace MarkdownDocumentConverter.Tests.Unit;

public sealed class ErrorMapperTests
{
    [Fact]
    public void Missing_input_maps_to_friendly_message()
    {
        var error = ErrorMapper.ToUserError(new FileNotFoundException("missing"));

        Assert.Equal(ConversionErrorCode.InputNotFound, error.Code);
        Assert.Contains("无法读取 Markdown 文件", error.UserMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Missing_engine_maps_to_engine_missing()
    {
        var error = ErrorMapper.ToUserError(new ResourceNotFoundException("missing pandoc"));

        Assert.Equal(ConversionErrorCode.EngineMissing, error.Code);
    }

    [Fact]
    public void Timeout_maps_to_engine_timeout()
    {
        var exception = new ProcessExecutionException(
            "timed out",
            "pandoc.exe",
            string.Empty,
            string.Empty,
            null,
            timedOut: true,
            TimeSpan.FromSeconds(120));

        var error = ErrorMapper.ToUserError(exception);

        Assert.Equal(ConversionErrorCode.EngineTimeout, error.Code);
    }

    [Fact]
    public void Engine_failure_preserves_stderr_for_diagnostics()
    {
        var exception = new ProcessExecutionException(
            "exit 1",
            "pandoc.exe",
            string.Empty,
            "unknown input format",
            1,
            timedOut: false,
            TimeSpan.FromSeconds(1));

        var error = ErrorMapper.ToUserError(exception);

        Assert.Equal(ConversionErrorCode.EngineFailed, error.Code);
        Assert.Contains("unknown input format", error.TechnicalDetails, StringComparison.Ordinal);
    }

    [Fact]
    public void Cancellation_and_settings_errors_have_distinct_codes()
    {
        Assert.Equal(
            ConversionErrorCode.Cancelled,
            ErrorMapper.ToUserError(new OperationCanceledException()).Code);

        Assert.Equal(
            ConversionErrorCode.SettingsInvalid,
            ErrorMapper.ToUserError(new SettingsException("bad config")).Code);
    }
}

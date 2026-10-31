using MarkdownDocumentConverter.Core.Exceptions;
using MarkdownDocumentConverter.Core.Models;
using MarkdownDocumentConverter.Core.Serialization;

namespace MarkdownDocumentConverter.Core.Services;

public static class ErrorMapper
{
    public static ConversionError ToUserError(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            OperationCanceledException => new ConversionError(
                ConversionErrorCode.Cancelled,
                "转换已取消。"),

            FileNotFoundException => new ConversionError(
                ConversionErrorCode.InputNotFound,
                "无法读取 Markdown 文件。文件可能不存在、被占用或没有访问权限。",
                exception.Message),

            UnauthorizedAccessException => new ConversionError(
                ConversionErrorCode.InputAccessDenied,
                "无法访问文件。请检查文件权限或是否被其他程序占用。",
                exception.Message),

            ResourceNotFoundException => new ConversionError(
                ConversionErrorCode.EngineMissing,
                "转换引擎不可用。请检查应用安装是否完整。",
                exception.Message),

            ProcessExecutionException { TimedOut: true } processException => new ConversionError(
                ConversionErrorCode.EngineTimeout,
                "文档转换超时。请稍后重试，或检查文档是否过于复杂。",
                processException.Message),

            ProcessExecutionException processException => new ConversionError(
                ConversionErrorCode.EngineFailed,
                "文档转换失败。转换引擎返回错误，请查看详细日志。",
                BuildEngineDetails(processException)),

            SettingsException => new ConversionError(
                ConversionErrorCode.SettingsInvalid,
                "配置文件无效或无法读写。将使用默认设置。",
                exception.Message),

            ConversionException => new ConversionError(
                ConversionErrorCode.Unknown,
                "文档转换失败。请查看详细日志。",
                exception.Message),

            _ => new ConversionError(
                ConversionErrorCode.Unknown,
                "发生未知错误。请查看详细日志。",
                exception.Message),
        };
    }

    private static string BuildEngineDetails(ProcessExecutionException exception)
    {
        if (string.IsNullOrWhiteSpace(exception.StandardError))
        {
            return exception.Message;
        }

        return $"{exception.Message}{Environment.NewLine}{exception.StandardError.Trim()}";
    }
}

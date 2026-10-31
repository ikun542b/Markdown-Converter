using System.Text.Json;
using System.Text.Json.Serialization;
using MarkdownDocumentConverter.Core.Models;

namespace MarkdownDocumentConverter.Core.Serialization;

public sealed class OutputFormatJsonConverter : JsonConverter<OutputFormat>
{
    public override OutputFormat Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var value = reader.GetString();

        return value?.Trim().ToLowerInvariant() switch
        {
            "docx" or "word" => OutputFormat.Docx,
            "pdf" => OutputFormat.Pdf,
            _ => throw new JsonException($"未知输出格式：{value}"),
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        OutputFormat value,
        JsonSerializerOptions options)
    {
        var text = value switch
        {
            OutputFormat.Docx => "docx",
            OutputFormat.Pdf => "pdf",
            _ => throw new JsonException($"无法序列化组合输出格式：{value}"),
        };

        writer.WriteStringValue(text);
    }
}

public sealed class OverwritePolicyJsonConverter : JsonConverter<OverwritePolicy>
{
    public override OverwritePolicy Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var value = reader.GetString();

        return value?.Trim().ToLowerInvariant() switch
        {
            "rename" => OverwritePolicy.Rename,
            "overwrite" => OverwritePolicy.Overwrite,
            "skip" => OverwritePolicy.Skip,
            _ => throw new JsonException($"未知覆盖策略：{value}"),
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        OverwritePolicy value,
        JsonSerializerOptions options)
    {
        var text = value switch
        {
            OverwritePolicy.Rename => "rename",
            OverwritePolicy.Overwrite => "overwrite",
            OverwritePolicy.Skip => "skip",
            _ => throw new JsonException($"未知覆盖策略：{value}"),
        };

        writer.WriteStringValue(text);
    }
}

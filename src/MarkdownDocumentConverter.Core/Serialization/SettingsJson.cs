using System.Text.Json;
using System.Text.Json.Serialization;

namespace MarkdownDocumentConverter.Core.Serialization;

public static class SettingsJson
{
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
        };

        options.Converters.Add(new OutputFormatJsonConverter());
        options.Converters.Add(new OverwritePolicyJsonConverter());
        options.Converters.Add(new JsonStringEnumConverter());

        return options;
    }
}

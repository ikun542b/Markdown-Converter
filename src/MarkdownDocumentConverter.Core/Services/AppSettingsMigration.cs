using MarkdownDocumentConverter.Core.Models;
using MarkdownDocumentConverter.Core.Serialization;

namespace MarkdownDocumentConverter.Core.Services;

public static class AppSettingsMigration
{
    private static readonly string[] ValidThemes = { "system", "light", "dark" };
    private static readonly string[] ValidOutputModes = { "same_directory", "fixed" };

    public static AppSettings Normalize(AppSettings? settings)
    {
        var value = settings ?? new AppSettings();

        if (value.ConfigVersion > AppSettings.CurrentConfigVersion)
        {
            throw new SettingsException(
                $"配置文件版本 {value.ConfigVersion} 高于当前支持的版本 {AppSettings.CurrentConfigVersion}。");
        }

        if (value.ConfigVersion < AppSettings.CurrentConfigVersion)
        {
            value.ConfigVersion = AppSettings.CurrentConfigVersion;
        }

        value.Theme = NormalizeChoice(value.Theme, ValidThemes, "system");
        value.DefaultOutputMode = NormalizeChoice(value.DefaultOutputMode, ValidOutputModes, "same_directory");
        value.Style = string.IsNullOrWhiteSpace(value.Style) ? "modern" : value.Style.Trim();
        value.FontFamily = string.IsNullOrWhiteSpace(value.FontFamily) ? null : value.FontFamily.Trim();
        value.FontScale = Math.Clamp(value.FontScale, 0.5, 2.0);
        value.ParallelJobs = Math.Clamp(value.ParallelJobs, 1, 8);
        value.LogRetentionCount = Math.Clamp(value.LogRetentionCount, 1, 100);
        value.ConversionTimeoutSeconds = Math.Clamp(value.ConversionTimeoutSeconds, 10, 1800);

        if (!Enum.IsDefined(value.PageSize))
        {
            value.PageSize = PageSize.A4;
        }

        if (!Enum.IsDefined(value.OverwritePolicy))
        {
            value.OverwritePolicy = OverwritePolicy.Rename;
        }

        value.DefaultFormats = NormalizeFormats(value.DefaultFormats);

        return value;
    }

    private static List<OutputFormat> NormalizeFormats(List<OutputFormat>? formats)
    {
        var normalized = formats?
            .Where(format => format is OutputFormat.Docx or OutputFormat.Pdf)
            .Distinct()
            .ToList() ?? new List<OutputFormat>();

        if (normalized.Count == 0)
        {
            normalized.Add(OutputFormat.Docx);
            normalized.Add(OutputFormat.Pdf);
        }

        return normalized;
    }

    private static string NormalizeChoice(string? value, string[] validValues, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var normalized = value.Trim().ToLowerInvariant();
        return validValues.Contains(normalized, StringComparer.Ordinal) ? normalized : fallback;
    }
}

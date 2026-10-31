using System.Text.Json.Serialization;

namespace MarkdownDocumentConverter.Core.Models;

public sealed class AppSettings
{
    public const int CurrentConfigVersion = 1;

    [JsonPropertyName("config_version")]
    public int ConfigVersion { get; set; } = CurrentConfigVersion;

    [JsonPropertyName("theme")]
    public string Theme { get; set; } = "system";

    [JsonPropertyName("default_formats")]
    public List<OutputFormat> DefaultFormats { get; set; } =
        new() { OutputFormat.Docx, OutputFormat.Pdf };

    [JsonPropertyName("default_output_mode")]
    public string DefaultOutputMode { get; set; } = "same_directory";

    [JsonPropertyName("default_output_directory")]
    public string? DefaultOutputDirectory { get; set; }

    [JsonPropertyName("page_size")]
    public PageSize PageSize { get; set; } = PageSize.A4;

    [JsonPropertyName("style")]
    public string Style { get; set; } = "modern";

    [JsonPropertyName("font_family")]
    public string? FontFamily { get; set; }

    [JsonPropertyName("font_scale")]
    public double FontScale { get; set; } = 1.0;

    [JsonPropertyName("recursive")]
    public bool Recursive { get; set; } = true;

    [JsonPropertyName("overwrite_policy")]
    public OverwritePolicy OverwritePolicy { get; set; } = OverwritePolicy.Rename;

    [JsonPropertyName("open_output_folder_after_complete")]
    public bool OpenOutputFolderAfterComplete { get; set; } = true;

    [JsonPropertyName("parallel_jobs")]
    public int ParallelJobs { get; set; } = 1;

    [JsonPropertyName("retain_intermediate_files")]
    public bool RetainIntermediateFiles { get; set; }

    [JsonPropertyName("conversion_timeout_seconds")]
    public int ConversionTimeoutSeconds { get; set; } = 120;

    [JsonPropertyName("log_retention_count")]
    public int LogRetentionCount { get; set; } = 10;

    [JsonPropertyName("pandoc_path")]
    public string? PandocPath { get; set; }

    [JsonPropertyName("typst_path")]
    public string? TypstPath { get; set; }
}

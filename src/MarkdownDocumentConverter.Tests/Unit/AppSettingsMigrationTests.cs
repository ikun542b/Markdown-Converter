using MarkdownDocumentConverter.Core.Models;
using MarkdownDocumentConverter.Core.Services;

namespace MarkdownDocumentConverter.Tests.Unit;

public sealed class AppSettingsMigrationTests
{
    [Fact]
    public void Invalid_values_are_clamped_or_replaced_with_defaults()
    {
        var settings = new AppSettings
        {
            ConfigVersion = 0,
            Theme = "neon",
            DefaultOutputMode = "unknown",
            DefaultFormats = new List<OutputFormat>(),
            PageSize = (PageSize)99,
            Style = "  ",
            FontScale = 9,
            ParallelJobs = 0,
            LogRetentionCount = 0,
            ConversionTimeoutSeconds = 1,
            OverwritePolicy = (OverwritePolicy)99,
        };

        var normalized = AppSettingsMigration.Normalize(settings);

        Assert.Equal(AppSettings.CurrentConfigVersion, normalized.ConfigVersion);
        Assert.Equal("system", normalized.Theme);
        Assert.Equal("same_directory", normalized.DefaultOutputMode);
        Assert.Equal(new[] { OutputFormat.Docx, OutputFormat.Pdf }, normalized.DefaultFormats);
        Assert.Equal(PageSize.A4, normalized.PageSize);
        Assert.Equal("modern", normalized.Style);
        Assert.Equal(2.0, normalized.FontScale);
        Assert.Equal(1, normalized.ParallelJobs);
        Assert.Equal(1, normalized.LogRetentionCount);
        Assert.Equal(10, normalized.ConversionTimeoutSeconds);
        Assert.Equal(OverwritePolicy.Rename, normalized.OverwritePolicy);
    }

    [Fact]
    public void Unknown_output_formats_are_removed()
    {
        var settings = new AppSettings
        {
            DefaultFormats = new List<OutputFormat>
            {
                OutputFormat.Docx,
                OutputFormat.None,
                OutputFormat.All,
                OutputFormat.Docx,
            },
        };

        var normalized = AppSettingsMigration.Normalize(settings);

        Assert.Equal(new[] { OutputFormat.Docx }, normalized.DefaultFormats);
    }
}

using MarkdownDocumentConverter.Core.Infrastructure;
using MarkdownDocumentConverter.Core.Models;
using MarkdownDocumentConverter.Core.Serialization;
using MarkdownDocumentConverter.Core.Services;
using MarkdownDocumentConverter.Tests.Infrastructure;

namespace MarkdownDocumentConverter.Tests.Integration;

public sealed class SettingsServiceTests
{
    [Fact]
    public async Task Missing_file_returns_normalized_defaults()
    {
        using var directory = new TestOutputDirectory();
        var store = new JsonSettingsStore(directory.File("settings.json"));
        var service = new SettingsService(store);

        var settings = await service.LoadAsync();

        Assert.Equal(AppSettings.CurrentConfigVersion, settings.ConfigVersion);
        Assert.Equal(PageSize.A4, settings.PageSize);
        Assert.Equal(new[] { OutputFormat.Docx, OutputFormat.Pdf }, settings.DefaultFormats);
    }

    [Fact]
    public async Task Round_trip_preserves_values_and_uses_readable_json()
    {
        using var directory = new TestOutputDirectory();
        var store = new JsonSettingsStore(directory.File("settings.json"));
        var service = new SettingsService(store);

        var original = new AppSettings
        {
            Theme = "dark",
            DefaultFormats = new List<OutputFormat> { OutputFormat.Pdf },
            PageSize = PageSize.A5,
            Style = "academic",
            FontScale = 1.2,
            OverwritePolicy = OverwritePolicy.Skip,
            ParallelJobs = 2,
        };

        await service.SaveAsync(original);
        var loaded = await service.LoadAsync();

        Assert.Equal("dark", loaded.Theme);
        Assert.Equal(new[] { OutputFormat.Pdf }, loaded.DefaultFormats);
        Assert.Equal(PageSize.A5, loaded.PageSize);
        Assert.Equal("academic", loaded.Style);
        Assert.Equal(1.2, loaded.FontScale);
        Assert.Equal(OverwritePolicy.Skip, loaded.OverwritePolicy);
        Assert.Equal(2, loaded.ParallelJobs);

        var json = await File.ReadAllTextAsync(store.SettingsFilePath);
        Assert.Contains("\"config_version\": 1", json, StringComparison.Ordinal);
        Assert.Contains("\"pdf\"", json, StringComparison.Ordinal);
        Assert.Contains("\"skip\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invalid_json_is_reported_as_settings_exception()
    {
        using var directory = new TestOutputDirectory();
        var store = new JsonSettingsStore(directory.File("settings.json"));
        var service = new SettingsService(store);
        await File.WriteAllTextAsync(store.SettingsFilePath, "{ not valid json");

        await Assert.ThrowsAsync<SettingsException>(() => service.LoadAsync());
    }

    [Fact]
    public async Task Future_config_version_is_rejected()
    {
        using var directory = new TestOutputDirectory();
        var store = new JsonSettingsStore(directory.File("settings.json"));
        var service = new SettingsService(store);
        await File.WriteAllTextAsync(store.SettingsFilePath, "{\"config_version\": 99}");

        await Assert.ThrowsAsync<SettingsException>(() => service.LoadAsync());
    }
}

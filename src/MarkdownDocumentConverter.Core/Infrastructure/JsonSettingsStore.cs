using System.Text.Json;
using MarkdownDocumentConverter.Core.Abstractions;
using MarkdownDocumentConverter.Core.Models;
using MarkdownDocumentConverter.Core.Serialization;

namespace MarkdownDocumentConverter.Core.Infrastructure;

public sealed class JsonSettingsStore : ISettingsStore
{
    public JsonSettingsStore(string? settingsFilePath = null)
    {
        SettingsFilePath = Path.GetFullPath(
            settingsFilePath ?? Path.Combine(GetDefaultDirectory(), "settings.json"));
    }

    public string SettingsFilePath { get; }

    public async Task<AppSettings?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(SettingsFilePath))
        {
            return null;
        }

        try
        {
            await using var stream = new FileStream(
                SettingsFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            return await JsonSerializer.DeserializeAsync<AppSettings>(
                stream,
                SettingsJson.Options,
                cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            throw new SettingsException($"配置文件格式无效：{SettingsFilePath}", ex);
        }
        catch (IOException ex)
        {
            throw new SettingsException($"无法读取配置文件：{SettingsFilePath}", ex);
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var directory = Path.GetDirectoryName(SettingsFilePath)
            ?? throw new SettingsException($"配置文件路径无效：{SettingsFilePath}");

        Directory.CreateDirectory(directory);
        var temporaryPath = SettingsFilePath + ".tmp";

        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.Asynchronous))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    settings,
                    SettingsJson.Options,
                    cancellationToken).ConfigureAwait(false);

                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            File.Move(temporaryPath, SettingsFilePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            TryDelete(temporaryPath);
            throw new SettingsException($"无法保存配置文件：{SettingsFilePath}", ex);
        }
        catch (Exception)
        {
            TryDelete(temporaryPath);
            throw;
        }
    }

    private static string GetDefaultDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MarkdownDocumentConverter");
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // 忽略清理失败。
        }
        catch (UnauthorizedAccessException)
        {
            // 忽略清理失败。
        }
    }
}

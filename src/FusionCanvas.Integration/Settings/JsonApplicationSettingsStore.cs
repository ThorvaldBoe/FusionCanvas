using System.Text.Json;
using FusionCanvas.Application.Settings;

namespace FusionCanvas.Integration.Settings;

public sealed class JsonApplicationSettingsStore : IApplicationSettingsStore
{
    private const int SupportedVersion = 1;

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public JsonApplicationSettingsStore(string settingsPath)
    {
        if (string.IsNullOrWhiteSpace(settingsPath))
        {
            throw new ArgumentException("The application settings path must not be empty.", nameof(settingsPath));
        }

        SettingsPath = Path.GetFullPath(settingsPath);
    }

    public string SettingsPath { get; }

    public async Task<ApplicationSettingsLoadResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(SettingsPath))
        {
            return ApplicationSettingsLoadResult.Defaulted();
        }

        SettingsDocument document;
        try
        {
            await using var stream = new FileStream(
                SettingsPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);

            document = await JsonSerializer.DeserializeAsync<SettingsDocument>(stream, ReadOptions, cancellationToken)
                ?? throw new JsonException("The application settings document deserialized to null.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (JsonException)
        {
            return ApplicationSettingsLoadResult.Defaulted("The saved appearance preference is invalid and could not be read.");
        }
        catch (IOException)
        {
            return ApplicationSettingsLoadResult.Defaulted("The saved appearance preference could not be read.");
        }
        catch (UnauthorizedAccessException)
        {
            return ApplicationSettingsLoadResult.Defaulted("The saved appearance preference could not be read.");
        }

        if (document.Version != SupportedVersion)
        {
            return ApplicationSettingsLoadResult.Defaulted("The saved appearance preference uses an unsupported version.");
        }

        return ApplicationSettingsLoadResult.Success(new ApplicationSettings(document.DarkMode));
    }

    public async Task<ApplicationSettingsSaveResult> SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var directory = Path.GetDirectoryName(SettingsPath);
        var tempPath = SettingsPath + ".tmp";

        try
        {
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    new SettingsDocument { Version = SupportedVersion, DarkMode = settings.DarkMode },
                    WriteOptions,
                    cancellationToken);
            }

            File.Move(tempPath, SettingsPath, overwrite: true);
            return ApplicationSettingsSaveResult.Success;
        }
        catch (OperationCanceledException)
        {
            TryDelete(tempPath);
            throw;
        }
        catch (IOException)
        {
            TryDelete(tempPath);
            return ApplicationSettingsSaveResult.Failed("The appearance preference could not be saved and may not survive restart.");
        }
        catch (UnauthorizedAccessException)
        {
            TryDelete(tempPath);
            return ApplicationSettingsSaveResult.Failed("The appearance preference could not be saved and may not survive restart.");
        }
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch { }
    }

    private sealed class SettingsDocument
    {
        public int Version { get; set; }
        public bool DarkMode { get; set; }
    }
}

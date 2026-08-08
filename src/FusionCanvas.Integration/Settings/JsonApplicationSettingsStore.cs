using System.Text.Json;
using FusionCanvas.Application.AI;
using FusionCanvas.Application.Settings;

namespace FusionCanvas.Integration.Settings;

public sealed class JsonApplicationSettingsStore : IApplicationSettingsStore
{
    private const int SupportedVersion = 3;

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

        JsonDocument json;
        try
        {
            await using var stream = new FileStream(
                SettingsPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);

            json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken)
                ?? throw new JsonException("The application settings document deserialized to null.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (JsonException)
        {
            return ApplicationSettingsLoadResult.Defaulted("The saved application settings are invalid and could not be read.");
        }
        catch (IOException)
        {
            return ApplicationSettingsLoadResult.Defaulted("The saved application settings could not be read.");
        }
        catch (UnauthorizedAccessException)
        {
            return ApplicationSettingsLoadResult.Defaulted("The saved application settings could not be read.");
        }

        using (json)
        {
            var root = json.RootElement;
            if (!TryGetInt32(root, "version", out var version) || version is < 1 or > SupportedVersion)
            {
                return ApplicationSettingsLoadResult.Defaulted("The saved application settings use an unsupported version.");
            }

            if (!TryGetBoolean(root, "darkMode", out var darkMode))
            {
                return ApplicationSettingsLoadResult.Defaulted(
                    "The saved application settings contain an invalid appearance preference.");
            }
            if (version == 1)
            {
                return ApplicationSettingsLoadResult.Success(new ApplicationSettings(darkMode));
            }

            var activeWorkspaceId = TryReadGuid(root, "activeWorkspaceId");

            if (!TryGetProperty(root, "ai", out var aiElement))
            {
                var noAiLayout = TryReadWindowLayout(root, out var noAiLayoutWarning);
                return new ApplicationSettingsLoadResult(
                    new ApplicationSettings(darkMode, AiConfigurationSettings.Default, noAiLayout, activeWorkspaceId),
                    UsedDefault: false,
                    noAiLayoutWarning);
            }

            AiConfigurationSettings aiSettings;
            string? warning = null;
            try
            {
                var ai = aiElement.Deserialize<AiConfigurationSettings>(ReadOptions);
                aiSettings = ai is null ? AiConfigurationSettings.Default : Normalize(ai);
                warning = ai is null ? "The saved AI settings were invalid and were reset." : null;
            }
            catch (JsonException)
            {
                aiSettings = AiConfigurationSettings.Default;
                warning = "The saved AI settings were invalid and were reset.";
            }

            var layout = TryReadWindowLayout(root, out var layoutWarning);
            warning = CombineWarnings(warning, layoutWarning);
            return new ApplicationSettingsLoadResult(
                new ApplicationSettings(darkMode, aiSettings, layout, activeWorkspaceId),
                UsedDefault: false,
                warning);
        }
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
                    new SettingsDocument
                    {
                        Version = SupportedVersion,
                        DarkMode = settings.DarkMode,
                        Ai = settings.Ai,
                        WindowLayout = settings.WindowLayout,
                        ActiveWorkspaceId = settings.ActiveWorkspaceId
                    },
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
            return ApplicationSettingsSaveResult.Failed("The application settings could not be saved and may not survive restart.");
        }
        catch (UnauthorizedAccessException)
        {
            TryDelete(tempPath);
            return ApplicationSettingsSaveResult.Failed("The application settings could not be saved and may not survive restart.");
        }
    }

    private static AiConfigurationSettings Normalize(AiConfigurationSettings settings) =>
        settings with
        {
            General = Normalize(settings.General),
            Ideation = settings.Ideation is null
                ? AiPurposeProfileSettings.InheritGeneral
                : Normalize(settings.Ideation),
            Concept = settings.Concept is null
                ? AiPurposeProfileSettings.InheritGeneral
                : Normalize(settings.Concept),
            Sll = settings.Sll is null
                ? AiPurposeProfileSettings.InheritGeneral
                : Normalize(settings.Sll)
        };

    private static AiPurposeProfileSettings Normalize(AiPurposeProfileSettings settings) =>
        settings with
        {
            CustomProfile = Normalize(settings.CustomProfile)
        };

    private static AiProfileSettings Normalize(AiProfileSettings? settings) =>
        settings is null
            ? AiProfileSettings.Empty
            : settings with
            {
                StopSequences = settings.StopSequences ?? [],
                Reasoning = settings.Reasoning is null ||
                    !Enum.IsDefined(settings.Reasoning.Mode)
                    ? AiReasoningSettings.ProviderDefault
                    : settings.Reasoning
        };

    private static WindowLayoutSettings? TryReadWindowLayout(JsonElement root, out string? warning)
    {
        warning = null;
        if (!TryGetProperty(root, "windowLayout", out var element))
        {
            return null;
        }

        if (element.ValueKind != JsonValueKind.Object ||
            !TryGetInt32(element, "positionX", out var positionX) ||
            !TryGetInt32(element, "positionY", out var positionY) ||
            !TryGetFinitePositiveDouble(element, "width", out var width) ||
            !TryGetFinitePositiveDouble(element, "height", out var height) ||
            !TryGetFinitePositiveDouble(element, "navigationWidth", out var navigationWidth))
        {
            warning = "The saved window layout was invalid and was reset.";
            return null;
        }

        return new WindowLayoutSettings(positionX, positionY, width, height, navigationWidth);
    }

    private static bool TryGetFinitePositiveDouble(JsonElement element, string name, out double value)
    {
        value = default;
        return TryGetProperty(element, name, out var property) &&
               property.ValueKind == JsonValueKind.Number &&
               property.TryGetDouble(out value) &&
               double.IsFinite(value) &&
               value > 0;
    }

    private static string? CombineWarnings(string? first, string? second) =>
        first is null ? second : second is null ? first : $"{first} {second}";

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool TryGetInt32(JsonElement element, string name, out int value)
    {
        value = default;
        return TryGetProperty(element, name, out var property) && property.TryGetInt32(out value);
    }

    private static bool TryGetBoolean(JsonElement element, string name, out bool value)
    {
        value = default;
        if (!TryGetProperty(element, name, out var property) ||
            property.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            return false;
        }

        value = property.GetBoolean();
        return true;
    }

    private static Guid? TryReadGuid(JsonElement element, string name)
    {
        return TryGetProperty(element, name, out var property) &&
               property.ValueKind == JsonValueKind.String &&
               property.TryGetGuid(out var value)
            ? value
            : null;
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
        public AiConfigurationSettings Ai { get; set; } = AiConfigurationSettings.Default;
        public WindowLayoutSettings? WindowLayout { get; set; }
        public Guid? ActiveWorkspaceId { get; set; }
    }
}

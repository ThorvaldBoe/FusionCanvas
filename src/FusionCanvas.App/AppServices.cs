using FusionCanvas.App.Settings;
using FusionCanvas.Application.AI;
using FusionCanvas.Application.Items;
using ImportItemCsvCodec = FusionCanvas.Application.Items.Import.IItemCsvCodec;
using FusionCanvas.Application.Settings;

namespace FusionCanvas.App;

public sealed class AppServices : IDisposable
{
    private readonly HttpClient _httpClient;
    private bool _disposed;

    public AppServices(
        HttpClient httpClient,
        IApplicationSettingsStore settingsStore,
        SettingsViewModel settings,
        IAiTextGenerationService aiTextGeneration,
        IItemCsvCodec itemCsvExportCodec,
        ImportItemCsvCodec itemCsvImportCodec)
    {
        _httpClient = httpClient;
        SettingsStore = settingsStore;
        Settings = settings;
        AiTextGeneration = aiTextGeneration;
        ItemCsvExportCodec = itemCsvExportCodec;
        ItemCsvImportCodec = itemCsvImportCodec;
    }

    public IApplicationSettingsStore SettingsStore { get; }
    public SettingsViewModel Settings { get; }
    public IAiTextGenerationService AiTextGeneration { get; }
    public IItemCsvCodec ItemCsvExportCodec { get; }
    public ImportItemCsvCodec ItemCsvImportCodec { get; }

    public async Task FlushAsync()
    {
        if (!_disposed)
        {
            await Settings.FlushAsync().ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _httpClient.Dispose();
    }
}

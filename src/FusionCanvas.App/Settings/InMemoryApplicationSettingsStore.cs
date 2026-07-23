using FusionCanvas.Application.Settings;

namespace FusionCanvas.App.Settings;

public sealed class InMemoryApplicationSettingsStore : IApplicationSettingsStore
{
    private ApplicationSettings _current = ApplicationSettings.Default;

    public ApplicationSettings Current => _current;

    public Task<ApplicationSettingsLoadResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(ApplicationSettingsLoadResult.Success(_current));
    }

    public Task<ApplicationSettingsSaveResult> SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _current = settings;
        return Task.FromResult(ApplicationSettingsSaveResult.Success);
    }
}

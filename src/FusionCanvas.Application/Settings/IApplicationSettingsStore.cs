
namespace FusionCanvas.Application.Settings;

public interface IApplicationSettingsStore
{
    Task<ApplicationSettingsLoadResult> LoadAsync(CancellationToken cancellationToken = default);

    Task<ApplicationSettingsSaveResult> SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken = default);
}

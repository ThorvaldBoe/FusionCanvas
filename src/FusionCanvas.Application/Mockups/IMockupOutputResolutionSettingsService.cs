using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.Application.Mockups;

public interface IMockupOutputResolutionSettingsService
{
    Task<MockupOutputResolutionSettingsState> LoadAsync(Guid storeId, CancellationToken cancellationToken = default);

    Task<MockupOutputResolutionSettingsResult> SaveAsync(Guid storeId, int maximumLongEdgePixels, CancellationToken cancellationToken = default);
}

using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.Application.Mockups;

public sealed record MockupOutputResolutionSettingsState(
    Guid StoreId,
    bool IsReadOnly,
    MockupOutputResolutionPolicy Policy,
    string? Error = null);

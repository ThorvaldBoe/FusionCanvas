using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.Application.Mockups;

public sealed record MockupOutputResolutionSettingsResult(
    bool Succeeded,
    string? Error,
    MockupOutputResolutionSettingsState State)
{
    public static MockupOutputResolutionSettingsResult Success(MockupOutputResolutionSettingsState state) =>
        new(true, null, state);

    public static MockupOutputResolutionSettingsResult Failure(string error, MockupOutputResolutionSettingsState state) =>
        new(false, error, state);
}

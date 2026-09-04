namespace FusionCanvas.Application.AI;

public sealed record AiPurposeProfileSettings(
    bool UseGeneral,
    bool HasCustomProfile,
    AiProfileSettings CustomProfile)
{
    public static AiPurposeProfileSettings InheritGeneral { get; } =
        new(UseGeneral: true, HasCustomProfile: false, AiProfileSettings.Empty);
}

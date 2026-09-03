namespace FusionCanvas.Application.AI;

public sealed record AiConfigurationSettings(
    bool RequireZeroDataRetention,
    bool AdvancedMode,
    AiProfileSettings General,
    AiPurposeProfileSettings Ideation,
    AiPurposeProfileSettings Concept,
    AiPurposeProfileSettings Sll)
{
    public static AiConfigurationSettings Default { get; } = new(
        RequireZeroDataRetention: true,
        AdvancedMode: false,
        General: AiProfileSettings.Empty,
        Ideation: AiPurposeProfileSettings.InheritGeneral,
        Concept: AiPurposeProfileSettings.InheritGeneral,
        Sll: AiPurposeProfileSettings.InheritGeneral);
}

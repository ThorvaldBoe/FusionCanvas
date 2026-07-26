
namespace FusionCanvas.Application.Settings;

public sealed record ApplicationSettingsSaveResult(bool Saved, string? Warning)
{
    public static ApplicationSettingsSaveResult Success { get; } = new(Saved: true, Warning: null);

    public static ApplicationSettingsSaveResult Failed(string warning)
        => new(Saved: false, warning);
}

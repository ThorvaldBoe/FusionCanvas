using FusionCanvas.Domain.Snowclones;

namespace FusionCanvas.Application.Snowclones;

public sealed record SnowcloneLibrarySnapshot(
    IReadOnlyList<Snowclone> Snowclones,
    bool StarterLibraryInitialized)
{
    public static SnowcloneLibrarySnapshot Empty { get; } = new([], false);
}

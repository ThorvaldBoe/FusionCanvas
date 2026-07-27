namespace FusionCanvas.Application.Snowclones;

public sealed record SnowcloneLibraryState(
    IReadOnlyList<SnowcloneSummary> AllSnowclones,
    IReadOnlyList<SnowcloneSummary> VisibleSnowclones,
    bool StarterLibraryInitialized,
    string SearchText)
{
    public static SnowcloneLibraryState Empty { get; } = new([], [], false, string.Empty);
}

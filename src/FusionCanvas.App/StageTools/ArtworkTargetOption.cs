using FusionCanvas.Domain.Catalog;

namespace FusionCanvas.App.StageTools;

public sealed record ArtworkTargetOption(Guid Id, string Name, string Details, bool IsPrimary, bool RecommendsTransparency, int Width, int Height)
{
    public override string ToString() => IsPrimary ? $"{Name} (Primary)" : Name;
}

using FusionCanvas.Domain.Catalog;

namespace FusionCanvas.App.StageTools;

public sealed record ArtworkTargetOption(Guid Id, string Name, string Details, bool IsPrimary, bool RecommendsTransparency)
{
    public override string ToString() => IsPrimary ? $"{Name} (Primary)" : Name;
}

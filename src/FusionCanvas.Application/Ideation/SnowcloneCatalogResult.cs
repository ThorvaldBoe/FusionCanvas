using FusionCanvas.Domain.Ideation;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Application.AI;

namespace FusionCanvas.Application.Ideation;

public sealed record SnowcloneCatalogResult(
    bool Succeeded,
    IReadOnlyList<IdeationSnowcloneSelection> Selections,
    string? Error)
{
    public static SnowcloneCatalogResult Success(IReadOnlyList<IdeationSnowcloneSelection> selections) =>
        new(true, selections, null);

    public static SnowcloneCatalogResult Failure(string error) =>
        new(false, [], error);
}

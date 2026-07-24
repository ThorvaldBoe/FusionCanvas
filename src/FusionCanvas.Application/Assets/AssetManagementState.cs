using FusionCanvas.Domain.Assets;

namespace FusionCanvas.Application.Assets;

public sealed record AssetManagementState(
    AssetContextDescriptor? Context,
    IReadOnlyList<AssetSummary> Assets,
    IReadOnlyList<AssetKind> AvailablePurposes,
    string? Error)
{
    public static AssetManagementState Empty { get; } = new(null, [], AssetPurposePolicy.AvailablePurposes, null);
}

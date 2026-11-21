using FusionCanvas.Domain.ArtworkStyles;

namespace FusionCanvas.Application.ArtworkStyles;

public sealed record ArtworkStyleManagementState(
    Guid WorkspaceId,
    IReadOnlyList<ArtworkStyle> Styles,
    IReadOnlyDictionary<Guid, IReadOnlyList<ArtworkStyleReferenceState>> References)
{
    public IReadOnlyDictionary<Guid, int> AssignedItemCounts { get; init; } = new Dictionary<Guid, int>();
}

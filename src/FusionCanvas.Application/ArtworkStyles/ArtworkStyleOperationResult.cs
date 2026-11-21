namespace FusionCanvas.Application.ArtworkStyles;

public sealed record ArtworkStyleOperationResult(bool Succeeded, string? Error, ArtworkStyleManagementState? State, Guid? SavedStyleId = null, int AffectedItemCount = 0)
{
    public static ArtworkStyleOperationResult Failure(string error, ArtworkStyleManagementState? state = null) => new(false, error, state);

    public static ArtworkStyleOperationResult Success(ArtworkStyleManagementState state, Guid? savedStyleId = null, int affectedItemCount = 0) => new(true, null, state, savedStyleId, affectedItemCount);
}

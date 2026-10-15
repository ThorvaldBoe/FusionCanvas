namespace FusionCanvas.Application.DesignFiles;

public sealed record ArtworkInspectionResult(
    bool Succeeded,
    string? Error,
    Guid AssetId,
    string? AssetName,
    byte[]? Png,
    ArtworkImageInspection? Inspection)
{
    public static ArtworkInspectionResult Success(Guid assetId, string assetName, byte[] png, ArtworkImageInspection inspection) =>
        new(true, null, assetId, assetName, png, inspection);

    public static ArtworkInspectionResult Failure(string error) =>
        new(false, string.IsNullOrWhiteSpace(error) ? "The artwork could not be inspected." : error, Guid.Empty, null, null, null);
}

namespace FusionCanvas.Application.DesignFiles;

public sealed record GlobalColorRemovalAvailabilityResult(
    bool Available,
    string? Reason)
{
    public static GlobalColorRemovalAvailabilityResult Success() => new(true, null);

    public static GlobalColorRemovalAvailabilityResult Failure(string reason) =>
        new(false, string.IsNullOrWhiteSpace(reason)
            ? "Global color removal is unavailable for this image."
            : reason);
}

public sealed record GlobalColorRemovalSourceResult(
    bool Succeeded,
    string? Error,
    Guid AssetId,
    string? SourceName,
    byte[]? Content)
{
    public static GlobalColorRemovalSourceResult Success(Guid assetId, string sourceName, byte[] content) =>
        new(true, null, assetId, sourceName, content);

    public static GlobalColorRemovalSourceResult Failure(string error) =>
        new(false, string.IsNullOrWhiteSpace(error) ? "The image could not be opened." : error, Guid.Empty, null, null);
}

public sealed record GlobalColorRemovalPreviewResult(
    bool Succeeded,
    string? Error,
    GlobalColorRemovalRasterPreview? Preview)
{
    public static GlobalColorRemovalPreviewResult Success(GlobalColorRemovalRasterPreview preview) =>
        new(true, null, preview);

    public static GlobalColorRemovalPreviewResult Failure(string error) =>
        new(false, string.IsNullOrWhiteSpace(error) ? "The color-removal preview failed." : error, null);
}

public sealed record GlobalColorRemovalSampleResult(
    bool Succeeded,
    string? Error,
    GlobalColorRemovalColor? Color)
{
    public static GlobalColorRemovalSampleResult Success(GlobalColorRemovalColor color) =>
        new(true, null, color);

    public static GlobalColorRemovalSampleResult Failure(string error) =>
        new(false, string.IsNullOrWhiteSpace(error) ? "The image color could not be read." : error, null);
}

public sealed record GlobalColorRemovalDerivedAsset(
    Guid AssetId,
    string Name,
    string WorkspaceRelativePath);

public sealed record GlobalColorRemovalApplyResult(
    bool Succeeded,
    string? Error,
    GlobalColorRemovalDerivedAsset? Asset,
    int MatchedPixelCount)
{
    public static GlobalColorRemovalApplyResult Success(GlobalColorRemovalDerivedAsset asset, int matchedPixelCount) =>
        new(true, null, asset, matchedPixelCount);

    public static GlobalColorRemovalApplyResult Failure(string error) =>
        new(false, string.IsNullOrWhiteSpace(error) ? "The color-removal operation failed." : error, null, 0);
}

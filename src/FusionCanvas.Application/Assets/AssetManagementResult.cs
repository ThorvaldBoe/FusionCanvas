namespace FusionCanvas.Application.Assets;

public sealed record AssetManagementResult(
    bool Succeeded,
    string? Error,
    AssetSummary? Asset,
    AssetManagementState State)
{
    public static AssetManagementResult Success(AssetSummary? asset, AssetManagementState state) =>
        new(true, null, asset, state);

    public static AssetManagementResult Failure(string error, AssetManagementState state) =>
        new(false, string.IsNullOrWhiteSpace(error) ? "Asset operation failed." : error, null, state);
}

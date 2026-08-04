namespace FusionCanvas.Application.Products;

public sealed record DesignTargetSelectionResult
(
    bool Succeeded,
    string? Error,
    DesignTargetSelectionState State)
{
    public static DesignTargetSelectionResult Success(DesignTargetSelectionState state) =>
        new(true, null, state);

    public static DesignTargetSelectionResult Failure(string error, DesignTargetSelectionState state) =>
        new(false, string.IsNullOrWhiteSpace(error) ? "Design target selection failed." : error, state);
}

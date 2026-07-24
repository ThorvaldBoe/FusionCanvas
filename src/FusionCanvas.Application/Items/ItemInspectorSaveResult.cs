namespace FusionCanvas.Application.Items;

public sealed record ItemInspectorSaveResult(
    bool Succeeded,
    string? Error,
    ItemInspectorState? State)
{
    public static ItemInspectorSaveResult Success(ItemInspectorState state) =>
        new(true, null, state);

    public static ItemInspectorSaveResult Failure(string error) =>
        new(false, string.IsNullOrWhiteSpace(error) ? "Inspector save failed." : error, null);
}

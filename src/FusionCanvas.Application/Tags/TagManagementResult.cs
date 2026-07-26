namespace FusionCanvas.Application.Tags;

public sealed record TagManagementResult(
    bool Succeeded,
    string? Error,
    TagSummary? Tag,
    int AffectedItemCount,
    TagManagementState State)
{
    public static TagManagementResult Success(TagSummary? tag, TagManagementState state, int affectedListingCount = 0) =>
        new(true, null, tag, affectedListingCount, state);

    public static TagManagementResult Failure(string error, TagManagementState state) =>
        new(false, string.IsNullOrWhiteSpace(error) ? "Tag operation failed." : error, null, 0, state);
}

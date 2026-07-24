namespace FusionCanvas.Application.Tags;

public sealed record TagApplicationResult(
    bool Succeeded,
    string? Error,
    TagSummary? Tag,
    bool CreatedNewTag,
    TagManagementState State)
{
    public static TagApplicationResult Applied(TagSummary tag, bool createdNewTag, TagManagementState state) =>
        new(true, null, tag, createdNewTag, state);

    public static TagApplicationResult NeedsRestoreConfirmation(TagSummary archivedTag, TagManagementState state) =>
        new(false, "An archived tag with this name already exists. Restore it first?", archivedTag, false, state);

    public static TagApplicationResult Failure(string error, TagManagementState state) =>
        new(false, string.IsNullOrWhiteSpace(error) ? "Tag operation failed." : error, null, false, state);
}

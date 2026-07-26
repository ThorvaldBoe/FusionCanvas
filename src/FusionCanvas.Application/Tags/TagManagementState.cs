namespace FusionCanvas.Application.Tags;

public sealed record TagManagementState(
    Guid? ActiveStoreId,
    IReadOnlyList<TagSummary> ActiveTags,
    IReadOnlyList<TagSummary> ArchivedTags,
    bool NeedsFirstTag);

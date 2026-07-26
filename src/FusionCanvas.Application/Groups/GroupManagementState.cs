namespace FusionCanvas.Application.Groups;

public sealed record GroupManagementState(
    Guid? ActiveStoreId,
    Guid? ActiveNicheId,
    Guid? ActiveGroupId,
    GroupSummary? ActiveGroup,
    IReadOnlyList<GroupSummary> ActiveGroups,
    IReadOnlyList<GroupSummary> ArchivedGroupRoots,
    IReadOnlyList<GroupSummary> ArchivedGroups,
    IReadOnlyList<GroupDestination> ValidDestinations,
    bool NeedsFirstGroup);

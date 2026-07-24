namespace FusionCanvas.Application.Groups;

public sealed record GroupManagementCreateRequest(GroupParentReference Parent, string Name, GroupContext? Context = null);

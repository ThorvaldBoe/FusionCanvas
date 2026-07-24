namespace FusionCanvas.Application.Groups;

public sealed record GroupManagementUpdateRequest(Guid GroupId, string Name, GroupContext? Context = null);

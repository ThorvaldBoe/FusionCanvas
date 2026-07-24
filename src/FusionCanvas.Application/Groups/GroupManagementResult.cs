namespace FusionCanvas.Application.Groups;

public sealed record GroupManagementResult(
    bool Succeeded,
    string? Error,
    GroupSummary? Group,
    GroupManagementState State)
{
    public static GroupManagementResult Success(GroupSummary? group, GroupManagementState state) =>
        new(true, null, group, state);

    public static GroupManagementResult Failure(string error, GroupManagementState state) =>
        new(false, string.IsNullOrWhiteSpace(error) ? "Group operation failed." : error, null, state);
}

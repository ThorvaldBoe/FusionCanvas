namespace FusionCanvas.Application.Versioning;

public sealed record ApplicationVersionInfo(
    string ProductVersion,
    string InformationalVersion,
    string CommitId)
{
    public static ApplicationVersionInfo Unknown { get; } = new(
        ProductVersion: "0.0.0",
        InformationalVersion: "0.0.0",
        CommitId: UnknownCommitId);

    public const string UnknownCommitId = "unknown";

    public bool IsCommitKnown => !string.IsNullOrEmpty(CommitId) && CommitId != UnknownCommitId;
}

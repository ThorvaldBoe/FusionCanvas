namespace FusionCanvas.ApiVerification.Sessions;

internal sealed record VerificationSessionReport(
    int SchemaVersion,
    string RunId,
    string Provider,
    string Scenario,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    string Status,
    IReadOnlyList<VerificationOperationResult> Operations,
    IReadOnlyList<VerificationFinding> Findings,
    decimal? EstimatedMaximumCostUsd,
    decimal? ActualCostUsd,
    IReadOnlyList<string> CleanupActions);

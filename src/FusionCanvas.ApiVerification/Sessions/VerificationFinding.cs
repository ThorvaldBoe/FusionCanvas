namespace FusionCanvas.ApiVerification.Sessions;

internal sealed record VerificationFinding(
    string Id,
    string OperationId,
    string Status,
    string Concern,
    string Expected,
    string Actual,
    string? ProductionLocation,
    DateTimeOffset CreatedAt);

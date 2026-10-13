namespace FusionCanvas.ApiVerification.Sessions;

internal sealed record VerificationOperationResult(
    string OperationId,
    string Status,
    string? Summary = null,
    string? Diagnostic = null,
    string? ModelId = null,
    string? ProfileFingerprint = null,
    decimal? CostUsd = null,
    string? ResourceId = null);

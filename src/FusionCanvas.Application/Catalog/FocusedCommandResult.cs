namespace FusionCanvas.Application.Catalog;

public sealed record FocusedCommandResult(bool Succeeded, string? Error, OfferingManagementState State, IReadOnlyList<string>? Details = null);

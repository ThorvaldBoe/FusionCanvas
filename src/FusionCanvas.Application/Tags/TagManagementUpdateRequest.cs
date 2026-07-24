namespace FusionCanvas.Application.Tags;

public sealed record TagManagementUpdateRequest(
    Guid TagId,
    string Name,
    string? Description = null,
    string? Color = null);

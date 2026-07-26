namespace FusionCanvas.Application.Tags;

public sealed record TagManagementCreateRequest(
    Guid StoreId,
    string Name,
    string? Description = null,
    string? Color = null);

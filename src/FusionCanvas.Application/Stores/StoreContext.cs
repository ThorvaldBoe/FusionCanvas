namespace FusionCanvas.Application.Stores;

public sealed record StoreContext(
    string? Description = null,
    string? Notes = null,
    string? TargetMarket = null,
    string? BrandDirection = null,
    string? PlanningContext = null);

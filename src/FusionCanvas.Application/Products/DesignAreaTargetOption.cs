namespace FusionCanvas.Application.Products;

public sealed record DesignAreaTargetOption
(
    Guid DesignAreaId,
    string ProductName,
    string OfferingName,
    string Position,
    string DecorationMethod,
    int Width,
    int Height,
    bool IsChoiceNetwork,
    bool IsSelected);

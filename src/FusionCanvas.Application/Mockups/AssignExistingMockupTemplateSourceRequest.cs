namespace FusionCanvas.Application.Mockups;

public sealed record AssignExistingMockupTemplateSourceRequest(
    Guid StoreId,
    Guid TemplateId,
    Guid ExistingSourceImageId,
    IReadOnlyList<Guid> OptionValueIds,
    bool ReuseMapping = true);

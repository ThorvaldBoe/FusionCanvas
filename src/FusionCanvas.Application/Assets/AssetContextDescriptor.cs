namespace FusionCanvas.Application.Assets;

public sealed record AssetContextDescriptor(
    Guid StoreId,
    AssetContextReference Reference,
    string DisplayName,
    string ContextKindLabel);

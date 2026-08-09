using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Catalog;

public sealed record CatalogSetupState(
    Guid StoreId,
    bool IsReadOnly,
    IReadOnlyList<Blueprint> Blueprints,
    IReadOnlyList<PrintProvider> PrintProviders,
    IReadOnlyList<BlueprintOffering> Offerings,
    IReadOnlyList<OfferingOption> Options,
    IReadOnlyList<OfferingOptionValue> OptionValues,
    IReadOnlyList<OfferingVariant> Variants,
    IReadOnlyList<OfferingPlaceholder> Placeholders);

public sealed record CatalogSetupResult(bool Succeeded, string? Error, CatalogSetupState State, WorkspaceSnapshot? Snapshot = null)
{
    public static CatalogSetupResult Success(CatalogSetupState state) => new(true, null, state);
    public static CatalogSetupResult Failure(string error, CatalogSetupState state) => new(false, error, state);
}

public sealed record CreateBlueprintRequest(Guid StoreId, string Name, string? Description = null);
public sealed record CreatePrintProviderRequest(Guid StoreId, string Name, string? ExternalProviderId = null);
public sealed record CreateOfferingRequest(Guid StoreId, Guid BlueprintId, string Name, BlueprintOfferingKind Kind, Guid? PrintProviderId = null, string? ProviderNetworkCode = null, string? Description = null, string? ExternalOfferingId = null);
public sealed record CreateOfferingOptionRequest(Guid OfferingId, OptionKind OptionKind, string Name, int SortOrder = 0);
public sealed record CreateOptionValueRequest(Guid OfferingId, Guid OptionId, string Value, int SortOrder = 0);
public sealed record CreateOfferingVariantRequest(Guid OfferingId, string Name, IReadOnlyList<Guid> OptionValueIds);
public sealed record CreateOfferingPlaceholderRequest(Guid OfferingId, string Name, string Position, string DecorationMethod, int Width, int Height, IReadOnlyList<Guid> VariantIds, string? Description = null);
public sealed record ArchiveCatalogRecordRequest(Guid StoreId, CatalogRecordKind Kind, Guid RecordId);
public sealed record UpdateCatalogRecordRequest(Guid StoreId, CatalogRecordKind Kind, Guid RecordId, string? Name = null, string? Description = null, string? Position = null, string? DecorationMethod = null, int? Width = null, int? Height = null, string? ProviderNetworkCode = null, Guid? DefaultPlaceholderId = null);

public enum CatalogRecordKind
{
    Blueprint,
    PrintProvider,
    Offering,
    Option,
    OptionValue,
    Variant,
    Placeholder
}

public interface ICatalogSetupService
{
    Task<CatalogSetupState> LoadForStoreAsync(Guid storeId, CancellationToken cancellationToken = default);
    Task<CatalogSetupResult> CreateBlueprintAsync(CreateBlueprintRequest request, CancellationToken cancellationToken = default);
    Task<CatalogSetupResult> CreatePrintProviderAsync(CreatePrintProviderRequest request, CancellationToken cancellationToken = default);
    Task<CatalogSetupResult> CreateOfferingAsync(CreateOfferingRequest request, CancellationToken cancellationToken = default);
    Task<CatalogSetupResult> CreateOptionAsync(CreateOfferingOptionRequest request, CancellationToken cancellationToken = default);
    Task<CatalogSetupResult> CreateOptionValueAsync(CreateOptionValueRequest request, CancellationToken cancellationToken = default);
    Task<CatalogSetupResult> CreateVariantAsync(CreateOfferingVariantRequest request, CancellationToken cancellationToken = default);
    Task<CatalogSetupResult> CreatePlaceholderAsync(CreateOfferingPlaceholderRequest request, CancellationToken cancellationToken = default);
    Task<CatalogSetupResult> ArchiveAsync(ArchiveCatalogRecordRequest request, CancellationToken cancellationToken = default);
    Task<CatalogSetupResult> RestoreAsync(ArchiveCatalogRecordRequest request, CancellationToken cancellationToken = default);
    Task<CatalogSetupResult> UpdateAsync(UpdateCatalogRecordRequest request, CancellationToken cancellationToken = default);
    Task<CatalogSetupResult> DeleteAsync(ArchiveCatalogRecordRequest request, CancellationToken cancellationToken = default);
}

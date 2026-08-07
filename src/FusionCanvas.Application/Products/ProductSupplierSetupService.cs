using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Application.Workspaces;

namespace FusionCanvas.Application.Products;

public sealed class ProductSupplierSetupService : IProductSupplierSetupService
{
    private readonly IWorkspaceRepository _repository;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<Guid> _newId;

    public ProductSupplierSetupService(
        IWorkspaceRepository repository,
        Func<DateTimeOffset>? clock = null,
        Func<Guid>? newId = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _newId = newId ?? Guid.NewGuid;
    }

    public async Task<ProductSupplierSetupState> LoadForStoreAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        return BuildState(snapshot, storeId);
    }

    public async Task<ProductSupplierSetupResult> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var store = snapshot.Stores.SingleOrDefault(item => item.Id == request.StoreId);
        if (store is null)
        {
            return ProductSupplierSetupResult.Failure("Store was not found.", BuildState(snapshot, request.StoreId));
        }

        if (store.IsArchived)
        {
            return ProductSupplierSetupResult.Failure("Archived Store catalogs are read-only.", BuildState(snapshot, request.StoreId));
        }

        var name = NormalizeRequired(request.Name);
        if (name is null)
        {
            return ProductSupplierSetupResult.Failure("Product name is required.", BuildState(snapshot, request.StoreId));
        }

        if (snapshot.StoreProducts.Any(product => product.StoreId == request.StoreId && string.Equals(product.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return ProductSupplierSetupResult.Failure("This Store already has a product with this name.", BuildState(snapshot, request.StoreId));
        }

        var now = _clock();
        var product = new StoreProduct(
            _newId(), request.StoreId, name, NormalizeOptional(request.Description),
            NormalizeOptional(request.ExternalProductId), now, now, "{}");
        var updated = snapshot with { StoreProducts = [.. snapshot.StoreProducts, product] };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        return ProductSupplierSetupResult.Success(BuildState(updated, request.StoreId));
    }

    public async Task<ProductSupplierSetupResult> UpdateProductAsync(UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var product = snapshot.StoreProducts.SingleOrDefault(item => item.Id == request.ProductId);
        if (product is null)
        {
            return ProductSupplierSetupResult.Failure("Product was not found.", BuildState(snapshot, null));
        }

        var readOnly = ReadOnlyCheck(snapshot, product.StoreId);
        if (readOnly is not null)
        {
            return ProductSupplierSetupResult.Failure(readOnly, BuildState(snapshot, product.StoreId));
        }

        var name = NormalizeRequired(request.Name);
        if (name is null)
        {
            return ProductSupplierSetupResult.Failure("Product name is required.", BuildState(snapshot, product.StoreId));
        }

        if (snapshot.StoreProducts.Any(candidate =>
            candidate.Id != product.Id &&
            candidate.StoreId == product.StoreId &&
            string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return ProductSupplierSetupResult.Failure("This Store already has a product with this name.", BuildState(snapshot, product.StoreId));
        }

        var updatedProduct = product with
        {
            Name = name,
            Description = NormalizeOptional(request.Description),
            ExternalProductId = NormalizeOptional(request.ExternalProductId),
            UpdatedAt = _clock()
        };
        var updated = snapshot with
        {
            StoreProducts = snapshot.StoreProducts.Select(candidate => candidate.Id == updatedProduct.Id ? updatedProduct : candidate).ToArray()
        };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        return ProductSupplierSetupResult.Success(BuildState(updated, product.StoreId));
    }

    public async Task<ProductSupplierSetupResult> DeleteProductAsync(DeleteProductRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var product = snapshot.StoreProducts.SingleOrDefault(item => item.Id == request.ProductId);
        if (product is null)
        {
            return ProductSupplierSetupResult.Failure("Product was not found.", BuildState(snapshot, null));
        }

        var readOnly = ReadOnlyCheck(snapshot, product.StoreId);
        if (readOnly is not null)
        {
            return ProductSupplierSetupResult.Failure(readOnly, BuildState(snapshot, product.StoreId));
        }

        if (!request.Confirm)
        {
            return ProductSupplierSetupResult.Failure("Permanent deletion requires confirmation.", BuildState(snapshot, product.StoreId));
        }

        var offeringIds = snapshot.FulfillmentOfferings.Where(offering => offering.StoreProductId == product.Id).Select(offering => offering.Id).ToHashSet();
        var areaIds = snapshot.DesignAreas.Where(area => offeringIds.Contains(area.FulfillmentOfferingId)).Select(area => area.Id).ToHashSet();
        var isReferencedByConfig = snapshot.ItemListingConfigurations.Any(c => offeringIds.Contains(c.OfferingId));
        var isReferencedBySlot = snapshot.DesignSlotAssignments.Any(a => areaIds.Contains(a.DesignAreaId));

        if (offeringIds.Count > 0 || isReferencedByConfig || isReferencedBySlot)
        {
            return ProductSupplierSetupResult.Failure(
                "This product has offerings, configurations, or slot references. Remove those first.",
                BuildState(snapshot, product.StoreId));
        }

        var updated = snapshot with
        {
            StoreProducts = snapshot.StoreProducts.Where(candidate => candidate.Id != product.Id).ToArray()
        };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        return ProductSupplierSetupResult.Success(BuildState(updated, product.StoreId));
    }

    public async Task<ProductSupplierSetupResult> CreateOfferingAsync(CreateOfferingRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var product = snapshot.StoreProducts.SingleOrDefault(item => item.Id == request.ProductId);
        if (product is null)
        {
            return ProductSupplierSetupResult.Failure("Product was not found.", BuildState(snapshot, null));
        }

        var readOnly = ReadOnlyCheck(snapshot, product.StoreId);
        if (readOnly is not null)
        {
            return ProductSupplierSetupResult.Failure(readOnly, BuildState(snapshot, product.StoreId));
        }

        var name = NormalizeRequired(request.Name);
        if (name is null)
        {
            return ProductSupplierSetupResult.Failure("Offering name is required.", BuildState(snapshot, product.StoreId));
        }

        if (snapshot.FulfillmentOfferings.Any(offering =>
            offering.StoreProductId == product.Id && string.Equals(offering.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return ProductSupplierSetupResult.Failure("This product already has an offering with this name.", BuildState(snapshot, product.StoreId));
        }

        var now = _clock();
        FulfillmentOffering offering;
        try
        {
            offering = new FulfillmentOffering(
                _newId(), product.Id, name, NormalizeOptional(request.Description), request.Kind,
                NormalizeOptional(request.ProviderName), NormalizeOptional(request.ExternalOfferingId), now, now, "{}");
        }
        catch (ArgumentException exception)
        {
            return ProductSupplierSetupResult.Failure(exception.Message, BuildState(snapshot, product.StoreId));
        }

        var updated = snapshot with { FulfillmentOfferings = [.. snapshot.FulfillmentOfferings, offering] };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        return ProductSupplierSetupResult.Success(BuildState(updated, product.StoreId));
    }

    public async Task<ProductSupplierSetupResult> UpdateOfferingAsync(UpdateOfferingRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var offering = snapshot.FulfillmentOfferings.SingleOrDefault(item => item.Id == request.OfferingId);
        if (offering is null)
        {
            return ProductSupplierSetupResult.Failure("Offering was not found.", BuildState(snapshot, null));
        }

        var storeId = ProductStoreIdOf(snapshot, offering.StoreProductId);
        var readOnly = ReadOnlyCheck(snapshot, storeId);
        if (readOnly is not null)
        {
            return ProductSupplierSetupResult.Failure(readOnly, BuildState(snapshot, storeId));
        }

        var name = NormalizeRequired(request.Name);
        if (name is null)
        {
            return ProductSupplierSetupResult.Failure("Offering name is required.", BuildState(snapshot, storeId));
        }

        if (snapshot.FulfillmentOfferings.Any(candidate =>
            candidate.Id != offering.Id &&
            candidate.StoreProductId == offering.StoreProductId &&
            string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return ProductSupplierSetupResult.Failure("This product already has an offering with this name.", BuildState(snapshot, storeId));
        }

        FulfillmentOffering updatedOffering;
        try
        {
            updatedOffering = new FulfillmentOffering(
                offering.Id, offering.StoreProductId, name, NormalizeOptional(request.Description), request.Kind,
                NormalizeOptional(request.ProviderName), NormalizeOptional(request.ExternalOfferingId), offering.CreatedAt, _clock(), offering.MetadataJson);
        }
        catch (ArgumentException exception)
        {
            return ProductSupplierSetupResult.Failure(exception.Message, BuildState(snapshot, storeId));
        }

        var updated = snapshot with
        {
            FulfillmentOfferings = snapshot.FulfillmentOfferings.Select(candidate => candidate.Id == updatedOffering.Id ? updatedOffering : candidate).ToArray()
        };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        return ProductSupplierSetupResult.Success(BuildState(updated, storeId));
    }

    public async Task<ProductSupplierSetupResult> DeleteOfferingAsync(DeleteOfferingRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var offering = snapshot.FulfillmentOfferings.SingleOrDefault(item => item.Id == request.OfferingId);
        if (offering is null)
        {
            return ProductSupplierSetupResult.Failure("Offering was not found.", BuildState(snapshot, null));
        }

        var storeId = ProductStoreIdOf(snapshot, offering.StoreProductId);
        var readOnly = ReadOnlyCheck(snapshot, storeId);
        if (readOnly is not null)
        {
            return ProductSupplierSetupResult.Failure(readOnly, BuildState(snapshot, storeId));
        }

        if (!request.Confirm)
        {
            return ProductSupplierSetupResult.Failure("Permanent deletion requires confirmation.", BuildState(snapshot, storeId));
        }

        var areaIds = snapshot.DesignAreas.Where(area => area.FulfillmentOfferingId == offering.Id).Select(area => area.Id).ToHashSet();
        var hasVariants = snapshot.ProductVariants.Any(variant => variant.FulfillmentOfferingId == offering.Id);
        var hasAreas = areaIds.Count > 0;
        var isReferenced = snapshot.ItemListingConfigurations.Any(c => c.OfferingId == offering.Id);

        if (hasVariants || hasAreas || isReferenced)
        {
            return ProductSupplierSetupResult.Failure(
                "This offering is selected or contains variants or design areas. Remove those first or replace the configuration.",
                BuildState(snapshot, storeId));
        }

        var updated = snapshot with
        {
            FulfillmentOfferings = snapshot.FulfillmentOfferings.Where(candidate => candidate.Id != offering.Id).ToArray()
        };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        return ProductSupplierSetupResult.Success(BuildState(updated, storeId));
    }

    public async Task<ProductSupplierSetupResult> CreateVariantAsync(CreateVariantRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var offering = snapshot.FulfillmentOfferings.SingleOrDefault(item => item.Id == request.OfferingId);
        if (offering is null)
        {
            return ProductSupplierSetupResult.Failure("Offering was not found.", BuildState(snapshot, null));
        }

        var storeId = ProductStoreIdOf(snapshot, offering.StoreProductId);
        var readOnly = ReadOnlyCheck(snapshot, storeId);
        if (readOnly is not null)
        {
            return ProductSupplierSetupResult.Failure(readOnly, BuildState(snapshot, storeId));
        }

        ProductVariant variant;
        try
        {
            variant = new ProductVariant(_newId(), offering.Id, ToVariantOptions(request.Options), _clock(), _clock());
        }
        catch (ArgumentException exception)
        {
            return ProductSupplierSetupResult.Failure(exception.Message, BuildState(snapshot, storeId));
        }

        var updated = snapshot with { ProductVariants = [.. snapshot.ProductVariants, variant] };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        return ProductSupplierSetupResult.Success(BuildState(updated, storeId));
    }

    public async Task<ProductSupplierSetupResult> UpdateVariantAsync(UpdateVariantRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var variant = snapshot.ProductVariants.SingleOrDefault(item => item.Id == request.VariantId);
        if (variant is null)
        {
            return ProductSupplierSetupResult.Failure("Variant was not found.", BuildState(snapshot, null));
        }

        var readOnly = ReadOnlyCheck(snapshot, offeringStoreIdOf(snapshot, variant.FulfillmentOfferingId));
        if (readOnly is not null)
        {
            return ProductSupplierSetupResult.Failure(readOnly, BuildState(snapshot, offeringStoreIdOf(snapshot, variant.FulfillmentOfferingId)));
        }

        ProductVariant updatedVariant;
        try
        {
            updatedVariant = new ProductVariant(variant.Id, variant.FulfillmentOfferingId, ToVariantOptions(request.Options), variant.CreatedAt, _clock());
        }
        catch (ArgumentException exception)
        {
            return ProductSupplierSetupResult.Failure(exception.Message, BuildState(snapshot, offeringStoreIdOf(snapshot, variant.FulfillmentOfferingId)));
        }

        var updated = snapshot with
        {
            ProductVariants = snapshot.ProductVariants.Select(candidate => candidate.Id == updatedVariant.Id ? updatedVariant : candidate).ToArray()
        };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        return ProductSupplierSetupResult.Success(BuildState(updated, offeringStoreIdOf(snapshot, variant.FulfillmentOfferingId)));
    }

    public async Task<ProductSupplierSetupResult> DeleteVariantAsync(DeleteVariantRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var variant = snapshot.ProductVariants.SingleOrDefault(item => item.Id == request.VariantId);
        if (variant is null)
        {
            return ProductSupplierSetupResult.Failure("Variant was not found.", BuildState(snapshot, null));
        }

        var storeId = offeringStoreIdOf(snapshot, variant.FulfillmentOfferingId);
        var readOnly = ReadOnlyCheck(snapshot, storeId);
        if (readOnly is not null)
        {
            return ProductSupplierSetupResult.Failure(readOnly, BuildState(snapshot, storeId));
        }

        if (!request.Confirm)
        {
            return ProductSupplierSetupResult.Failure("Permanent deletion requires confirmation.", BuildState(snapshot, storeId));
        }

        var isReferencedByArea = snapshot.DesignAreas.Any(area =>
            area.FulfillmentOfferingId == variant.FulfillmentOfferingId && area.VariantIds.Contains(variant.Id));
        if (isReferencedByArea)
        {
            return ProductSupplierSetupResult.Failure("This variant is referenced by a design area. Remove it from the area first.", BuildState(snapshot, storeId));
        }

        var updated = snapshot with
        {
            ProductVariants = snapshot.ProductVariants.Where(candidate => candidate.Id != variant.Id).ToArray()
        };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        return ProductSupplierSetupResult.Success(BuildState(updated, storeId));
    }

    public async Task<ProductSupplierSetupResult> CreateDesignAreaAsync(CreateDesignAreaRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var offering = snapshot.FulfillmentOfferings.SingleOrDefault(item => item.Id == request.OfferingId);
        if (offering is null)
        {
            return ProductSupplierSetupResult.Failure("Offering was not found.", BuildState(snapshot, null));
        }

        var storeId = ProductStoreIdOf(snapshot, offering.StoreProductId);
        var readOnly = ReadOnlyCheck(snapshot, storeId);
        if (readOnly is not null)
        {
            return ProductSupplierSetupResult.Failure(readOnly, BuildState(snapshot, storeId));
        }

        var ownership = ValidateApplicableVariants(snapshot, offering.Id, request.VariantIds);
        if (ownership is not null)
        {
            return ProductSupplierSetupResult.Failure(ownership, BuildState(snapshot, storeId));
        }

        DesignArea area;
        try
        {
            area = new DesignArea(
                _newId(), offering.Id, NormalizeRequired(request.Name) ?? string.Empty, null, request.Position,
                request.DecorationMethod, request.Width, request.Height, request.VariantIds, _clock(), _clock(), "{}");
        }
        catch (ArgumentException exception)
        {
            return ProductSupplierSetupResult.Failure(exception.Message, BuildState(snapshot, storeId));
        }

        var updated = snapshot with { DesignAreas = [.. snapshot.DesignAreas, area] };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        return ProductSupplierSetupResult.Success(BuildState(updated, storeId));
    }

    public async Task<ProductSupplierSetupResult> UpdateDesignAreaAsync(UpdateDesignAreaRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var area = snapshot.DesignAreas.SingleOrDefault(item => item.Id == request.DesignAreaId);
        if (area is null)
        {
            return ProductSupplierSetupResult.Failure("Design area was not found.", BuildState(snapshot, null));
        }

        var storeId = offeringStoreIdOf(snapshot, area.FulfillmentOfferingId);
        var readOnly = ReadOnlyCheck(snapshot, storeId);
        if (readOnly is not null)
        {
            return ProductSupplierSetupResult.Failure(readOnly, BuildState(snapshot, storeId));
        }

        var ownership = ValidateApplicableVariants(snapshot, area.FulfillmentOfferingId, request.VariantIds);
        if (ownership is not null)
        {
            return ProductSupplierSetupResult.Failure(ownership, BuildState(snapshot, storeId));
        }

        DesignArea updatedArea;
        try
        {
            updatedArea = new DesignArea(
                area.Id, area.FulfillmentOfferingId, NormalizeRequired(request.Name) ?? string.Empty, area.Description,
                request.Position, request.DecorationMethod, request.Width, request.Height, request.VariantIds, area.CreatedAt, _clock(), area.MetadataJson);
        }
        catch (ArgumentException exception)
        {
            return ProductSupplierSetupResult.Failure(exception.Message, BuildState(snapshot, storeId));
        }

        var updated = snapshot with
        {
            DesignAreas = snapshot.DesignAreas.Select(candidate => candidate.Id == updatedArea.Id ? updatedArea : candidate).ToArray()
        };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        return ProductSupplierSetupResult.Success(BuildState(updated, storeId));
    }

    public async Task<ProductSupplierSetupResult> DeleteDesignAreaAsync(DeleteDesignAreaRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var area = snapshot.DesignAreas.SingleOrDefault(item => item.Id == request.DesignAreaId);
        if (area is null)
        {
            return ProductSupplierSetupResult.Failure("Design area was not found.", BuildState(snapshot, null));
        }

        var storeId = offeringStoreIdOf(snapshot, area.FulfillmentOfferingId);
        var readOnly = ReadOnlyCheck(snapshot, storeId);
        if (readOnly is not null)
        {
            return ProductSupplierSetupResult.Failure(readOnly, BuildState(snapshot, storeId));
        }

        if (!request.Confirm)
        {
            return ProductSupplierSetupResult.Failure("Permanent deletion requires confirmation.", BuildState(snapshot, storeId));
        }

        var isReferenced = snapshot.DesignSlotAssignments.Any(a => a.DesignAreaId == area.Id);
        if (isReferenced)
        {
            return ProductSupplierSetupResult.Failure(
                "This printable area is referenced by one or more slot assignments. Clear those first.",
                BuildState(snapshot, storeId));
        }

        var updated = snapshot with
        {
            DesignAreas = snapshot.DesignAreas.Where(candidate => candidate.Id != area.Id).ToArray()
        };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);

        return ProductSupplierSetupResult.Success(BuildState(updated, storeId));
    }

    private static IReadOnlyList<VariantOption> ToVariantOptions(IReadOnlyList<VariantOptionDraft> drafts) =>
        drafts is null || drafts.Count == 0
            ? throw new ArgumentException("A product variant must define at least one option.")
            : drafts.Select(draft => new VariantOption(draft.Name, draft.Value)).ToArray();

    private static string? ValidateApplicableVariants(WorkspaceSnapshot snapshot, Guid offeringId, IReadOnlyList<Guid>? variantIds)
    {
        if (variantIds is null || variantIds.Count == 0)
        {
            return null;
        }

        var invalid = variantIds.FirstOrDefault(id => snapshot.ProductVariants.All(variant => !(variant.Id == id && variant.FulfillmentOfferingId == offeringId)));
        return invalid == Guid.Empty
            ? null
            : "A printable area may only apply to variants from its own offering.";
    }

    private Guid? offeringStoreIdOf(WorkspaceSnapshot snapshot, Guid offeringId)
    {
        var offering = snapshot.FulfillmentOfferings.SingleOrDefault(candidate => candidate.Id == offeringId);
        return offering is null ? null : ProductStoreIdOf(snapshot, offering.StoreProductId);
    }

    private static Guid? ProductStoreIdOf(WorkspaceSnapshot snapshot, Guid productId) =>
        snapshot.StoreProducts.SingleOrDefault(candidate => candidate.Id == productId)?.StoreId;

    private static string? ReadOnlyCheck(WorkspaceSnapshot snapshot, Guid? storeId)
    {
        if (storeId is not Guid id || id == Guid.Empty)
        {
            return "The owning Store was not found.";
        }

        var store = snapshot.Stores.SingleOrDefault(candidate => candidate.Id == id);
        if (store is null)
        {
            return "The owning Store was not found.";
        }

        if (store.IsArchived)
        {
            return "Archived Store catalogs are read-only.";
        }

        return null;
    }

    private static string? NormalizeRequired(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private ProductSupplierSetupState BuildState(WorkspaceSnapshot snapshot, Guid? storeId)
    {
        if (storeId is not Guid id)
        {
            return new ProductSupplierSetupState(null, false, false, []);
        }

        var store = snapshot.Stores.SingleOrDefault(candidate => candidate.Id == id);
        var products = snapshot.StoreProducts
            .Where(product => product.StoreId == id)
            .OrderBy(product => product.Name, StringComparer.OrdinalIgnoreCase)
            .Select(product => ToSummary(snapshot, product))
            .ToArray();

        return new ProductSupplierSetupState(
            id,
            store is { IsArchived: true },
            products.Length == 0,
            products);
    }

    private static StoreProductSummary ToSummary(WorkspaceSnapshot snapshot, StoreProduct product)
    {
        var offerings = snapshot.FulfillmentOfferings
            .Where(offering => offering.StoreProductId == product.Id)
            .OrderBy(offering => offering.Name, StringComparer.OrdinalIgnoreCase)
            .Select(offering => ToSummary(snapshot, offering))
            .ToArray();
        return new StoreProductSummary(product.Id, product.StoreId, product.Name, product.Description, product.ExternalProductId, offerings);
    }

    private static FulfillmentOfferingSummary ToSummary(WorkspaceSnapshot snapshot, FulfillmentOffering offering)
    {
        var variants = snapshot.ProductVariants
            .Where(variant => variant.FulfillmentOfferingId == offering.Id)
            .OrderBy(variant => variant.CreatedAt)
            .ThenBy(variant => variant.Id)
            .Select(variant => new ProductVariantSummary(variant.Id, variant.FulfillmentOfferingId, variant.Options))
            .ToArray();
        var areas = snapshot.DesignAreas
            .Where(area => area.FulfillmentOfferingId == offering.Id)
            .OrderBy(area => area.Name, StringComparer.OrdinalIgnoreCase)
            .Select(area => new DesignAreaSummary(
                area.Id, area.FulfillmentOfferingId, area.Name, area.Position, area.DecorationMethod,
                area.Width, area.Height, area.VariantIds, offering.Kind == FulfillmentKind.PrintifyChoiceNetwork))
            .ToArray();
        return new FulfillmentOfferingSummary(
            offering.Id, offering.StoreProductId, offering.Name, offering.Description, offering.Kind, offering.ProviderName,
            offering.ExternalOfferingId, variants, areas);
    }
}

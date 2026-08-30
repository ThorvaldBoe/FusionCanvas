using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Catalog;

public sealed class OfferingManagementService : IOfferingManagementService
{
    private readonly IWorkspaceRepository _repository;
    private readonly IProviderCatalogCandidateSource _providerCatalog;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<Guid> _newId;

    public OfferingManagementService(IWorkspaceRepository repository, IProviderCatalogCandidateSource? providerCatalog = null, Func<DateTimeOffset>? clock = null, Func<Guid>? newId = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _providerCatalog = providerCatalog ?? new UnavailableProviderCatalogCandidateSource();
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _newId = newId ?? Guid.NewGuid;
    }

    public async Task<IReadOnlyList<BlueprintOfferingSetupSummary>> LoadForBlueprintAsync(Guid storeId, Guid blueprintId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        RequireBlueprint(snapshot, storeId, blueprintId);
        return snapshot.BlueprintOfferings
            .Where(value => value.StoreId == storeId && value.BlueprintId == blueprintId)
            .OrderBy(value => value.Name)
            .Select(value => ToSummary(snapshot, value))
            .ToArray();
    }

    public async Task<OfferingManagementState> LoadOfferingAsync(OfferingContext context, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        return BuildOfferingState(snapshot, context);
    }

    private static OfferingManagementState BuildOfferingState(WorkspaceSnapshot snapshot, OfferingContext context)
    {
        var store = snapshot.Stores.SingleOrDefault(value => value.Id == context.StoreId)
            ?? throw new InvalidOperationException("Store was not found.");
        var blueprint = RequireBlueprint(snapshot, context.StoreId, context.BlueprintId);
        var offering = snapshot.BlueprintOfferings.SingleOrDefault(value => value.Id == context.OfferingId && value.StoreId == context.StoreId && value.BlueprintId == context.BlueprintId)
            ?? throw new InvalidOperationException("Blueprint Offering was not found in the selected Store and Blueprint.");

        var variants = snapshot.OfferingVariants.Where(value => value.OfferingId == offering.Id).OrderBy(value => value.Name).ToArray();
        var designAreas = snapshot.OfferingPlaceholders.Where(value => value.OfferingId == offering.Id).OrderBy(value => value.Name).ToArray();
        var activeVariantIds = variants.Where(value => !value.IsArchived).Select(value => value.Id).ToHashSet();
        var templates = snapshot.MockupTemplates.Where(value => value.BlueprintOfferingId == offering.Id).OrderBy(value => value.Name).ToArray();
        return new OfferingManagementState(
            context,
            store.IsArchived || blueprint.IsArchived || offering.IsArchived,
            blueprint,
            offering,
            ToSummary(snapshot, offering),
            snapshot.OfferingOptions.Where(value => value.OfferingId == offering.Id).OrderBy(value => value.SortOrder).ToArray(),
            snapshot.OfferingOptionValues.Where(value => value.OfferingId == offering.Id).OrderBy(value => value.SortOrder).ToArray(),
            variants,
            designAreas,
            designAreas.Select(value => new DesignAreaSetupSummary(
                value.Id,
                value.Name,
                value.Position,
                value.Width,
                value.Height,
                value.MaximumPhysicalSize,
                value.ArtworkGuidance,
                activeVariantIds.SetEquals(value.VariantIds),
                value.VariantIds.Count,
                value.ProviderReference)).ToArray(),
            templates,
            templates.Select(template =>
            {
                var colorIds = snapshot.MockupTemplateColorVariants.Where(value => value.MockupTemplateId == template.Id && !value.IsArchived).Select(value => value.ColorOptionValueId).ToArray();
                var target = designAreas.SingleOrDefault(value => value.Id == template.TargetPlaceholderId);
                var revision = snapshot.MockupTemplateRevisions.SingleOrDefault(value => value.MockupTemplateId == template.Id && value.RevisionNumber == template.CurrentRevision);
                var effectiveRevision = revision ?? new MockupTemplateRevision(template.Id, template.Id, template.CurrentRevision, template.TargetPlaceholderId, template.CreatedAt);
                var readiness = MockupTemplateReadinessPolicy.Evaluate(new(template, effectiveRevision, colorIds,
                    snapshot.OfferingOptions, snapshot.OfferingOptionValues, variants, designAreas));
                return new MockupTemplateSetupSummary(
                    template.Id,
                    template.Name,
                    target?.Id,
                    target?.Name,
                    colorIds,
                    variants.Where(variant => !variant.IsArchived && variant.OptionValueIds.Any(colorIds.Contains)).Select(variant => variant.Id).ToArray(),
                    revision?.ProviderMockupReference,
                    template.CurrentRevision,
                    template.IsArchived,
                    readiness.Lifecycle,
                    readiness.Blockers);
            }).ToArray());
    }

    public async Task<FocusedCommandResult> CreateVariantAsync(CreateFocusedVariantRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var (store, blueprint, offering) = RequireContext(snapshot, request.Context);
        if (store.IsArchived || blueprint.IsArchived || offering.IsArchived)
            return Failure(snapshot, request.Context, "Archived catalog records are read-only.");
        var requestedIds = request.OptionValueIds.Distinct().ToArray();
        var values = snapshot.OfferingOptionValues.Where(value => requestedIds.Contains(value.Id)).ToArray();
        if (values.Length != requestedIds.Length || values.Any(value => value.OfferingId != offering.Id || value.IsArchived))
            return Failure(snapshot, request.Context, "A Variant may only use active Option Values from this Offering.");
        var optionIds = values.Select(value => value.OptionId).ToArray();
        if (optionIds.Length != optionIds.Distinct().Count())
            return Failure(snapshot, request.Context, "A Variant cannot contain two values from the same Option.");
        if (snapshot.OfferingVariants.Any(value => value.OfferingId == offering.Id && !value.IsArchived && value.OptionValueIds.OrderBy(id => id).SequenceEqual(requestedIds.OrderBy(id => id))))
            return Failure(snapshot, request.Context, "An active sellable Variant already uses this combination.");

        var now = _clock();
        OfferingVariant variant;
        try
        {
            variant = new OfferingVariant(_newId(), offering.Id, request.Name, requestedIds, false, now, now);
        }
        catch (ArgumentException exception)
        {
            return Failure(snapshot, request.Context, exception.Message);
        }
        var updated = snapshot with { OfferingVariants = [.. snapshot.OfferingVariants, variant] };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        return Success(updated, request.Context);
    }

    public async Task<FocusedCommandResult> CreateDesignAreaAsync(CreateFocusedDesignAreaRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var (store, blueprint, offering) = RequireContext(snapshot, request.Context);
        if (store.IsArchived || blueprint.IsArchived || offering.IsArchived)
            return Failure(snapshot, request.Context, "Archived catalog records are read-only.");
        var variantIds = request.UseAllActiveVariants
            ? snapshot.OfferingVariants.Where(value => value.OfferingId == offering.Id && !value.IsArchived).Select(value => value.Id).ToArray()
            : request.VariantIds.Distinct().ToArray();
        if (variantIds.Any(id => snapshot.OfferingVariants.All(value => value.Id != id || value.OfferingId != offering.Id || value.IsArchived)))
            return Failure(snapshot, request.Context, "Design Area compatibility may only use active Variants from this Offering.");

        var now = _clock();
        OfferingPlaceholder designArea;
        try
        {
            designArea = new OfferingPlaceholder(_newId(), offering.Id, request.Name, request.Description, request.Placement, request.DecorationMethod, request.MaximumWidthPixels, request.MaximumHeightPixels, variantIds, false, now, now, providerReference: request.ProviderReference, artworkGuidance: request.ArtworkGuidance);
        }
        catch (ArgumentException exception)
        {
            return Failure(snapshot, request.Context, exception.Message);
        }
        var updated = snapshot with { OfferingPlaceholders = [.. snapshot.OfferingPlaceholders, designArea] };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        return Success(updated, request.Context);
    }

    public async Task<FocusedCommandResult> UpdateDesignAreaAsync(UpdateFocusedDesignAreaRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var (store, blueprint, offering) = RequireContext(snapshot, request.Context);
        if (store.IsArchived || blueprint.IsArchived || offering.IsArchived)
            return Failure(snapshot, request.Context, "Archived catalog records are read-only.");
        var existing = snapshot.OfferingPlaceholders.SingleOrDefault(value => value.Id == request.DesignAreaId && value.OfferingId == offering.Id);
        if (existing is null || existing.IsArchived)
            return Failure(snapshot, request.Context, "Design Area was not found in this Offering.");
        var variantIds = request.UseAllActiveVariants
            ? snapshot.OfferingVariants.Where(value => value.OfferingId == offering.Id && !value.IsArchived).Select(value => value.Id).ToArray()
            : request.VariantIds.Distinct().ToArray();
        if (variantIds.Any(id => snapshot.OfferingVariants.All(value => value.Id != id || value.OfferingId != offering.Id || value.IsArchived)))
            return Failure(snapshot, request.Context, "Design Area compatibility may only use active Variants from this Offering.");
        OfferingPlaceholder replacement;
        try
        {
            replacement = new OfferingPlaceholder(existing.Id, offering.Id, request.Name, request.Description, request.Placement,
                request.DecorationMethod, request.MaximumWidthPixels, request.MaximumHeightPixels, variantIds, false,
                existing.CreatedAt, _clock(), existing.MetadataJson, request.ProviderReference, request.ArtworkGuidance);
        }
        catch (ArgumentException exception) { return Failure(snapshot, request.Context, exception.Message); }
        var updated = snapshot with { OfferingPlaceholders = snapshot.OfferingPlaceholders.Select(value => value.Id == replacement.Id ? replacement : value).ToArray() };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        return Success(updated, request.Context);
    }

    public async Task<FocusedCommandResult> CreateMockupTemplateAsync(CreateFocusedMockupTemplateRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var (store, blueprint, offering) = RequireContext(snapshot, request.Context);
        if (store.IsArchived || blueprint.IsArchived || offering.IsArchived)
            return Failure(snapshot, request.Context, "Archived catalog records are read-only.");
        OfferingPlaceholder? designArea = null;
        if (request.TargetDesignAreaId is not null)
        {
            designArea = snapshot.OfferingPlaceholders.SingleOrDefault(value => value.Id == request.TargetDesignAreaId && value.OfferingId == offering.Id && !value.IsArchived);
            if (designArea is null)
                return Failure(snapshot, request.Context, "Target Design Area must be active and belong to this Offering.");
        }

        var colorIds = request.ColorOptionValueIds?.Distinct().ToArray() ?? [];
        var colors = colorIds.Select(id => FindTypedValue(snapshot, offering.Id, id, OptionKind.Color)).ToArray();
        if (colors.Any(value => value is null))
            return Failure(snapshot, request.Context, "Template applicability may only use active Color values from this Offering.");

        var impliedVariants = snapshot.OfferingVariants.Where(variant =>
            variant.OfferingId == offering.Id && !variant.IsArchived &&
            variant.OptionValueIds.Any(id => colorIds.Contains(id))).ToArray();
        var incompatible = designArea is null ? [] : impliedVariants.Where(value => !designArea.VariantIds.Contains(value.Id)).Select(value => value.Name).OrderBy(value => value).ToArray();
        if (incompatible.Length > 0)
            return Failure(snapshot, request.Context, "The target Design Area is not compatible with every Variant implied by the selected Colors.", incompatible);

        var now = _clock();
        MockupTemplate template;
        MockupTemplateRevision revision;
        try
        {
            template = new MockupTemplate(_newId(), offering.Id, designArea?.Id, request.Name, request.Description, 1, false, now, now);
            revision = new MockupTemplateRevision(_newId(), template.Id, 1, designArea?.Id, now, "Initial template configuration", request.ProviderMockupReference, request.ImageMapping);
        }
        catch (ArgumentException exception)
        {
            return Failure(snapshot, request.Context, exception.Message);
        }
        var bindings = colorIds.Select(id => new MockupTemplateColorVariant(_newId(), template.Id, id, false, now, now)).ToArray();
        var revisionColors = colorIds.Select(id => new MockupTemplateRevisionColor(_newId(), revision.Id, id)).ToArray();
        var updated = snapshot with
        {
            MockupTemplates = [.. snapshot.MockupTemplates, template],
            MockupTemplateColorVariants = [.. snapshot.MockupTemplateColorVariants, .. bindings],
            MockupTemplateRevisions = [.. snapshot.MockupTemplateRevisions, revision],
            MockupTemplateRevisionColors = [.. snapshot.MockupTemplateRevisionColors, .. revisionColors]
        };
        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        return Success(updated, request.Context) with { TemplateId = template.Id };
    }

    public async Task<BulkVariantPreview> PreviewBulkVariantsAsync(BulkVariantRequest request, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        RequireContext(snapshot, request.Context);
        var descriptor = await _providerCatalog.LoadAsync(request.Context, cancellationToken).ConfigureAwait(false);
        return BuildBulkPreview(snapshot, request, descriptor);
    }

    public async Task<BulkVariantResult> ConfirmBulkVariantsAsync(BulkVariantRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var (store, blueprint, offering) = RequireContext(snapshot, request.Context);
        if (store.IsArchived || blueprint.IsArchived || offering.IsArchived)
        {
            var readOnlyPreview = new BulkVariantPreview(request, false, "Archived catalog records are read-only.", []);
            return new BulkVariantResult(false, readOnlyPreview.Message, [], readOnlyPreview);
        }

        var descriptor = await _providerCatalog.LoadAsync(request.Context, cancellationToken).ConfigureAwait(false);
        var preview = BuildBulkPreview(snapshot, request, descriptor);
        if (!preview.CanConfirm)
            return new BulkVariantResult(false, preview.Message, [], preview);

        var color = snapshot.OfferingOptionValues.Single(value => value.Id == request.ColorOptionValueId);
        var sizes = snapshot.OfferingOptionValues.Where(value => preview.Candidates.Any(candidate => candidate.WillCreate && candidate.SizeOptionValueId == value.Id)).ToDictionary(value => value.Id);
        var now = _clock();
        var created = preview.Candidates.Where(value => value.WillCreate)
            .Select(candidate => new OfferingVariant(_newId(), offering.Id, $"{color.Value} / {sizes[candidate.SizeOptionValueId].Value}", [color.Id, candidate.SizeOptionValueId], false, now, now))
            .ToArray();
        cancellationToken.ThrowIfCancellationRequested();
        await _repository.SaveAsync(snapshot with { OfferingVariants = [.. snapshot.OfferingVariants, .. created] }, cancellationToken).ConfigureAwait(false);
        return new BulkVariantResult(true, null, created, preview);
    }

    private static BulkVariantPreview BuildBulkPreview(WorkspaceSnapshot snapshot, BulkVariantRequest request, ProviderCatalogCandidateDescriptor descriptor)
    {
        if (descriptor.Context != request.Context)
            return new BulkVariantPreview(request, false, "Provider catalog data did not match the selected Offering.", []);
        if (!descriptor.IsAvailable)
            return new BulkVariantPreview(request, false, descriptor.UnavailableReason ?? "Provider catalog data is unavailable.", []);

        var color = FindTypedValue(snapshot, request.Context.OfferingId, request.ColorOptionValueId, OptionKind.Color);
        if (color is null)
            return new BulkVariantPreview(request, false, "Select an active Color from this Offering.", []);

        var candidates = new List<BulkVariantCandidate>();
        foreach (var sizeId in request.EnabledSizeOptionValueIds.Distinct())
        {
            var size = FindTypedValue(snapshot, request.Context.OfferingId, sizeId, OptionKind.Size);
            if (size is null)
            {
                candidates.Add(new BulkVariantCandidate(sizeId, "Unknown size", false, "The Size is inactive or belongs to another Offering."));
                continue;
            }

            if (!descriptor.ValidColorSizeCombinations.Contains(new ProviderCatalogCombination(color.Id, size.Id)))
            {
                candidates.Add(new BulkVariantCandidate(size.Id, size.Value, false, "The provider catalog does not allow this Color and Size combination."));
                continue;
            }

            var exists = snapshot.OfferingVariants.Any(variant => variant.OfferingId == request.Context.OfferingId && !variant.IsArchived && variant.OptionValueIds.Count == 2 && variant.OptionValueIds.Contains(color.Id) && variant.OptionValueIds.Contains(size.Id));
            candidates.Add(new BulkVariantCandidate(size.Id, size.Value, !exists, exists ? "A sellable Variant already exists." : null));
        }

        var canConfirm = candidates.Any(value => value.WillCreate);
        return new BulkVariantPreview(request, canConfirm, canConfirm ? null : "There are no new valid Variants to add.", candidates);
    }

    private static OfferingOptionValue? FindTypedValue(WorkspaceSnapshot snapshot, Guid offeringId, Guid valueId, OptionKind kind)
    {
        var value = snapshot.OfferingOptionValues.SingleOrDefault(candidate => candidate.Id == valueId && candidate.OfferingId == offeringId && !candidate.IsArchived);
        var option = value is null ? null : snapshot.OfferingOptions.SingleOrDefault(candidate => candidate.Id == value.OptionId && candidate.OfferingId == offeringId && !candidate.IsArchived);
        return option?.OptionKind == kind ? value : null;
    }

    private static (FusionCanvas.Domain.Stores.Store Store, Blueprint Blueprint, BlueprintOffering Offering) RequireContext(WorkspaceSnapshot snapshot, OfferingContext context)
    {
        var store = snapshot.Stores.SingleOrDefault(value => value.Id == context.StoreId) ?? throw new InvalidOperationException("Store was not found.");
        var blueprint = RequireBlueprint(snapshot, context.StoreId, context.BlueprintId);
        var offering = snapshot.BlueprintOfferings.SingleOrDefault(value => value.Id == context.OfferingId && value.StoreId == context.StoreId && value.BlueprintId == context.BlueprintId)
            ?? throw new InvalidOperationException("Blueprint Offering was not found in the selected Store and Blueprint.");
        return (store, blueprint, offering);
    }

    private static FocusedCommandResult Success(WorkspaceSnapshot snapshot, OfferingContext context) => new(true, null, BuildOfferingState(snapshot, context));
    private static FocusedCommandResult Failure(WorkspaceSnapshot snapshot, OfferingContext context, string error, IReadOnlyList<string>? details = null) => new(false, error, BuildOfferingState(snapshot, context), details);

    private static Blueprint RequireBlueprint(WorkspaceSnapshot snapshot, Guid storeId, Guid blueprintId) =>
        snapshot.Blueprints.SingleOrDefault(value => value.Id == blueprintId && value.StoreId == storeId)
        ?? throw new InvalidOperationException("Blueprint was not found in the selected Store.");

    private static BlueprintOfferingSetupSummary ToSummary(WorkspaceSnapshot snapshot, BlueprintOffering offering)
    {
        var fulfillment = offering.Kind == BlueprintOfferingKind.FixedPrintProvider
            ? FixedProviderContext(snapshot, offering)
            : new OfferingFulfillmentContext(offering.Kind, ProviderNetworkName(offering.ProviderNetworkCode), true);
        return new BlueprintOfferingSetupSummary(
            new OfferingContext(offering.StoreId, offering.BlueprintId, offering.Id),
            offering.Name,
            offering.Description,
            offering.IsArchived,
            fulfillment,
            new OfferingSetupCounts(
                snapshot.OfferingVariants.Count(value => value.OfferingId == offering.Id && !value.IsArchived),
                snapshot.OfferingPlaceholders.Count(value => value.OfferingId == offering.Id && !value.IsArchived),
                snapshot.MockupTemplates.Count(value => value.BlueprintOfferingId == offering.Id && !value.IsArchived)));
    }

    private static OfferingFulfillmentContext FixedProviderContext(WorkspaceSnapshot snapshot, BlueprintOffering offering)
    {
        var provider = offering.PrintProviderId is Guid providerId
            ? snapshot.PrintProviders.SingleOrDefault(value => value.Id == providerId && value.StoreId == offering.StoreId)
            : null;
        return new OfferingFulfillmentContext(offering.Kind, provider?.Name ?? "Provider not configured", false);
    }

    private static string ProviderNetworkName(string? code) =>
        string.Equals(code, "printify-choice", StringComparison.OrdinalIgnoreCase) ? "Printify Choice Provider Network" : code ?? "Provider Network";
}

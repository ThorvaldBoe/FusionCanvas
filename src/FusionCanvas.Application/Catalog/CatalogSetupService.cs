using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Catalog;

public sealed class CatalogSetupService : ICatalogSetupService
{
    private readonly IWorkspaceRepository _repository;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<Guid> _newId;

    public CatalogSetupService(IWorkspaceRepository repository, Func<DateTimeOffset>? clock = null, Func<Guid>? newId = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _newId = newId ?? Guid.NewGuid;
    }

    public async Task<CatalogSetupState> LoadForStoreAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var synchronized = CatalogCompatibilitySynchronizer.SynchronizeStore(snapshot, storeId, _clock, _newId);
        if (synchronized.Changed)
        {
            await _repository.SaveAsync(synchronized.Snapshot, cancellationToken).ConfigureAwait(false);
        }

        return BuildState(synchronized.Snapshot, storeId);
    }

    public Task<CatalogSetupResult> CreateBlueprintAsync(CreateBlueprintRequest request, CancellationToken cancellationToken = default) =>
        MutateAsync(request.StoreId, snapshot =>
        {
            var storeCheck = EnsureWritableStore(snapshot, request.StoreId);
            if (storeCheck is not null) return Failure(snapshot, request.StoreId, storeCheck);
            if (snapshot.Blueprints.Any(value => value.StoreId == request.StoreId && !value.IsArchived && string.Equals(value.Name, request.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                return Failure(snapshot, request.StoreId, "An active Blueprint already uses this name in the Store.");
            var now = _clock();
            return Success(snapshot with { Blueprints = [.. snapshot.Blueprints, new Blueprint(_newId(), request.StoreId, request.Name, request.Description, false, now, now)] }, request.StoreId);
        }, cancellationToken);

    public Task<CatalogSetupResult> CreatePrintProviderAsync(CreatePrintProviderRequest request, CancellationToken cancellationToken = default) =>
        MutateAsync(request.StoreId, snapshot =>
        {
            var storeCheck = EnsureWritableStore(snapshot, request.StoreId);
            if (storeCheck is not null) return Failure(snapshot, request.StoreId, storeCheck);
            var now = _clock();
            return Success(snapshot with { PrintProviders = [.. snapshot.PrintProviders, new PrintProvider(_newId(), request.StoreId, request.Name, request.ExternalProviderId, false, now, now)] }, request.StoreId);
        }, cancellationToken);

    public Task<CatalogSetupResult> CreateOfferingAsync(CreateOfferingRequest request, CancellationToken cancellationToken = default) =>
        MutateAsync(request.StoreId, snapshot =>
        {
            var storeCheck = EnsureWritableStore(snapshot, request.StoreId);
            if (storeCheck is not null) return Failure(snapshot, request.StoreId, storeCheck);
            var blueprint = snapshot.Blueprints.SingleOrDefault(value => value.Id == request.BlueprintId && value.StoreId == request.StoreId);
            if (blueprint is null) return Failure(snapshot, request.StoreId, "Blueprint was not found in this Store.");
            if (request.Kind == BlueprintOfferingKind.FixedPrintProvider)
            {
                if (request.PrintProviderId is not Guid providerId || !snapshot.PrintProviders.Any(value => value.Id == providerId && value.StoreId == request.StoreId && !value.IsArchived))
                    return Failure(snapshot, request.StoreId, "A fixed-provider offering requires an active Print Provider from this Store.");
            }
            if (request.Kind == BlueprintOfferingKind.ProviderNetwork && string.IsNullOrWhiteSpace(request.ProviderNetworkCode))
                return Failure(snapshot, request.StoreId, "A Provider-Network offering requires a stable provider-network code.");
            var now = _clock();
            var offering = new BlueprintOffering(_newId(), blueprint.Id, request.StoreId, request.Name, request.Description, request.Kind, request.PrintProviderId, request.ProviderNetworkCode, null, request.ExternalOfferingId, false, now, now);
            return Success(snapshot with { BlueprintOfferings = [.. snapshot.BlueprintOfferings, offering] }, request.StoreId);
        }, cancellationToken);

    public Task<CatalogSetupResult> CreateOptionAsync(CreateOfferingOptionRequest request, CancellationToken cancellationToken = default) =>
        MutateAsync(null, snapshot =>
        {
            var offering = snapshot.BlueprintOfferings.SingleOrDefault(value => value.Id == request.OfferingId);
            if (offering is null) return Failure(snapshot, null, "Offering was not found.");
            var storeCheck = EnsureWritableStore(snapshot, offering.StoreId);
            if (storeCheck is not null) return Failure(snapshot, offering.StoreId, storeCheck);
            if (snapshot.Blueprints.Any(value => value.Id == offering.BlueprintId && value.IsArchived) || offering.IsArchived)
                return Failure(snapshot, offering.StoreId, "Archived catalog records are read-only.");
            if (snapshot.OfferingOptions.Any(value => value.OfferingId == request.OfferingId && value.OptionKind == request.OptionKind && !value.IsArchived))
                return Failure(snapshot, offering.StoreId, "An active Option with this semantic kind already exists for the offering.");
            var option = new OfferingOption(_newId(), request.OfferingId, request.OptionKind, request.Name, request.SortOrder);
            return Success(snapshot with { OfferingOptions = [.. snapshot.OfferingOptions, option] }, offering.StoreId);
        }, cancellationToken);

    public Task<CatalogSetupResult> CreateOptionValueAsync(CreateOptionValueRequest request, CancellationToken cancellationToken = default) =>
        MutateAsync(null, snapshot =>
        {
            var offering = snapshot.BlueprintOfferings.SingleOrDefault(value => value.Id == request.OfferingId);
            var option = snapshot.OfferingOptions.SingleOrDefault(value => value.Id == request.OptionId && value.OfferingId == request.OfferingId);
            if (offering is null || option is null) return Failure(snapshot, offering?.StoreId, "Option or offering was not found.");
            var storeCheck = EnsureWritableStore(snapshot, offering.StoreId);
            if (storeCheck is not null) return Failure(snapshot, offering.StoreId, storeCheck);
            if (offering.IsArchived || option.IsArchived) return Failure(snapshot, offering.StoreId, "Archived catalog records are read-only.");
            var normalizedValue = request.Value.Trim();
            if (string.IsNullOrWhiteSpace(normalizedValue)) return Failure(snapshot, offering.StoreId, "A value is required.");
            if (HasActiveOptionValue(snapshot, option.Id, normalizedValue)) return Failure(snapshot, offering.StoreId, "An active Option Value already uses this name.");
            var value = new OfferingOptionValue(_newId(), option.Id, offering.Id, normalizedValue, request.SortOrder);
            return Success(snapshot with { OfferingOptionValues = [.. snapshot.OfferingOptionValues, value] }, offering.StoreId);
        }, cancellationToken);

    public Task<CatalogSetupResult> CreateVariantAsync(CreateOfferingVariantRequest request, CancellationToken cancellationToken = default) =>
        MutateAsync(null, snapshot =>
        {
            var offering = snapshot.BlueprintOfferings.SingleOrDefault(value => value.Id == request.OfferingId);
            if (offering is null) return Failure(snapshot, null, "Offering was not found.");
            var storeCheck = EnsureWritableStore(snapshot, offering.StoreId);
            if (storeCheck is not null) return Failure(snapshot, offering.StoreId, storeCheck);
            if (offering.IsArchived) return Failure(snapshot, offering.StoreId, "Archived catalog records are read-only.");
            var values = snapshot.OfferingOptionValues.Where(value => request.OptionValueIds.Contains(value.Id)).ToArray();
            if (values.Length != request.OptionValueIds.Distinct().Count() || values.Any(value => value.OfferingId != offering.Id))
                return Failure(snapshot, offering.StoreId, "A Variant may only use Option Values from its own offering.");
            if (values.Any(value => value.IsArchived || snapshot.OfferingOptions.Any(option => option.Id == value.OptionId && option.IsArchived)))
                return Failure(snapshot, offering.StoreId, "Archived Option Values cannot be used in a Variant.");
            var optionIds = values.Select(value => value.OptionId).ToArray();
            if (optionIds.Length != optionIds.Distinct().Count()) return Failure(snapshot, offering.StoreId, "A Variant cannot contain two values from the same Option.");
            var combination = values.Select(value => value.Id).OrderBy(value => value).ToArray();
            if (snapshot.OfferingVariants.Where(value => value.OfferingId == offering.Id && !value.IsArchived).Any(value => value.OptionValueIds.OrderBy(id => id).SequenceEqual(combination)))
                return Failure(snapshot, offering.StoreId, "An active Variant with this Option Value combination already exists.");
            var now = _clock();
            var variant = new OfferingVariant(_newId(), offering.Id, request.Name, request.OptionValueIds, false, now, now);
            return Success(snapshot with { OfferingVariants = [.. snapshot.OfferingVariants, variant] }, offering.StoreId);
        }, cancellationToken);

    public Task<CatalogSetupResult> CreatePlaceholderAsync(CreateOfferingPlaceholderRequest request, CancellationToken cancellationToken = default) =>
        MutateAsync(null, snapshot =>
        {
            var offering = snapshot.BlueprintOfferings.SingleOrDefault(value => value.Id == request.OfferingId);
            if (offering is null) return Failure(snapshot, null, "Offering was not found.");
            var storeCheck = EnsureWritableStore(snapshot, offering.StoreId);
            if (storeCheck is not null) return Failure(snapshot, offering.StoreId, storeCheck);
            if (offering.IsArchived) return Failure(snapshot, offering.StoreId, "Archived catalog records are read-only.");
            var variantIds = request.UseAllActiveVariants
                ? snapshot.OfferingVariants.Where(value => value.OfferingId == offering.Id && !value.IsArchived).Select(value => value.Id).ToArray()
                : request.VariantIds.Distinct().ToArray();
            if (variantIds.Any(id => snapshot.OfferingVariants.All(value => value.Id != id || value.OfferingId != offering.Id)))
                return Failure(snapshot, offering.StoreId, "Placeholder compatibility must reference Variants from the same offering.");
            if (variantIds.Any(id => snapshot.OfferingVariants.Any(value => value.Id == id && value.IsArchived)))
                return Failure(snapshot, offering.StoreId, "Placeholder compatibility cannot reference archived Variants.");
            var now = _clock();
            OfferingPlaceholder placeholder;
            try
            {
                placeholder = new OfferingPlaceholder(_newId(), offering.Id, request.Name, request.Description, request.Position, request.DecorationMethod, request.Width, request.Height, variantIds, false, now, now, providerReference: request.ProviderReference, artworkGuidance: request.ArtworkGuidance);
            }
            catch (ArgumentException exception)
            {
                return Failure(snapshot, offering.StoreId, exception.Message);
            }
            return Success(snapshot with { OfferingPlaceholders = [.. snapshot.OfferingPlaceholders, placeholder] }, offering.StoreId);
        }, cancellationToken);

    public Task<CatalogSetupResult> ArchiveAsync(ArchiveCatalogRecordRequest request, CancellationToken cancellationToken = default) =>
        MutateAsync(request.StoreId, snapshot =>
        {
            var check = EnsureWritableStore(snapshot, request.StoreId);
            if (check is not null) return Failure(snapshot, request.StoreId, check);
            var dependencyError = GetDependencyError(snapshot, request);
            if (dependencyError is not null) return Failure(snapshot, request.StoreId, dependencyError);
            var updated = request.Kind switch
            {
                CatalogRecordKind.Blueprint => snapshot with { Blueprints = snapshot.Blueprints.Select(value => value.Id == request.RecordId ? value with { IsArchived = true } : value).ToArray() },
                CatalogRecordKind.PrintProvider => snapshot with { PrintProviders = snapshot.PrintProviders.Select(value => value.Id == request.RecordId ? value with { IsArchived = true } : value).ToArray() },
                CatalogRecordKind.Offering => snapshot with { BlueprintOfferings = snapshot.BlueprintOfferings.Select(value => value.Id == request.RecordId ? value with { IsArchived = true } : value).ToArray() },
                CatalogRecordKind.Option => snapshot with { OfferingOptions = snapshot.OfferingOptions.Select(value => value.Id == request.RecordId ? value with { IsArchived = true } : value).ToArray() },
                CatalogRecordKind.OptionValue => snapshot with { OfferingOptionValues = snapshot.OfferingOptionValues.Select(value => value.Id == request.RecordId ? value with { IsArchived = true } : value).ToArray() },
                CatalogRecordKind.Variant => snapshot with { OfferingVariants = snapshot.OfferingVariants.Select(value => value.Id == request.RecordId ? value with { IsArchived = true } : value).ToArray() },
                CatalogRecordKind.Placeholder => snapshot with { OfferingPlaceholders = snapshot.OfferingPlaceholders.Select(value => value.Id == request.RecordId ? value with { IsArchived = true } : value).ToArray() },
                _ => snapshot
            };
            return Success(updated, request.StoreId);
        }, cancellationToken);

    public Task<CatalogSetupResult> RestoreAsync(ArchiveCatalogRecordRequest request, CancellationToken cancellationToken = default) =>
        MutateAsync(request.StoreId, snapshot =>
        {
            var check = EnsureWritableStore(snapshot, request.StoreId);
            if (check is not null) return Failure(snapshot, request.StoreId, check);
            var updated = request.Kind switch
            {
                CatalogRecordKind.Blueprint => snapshot with { Blueprints = snapshot.Blueprints.Select(value => value.Id == request.RecordId ? value with { IsArchived = false } : value).ToArray() },
                CatalogRecordKind.PrintProvider => snapshot with { PrintProviders = snapshot.PrintProviders.Select(value => value.Id == request.RecordId ? value with { IsArchived = false } : value).ToArray() },
                CatalogRecordKind.Offering => snapshot with { BlueprintOfferings = snapshot.BlueprintOfferings.Select(value => value.Id == request.RecordId ? value with { IsArchived = false } : value).ToArray() },
                CatalogRecordKind.Option => snapshot with { OfferingOptions = snapshot.OfferingOptions.Select(value => value.Id == request.RecordId ? value with { IsArchived = false } : value).ToArray() },
                CatalogRecordKind.OptionValue => snapshot with { OfferingOptionValues = snapshot.OfferingOptionValues.Select(value => value.Id == request.RecordId ? value with { IsArchived = false } : value).ToArray() },
                CatalogRecordKind.Variant => snapshot with { OfferingVariants = snapshot.OfferingVariants.Select(value => value.Id == request.RecordId ? value with { IsArchived = false } : value).ToArray() },
                CatalogRecordKind.Placeholder => snapshot with { OfferingPlaceholders = snapshot.OfferingPlaceholders.Select(value => value.Id == request.RecordId ? value with { IsArchived = false } : value).ToArray() },
                _ => snapshot
            };
            return Success(updated, request.StoreId);
        }, cancellationToken);

    public Task<CatalogSetupResult> DeleteAsync(ArchiveCatalogRecordRequest request, CancellationToken cancellationToken = default) =>
        ArchiveAsync(request, cancellationToken);

    public Task<CatalogSetupResult> UpdateAsync(UpdateCatalogRecordRequest request, CancellationToken cancellationToken = default) =>
        MutateAsync(request.StoreId, snapshot =>
        {
            var check = EnsureWritableStore(snapshot, request.StoreId);
            if (check is not null) return Failure(snapshot, request.StoreId, check);
            var now = _clock();
            var recordExists = request.Kind switch
            {
                CatalogRecordKind.Blueprint => snapshot.Blueprints.Any(value => value.Id == request.RecordId && value.StoreId == request.StoreId),
                CatalogRecordKind.PrintProvider => snapshot.PrintProviders.Any(value => value.Id == request.RecordId && value.StoreId == request.StoreId),
                CatalogRecordKind.Offering => snapshot.BlueprintOfferings.Any(value => value.Id == request.RecordId && value.StoreId == request.StoreId),
                CatalogRecordKind.Option => snapshot.OfferingOptions.Any(value => value.Id == request.RecordId),
                CatalogRecordKind.OptionValue => snapshot.OfferingOptionValues.Any(value => value.Id == request.RecordId),
                CatalogRecordKind.Variant => snapshot.OfferingVariants.Any(value => value.Id == request.RecordId),
                CatalogRecordKind.Placeholder => snapshot.OfferingPlaceholders.Any(value => value.Id == request.RecordId),
                _ => false
            };
            if (!recordExists) return Failure(snapshot, request.StoreId, "Catalog record was not found.");
            var ownerStoreId = request.Kind switch
            {
                CatalogRecordKind.Blueprint => snapshot.Blueprints.Single(value => value.Id == request.RecordId).StoreId,
                CatalogRecordKind.PrintProvider => snapshot.PrintProviders.Single(value => value.Id == request.RecordId).StoreId,
                CatalogRecordKind.Offering => snapshot.BlueprintOfferings.Single(value => value.Id == request.RecordId).StoreId,
                CatalogRecordKind.Option => snapshot.BlueprintOfferings.Single(offering => offering.Id == snapshot.OfferingOptions.Single(value => value.Id == request.RecordId).OfferingId).StoreId,
                CatalogRecordKind.OptionValue => snapshot.BlueprintOfferings.Single(offering => offering.Id == snapshot.OfferingOptionValues.Single(value => value.Id == request.RecordId).OfferingId).StoreId,
                CatalogRecordKind.Variant => snapshot.BlueprintOfferings.Single(offering => offering.Id == snapshot.OfferingVariants.Single(value => value.Id == request.RecordId).OfferingId).StoreId,
                CatalogRecordKind.Placeholder => snapshot.BlueprintOfferings.Single(offering => offering.Id == snapshot.OfferingPlaceholders.Single(value => value.Id == request.RecordId).OfferingId).StoreId,
                _ => request.StoreId
            };
            if (ownerStoreId != request.StoreId) return Failure(snapshot, request.StoreId, "Catalog record does not belong to this Store.");
            if (request.Kind == CatalogRecordKind.Offering && request.DefaultPlaceholderId is Guid placeholderId &&
                snapshot.OfferingPlaceholders.All(value => value.Id != placeholderId || value.OfferingId != request.RecordId || value.IsArchived))
                return Failure(snapshot, request.StoreId, "The default Placeholder must belong to the offering and be active.");
            if (request.Kind == CatalogRecordKind.Offering && request.ProviderNetworkCode is not null)
            {
                var offering = snapshot.BlueprintOfferings.Single(value => value.Id == request.RecordId);
                if (offering.Kind != BlueprintOfferingKind.ProviderNetwork)
                    return Failure(snapshot, request.StoreId, "Only a Provider-Network offering has a provider-network code.");
                if (string.IsNullOrWhiteSpace(request.ProviderNetworkCode))
                    return Failure(snapshot, request.StoreId, "A Provider-Network offering requires a stable provider-network code.");
            }
            if (request.Kind == CatalogRecordKind.Offering && request.PrintProviderId is Guid printProviderId)
            {
                var offering = snapshot.BlueprintOfferings.Single(value => value.Id == request.RecordId);
                if (offering.Kind != BlueprintOfferingKind.FixedPrintProvider)
                    return Failure(snapshot, request.StoreId, "Only a fixed-provider offering can select a Print Provider.");
                if (!snapshot.PrintProviders.Any(value => value.Id == printProviderId && value.StoreId == request.StoreId && !value.IsArchived))
                    return Failure(snapshot, request.StoreId, "The selected Print Provider must be active and belong to this Store.");
            }
            if (request.Kind == CatalogRecordKind.OptionValue)
            {
                var optionValue = snapshot.OfferingOptionValues.Single(value => value.Id == request.RecordId);
                var normalizedValue = request.Name?.Trim();
                if (string.IsNullOrWhiteSpace(normalizedValue)) return Failure(snapshot, request.StoreId, "A value is required.");
                if (HasActiveOptionValue(snapshot, optionValue.OptionId, normalizedValue, optionValue.Id))
                    return Failure(snapshot, request.StoreId, "An active Option Value already uses this name.");
            }
            var updated = request.Kind switch
            {
                CatalogRecordKind.Blueprint => snapshot with { Blueprints = snapshot.Blueprints.Select(value => value.Id == request.RecordId && value.StoreId == request.StoreId ? value with { Name = Required(request.Name, value.Name), Description = request.Description ?? value.Description, UpdatedAt = now } : value).ToArray() },
                CatalogRecordKind.PrintProvider => snapshot with { PrintProviders = snapshot.PrintProviders.Select(value => value.Id == request.RecordId && value.StoreId == request.StoreId ? value with { Name = Required(request.Name, value.Name), UpdatedAt = now } : value).ToArray() },
                CatalogRecordKind.Offering => snapshot with { BlueprintOfferings = snapshot.BlueprintOfferings.Select(value => value.Id == request.RecordId && value.StoreId == request.StoreId ? value with { Name = Required(request.Name, value.Name), Description = request.Description ?? value.Description, PrintProviderId = request.PrintProviderId ?? value.PrintProviderId, ProviderNetworkCode = request.ProviderNetworkCode?.Trim().ToLowerInvariant() ?? value.ProviderNetworkCode, DefaultPlaceholderId = request.DefaultPlaceholderId ?? value.DefaultPlaceholderId, ExternalOfferingId = request.ExternalOfferingId ?? value.ExternalOfferingId, UpdatedAt = now } : value).ToArray() },
                CatalogRecordKind.Option => snapshot with { OfferingOptions = snapshot.OfferingOptions.Select(value => value.Id == request.RecordId ? value with { Name = Required(request.Name, value.Name) } : value).ToArray() },
                CatalogRecordKind.OptionValue => snapshot with { OfferingOptionValues = snapshot.OfferingOptionValues.Select(value => value.Id == request.RecordId ? value with { Value = request.Name!.Trim() } : value).ToArray() },
                CatalogRecordKind.Variant => snapshot with { OfferingVariants = snapshot.OfferingVariants.Select(value => value.Id == request.RecordId ? value with { Name = Required(request.Name, value.Name), UpdatedAt = now } : value).ToArray() },
                CatalogRecordKind.Placeholder => snapshot with { OfferingPlaceholders = snapshot.OfferingPlaceholders.Select(value => value.Id == request.RecordId ? value with { Name = Required(request.Name, value.Name), Description = request.Description ?? value.Description, Position = request.Position ?? value.Position, DecorationMethod = request.DecorationMethod ?? value.DecorationMethod, Width = request.Width ?? value.Width, Height = request.Height ?? value.Height, UpdatedAt = now } : value).ToArray() },
                _ => snapshot
            };
            return Success(updated, request.StoreId);
        }, cancellationToken);

    private async Task<CatalogSetupResult> MutateAsync(Guid? storeId, Func<WorkspaceSnapshot, CatalogSetupResult> mutation, CancellationToken cancellationToken)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var result = mutation(snapshot);
        if (!result.Succeeded) return result;
        var targetStoreId = storeId ?? result.State.StoreId;
        var synchronized = CatalogCompatibilitySynchronizer.SynchronizeStore(ToSnapshot(result), targetStoreId, _clock, _newId);
        await _repository.SaveAsync(synchronized.Snapshot, cancellationToken).ConfigureAwait(false);
        return CatalogSetupResult.Success(BuildState(synchronized.Snapshot, targetStoreId));
    }

    private static WorkspaceSnapshot ToSnapshot(CatalogSetupResult result) => result.Snapshot ?? throw new InvalidOperationException("Catalog mutation did not produce a snapshot.");

    private static string? EnsureWritableStore(WorkspaceSnapshot snapshot, Guid storeId)
    {
        var store = snapshot.Stores.SingleOrDefault(value => value.Id == storeId);
        return store is null ? "Store was not found." : store.IsArchived ? "Archived Store catalogs are read-only." : null;
    }

    private static string Required(string? requested, string current) => string.IsNullOrWhiteSpace(requested) ? current : requested.Trim();

    private static bool HasActiveOptionValue(WorkspaceSnapshot snapshot, Guid optionId, string value, Guid? excludingId = null) =>
        snapshot.OfferingOptionValues.Any(candidate => candidate.OptionId == optionId
            && !candidate.IsArchived
            && candidate.Id != excludingId
            && string.Equals(candidate.Value.Trim(), value, StringComparison.OrdinalIgnoreCase));

    private static string? GetDependencyError(WorkspaceSnapshot snapshot, ArchiveCatalogRecordRequest request)
    {
        var active = request.Kind switch
        {
            CatalogRecordKind.Blueprint => snapshot.BlueprintOfferings.Any(value => value.BlueprintId == request.RecordId && !value.IsArchived),
            CatalogRecordKind.PrintProvider => snapshot.BlueprintOfferings.Any(value => value.PrintProviderId == request.RecordId && !value.IsArchived),
            CatalogRecordKind.Offering => snapshot.OfferingOptions.Any(value => value.OfferingId == request.RecordId && !value.IsArchived)
                || snapshot.OfferingVariants.Any(value => value.OfferingId == request.RecordId && !value.IsArchived)
                || snapshot.OfferingPlaceholders.Any(value => value.OfferingId == request.RecordId && !value.IsArchived)
                || snapshot.MockupTemplates.Any(value => value.BlueprintOfferingId == request.RecordId && !value.IsArchived),
            CatalogRecordKind.Option => snapshot.OfferingOptionValues.Any(value => value.OptionId == request.RecordId && !value.IsArchived),
            CatalogRecordKind.OptionValue => snapshot.OfferingVariants.Any(value => value.OptionValueIds.Contains(request.RecordId) && !value.IsArchived)
                || snapshot.MockupTemplateColorVariants.Any(value => value.ColorOptionValueId == request.RecordId && !value.IsArchived),
            CatalogRecordKind.Variant => snapshot.OfferingPlaceholders.Any(value => value.VariantIds.Contains(request.RecordId) && !value.IsArchived),
            CatalogRecordKind.Placeholder => snapshot.MockupTemplates.Any(value => value.TargetPlaceholderId == request.RecordId && !value.IsArchived),
            _ => false
        };
        return active ? "This record is referenced by active catalog configuration. Archive or reassign dependents first." : null;
    }

    private static CatalogSetupResult Failure(WorkspaceSnapshot snapshot, Guid? storeId, string error) => CatalogSetupResult.Failure(error, BuildState(snapshot, storeId ?? Guid.Empty));
    private static CatalogSetupResult Success(WorkspaceSnapshot snapshot, Guid storeId) => new(true, null, BuildState(snapshot, storeId), snapshot);

    private static CatalogSetupState BuildState(WorkspaceSnapshot snapshot, Guid storeId) => new(
        storeId,
        snapshot.Stores.SingleOrDefault(value => value.Id == storeId)?.IsArchived ?? false,
        snapshot.Blueprints.Where(value => value.StoreId == storeId).ToArray(),
        snapshot.PrintProviders.Where(value => value.StoreId == storeId).ToArray(),
        snapshot.BlueprintOfferings.Where(value => value.StoreId == storeId).ToArray(),
        snapshot.OfferingOptions.Where(value => snapshot.BlueprintOfferings.Any(offering => offering.Id == value.OfferingId && offering.StoreId == storeId)).ToArray(),
        snapshot.OfferingOptionValues.Where(value => snapshot.BlueprintOfferings.Any(offering => offering.Id == value.OfferingId && offering.StoreId == storeId)).ToArray(),
        snapshot.OfferingVariants.Where(value => snapshot.BlueprintOfferings.Any(offering => offering.Id == value.OfferingId && offering.StoreId == storeId)).ToArray(),
        snapshot.OfferingPlaceholders.Where(value => snapshot.BlueprintOfferings.Any(offering => offering.Id == value.OfferingId && offering.StoreId == storeId)).ToArray());
}

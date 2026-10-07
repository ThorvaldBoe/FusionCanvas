using System.Text.Json;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Listings;

public sealed class ListingLifecycleService
{
    private const string ProviderKey = "printify";
    private readonly IWorkspaceRepository _repository;
    private readonly IListingConnectionPort _connection;
    private readonly IListingProductPort _products;
    private readonly IListingPublicationPort _publication;
    private readonly IListingImagePort? _images;
    private readonly Func<DateTimeOffset> _clock;

    public ListingLifecycleService(
        IWorkspaceRepository repository,
        IListingConnectionPort connection,
        IListingProductPort products,
        IListingPublicationPort publication,
        Func<DateTimeOffset>? clock = null,
        IListingImagePort? images = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _products = products ?? throw new ArgumentNullException(nameof(products));
        _publication = publication ?? throw new ArgumentNullException(nameof(publication));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _images = images;
    }

    public async Task<ListingLifecycleResult> CreateOrUpdateAsync(
        ListingConnectionRequest connectionRequest,
        ListingProductProjection projection,
        CancellationToken cancellationToken = default)
        => await CreateOrUpdateCoreAsync(connectionRequest, projection, forceLocal: false, cancellationToken).ConfigureAwait(false);

    public ListingProjectionResult Preview(ListingProjectionRequest request) =>
        ListingProjectionBuilder.Build(request);

    public Task<ListingReadiness> CheckConnectionAsync(
        ListingConnectionRequest request,
        CancellationToken cancellationToken = default) =>
        _connection.CheckAsync(request, cancellationToken);

    public async Task<ListingLifecycleResult> RefreshAsync(
        ListingConnectionRequest request,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var readiness = await _connection.CheckAsync(request, cancellationToken).ConfigureAwait(false);
        if (!readiness.ProductOperationsAvailable)
            return new(ListingLifecycleResultKind.Unavailable, Message: readiness.Issues.FirstOrDefault()?.Message ?? "Printify is unavailable.");

        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.ExternalListingMappings.SingleOrDefault(value => value.StoreId == request.StoreId && value.ItemId == itemId);
        if (existing?.ProductId is null)
            return new(ListingLifecycleResultKind.Missing, Message: "There is no mapped Printify product to refresh.");

        ListingRemoteProduct? remote;
        try
        {
            remote = await _products.GetAsync(request.ShopId, existing.ProductId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new(ListingLifecycleResultKind.Unavailable, ToApplicationMapping(existing), Message: $"Printify could not be refreshed. {exception.Message}");
        }

        if (remote is null)
        {
            var missing = existing with
            {
                SynchronizationState = ExternalListingSyncState.Missing,
                OperationState = ExternalListingOperationState.None,
                UpdatedAt = _clock()
            };
            await _repository.SaveAsync(ReplaceMapping(snapshot, missing), cancellationToken).ConfigureAwait(false);
            return new(ListingLifecycleResultKind.Missing, ToApplicationMapping(missing), Message: "The mapped Printify product could not be found.");
        }

        var comparison = string.IsNullOrWhiteSpace(existing.SnapshotJson)
            ? ListingDriftComparison.Empty
            : ListingDriftComparer.Compare(DeserializeSnapshot(existing.SnapshotJson), remote.Snapshot, DeserializeSnapshot(existing.SnapshotJson));
        if (!comparison.IsClean)
        {
            var changed = existing with
            {
                SynchronizationState = ExternalListingSyncState.RemoteChanged,
                OperationState = ExternalListingOperationState.None,
                UpdatedAt = _clock()
            };
            await _repository.SaveAsync(ReplaceMapping(snapshot, changed), cancellationToken).ConfigureAwait(false);
            return new(ListingLifecycleResultKind.Conflict, ToApplicationMapping(changed), new ListingLifecycleConflict(comparison), "Printify has changes that require a decision.");
        }

        var synchronized = existing with
        {
            ExternalPublicationId = remote.ExternalSalesChannelId ?? existing.ExternalPublicationId,
            ExternalHandle = remote.ExternalHandle ?? existing.ExternalHandle,
            PublicationState = (ExternalListingPublicationState)remote.PublicationState,
            SynchronizationState = ExternalListingSyncState.Synchronized,
            OperationState = ExternalListingOperationState.Succeeded,
            SnapshotJson = SerializeSnapshot(remote.Snapshot),
            LastSynchronizedAt = _clock(),
            UpdatedAt = _clock()
        };
        await _repository.SaveAsync(ReplaceMapping(snapshot, synchronized), cancellationToken).ConfigureAwait(false);
        return new(ListingLifecycleResultKind.Succeeded, ToApplicationMapping(synchronized), Message: "Printify listing refreshed.");
    }

    public async Task<ListingLifecycleResult> ReconcileAsync(
        ListingConnectionRequest request,
        ListingProductProjection projection,
        ListingConflictResolution resolution,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(projection);

        if (resolution == ListingConflictResolution.KeepLocal)
            return await CreateOrUpdateCoreAsync(request, projection, forceLocal: true, cancellationToken).ConfigureAwait(false);

        var readiness = await _connection.CheckAsync(request, cancellationToken).ConfigureAwait(false);
        if (!readiness.ProductOperationsAvailable)
            return new(ListingLifecycleResultKind.Unavailable, Message: readiness.Issues.FirstOrDefault()?.Message ?? "Printify is unavailable.");

        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.ExternalListingMappings.SingleOrDefault(value => value.StoreId == request.StoreId && value.ItemId == projection.ItemId);
        if (existing?.ProductId is null)
            return new(ListingLifecycleResultKind.Missing, Message: "There is no mapped Printify product to reconcile.");

        ListingRemoteProduct? remote;
        try
        {
            remote = await _products.GetAsync(request.ShopId, existing.ProductId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var uncertain = existing with
            {
                SynchronizationState = ExternalListingSyncState.Uncertain,
                OperationState = ExternalListingOperationState.NeedsReconciliation,
                OperationJson = JsonSerializer.Serialize(new { kind = ListingOperationKind.Update, error = exception.Message }),
                UpdatedAt = _clock()
            };
            await _repository.SaveAsync(ReplaceMapping(snapshot, uncertain), cancellationToken).ConfigureAwait(false);
            return new(ListingLifecycleResultKind.Uncertain, ToApplicationMapping(uncertain), Message: "The remote Printify values could not be read safely. Retry reconciliation when the connection is available.");
        }
        if (remote is null)
        {
            var missing = existing with { SynchronizationState = ExternalListingSyncState.Missing, OperationState = ExternalListingOperationState.None, UpdatedAt = _clock() };
            await _repository.SaveAsync(ReplaceMapping(snapshot, missing), cancellationToken).ConfigureAwait(false);
            return new(ListingLifecycleResultKind.Missing, ToApplicationMapping(missing), Message: "The mapped Printify product could not be found.");
        }

        var accepted = existing with
        {
            ExternalPublicationId = remote.ExternalSalesChannelId ?? existing.ExternalPublicationId,
            ExternalHandle = remote.ExternalHandle ?? existing.ExternalHandle,
            PublicationState = (ExternalListingPublicationState)remote.PublicationState,
            SynchronizationState = ExternalListingSyncState.Synchronized,
            OperationState = ExternalListingOperationState.Succeeded,
            SnapshotJson = SerializeSnapshot(remote.Snapshot),
            IntegrationValuesJson = SerializeIntegrationValues(IntegrationValuesFromSnapshot(remote.Snapshot)),
            OperationJson = null,
            LastSynchronizedAt = _clock(),
            UpdatedAt = _clock()
        };
        await _repository.SaveAsync(ReplaceMapping(snapshot, accepted), cancellationToken).ConfigureAwait(false);
        return new(ListingLifecycleResultKind.Succeeded, ToApplicationMapping(accepted), Message: "Remote Printify values were accepted. Design colors and source artwork were unchanged.");
    }

    private async Task<ListingLifecycleResult> CreateOrUpdateCoreAsync(
        ListingConnectionRequest connectionRequest,
        ListingProductProjection projection,
        bool forceLocal,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connectionRequest);
        ArgumentNullException.ThrowIfNull(projection);

        var readiness = await _connection.CheckAsync(connectionRequest, cancellationToken).ConfigureAwait(false);
        if (!readiness.ProductOperationsAvailable)
        {
            return new(ListingLifecycleResultKind.Unavailable, Message: readiness.Issues.FirstOrDefault()?.Message ?? "Printify is unavailable.");
        }

        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.ExternalListingMappings
            .SingleOrDefault(value => value.StoreId == connectionRequest.StoreId && value.ItemId == projection.ItemId);
        var effectiveProjection = forceLocal ? projection : ApplyIntegrationValues(projection, existing?.IntegrationValuesJson);
        var localSnapshot = ToSnapshot(effectiveProjection);

        if (existing is not null
            && (existing.SynchronizationState == ExternalListingSyncState.Uncertain
                || existing.OperationState == ExternalListingOperationState.NeedsReconciliation))
        {
            return new(ListingLifecycleResultKind.Uncertain, ToApplicationMapping(existing), Message: "Reconcile the previous Printify operation before retrying.");
        }

        if (existing?.ProductId is not null)
        {
            ListingRemoteProduct? remote;
            try
            {
                remote = await _products.GetAsync(connectionRequest.ShopId, existing.ProductId, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                var unavailable = existing with
                {
                    SynchronizationState = ExternalListingSyncState.Uncertain,
                    OperationState = ExternalListingOperationState.NeedsReconciliation,
                    OperationJson = JsonSerializer.Serialize(new { kind = ListingOperationKind.Update, error = exception.Message }),
                    UpdatedAt = _clock()
                };
                await _repository.SaveAsync(ReplaceMapping(snapshot, unavailable), cancellationToken).ConfigureAwait(false);
                return new(ListingLifecycleResultKind.Uncertain, ToApplicationMapping(unavailable), Message: "The existing Printify listing could not be read safely. Refresh before retrying.");
            }

            if (remote is null)
            {
                var missing = await SaveMappingAsync(snapshot, existing with
                {
                    SynchronizationState = ExternalListingSyncState.Missing,
                    OperationState = ExternalListingOperationState.None,
                    UpdatedAt = _clock()
                }, cancellationToken).ConfigureAwait(false);
                return new(ListingLifecycleResultKind.Missing, ToApplicationMapping(missing), Message: "The mapped Printify product could not be found.");
            }

            if (!forceLocal && !string.IsNullOrWhiteSpace(existing.SnapshotJson))
            {
                var last = DeserializeSnapshot(existing.SnapshotJson);
                var comparison = ListingDriftComparer.Compare(last, remote.Snapshot, localSnapshot);
                if (comparison.HasRemoteOnlyChanges || comparison.HasConflict)
                {
                    return new(ListingLifecycleResultKind.Conflict, ToApplicationMapping(existing), new ListingLifecycleConflict(comparison), "Printify has changes that require a decision before updating.");
                }
            }
        }

        var startedAt = _clock();
        var intent = existing is null
            ? NewMapping(connectionRequest, effectiveProjection, startedAt)
            : existing with
            {
                OperationState = ExternalListingOperationState.IntentRecorded,
                SynchronizationState = ExternalListingSyncState.Pending,
                IntegrationValuesJson = forceLocal ? null : existing.IntegrationValuesJson,
                UpdatedAt = startedAt
            };
        var intentSnapshot = ReplaceMapping(snapshot, intent);
        await _repository.SaveAsync(intentSnapshot, cancellationToken).ConfigureAwait(false);

        IReadOnlyDictionary<Guid, ListingImageReference>? images;
        try
        {
            images = await ResolveImagesAsync(existing, effectiveProjection, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var uncertain = intent with
            {
                SynchronizationState = ExternalListingSyncState.Uncertain,
                OperationState = ExternalListingOperationState.NeedsReconciliation,
                OperationJson = JsonSerializer.Serialize(new { kind = existing?.ProductId is null ? ListingOperationKind.Create : ListingOperationKind.Update, error = exception.Message }),
                UpdatedAt = _clock()
            };
            await _repository.SaveAsync(ReplaceMapping(intentSnapshot, uncertain), cancellationToken).ConfigureAwait(false);
            return new(ListingLifecycleResultKind.Uncertain, ToApplicationMapping(uncertain), Message: "Artwork upload could not be verified. Refresh before retrying.");
        }
        if (images is null)
        {
            return new(ListingLifecycleResultKind.Failed, ToApplicationMapping(existing ?? intent), Message: "Artwork upload is not available for this Printify connection.");
        }
        ListingMutationResult mutation;
        try
        {
            mutation = existing?.ProductId is null
                ? await _products.CreateAsync(connectionRequest.ShopId, effectiveProjection, images, cancellationToken).ConfigureAwait(false)
                : await _products.UpdateAsync(connectionRequest.ShopId, existing.ProductId, effectiveProjection, images, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var uncertain = intent with
            {
                SynchronizationState = ExternalListingSyncState.Uncertain,
                OperationState = ExternalListingOperationState.NeedsReconciliation,
                OperationJson = JsonSerializer.Serialize(new { kind = existing?.ProductId is null ? ListingOperationKind.Create : ListingOperationKind.Update, error = exception.Message }),
                UpdatedAt = _clock()
            };
            await _repository.SaveAsync(ReplaceMapping(intentSnapshot, uncertain), cancellationToken).ConfigureAwait(false);
            return new(ListingLifecycleResultKind.Uncertain, ToApplicationMapping(uncertain), Message: "The Printify mutation could not be verified. Refresh before retrying.");
        }

        if (!mutation.IsDefinitive)
        {
            var uncertain = intent with
            {
                SynchronizationState = ExternalListingSyncState.Uncertain,
                OperationState = ExternalListingOperationState.NeedsReconciliation,
                OperationJson = JsonSerializer.Serialize(new { kind = existing?.ProductId is null ? ListingOperationKind.Create : ListingOperationKind.Update, mutation.ErrorCode }),
                UpdatedAt = _clock()
            };
            await _repository.SaveAsync(ReplaceMapping(intentSnapshot, uncertain), cancellationToken).ConfigureAwait(false);
            return new(ListingLifecycleResultKind.Uncertain, ToApplicationMapping(uncertain), Message: mutation.ErrorMessage ?? "The Printify response was ambiguous. Refresh before retrying.");
        }

        if (string.IsNullOrWhiteSpace(mutation.ProductId))
        {
            var failed = intent with
            {
                SynchronizationState = ExternalListingSyncState.Failed,
                OperationState = ExternalListingOperationState.Failed,
                OperationJson = JsonSerializer.Serialize(new { mutation.ErrorCode, mutation.ErrorMessage }),
                UpdatedAt = _clock()
            };
            await _repository.SaveAsync(ReplaceMapping(intentSnapshot, failed), cancellationToken).ConfigureAwait(false);
            return new(ListingLifecycleResultKind.Failed, ToApplicationMapping(failed), Message: mutation.ErrorMessage ?? "Printify did not return a product identity.");
        }

        var remoteProduct = mutation.Product;
        var succeeded = intent with
        {
            ProductId = mutation.ProductId,
            ExternalPublicationId = remoteProduct?.ExternalSalesChannelId,
            ExternalHandle = remoteProduct?.ExternalHandle,
            SynchronizationState = ExternalListingSyncState.Synchronized,
            PublicationState = remoteProduct is null
                ? intent.PublicationState
                : (ExternalListingPublicationState)remoteProduct.PublicationState,
            OperationState = ExternalListingOperationState.Succeeded,
            SnapshotJson = SerializeSnapshot(localSnapshot),
            UploadReferencesJson = JsonSerializer.Serialize(images),
            IntegrationValuesJson = forceLocal ? null : intent.IntegrationValuesJson,
            OperationJson = null,
            LastSynchronizedAt = _clock(),
            UpdatedAt = _clock()
        };
        await _repository.SaveAsync(ReplaceMapping(intentSnapshot, succeeded), cancellationToken).ConfigureAwait(false);
        return new(ListingLifecycleResultKind.Succeeded, ToApplicationMapping(succeeded), Message: "Printify listing synchronized.");
    }

    public async Task<ListingLifecycleResult> PublishAsync(
        ListingConnectionRequest connectionRequest,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        if (!connectionRequest.RequirePublication)
        {
            return new(ListingLifecycleResultKind.NotAllowed, Message: "Publishing is available only for Shopify + Printify.");
        }

        return await MutatePublicationAsync(connectionRequest, itemId, publish: true, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ListingLifecycleResult> UnpublishAsync(
        ListingConnectionRequest connectionRequest,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        if (!connectionRequest.RequirePublication)
        {
            return new(ListingLifecycleResultKind.NotAllowed, Message: "Unpublishing is available only for Shopify + Printify.");
        }

        return await MutatePublicationAsync(connectionRequest, itemId, publish: false, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ListingLifecycleResult> ArchiveLocallyAsync(
        Guid storeId,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.ExternalListingMappings.SingleOrDefault(value => value.StoreId == storeId && value.ItemId == itemId);
        if (existing is null)
            return new(ListingLifecycleResultKind.Missing, Message: "There is no Printify mapping to archive.");

        var archived = existing with
        {
            SynchronizationState = ExternalListingSyncState.Archived,
            OperationState = ExternalListingOperationState.None,
            UpdatedAt = _clock()
        };
        await _repository.SaveAsync(ReplaceMapping(snapshot, archived), cancellationToken).ConfigureAwait(false);
        return new(ListingLifecycleResultKind.Succeeded, ToApplicationMapping(archived), Message: "The local Printify mapping was archived. The remote product was not changed.");
    }

    public async Task<ListingLifecycleResult> DeleteRemoteAsync(
        ListingConnectionRequest request,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var readiness = await _connection.CheckAsync(request, cancellationToken).ConfigureAwait(false);
        if (!readiness.ProductOperationsAvailable)
            return new(ListingLifecycleResultKind.Unavailable, Message: readiness.Issues.FirstOrDefault()?.Message ?? "Printify is unavailable.");

        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.ExternalListingMappings.SingleOrDefault(value => value.StoreId == request.StoreId && value.ItemId == itemId);
        if (existing?.ProductId is null)
            return new(ListingLifecycleResultKind.Missing, Message: "There is no known remote Printify product to delete.");
        if (existing.SynchronizationState is ExternalListingSyncState.Uncertain or ExternalListingSyncState.Pending
            || existing.OperationState == ExternalListingOperationState.NeedsReconciliation)
            return new(ListingLifecycleResultKind.Uncertain, ToApplicationMapping(existing), Message: "Reconcile the previous Printify operation before attempting deletion.");
        if (existing.PublicationState is ExternalListingPublicationState.Published or ExternalListingPublicationState.Publishing or ExternalListingPublicationState.Unpublishing)
            return new(ListingLifecycleResultKind.NotAllowed, ToApplicationMapping(existing), Message: "Unpublish the Printify product and verify that it is no longer published before deleting it.");

        ListingRemoteProduct? remote;
        try
        {
            remote = await _products.GetAsync(request.ShopId, existing.ProductId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var uncertain = existing with
            {
                SynchronizationState = ExternalListingSyncState.Uncertain,
                OperationState = ExternalListingOperationState.NeedsReconciliation,
                OperationJson = JsonSerializer.Serialize(new { kind = ListingOperationKind.Delete, error = exception.Message }),
                UpdatedAt = _clock()
            };
            await _repository.SaveAsync(ReplaceMapping(snapshot, uncertain), cancellationToken).ConfigureAwait(false);
            return new(ListingLifecycleResultKind.Uncertain, ToApplicationMapping(uncertain), Message: "The Printify product could not be checked safely. Refresh before attempting deletion again.");
        }
        if (remote is null)
            return new(ListingLifecycleResultKind.Missing, ToApplicationMapping(existing), Message: "The mapped Printify product no longer exists.");
        if (remote.IsLocked)
            return new(ListingLifecycleResultKind.NotAllowed, ToApplicationMapping(existing), Message: "Printify has locked this product. Refresh later before attempting deletion.");
        if (remote.PublicationState == ListingPublicationState.Published)
            return new(ListingLifecycleResultKind.NotAllowed, ToApplicationMapping(existing), Message: "Printify still reports this product as published. Unpublish it before deleting it.");

        var intent = existing with
        {
            OperationState = ExternalListingOperationState.IntentRecorded,
            SynchronizationState = ExternalListingSyncState.Pending,
            UpdatedAt = _clock()
        };
        var intentSnapshot = ReplaceMapping(snapshot, intent);
        await _repository.SaveAsync(intentSnapshot, cancellationToken).ConfigureAwait(false);
        ListingMutationResult result;
        try
        {
            result = await _products.DeleteAsync(request.ShopId, existing.ProductId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var uncertain = intent with
            {
                SynchronizationState = ExternalListingSyncState.Uncertain,
                OperationState = ExternalListingOperationState.NeedsReconciliation,
                OperationJson = JsonSerializer.Serialize(new { kind = ListingOperationKind.Delete, error = exception.Message }),
                UpdatedAt = _clock()
            };
            await _repository.SaveAsync(ReplaceMapping(intentSnapshot, uncertain), cancellationToken).ConfigureAwait(false);
            return new(ListingLifecycleResultKind.Uncertain, ToApplicationMapping(uncertain), Message: "The deletion request could not be verified. Refresh before retrying.");
        }
        if (!result.IsDefinitive)
        {
            var uncertain = intent with { SynchronizationState = ExternalListingSyncState.Uncertain, OperationState = ExternalListingOperationState.NeedsReconciliation, UpdatedAt = _clock() };
            await _repository.SaveAsync(ReplaceMapping(intentSnapshot, uncertain), cancellationToken).ConfigureAwait(false);
            return new(ListingLifecycleResultKind.Uncertain, ToApplicationMapping(uncertain), Message: result.ErrorMessage ?? "Deletion state is unverified. Refresh before retrying.");
        }
        if (result.ErrorCode is not null)
        {
            var failed = intent with { SynchronizationState = ExternalListingSyncState.Failed, OperationState = ExternalListingOperationState.Failed, UpdatedAt = _clock() };
            await _repository.SaveAsync(ReplaceMapping(intentSnapshot, failed), cancellationToken).ConfigureAwait(false);
            return new(ListingLifecycleResultKind.Failed, ToApplicationMapping(failed), Message: result.ErrorMessage);
        }

        var deleted = intent with
        {
            ProductId = null,
            ExternalPublicationId = null,
            ExternalHandle = null,
            SynchronizationState = ExternalListingSyncState.Deleted,
            PublicationState = ExternalListingPublicationState.NotApplicable,
            OperationState = ExternalListingOperationState.Succeeded,
            SnapshotJson = null,
            UploadReferencesJson = null,
            LastSynchronizedAt = _clock(),
            UpdatedAt = _clock()
        };
        await _repository.SaveAsync(ReplaceMapping(intentSnapshot, deleted), cancellationToken).ConfigureAwait(false);
        return new(ListingLifecycleResultKind.Succeeded, ToApplicationMapping(deleted), Message: "The remote Printify product was deleted.");
    }

    private async Task<ListingLifecycleResult> MutatePublicationAsync(
        ListingConnectionRequest request,
        Guid itemId,
        bool publish,
        CancellationToken cancellationToken)
    {
        var readiness = await _connection.CheckAsync(request, cancellationToken).ConfigureAwait(false);
        if (!readiness.PublicationOperationsAvailable)
        {
            return new(ListingLifecycleResultKind.Unavailable, Message: readiness.Issues.FirstOrDefault()?.Message ?? "Printify publication is unavailable.");
        }

        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.ExternalListingMappings
            .SingleOrDefault(value => value.StoreId == request.StoreId && value.ItemId == itemId);
        if (existing?.ProductId is null)
        {
            return new(ListingLifecycleResultKind.Missing, Message: "Create the Printify listing before publishing it.");
        }
        if (existing.SynchronizationState is ExternalListingSyncState.Uncertain or ExternalListingSyncState.Pending
            || existing.OperationState == ExternalListingOperationState.NeedsReconciliation)
        {
            return new(ListingLifecycleResultKind.Uncertain, ToApplicationMapping(existing), Message: "Reconcile the previous Printify operation before changing publication state.");
        }

        var intent = existing with
        {
            PublicationState = publish ? ExternalListingPublicationState.Publishing : ExternalListingPublicationState.Unpublishing,
            OperationState = ExternalListingOperationState.IntentRecorded,
            SynchronizationState = ExternalListingSyncState.Pending,
            UpdatedAt = _clock()
        };
        var intentSnapshot = ReplaceMapping(snapshot, intent);
        await _repository.SaveAsync(intentSnapshot, cancellationToken).ConfigureAwait(false);

        ListingMutationResult result;
        try
        {
            result = publish
                ? await _publication.PublishAsync(request.ShopId, existing.ProductId, cancellationToken).ConfigureAwait(false)
                : await _publication.UnpublishAsync(request.ShopId, existing.ProductId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var uncertain = intent with
            {
                PublicationState = ExternalListingPublicationState.Unverified,
                SynchronizationState = ExternalListingSyncState.Uncertain,
                OperationState = ExternalListingOperationState.NeedsReconciliation,
                OperationJson = JsonSerializer.Serialize(new { kind = publish ? ListingOperationKind.Publish : ListingOperationKind.Unpublish, error = exception.Message }),
                UpdatedAt = _clock()
            };
            await _repository.SaveAsync(ReplaceMapping(intentSnapshot, uncertain), cancellationToken).ConfigureAwait(false);
            return new(ListingLifecycleResultKind.Uncertain, ToApplicationMapping(uncertain), Message: "The publication operation could not be verified. Refresh before retrying.");
        }

        if (!result.IsDefinitive)
        {
            var uncertain = intent with
            {
                PublicationState = ExternalListingPublicationState.Unverified,
                SynchronizationState = ExternalListingSyncState.Uncertain,
                OperationState = ExternalListingOperationState.NeedsReconciliation,
                UpdatedAt = _clock()
            };
            await _repository.SaveAsync(ReplaceMapping(intentSnapshot, uncertain), cancellationToken).ConfigureAwait(false);
            return new(ListingLifecycleResultKind.Uncertain, ToApplicationMapping(uncertain), Message: result.ErrorMessage ?? "Publication state is unverified. Refresh before retrying.");
        }

        if (result.ErrorCode is not null)
        {
            var failed = intent with
            {
                PublicationState = ExternalListingPublicationState.Unverified,
                SynchronizationState = ExternalListingSyncState.Failed,
                OperationState = ExternalListingOperationState.Failed,
                OperationJson = JsonSerializer.Serialize(new { kind = publish ? ListingOperationKind.Publish : ListingOperationKind.Unpublish, error = result.ErrorMessage }),
                UpdatedAt = _clock()
            };
            await _repository.SaveAsync(ReplaceMapping(intentSnapshot, failed), cancellationToken).ConfigureAwait(false);
            return new(ListingLifecycleResultKind.Failed, ToApplicationMapping(failed), Message: result.ErrorMessage);
        }

        if (result.ErrorCode is null)
        {
            ListingRemoteProduct? verified;
            try
            {
                verified = await _products.GetAsync(request.ShopId, existing.ProductId, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                var uncertain = intent with
                {
                    PublicationState = ExternalListingPublicationState.Unverified,
                    SynchronizationState = ExternalListingSyncState.Uncertain,
                    OperationState = ExternalListingOperationState.NeedsReconciliation,
                    OperationJson = JsonSerializer.Serialize(new { kind = publish ? ListingOperationKind.Publish : ListingOperationKind.Unpublish, error = exception.Message }),
                    UpdatedAt = _clock()
                };
                await _repository.SaveAsync(ReplaceMapping(intentSnapshot, uncertain), cancellationToken).ConfigureAwait(false);
                return new(ListingLifecycleResultKind.Uncertain, ToApplicationMapping(uncertain), Message: "Printify accepted the publication request, but the resulting state could not be verified.");
            }

            if (verified is null)
            {
                return new(ListingLifecycleResultKind.Missing, ToApplicationMapping(existing), Message: "Printify no longer returns the mapped product after the publication request.");
            }

            var expected = publish ? ListingPublicationState.Published : ListingPublicationState.Unpublished;
            if (verified.PublicationState != expected)
            {
                var uncertain = intent with
                {
                    PublicationState = ExternalListingPublicationState.Unverified,
                    SynchronizationState = ExternalListingSyncState.Uncertain,
                    OperationState = ExternalListingOperationState.NeedsReconciliation,
                    OperationJson = JsonSerializer.Serialize(new { kind = publish ? ListingOperationKind.Publish : ListingOperationKind.Unpublish, expected = expected.ToString(), actual = verified.PublicationState.ToString() }),
                    UpdatedAt = _clock()
                };
                await _repository.SaveAsync(ReplaceMapping(intentSnapshot, uncertain), cancellationToken).ConfigureAwait(false);
                return new(ListingLifecycleResultKind.Uncertain, ToApplicationMapping(uncertain), Message: "Printify returned a publication state different from the requested state. Refresh before retrying.");
            }

            result = result with { Product = verified };
        }

        var completed = intent with
        {
            PublicationState = result.Product is not null
                ? (ExternalListingPublicationState)result.Product.PublicationState
                : (publish ? ExternalListingPublicationState.Published : ExternalListingPublicationState.Unpublished),
            ExternalPublicationId = result.Product?.ExternalSalesChannelId ?? intent.ExternalPublicationId,
            ExternalHandle = result.Product?.ExternalHandle ?? intent.ExternalHandle,
            SynchronizationState = ExternalListingSyncState.Synchronized,
            OperationState = result.ErrorCode is null ? ExternalListingOperationState.Succeeded : ExternalListingOperationState.Failed,
            UpdatedAt = _clock()
        };
        await _repository.SaveAsync(ReplaceMapping(intentSnapshot, completed), cancellationToken).ConfigureAwait(false);
        return new(result.ErrorCode is null ? ListingLifecycleResultKind.Succeeded : ListingLifecycleResultKind.Failed, ToApplicationMapping(completed), Message: result.ErrorMessage);
    }

    private static ExternalListingMapping NewMapping(ListingConnectionRequest request, ListingProductProjection projection, DateTimeOffset now) =>
        new(
            request.StoreId,
            projection.ItemId,
            ProviderKey,
            request.ShopId,
            null,
            null,
            null,
            ExternalListingSyncState.Pending,
            request.RequirePublication ? ExternalListingPublicationState.Unpublished : ExternalListingPublicationState.NotApplicable,
            ExternalListingOperationState.IntentRecorded,
            null,
            null,
            null,
            now,
            now);

    private static WorkspaceSnapshot ReplaceMapping(WorkspaceSnapshot snapshot, ExternalListingMapping mapping) =>
        snapshot with
        {
            ExternalListingMappings = snapshot.ExternalListingMappings
                .Where(value => value.StoreId != mapping.StoreId || value.ItemId != mapping.ItemId)
                .Append(mapping)
                .ToArray()
        };

    private async Task<ExternalListingMapping> SaveMappingAsync(
        WorkspaceSnapshot snapshot,
        ExternalListingMapping mapping,
        CancellationToken cancellationToken)
    {
        await _repository.SaveAsync(ReplaceMapping(snapshot, mapping), cancellationToken).ConfigureAwait(false);
        return mapping;
    }

    private static ListingSnapshot ToSnapshot(ListingProductProjection projection)
    {
        var fields = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["title"] = projection.Title,
            ["description"] = projection.Description,
            ["shippingProfile"] = projection.ShippingProfile,
            ["variants"] = string.Join("|", projection.Variants.Select(value => $"{value.ExternalVariantId?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? value.SourceVariantId.ToString("D")}:{value.Price.RetailPrice.ToString(System.Globalization.CultureInfo.InvariantCulture)}"))
        };
        return new ListingSnapshot(fields);
    }

    private static string SerializeSnapshot(ListingSnapshot snapshot) => JsonSerializer.Serialize(snapshot.Fields);

    private static string? SerializeIntegrationValues(ListingIntegrationValues values) =>
        values.Title is null
            && values.Description is null
            && values.ShippingProfile is null
            && values.VariantRetailPrices.Count == 0
            ? null
            : JsonSerializer.Serialize(values);

    private static ListingIntegrationValues IntegrationValuesFromSnapshot(ListingSnapshot snapshot)
    {
        var prices = new Dictionary<int, decimal>();
        if (snapshot.Fields.TryGetValue("variants", out var variants) && !string.IsNullOrWhiteSpace(variants))
        {
            foreach (var entry in variants.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var parts = entry.Split(':', 2, StringSplitOptions.TrimEntries);
                if (parts.Length == 2
                    && int.TryParse(parts[0], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var variantId)
                    && decimal.TryParse(parts[1], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var price))
                    prices[variantId] = price;
            }
        }

        snapshot.Fields.TryGetValue("title", out var title);
        snapshot.Fields.TryGetValue("description", out var description);
        snapshot.Fields.TryGetValue("shippingProfile", out var shippingProfile);
        return new(title, description, shippingProfile, prices);
    }

    private static ListingProductProjection ApplyIntegrationValues(ListingProductProjection projection, string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return projection;

        ListingIntegrationValues? values;
        try
        {
            values = JsonSerializer.Deserialize<ListingIntegrationValues>(json);
        }
        catch (JsonException)
        {
            return projection;
        }

        if (values is null)
            return projection;

        var variants = values.VariantRetailPrices.Count == 0
            ? projection.Variants
            : projection.Variants.Select(variant =>
            {
                if (variant.ExternalVariantId is int externalVariantId
                    && values.VariantRetailPrices.TryGetValue(externalVariantId, out var retailPrice))
                {
                    return variant with { Price = variant.Price with { RetailPrice = retailPrice } };
                }

                return variant;
            }).ToArray();

        return projection with
        {
            Title = values.Title ?? projection.Title,
            Description = values.Description ?? projection.Description,
            ShippingProfile = values.ShippingProfile ?? projection.ShippingProfile,
            Variants = variants
        };
    }

    private async Task<IReadOnlyDictionary<Guid, ListingImageReference>?> ResolveImagesAsync(
        ExternalListingMapping? existing,
        ListingProductProjection projection,
        CancellationToken cancellationToken)
    {
        if (projection.Artwork.Count == 0)
            return new Dictionary<Guid, ListingImageReference>();
        if (_images is null)
            return null;

        var cached = DeserializeImages(existing?.UploadReferencesJson);
        var resolved = new Dictionary<Guid, ListingImageReference>();
        foreach (var artwork in projection.Artwork)
        {
            if (cached.TryGetValue(artwork.AssetId, out var previous)
                && string.Equals(previous.Fingerprint, AssetFingerprint(artwork), StringComparison.Ordinal))
            {
                resolved[artwork.AssetId] = previous;
                continue;
            }

            resolved[artwork.AssetId] = await _images.UploadAsync(
                new ListingImageUpload(artwork.AssetId, artwork.AssetPath, AssetFingerprint(artwork)),
                cancellationToken).ConfigureAwait(false);
        }

        return resolved;
    }

    private static string AssetFingerprint(ListingArtworkProjection artwork) =>
        $"{artwork.AssetId:N}:{artwork.Artwork.Width}x{artwork.Artwork.Height}:{artwork.AssetPath}";

    private static IReadOnlyDictionary<Guid, ListingImageReference> DeserializeImages(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<Guid, ListingImageReference>();
        try
        {
            return JsonSerializer.Deserialize<Dictionary<Guid, ListingImageReference>>(json)
                ?? new Dictionary<Guid, ListingImageReference>();
        }
        catch (JsonException)
        {
            return new Dictionary<Guid, ListingImageReference>();
        }
    }

    private static ListingSnapshot DeserializeSnapshot(string json)
    {
        var fields = JsonSerializer.Deserialize<Dictionary<string, string?>>(json)
            ?? new Dictionary<string, string?>(StringComparer.Ordinal);
        return new ListingSnapshot(fields);
    }

    private static ListingMapping ToApplicationMapping(ExternalListingMapping mapping) =>
        new(
            mapping.StoreId,
            mapping.ItemId,
            mapping.ProductId is null ? null : new ListingExternalIdentity(mapping.ShopId, mapping.ProductId, mapping.ExternalPublicationId, mapping.ExternalHandle),
            (ListingSynchronizationState)mapping.SynchronizationState,
            (ListingPublicationState)mapping.PublicationState,
            mapping.OperationState == ExternalListingOperationState.None
                ? null
                : new ListingOperation(
                    ListingOperationKind.None,
                    (ListingOperationState)mapping.OperationState,
                    mapping.UpdatedAt,
                    ErrorMessage: mapping.OperationJson));
}

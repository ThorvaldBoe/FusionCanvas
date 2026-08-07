using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Application.Workspaces;

namespace FusionCanvas.Application.DesignFiles;

public sealed class DesignStageService : IDesignStageService
{
    private readonly IWorkspaceRepository _repository;
    private readonly IWorkspaceFileStore _fileStore;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<Guid> _newId;

    public DesignStageService(
        IWorkspaceRepository repository,
        IWorkspaceFileStore fileStore,
        Func<DateTimeOffset>? clock = null,
        Func<Guid>? newId = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _newId = newId ?? Guid.NewGuid;
    }

    public async Task<DesignStageState> LoadDesignStageStateAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        return BuildState(snapshot, itemId);
    }

    public async Task<DesignStageResult> SelectConfigurationAsync(Guid itemId, Guid offeringId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var item = snapshot.Items.SingleOrDefault(i => i.Id == itemId);
        if (item is null)
        {
            return DesignStageResult.Failure("Item was not found.", BuildState(snapshot, itemId));
        }

        var editDecision = ItemWorkflowPolicy.CanPerformOperation(item, ItemOperationKind.DesignStage);
        if (!editDecision.IsAllowed)
        {
            return DesignStageResult.Failure(editDecision.Reason, BuildState(snapshot, itemId));
        }

        if (!DesignStagePolicy.IsValidConfiguration(snapshot, itemId, offeringId))
        {
            return DesignStageResult.Failure("The selected configuration is not valid for this item.", BuildState(snapshot, itemId));
        }

        // Clear existing slot assignments for areas not in the new offering
        var newAreaIds = DesignStagePolicy.AreaIdsForOffering(snapshot.DesignAreas, offeringId).ToHashSet();
        var oldConfig = snapshot.ItemListingConfigurations.SingleOrDefault(c => c.ItemId == itemId);
        var oldRows = snapshot.DesignVariantRows.Where(r => r.ItemId == itemId).Select(r => r.Id).ToHashSet();

        var keptAssignments = snapshot.DesignSlotAssignments
            .Where(a => newAreaIds.Contains(a.DesignAreaId) && !oldRows.Contains(a.RowId))
            .ToArray();

        var updated = snapshot with
        {
            ItemListingConfigurations = [.. snapshot.ItemListingConfigurations.Where(c => c.ItemId != itemId),
                new ItemListingConfiguration(itemId, offeringId)],
            DesignSlotAssignments = keptAssignments,
            // Clear rows and selected colors when switching configuration
            DesignSelectedColors = [.. snapshot.DesignSelectedColors.Where(c => c.ItemId != itemId)],
            DesignVariantRows = [.. snapshot.DesignVariantRows.Where(r => r.ItemId != itemId)],
            DesignVariantRowColors = [.. snapshot.DesignVariantRowColors.Where(c => !oldRows.Contains(c.RowId))]
        };

        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        return DesignStageResult.Success(BuildState(updated, itemId));
    }

    public async Task<DesignStageResult> AddSelectedColorAsync(Guid itemId, string colorValue, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var item = snapshot.Items.SingleOrDefault(i => i.Id == itemId);
        if (item is null)
        {
            return DesignStageResult.Failure("Item was not found.");
        }

        var editDecision = ItemWorkflowPolicy.CanPerformOperation(item, ItemOperationKind.DesignStage);
        if (!editDecision.IsAllowed)
        {
            return DesignStageResult.Failure(editDecision.Reason, BuildState(snapshot, itemId));
        }

        var cleaned = colorValue?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return DesignStageResult.Failure("Color value is required.", BuildState(snapshot, itemId));
        }

        // Check if already selected
        if (snapshot.DesignSelectedColors.Any(c => c.ItemId == itemId &&
            string.Equals(c.ColorValue, cleaned, StringComparison.OrdinalIgnoreCase)))
        {
            return DesignStageResult.Success(BuildState(snapshot, itemId)); // Idempotent
        }

        // Add to selected colors
        var newColor = new DesignSelectedColor(itemId, cleaned);
        var updatedSelected = new List<DesignSelectedColor>(snapshot.DesignSelectedColors) { newColor };

        // Ensure there is a default row with this color
        var defaultRow = snapshot.DesignVariantRows.SingleOrDefault(r => r.ItemId == itemId && r.IsDefault);
        List<DesignVariantRow> updatedRows;
        List<DesignVariantRowColor> updatedRowColors;
        IReadOnlyList<DesignSlotAssignment> updatedAssignments = snapshot.DesignSlotAssignments;

        // Get area IDs for slot creation (if config is available)
        var config = snapshot.ItemListingConfigurations.SingleOrDefault(c => c.ItemId == itemId);
        var areaIds = config is not null
            ? DesignStagePolicy.AreaIdsForOffering(snapshot.DesignAreas, config.OfferingId)
            : [];

        if (defaultRow is null)
        {
            // Create default row with empty slot assignments for each design area
            var rowId = _newId();
            defaultRow = new DesignVariantRow(rowId, itemId, true, 0);
            updatedRows = [.. snapshot.DesignVariantRows, defaultRow];
            updatedRowColors = [.. snapshot.DesignVariantRowColors, new DesignVariantRowColor(rowId, cleaned)];

            if (areaIds.Count > 0)
            {
                var newAssignments = areaIds
                    .Select(areaId => new DesignSlotAssignment(rowId, areaId, null))
                    .ToArray();
                updatedAssignments = [.. snapshot.DesignSlotAssignments, .. newAssignments];
            }
        }
        else
        {
            updatedRows = [.. snapshot.DesignVariantRows];
            updatedRowColors = [.. snapshot.DesignVariantRowColors, new DesignVariantRowColor(defaultRow.Id, cleaned)];

            // If the default row exists but has no slot assignments (e.g., from before this fix), create them
            var existingDefaultAssignments = snapshot.DesignSlotAssignments
                .Where(a => a.RowId == defaultRow.Id)
                .Select(a => a.DesignAreaId)
                .ToHashSet();
            var missingAreaIds = areaIds.Where(id => !existingDefaultAssignments.Contains(id)).ToArray();
            if (missingAreaIds.Length > 0)
            {
                var newAssignments = missingAreaIds
                    .Select(areaId => new DesignSlotAssignment(defaultRow.Id, areaId, null))
                    .ToArray();
                updatedAssignments = [.. snapshot.DesignSlotAssignments, .. newAssignments];
            }
        }

        var updated = snapshot with
        {
            DesignSelectedColors = updatedSelected,
            DesignVariantRows = updatedRows,
            DesignVariantRowColors = updatedRowColors,
            DesignSlotAssignments = updatedAssignments
        };

        // Validate partition
        DesignStagePolicy.ValidatePartition(itemId, updated.DesignSelectedColors, updated.DesignVariantRows, updated.DesignVariantRowColors);

        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        return DesignStageResult.Success(BuildState(updated, itemId));
    }

    public async Task<DesignStageResult> RemoveSelectedColorAsync(Guid itemId, string colorValue, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var item = snapshot.Items.SingleOrDefault(i => i.Id == itemId);
        if (item is null)
        {
            return DesignStageResult.Failure("Item was not found.");
        }

        var editDecision = ItemWorkflowPolicy.CanPerformOperation(item, ItemOperationKind.DesignStage);
        if (!editDecision.IsAllowed)
        {
            return DesignStageResult.Failure(editDecision.Reason, BuildState(snapshot, itemId));
        }

        var cleaned = colorValue?.Trim() ?? string.Empty;
        var updatedSelected = snapshot.DesignSelectedColors
            .Where(c => !(c.ItemId == itemId && string.Equals(c.ColorValue, cleaned, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        // Remove from row colors
        var rowsForItem = snapshot.DesignVariantRows.Where(r => r.ItemId == itemId).Select(r => r.Id).ToHashSet();
        var updatedRowColors = snapshot.DesignVariantRowColors
            .Where(c => !(rowsForItem.Contains(c.RowId) &&
                string.Equals(c.ColorValue, cleaned, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        var updated = snapshot with
        {
            DesignSelectedColors = updatedSelected,
            DesignVariantRowColors = updatedRowColors
        };

        // Validate partition (should pass since we removed the color cleanly)
        if (updatedSelected.Any(c => c.ItemId == itemId))
        {
            DesignStagePolicy.ValidatePartition(itemId, updated.DesignSelectedColors, updated.DesignVariantRows, updated.DesignVariantRowColors);
        }

        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        return DesignStageResult.Success(BuildState(updated, itemId));
    }

    public async Task<DesignStageResult> MakeSpecificForColorAsync(Guid itemId, string colorValue, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var item = snapshot.Items.SingleOrDefault(i => i.Id == itemId);
        if (item is null)
        {
            return DesignStageResult.Failure("Item was not found.");
        }

        var editDecision = ItemWorkflowPolicy.CanPerformOperation(item, ItemOperationKind.DesignStage);
        if (!editDecision.IsAllowed)
        {
            return DesignStageResult.Failure(editDecision.Reason, BuildState(snapshot, itemId));
        }

        var cleaned = colorValue?.Trim() ?? string.Empty;
        var config = snapshot.ItemListingConfigurations.SingleOrDefault(c => c.ItemId == itemId);
        if (config is null)
        {
            return DesignStageResult.Failure("Item has no listing configuration.", BuildState(snapshot, itemId));
        }

        // Find the current row that serves this color
        var currentRowColor = snapshot.DesignVariantRowColors
            .FirstOrDefault(c => string.Equals(c.ColorValue, cleaned, StringComparison.OrdinalIgnoreCase) &&
                snapshot.DesignVariantRows.Any(r => r.Id == c.RowId && r.ItemId == itemId));

        if (currentRowColor is null)
        {
            return DesignStageResult.Failure($"Color '{cleaned}' is not in any row.", BuildState(snapshot, itemId));
        }

        // Create new specific row
        var newRowId = _newId();
        var maxSort = snapshot.DesignVariantRows
            .Where(r => r.ItemId == itemId)
            .Select(r => r.SortOrder)
            .DefaultIfEmpty(-1)
            .Max();
        var newRow = new DesignVariantRow(newRowId, itemId, false, maxSort + 1);

        // Move color from old row to new row
        var updatedRowColors = snapshot.DesignVariantRowColors
            .Where(c => !(c.RowId == currentRowColor.RowId &&
                string.Equals(c.ColorValue, cleaned, StringComparison.OrdinalIgnoreCase)))
            .Concat([new DesignVariantRowColor(newRowId, cleaned)])
            .ToArray();

        // Create empty slots for the new row for each design area
        var areaIds = DesignStagePolicy.AreaIdsForOffering(snapshot.DesignAreas, config.OfferingId);
        var newAssignments = areaIds
            .Select(areaId => new DesignSlotAssignment(newRowId, areaId, null))
            .ToArray();

        var updatedRows = new List<DesignVariantRow>([.. snapshot.DesignVariantRows, newRow]);
        var updated = snapshot with
        {
            DesignVariantRows = updatedRows,
            DesignVariantRowColors = updatedRowColors,
            DesignSlotAssignments = [.. snapshot.DesignSlotAssignments, .. newAssignments]
        };

        // Check if old row is now empty — if so remove it along with its slot assignments
        var oldRowRemaining = updatedRowColors.Count(c => c.RowId == currentRowColor.RowId);
        if (oldRowRemaining == 0 && !snapshot.DesignVariantRows.Single(r => r.Id == currentRowColor.RowId).IsDefault)
        {
            updated = updated with
            {
                DesignVariantRows = [.. updated.DesignVariantRows.Where(r => r.Id != currentRowColor.RowId)],
                DesignVariantRowColors = [.. updated.DesignVariantRowColors.Where(c => c.RowId != currentRowColor.RowId)],
                DesignSlotAssignments = [.. updated.DesignSlotAssignments.Where(a => a.RowId != currentRowColor.RowId)]
            };
        }

        DesignStagePolicy.ValidatePartition(itemId, updated.DesignSelectedColors, updated.DesignVariantRows, updated.DesignVariantRowColors);

        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        return DesignStageResult.Success(BuildState(updated, itemId));
    }

    public async Task<DesignStageResult> RemoveSpecificRowAsync(Guid itemId, Guid rowId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var item = snapshot.Items.SingleOrDefault(i => i.Id == itemId);
        if (item is null)
        {
            return DesignStageResult.Failure("Item was not found.");
        }

        var editDecision = ItemWorkflowPolicy.CanPerformOperation(item, ItemOperationKind.DesignStage);
        if (!editDecision.IsAllowed)
        {
            return DesignStageResult.Failure(editDecision.Reason, BuildState(snapshot, itemId));
        }

        var row = snapshot.DesignVariantRows.SingleOrDefault(r => r.Id == rowId && r.ItemId == itemId);
        if (row is null)
        {
            return DesignStageResult.Failure("Row was not found.", BuildState(snapshot, itemId));
        }

        if (row.IsDefault)
        {
            return DesignStageResult.Failure("Cannot remove the default row.", BuildState(snapshot, itemId));
        }

        // Find the default row
        var defaultRow = snapshot.DesignVariantRows.Single(r => r.ItemId == itemId && r.IsDefault);

        // Move all colors from this row to the default row
        var movingColors = snapshot.DesignVariantRowColors
            .Where(c => c.RowId == rowId)
            .ToArray();

        var updatedRowColors = snapshot.DesignVariantRowColors
            .Where(c => c.RowId != rowId)
            .Concat(movingColors.Select(c => new DesignVariantRowColor(defaultRow.Id, c.ColorValue)))
            .ToArray();

        // Remove the specific row and its slot assignments
        var updated = snapshot with
        {
            DesignVariantRows = [.. snapshot.DesignVariantRows.Where(r => r.Id != rowId)],
            DesignVariantRowColors = updatedRowColors,
            DesignSlotAssignments = [.. snapshot.DesignSlotAssignments.Where(a => a.RowId != rowId)]
        };

        DesignStagePolicy.ValidatePartition(itemId, updated.DesignSelectedColors, updated.DesignVariantRows, updated.DesignVariantRowColors);

        await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        return DesignStageResult.Success(BuildState(updated, itemId));
    }

    public async Task<DesignStageResult> AssignSlotImageAsync(Guid itemId, Guid rowId, Guid designAreaId, string sourcePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return DesignStageResult.Failure("A source file path is required.");
        }

        if (!IsPng(sourcePath))
        {
            return DesignStageResult.Failure("Final design slot images must be PNG.");
        }

        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var item = snapshot.Items.SingleOrDefault(i => i.Id == itemId);
        if (item is null)
        {
            return DesignStageResult.Failure("Item was not found.");
        }

        var editDecision = ItemWorkflowPolicy.CanPerformOperation(item, ItemOperationKind.DesignStage);
        if (!editDecision.IsAllowed)
        {
            return DesignStageResult.Failure(editDecision.Reason, BuildState(snapshot, itemId));
        }

        // Check slot exists
        var existing = snapshot.DesignSlotAssignments.SingleOrDefault(a => a.RowId == rowId && a.DesignAreaId == designAreaId);
        if (existing is null)
        {
            return DesignStageResult.Failure("Slot assignment not found for this row and design area.", BuildState(snapshot, itemId));
        }

        return await ImportAndAssignSlotAsync(snapshot, item, rowId, designAreaId, sourcePath, existing.AssetId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<DesignStageResult> ReplaceSlotImageAsync(Guid itemId, Guid rowId, Guid designAreaId, string sourcePath, CancellationToken cancellationToken = default)
    {
        // Same logic as assign - just replaces
        return await AssignSlotImageAsync(itemId, rowId, designAreaId, sourcePath, cancellationToken).ConfigureAwait(false);
    }

    public async Task<DesignStageResult> RemoveSlotImageAsync(Guid itemId, Guid rowId, Guid designAreaId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var item = snapshot.Items.SingleOrDefault(i => i.Id == itemId);
        if (item is null)
        {
            return DesignStageResult.Failure("Item was not found.");
        }

        var editDecision = ItemWorkflowPolicy.CanPerformOperation(item, ItemOperationKind.DesignStage);
        if (!editDecision.IsAllowed)
        {
            return DesignStageResult.Failure(editDecision.Reason, BuildState(snapshot, itemId));
        }

        var assignment = snapshot.DesignSlotAssignments.SingleOrDefault(a => a.RowId == rowId && a.DesignAreaId == designAreaId);
        if (assignment is null)
        {
            return DesignStageResult.Failure("Slot assignment not found.", BuildState(snapshot, itemId));
        }

        var assetId = assignment.AssetId;

        // Remove slot binding + asset + link in one save
        var updated = snapshot with
        {
            DesignSlotAssignments = [.. snapshot.DesignSlotAssignments
                .Where(a => !(a.RowId == rowId && a.DesignAreaId == designAreaId)),
                new DesignSlotAssignment(rowId, designAreaId, null)],
            Assets = assetId is not null
                ? [.. snapshot.Assets.Where(a => a.Id != assetId)]
                : snapshot.Assets,
            AssetLinks = assetId is not null
                ? [.. snapshot.AssetLinks.Where(l => l.AssetId != assetId)]
                : snapshot.AssetLinks
        };

        try
        {
            await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return DesignStageResult.Failure($"Slot image removal could not be persisted. {exception.Message}", BuildState(snapshot, itemId));
        }

        // Best-effort file cleanup after successful save
        if (assetId is not null)
        {
            var oldAsset = snapshot.Assets.SingleOrDefault(a => a.Id == assetId);
            if (oldAsset is not null)
            {
                _fileStore.TryDelete(oldAsset.WorkspaceRelativePath);
            }
        }

        return DesignStageResult.Success(BuildState(updated, itemId));
    }

    public async Task<Stream> OpenSlotPreviewAsync(Guid rowId, Guid designAreaId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var assignment = snapshot.DesignSlotAssignments.SingleOrDefault(a => a.RowId == rowId && a.DesignAreaId == designAreaId)
            ?? throw new InvalidOperationException("Slot assignment not found.");

        if (assignment.AssetId is null)
        {
            throw new InvalidOperationException("Slot has no image assigned.");
        }

        var asset = snapshot.Assets.SingleOrDefault(a => a.Id == assignment.AssetId)
            ?? throw new InvalidOperationException("Slot asset not found.");

        return await _fileStore.OpenReadAsync(asset.WorkspaceRelativePath, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExportSlotImageAsync(Guid rowId, Guid designAreaId, string destinationPath, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var assignment = snapshot.DesignSlotAssignments.SingleOrDefault(a => a.RowId == rowId && a.DesignAreaId == designAreaId)
            ?? throw new InvalidOperationException("Slot assignment not found.");

        if (assignment.AssetId is null)
        {
            throw new InvalidOperationException("Slot has no image assigned.");
        }

        var asset = snapshot.Assets.SingleOrDefault(a => a.Id == assignment.AssetId)
            ?? throw new InvalidOperationException("Slot asset not found.");

        await _fileStore.ExportCopyAsync(asset.WorkspaceRelativePath, destinationPath, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExportSupportingImageAsync(Guid assetId, string destinationPath, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var asset = snapshot.Assets.SingleOrDefault(a => a.Id == assetId)
            ?? throw new InvalidOperationException("Supporting image asset not found.");

        await _fileStore.ExportCopyAsync(asset.WorkspaceRelativePath, destinationPath, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<DesignSlotSummary>> ListSupportingImagesAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        return ListSupportingImages(snapshot, itemId);
    }

    public async Task<DesignStageResult> ImportSupportingImageAsync(Guid itemId, string sourcePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return DesignStageResult.Failure("A source file path is required.");
        }

        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var item = snapshot.Items.SingleOrDefault(i => i.Id == itemId);
        if (item is null)
        {
            return DesignStageResult.Failure("Item was not found.");
        }

        var editDecision = ItemWorkflowPolicy.CanPerformOperation(item, ItemOperationKind.DesignStage);
        if (!editDecision.IsAllowed)
        {
            return DesignStageResult.Failure(editDecision.Reason, BuildState(snapshot, itemId));
        }

        ManagedWorkspaceFile imported;
        try
        {
            imported = await _fileStore.ImportAsync(sourcePath, AssetKind.ReferenceImage, cancellationToken).ConfigureAwait(false);
        }
        catch (FileNotFoundException)
        {
            return DesignStageResult.Failure("The selected source file was not found.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return DesignStageResult.Failure($"The supporting image could not be imported. {exception.Message}");
        }

        var assetId = _newId();
        var now = _clock();
        var asset = new Asset(
            assetId,
            item.StoreId,
            Path.GetFileName(sourcePath),
            null,
            AssetKind.ReferenceImage,
            imported.WorkspaceRelativePath,
            imported.OriginalSourcePath,
            isMissing: false,
            isArchived: false,
            now,
            now,
            "{}");
        var link = new AssetLink(assetId, WorkspaceEntityKind.Item, itemId);

        var updated = snapshot with
        {
            Assets = [.. snapshot.Assets, asset],
            AssetLinks = [.. snapshot.AssetLinks, link]
        };

        try
        {
            await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _fileStore.TryDelete(imported.WorkspaceRelativePath);
            return DesignStageResult.Failure($"The supporting image record could not be persisted. {exception.Message}");
        }

        return DesignStageResult.Success(BuildState(updated, itemId));
    }

    public async Task<DesignStageResult> RemoveSupportingImageAsync(Guid itemId, Guid assetId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var item = snapshot.Items.SingleOrDefault(i => i.Id == itemId);
        if (item is null)
        {
            return DesignStageResult.Failure("Item was not found.");
        }

        var editDecision = ItemWorkflowPolicy.CanPerformOperation(item, ItemOperationKind.DesignStage);
        if (!editDecision.IsAllowed)
        {
            return DesignStageResult.Failure(editDecision.Reason, BuildState(snapshot, itemId));
        }

        var asset = snapshot.Assets.SingleOrDefault(a => a.Id == assetId);
        if (asset is null)
        {
            return DesignStageResult.Failure("Supporting image not found.");
        }

        var updated = snapshot with
        {
            Assets = [.. snapshot.Assets.Where(a => a.Id != assetId)],
            AssetLinks = [.. snapshot.AssetLinks.Where(l => l.AssetId != assetId)]
        };

        try
        {
            await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return DesignStageResult.Failure($"The supporting image removal could not be persisted. {exception.Message}");
        }

        _fileStore.TryDelete(asset.WorkspaceRelativePath);
        return DesignStageResult.Success(BuildState(updated, itemId));
    }

    // --- Private helpers ---

    private async Task<DesignStageResult> ImportAndAssignSlotAsync(
        WorkspaceSnapshot snapshot,
        Item item,
        Guid rowId,
        Guid designAreaId,
        string sourcePath,
        Guid? oldAssetId,
        CancellationToken cancellationToken)
    {
        ManagedWorkspaceFile imported;
        try
        {
            imported = await _fileStore.ImportAsync(sourcePath, AssetKind.ExportedImage, cancellationToken).ConfigureAwait(false);
        }
        catch (FileNotFoundException)
        {
            return DesignStageResult.Failure("The selected source file was not found.", BuildState(snapshot, item.Id));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return DesignStageResult.Failure($"The slot image could not be imported. {exception.Message}", BuildState(snapshot, item.Id));
        }

        var assetId = _newId();
        var now = _clock();
        var asset = new Asset(
            assetId,
            item.StoreId,
            Path.GetFileName(sourcePath),
            null,
            AssetKind.ExportedImage,
            imported.WorkspaceRelativePath,
            imported.OriginalSourcePath,
            isMissing: false,
            isArchived: false,
            now,
            now,
            "{}");
        var link = new AssetLink(assetId, WorkspaceEntityKind.Item, item.Id);

        // Update slot assignment with new asset and remove old asset if replacing
        var updatedAssignments = snapshot.DesignSlotAssignments
            .Where(a => !(a.RowId == rowId && a.DesignAreaId == designAreaId))
            .Concat([new DesignSlotAssignment(rowId, designAreaId, assetId)])
            .ToArray();

        var updatedAssets = new List<Asset>(snapshot.Assets) { asset };
        var updatedLinks = new List<AssetLink>(snapshot.AssetLinks) { link };

        // Handle old asset removal
        if (oldAssetId is not null)
        {
            var oldAsset = snapshot.Assets.SingleOrDefault(a => a.Id == oldAssetId);
            if (oldAsset is not null)
            {
                updatedAssets.RemoveAll(a => a.Id == oldAssetId);
                updatedLinks.RemoveAll(l => l.AssetId == oldAssetId);
            }
        }

        var updated = snapshot with
        {
            DesignSlotAssignments = updatedAssignments,
            Assets = updatedAssets,
            AssetLinks = updatedLinks
        };

        try
        {
            await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _fileStore.TryDelete(imported.WorkspaceRelativePath);
            return DesignStageResult.Failure($"The slot image could not be persisted. {exception.Message}", BuildState(snapshot, item.Id));
        }

        // Best-effort old file cleanup
        if (oldAssetId is not null)
        {
            var oldAsset = snapshot.Assets.SingleOrDefault(a => a.Id == oldAssetId);
            if (oldAsset is not null)
            {
                _fileStore.TryDelete(oldAsset.WorkspaceRelativePath);
            }
        }

        return DesignStageResult.Success(BuildState(updated, item.Id));
    }

    private DesignStageState BuildState(WorkspaceSnapshot snapshot, Guid itemId)
    {
        var item = snapshot.Items.SingleOrDefault(i => i.Id == itemId);
        if (item is null)
        {
            return new DesignStageState(itemId, true, "Item was not found.", null, null, null, null, [], [], [], [], []);
        }

        var editDecision = ItemWorkflowPolicy.CanPerformOperation(item, ItemOperationKind.DesignStage);
        var store = snapshot.Stores.SingleOrDefault(s => s.Id == item.StoreId);
        var isReadOnly = store is { IsArchived: true } || !editDecision.IsAllowed;
        var readOnlyReason = isReadOnly
            ? (store is { IsArchived: true }
                ? "This item's Store is archived and its Design stage is read-only."
                : editDecision.Reason)
            : string.Empty;

        var config = snapshot.ItemListingConfigurations.SingleOrDefault(c => c.ItemId == itemId);

        // Available offerings: all offerings whose product belongs to this item's store
        var storeProductIds = snapshot.StoreProducts
            .Where(p => p.StoreId == item.StoreId)
            .Select(p => p.Id)
            .ToHashSet();
        var availableOfferings = snapshot.FulfillmentOfferings
            .Where(o => storeProductIds.Contains(o.StoreProductId))
            .ToArray();

        // Available colors: from the selected offering's variants
        var configOfferingId = config?.OfferingId;
        var availableColors = configOfferingId is not null
            ? DesignStagePolicy.AvailableColors(snapshot.ProductVariants, configOfferingId.Value)
            : [];

        // Selected colors
        var selectedColors = snapshot.DesignSelectedColors
            .Where(c => c.ItemId == itemId)
            .Select(c => c.ColorValue)
            .ToArray();

        // Rows with slots
        var rows = snapshot.DesignVariantRows
            .Where(r => r.ItemId == itemId)
            .OrderBy(r => r.SortOrder)
            .Select(r =>
            {
                var colorValues = snapshot.DesignVariantRowColors
                    .Where(c => c.RowId == r.Id)
                    .Select(c => c.ColorValue)
                    .ToArray();

                var areaIds = configOfferingId is not null
                    ? DesignStagePolicy.AreaIdsForOffering(snapshot.DesignAreas, configOfferingId.Value)
                    : [];

                var slots = areaIds.Select(areaId =>
                {
                    var area = snapshot.DesignAreas.Single(a => a.Id == areaId);
                    var assignment = snapshot.DesignSlotAssignments
                        .SingleOrDefault(a => a.RowId == r.Id && a.DesignAreaId == areaId);
                    var asset = assignment?.AssetId is not null
                        ? snapshot.Assets.SingleOrDefault(a => a.Id == assignment.AssetId)
                        : null;

                    return new DesignSlotSummary(
                        areaId,
                        area.Name,
                        assignment?.AssetId,
                        ResolveThumbnailPath(asset),
                        asset?.IsMissing ?? false,
                        asset is not null && !asset.IsMissing,
                        asset is not null && !asset.IsMissing);
                }).ToArray();

                return new DesignRowSummary(r.Id, r.IsDefault, r.SortOrder, colorValues, slots);
            }).ToArray();

        var supportingImages = ListSupportingImages(snapshot, itemId);

        var offeringName = configOfferingId is not null
            ? availableOfferings.SingleOrDefault(o => o.Id == configOfferingId)?.Name
            : null;

        var offeringKind = configOfferingId is not null
            ? availableOfferings.SingleOrDefault(o => o.Id == configOfferingId)?.Kind
            : null;

        var offeringProviderName = configOfferingId is not null
            ? availableOfferings.SingleOrDefault(o => o.Id == configOfferingId)?.ProviderName
            : null;

        return new DesignStageState(
            itemId,
            isReadOnly,
            readOnlyReason,
            configOfferingId,
            offeringName,
            offeringKind,
            offeringProviderName,
            availableOfferings,
            availableColors,
            selectedColors,
            rows,
            supportingImages);
    }

    private IReadOnlyList<DesignSlotSummary> ListSupportingImages(WorkspaceSnapshot snapshot, Guid itemId)
    {
        return snapshot.AssetLinks
            .Where(l => l.EntityKind == WorkspaceEntityKind.Item && l.EntityId == itemId)
            .Select(l => snapshot.Assets.SingleOrDefault(a => a.Id == l.AssetId))
            .Where(a => a is not null)
            .Where(a => a!.Kind != AssetKind.ExportedImage) // Exclude slot images
            .Select(a => new DesignSlotSummary(
                a!.Id,
                a.Name,
                a.Id,
                ResolveThumbnailPath(a),
                a.IsMissing,
                !a.IsMissing,
                !a.IsMissing))
            .ToArray();
    }

    private string? ResolveThumbnailPath(Asset? asset)
    {
        if (asset is null || asset.IsMissing) return null;
        // Resolve the workspace-relative path against the file store root so
        // the UI can bind an absolute path that actually renders.
        return Path.Combine(_fileStore.WorkspaceRoot, asset.WorkspaceRelativePath);
    }

    private static bool IsPng(string path) =>
        Path.GetExtension(path).Equals(".png", StringComparison.OrdinalIgnoreCase);
}
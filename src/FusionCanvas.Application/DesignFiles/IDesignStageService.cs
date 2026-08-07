namespace FusionCanvas.Application.DesignFiles;

/// <summary>
/// Application service for Design Stage operations: listing configuration
/// selection, color working set, variant rows, slot assignments, and
/// supporting images.
/// </summary>
public interface IDesignStageService
{
    /// <summary>Loads the full Design Stage state for an item.</summary>
    Task<DesignStageState> LoadDesignStageStateAsync(Guid itemId, CancellationToken cancellationToken = default);

    /// <summary>Selects a catalog offering as the item's listing configuration.</summary>
    Task<DesignStageResult> SelectConfigurationAsync(Guid itemId, Guid offeringId, CancellationToken cancellationToken = default);

    /// <summary>Adds a color to the working set.</summary>
    Task<DesignStageResult> AddSelectedColorAsync(Guid itemId, string colorValue, CancellationToken cancellationToken = default);

    /// <summary>Removes a color from the working set and any row it belongs to.</summary>
    Task<DesignStageResult> RemoveSelectedColorAsync(Guid itemId, string colorValue, CancellationToken cancellationToken = default);

    /// <summary>Moves a color out of its current row into a new specific row.</summary>
    Task<DesignStageResult> MakeSpecificForColorAsync(Guid itemId, string colorValue, CancellationToken cancellationToken = default);

    /// <summary>Atomically reverts a specific row's colors to the default row and removes the specific row and its slot assignments.</summary>
    Task<DesignStageResult> RemoveSpecificRowAsync(Guid itemId, Guid rowId, CancellationToken cancellationToken = default);

    /// <summary>Assigns a source image to a slot cell.</summary>
    Task<DesignStageResult> AssignSlotImageAsync(Guid itemId, Guid rowId, Guid designAreaId, string sourcePath, CancellationToken cancellationToken = default);

    /// <summary>Replaces a slot cell's image.</summary>
    Task<DesignStageResult> ReplaceSlotImageAsync(Guid itemId, Guid rowId, Guid designAreaId, string sourcePath, CancellationToken cancellationToken = default);

    /// <summary>Removes a slot cell's image.</summary>
    Task<DesignStageResult> RemoveSlotImageAsync(Guid itemId, Guid rowId, Guid designAreaId, CancellationToken cancellationToken = default);

    /// <summary>Opens a preview stream for a slot's image.</summary>
    Task<Stream> OpenSlotPreviewAsync(Guid rowId, Guid designAreaId, CancellationToken cancellationToken = default);

    /// <summary>Exports a slot's image to a destination path.</summary>
    Task ExportSlotImageAsync(Guid rowId, Guid designAreaId, string destinationPath, CancellationToken cancellationToken = default);

    /// <summary>Exports a supporting image asset to a destination path.</summary>
    Task ExportSupportingImageAsync(Guid assetId, string destinationPath, CancellationToken cancellationToken = default);

    /// <summary>Lists supporting images for an item.</summary>
    Task<IReadOnlyList<DesignSlotSummary>> ListSupportingImagesAsync(Guid itemId, CancellationToken cancellationToken = default);

    /// <summary>Imports a supporting image for an item.</summary>
    Task<DesignStageResult> ImportSupportingImageAsync(Guid itemId, string sourcePath, CancellationToken cancellationToken = default);

    /// <summary>Removes a supporting image.</summary>
    Task<DesignStageResult> RemoveSupportingImageAsync(Guid itemId, Guid assetId, CancellationToken cancellationToken = default);
}
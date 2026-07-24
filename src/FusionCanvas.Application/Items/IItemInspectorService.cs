namespace FusionCanvas.Application.Items;

public interface IItemInspectorService
{
    Task<ItemInspectorState?> LoadAsync(Guid itemId, CancellationToken cancellationToken = default);

    Task<ItemInspectorSaveResult> SaveAsync(ItemInspectorSaveRequest request, CancellationToken cancellationToken = default);

    Task<ItemInspectorSaveResult> SaveStageAsync(ItemStageAwareSaveRequest request, CancellationToken cancellationToken = default);
}

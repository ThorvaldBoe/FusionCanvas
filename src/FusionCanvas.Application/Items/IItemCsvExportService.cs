using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Items;

public interface IItemCsvExportService
{
    IReadOnlyList<ItemCsvRow> Project(WorkspaceSnapshot snapshot, WorkspaceEntityKind topicKind, Guid topicId);
    IReadOnlyList<ItemCsvRow> ProjectSelected(WorkspaceSnapshot snapshot, IReadOnlyList<Guid> itemIds);
}

namespace FusionCanvas.Application.WorkspaceTree;

public sealed class WorkspaceTreeSelectionCoordinator
{
    public event EventHandler<WorkspaceTreeSelection?>? SelectionChanged;

    public WorkspaceTreeSelection? Selected { get; private set; }

    public void Select(WorkspaceTreeSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        if (selection.Id == Guid.Empty)
        {
            throw new ArgumentException("Selection identifier must not be empty.", nameof(selection));
        }

        if (Selected == selection)
        {
            return;
        }

        Selected = selection;
        SelectionChanged?.Invoke(this, Selected);
    }

    public void Clear()
    {
        if (Selected is null)
        {
            return;
        }

        Selected = null;
        SelectionChanged?.Invoke(this, null);
    }
}

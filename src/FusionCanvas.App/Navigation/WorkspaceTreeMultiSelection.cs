using FusionCanvas.Application.WorkspaceTree;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Navigation;

/// <summary>
/// Session-only selection state for the workspace tree. The canonical active
/// selection remains owned by <see cref="WorkspaceTreeSelectionCoordinator"/>.
/// </summary>
public sealed class WorkspaceTreeMultiSelection
{
    private readonly List<Guid> _selectedIds = [];

    public IReadOnlyList<Guid> SelectedIds => _selectedIds;
    public Guid? ActiveId { get; private set; }
    public Guid? AnchorId { get; private set; }
    public bool HasSelection => _selectedIds.Count > 0;
    public int Count => _selectedIds.Count;

    public bool Contains(Guid id) => _selectedIds.Contains(id);

    public void Replace(Guid id)
    {
        _selectedIds.Clear();
        _selectedIds.Add(id);
        ActiveId = id;
        AnchorId = id;
    }

    public void Toggle(Guid id)
    {
        if (_selectedIds.Remove(id))
        {
            ActiveId = _selectedIds.Count == 0 ? ActiveId : id;
            return;
        }

        _selectedIds.Add(id);
        ActiveId = id;
        AnchorId ??= id;
    }

    public void SelectRange(IReadOnlyList<Guid> visibleIds, Guid id, bool extend)
    {
        ArgumentNullException.ThrowIfNull(visibleIds);

        if (!AnchorId.HasValue || !visibleIds.Contains(AnchorId.Value) || !visibleIds.Contains(id))
        {
            if (extend)
            {
                Add(id);
            }
            else
            {
                Replace(id);
            }

            return;
        }

        var anchorIndex = FindIndex(visibleIds, AnchorId.Value);
        var clickedIndex = FindIndex(visibleIds, id);
        var start = Math.Min(anchorIndex, clickedIndex);
        var end = Math.Max(anchorIndex, clickedIndex);
        if (!extend)
        {
            _selectedIds.Clear();
        }

        foreach (var rangeId in visibleIds.Skip(start).Take(end - start + 1))
        {
            Add(rangeId);
        }

        ActiveId = id;
    }

    public void SelectAll(IEnumerable<Guid> visibleIds)
    {
        ArgumentNullException.ThrowIfNull(visibleIds);

        _selectedIds.Clear();
        foreach (var id in visibleIds)
        {
            Add(id);
        }

        ActiveId = _selectedIds.Count == 0 ? null : _selectedIds[^1];
        AnchorId = _selectedIds.Count == 0 ? null : _selectedIds[0];
    }

    public void Reconcile(IEnumerable<Guid> availableIds)
    {
        ArgumentNullException.ThrowIfNull(availableIds);

        var available = availableIds.ToHashSet();
        _selectedIds.RemoveAll(id => !available.Contains(id));
        if (ActiveId is Guid activeId && !available.Contains(activeId))
        {
            ActiveId = _selectedIds.Count == 0 ? null : _selectedIds[^1];
        }

        if (AnchorId is Guid anchorId && !available.Contains(anchorId))
        {
            AnchorId = _selectedIds.Count == 0 ? null : _selectedIds[0];
        }
    }

    public void Clear()
    {
        _selectedIds.Clear();
        ActiveId = null;
        AnchorId = null;
    }

    public void Restore(IEnumerable<Guid> selectedIds, Guid? activeId, Guid? anchorId)
    {
        ArgumentNullException.ThrowIfNull(selectedIds);

        _selectedIds.Clear();
        foreach (var id in selectedIds.Distinct())
        {
            _selectedIds.Add(id);
        }

        ActiveId = activeId is Guid active && _selectedIds.Contains(active)
            ? active
            : _selectedIds.Count == 0 ? null : _selectedIds[^1];
        AnchorId = anchorId is Guid anchor && _selectedIds.Contains(anchor)
            ? anchor
            : _selectedIds.Count == 0 ? null : _selectedIds[0];
    }

    private void Add(Guid id)
    {
        if (!_selectedIds.Contains(id))
        {
            _selectedIds.Add(id);
        }
    }

    private static int FindIndex(IReadOnlyList<Guid> ids, Guid id)
    {
        for (var index = 0; index < ids.Count; index++)
        {
            if (ids[index] == id)
            {
                return index;
            }
        }

        return -1;
    }
}

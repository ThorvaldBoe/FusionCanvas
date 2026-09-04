using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.App.Items;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Navigation;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Application.WorkspaceTree;
using FusionCanvas.Application.Groups;
using FusionCanvas.Application.Items;
using FusionCanvas.Application.Tags;
using FusionCanvas.Application.Workspaces;

namespace FusionCanvas.App.Navigation;

public sealed class TagFilterEntryViewModel : INotifyPropertyChanged
{
    private readonly WorkspaceTreeViewModel _owner;

    public TagFilterEntryViewModel(WorkspaceTreeViewModel owner, Guid id, string name)
    {
        _owner = owner;
        Id = id;
        Name = name;
    }

    public Guid Id { get; }
    public string Name { get; }

    public bool IsSelected
    {
        get => _owner.IsTagSelected(Id);
        set => _owner.SetTagSelected(Id, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    internal void RaiseIsSelectedChanged() =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
}

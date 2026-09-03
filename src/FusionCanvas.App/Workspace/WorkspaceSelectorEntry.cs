using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Application.Workspaces.Transfer;

namespace FusionCanvas.App.Workspace;

public sealed record WorkspaceSelectorEntry(WorkspaceSummary Workspace, bool IsSelected)
{
    public Guid Id => Workspace.Id;

    public string Name => Workspace.Name;
}

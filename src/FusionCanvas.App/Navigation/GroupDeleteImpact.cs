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

public sealed record GroupDeleteImpact(
    Guid GroupId,
    string GroupName,
    int DescendantGroupCount,
    int ItemCount,
    IReadOnlySet<Guid> DeletedEntityIds);

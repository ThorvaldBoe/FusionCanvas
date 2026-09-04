using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Items;
using FusionCanvas.Application.Items;
using FusionCanvas.Application.Tags;
using FusionCanvas.Application.TitleOptimization;

namespace FusionCanvas.App.Items;

public sealed class ItemInspectorLifecycleEventArgs(ItemManagementResult result, bool deleted) : EventArgs
{
    public ItemManagementResult Result { get; } = result;
    public bool Deleted { get; } = deleted;
}

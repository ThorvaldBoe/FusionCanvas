using Avalonia.Controls;
using Avalonia.Interactivity;
using FusionCanvas.App.Navigation;

namespace FusionCanvas.App.Groups;

public sealed record GroupDeleteConfirmationViewModel
{
    public GroupDeleteConfirmationViewModel(GroupDeleteImpact impact)
    {
        Title = $"Delete '{impact.GroupName}'?";
        var subgroupLabel = impact.DescendantGroupCount == 1 ? "subgroup" : "subgroups";
        var itemLabel = impact.ItemCount == 1 ? "item" : "items";
        WarningMessage = $"The selected group, {impact.DescendantGroupCount} {subgroupLabel}, and {impact.ItemCount} {itemLabel} will be permanently lost.";
    }

    public string Title { get; }

    public string WarningMessage { get; }
}

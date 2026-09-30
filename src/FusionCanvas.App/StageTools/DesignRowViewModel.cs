using System.Collections.ObjectModel;
using FusionCanvas.Application.DesignFiles;
using FusionCanvas.Domain.Products;

namespace FusionCanvas.App.StageTools;

public sealed class DesignRowViewModel
{
    public DesignRowViewModel(DesignRowSummary summary, bool isReadOnly)
    {
        RowId = summary.RowId;
        IsDefault = summary.IsDefault;
        SortOrder = summary.SortOrder;
        ColorValues = [.. summary.ColorValues];
        IsReadOnly = isReadOnly;
        foreach (var s in summary.Slots)
        {
            Slots.Add(new DesignSlotViewModel(s, isReadOnly));
        }
    }

    public Guid RowId { get; }
    public bool IsDefault { get; }
    public int SortOrder { get; }
    public bool IsReadOnly { get; }
    public IReadOnlyList<string> ColorValues { get; }
    public string ColorChips => ColorValues.Count > 0 ? string.Join(", ", ColorValues) : "(no colors)";
    public ObservableCollection<DesignSlotViewModel> Slots { get; } = [];

    public bool CanRemove => !IsDefault && !IsReadOnly;

}

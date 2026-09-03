using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Avalonia.Media.Imaging;
using FusionCanvas.Application.DesignFiles;
using FusionCanvas.Domain.Products;

namespace FusionCanvas.App.StageTools;

public sealed class DesignRowViewModel : INotifyPropertyChanged
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

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid RowId { get; }
    public bool IsDefault { get; }
    public int SortOrder { get; }
    public bool IsReadOnly { get; }
    public IReadOnlyList<string> ColorValues { get; }
    public string ColorChips => ColorValues.Count > 0 ? string.Join(", ", ColorValues) : "(no colors)";
    public ObservableCollection<DesignSlotViewModel> Slots { get; } = [];

    public bool CanRemove => !IsDefault && !IsReadOnly;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

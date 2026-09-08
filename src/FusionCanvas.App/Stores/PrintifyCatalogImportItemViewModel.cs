using System.ComponentModel;
using FusionCanvas.Application.Stores.Printify;

namespace FusionCanvas.App.Stores;

public sealed class PrintifyCatalogImportItemViewModel(PrintifyCatalogBlueprintSummary blueprint) : INotifyPropertyChanged
{
    private bool _isSelected;

    public int Id => blueprint.Id;
    public string Title => blueprint.Title;
    public string Subtitle => string.Join(" · ", new[] { blueprint.Brand, blueprint.Model }.Where(value => !string.IsNullOrWhiteSpace(value)));
    public bool IsSelected { get => _isSelected; set { if (_isSelected == value) return; _isSelected = value; PropertyChanged?.Invoke(this, new(nameof(IsSelected))); } }
    public event PropertyChangedEventHandler? PropertyChanged;
}

using System.ComponentModel;
using FusionCanvas.Application.Stores.Printify;

namespace FusionCanvas.App.Stores;

public sealed class PrintifyCatalogImportItemViewModel(PrintifyShopProductSummary product) : INotifyPropertyChanged
{
    private bool _isSelected;

    public string Id => product.ProductId;
    public string Title => product.Title;
    public string Subtitle => $"Blueprint {product.BlueprintId} · Provider {product.ProviderId}";
    public bool IsSelected { get => _isSelected; set { if (_isSelected == value) return; _isSelected = value; PropertyChanged?.Invoke(this, new(nameof(IsSelected))); } }
    public event PropertyChangedEventHandler? PropertyChanged;
}

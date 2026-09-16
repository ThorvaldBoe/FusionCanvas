using System.ComponentModel;
using FusionCanvas.Application.Stores.Printify;

namespace FusionCanvas.App.Stores;

public sealed class PrintifyCatalogImportItemViewModel(PrintifyShopProductSummary product) : INotifyPropertyChanged
{
    private bool _isSelected;

    public string Id => product.ProductId;
    public string BlueprintName => string.IsNullOrWhiteSpace(product.BlueprintName)
        ? $"Blueprint {product.BlueprintId}"
        : product.BlueprintName!;
    public string ProductTitle => product.Title;
    public string Subtitle => $"Product {product.ProductId} · Provider {product.ProviderId}";
    public bool IsSelected { get => _isSelected; set { if (_isSelected == value) return; _isSelected = value; PropertyChanged?.Invoke(this, new(nameof(IsSelected))); } }
    public event PropertyChangedEventHandler? PropertyChanged;
}

using System.ComponentModel;
using FusionCanvas.Application.Stores.Printify;

namespace FusionCanvas.App.Stores;

public sealed class PrintifyCatalogImportItemViewModel(PrintifyShopBlueprintSummary blueprint) : INotifyPropertyChanged
{
    private bool _isSelected;

    public int BlueprintId => blueprint.BlueprintId;
    public int Id => BlueprintId;
    public string BlueprintName => string.IsNullOrWhiteSpace(blueprint.BlueprintName)
        ? $"Blueprint {blueprint.BlueprintId}"
        : blueprint.BlueprintName;
    public string ProviderSummary => string.Join(", ", blueprint.Providers.Select(provider => provider.Name));
    public string ShopProductSummary => blueprint.ShopProductCount == 1
        ? $"1 shop product · {string.Join(", ", blueprint.ShopProductTitles)}"
        : $"{blueprint.ShopProductCount} shop products · {string.Join(", ", blueprint.ShopProductTitles)}";
    public string SelectionAccessibleName => $"{BlueprintName} — Providers: {ProviderSummary} — {ShopProductSummary}";
    public bool IsSelected { get => _isSelected; set { if (_isSelected == value) return; _isSelected = value; PropertyChanged?.Invoke(this, new(nameof(IsSelected))); } }
    public event PropertyChangedEventHandler? PropertyChanged;
}

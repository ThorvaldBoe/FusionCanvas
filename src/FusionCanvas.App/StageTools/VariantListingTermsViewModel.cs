using System.ComponentModel;
using System.Globalization;

namespace FusionCanvas.App.StageTools;

public sealed class VariantListingTermsViewModel : INotifyPropertyChanged
{
    private string _sellingPrice;
    private string _fulfillmentCost;

    public VariantListingTermsViewModel(Guid variantId, string name, decimal? price, decimal? cost)
    {
        VariantId = variantId;
        Name = name;
        _sellingPrice = Format(price);
        _fulfillmentCost = Format(cost);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public Guid VariantId { get; }
    public string Name { get; }
    public string SellingPrice
    {
        get => _sellingPrice;
        set
        {
            if (_sellingPrice == value) return;
            _sellingPrice = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SellingPrice)));
        }
    }
    public string FulfillmentCost
    {
        get => _fulfillmentCost;
        set
        {
            if (_fulfillmentCost == value) return;
            _fulfillmentCost = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FulfillmentCost)));
        }
    }

    private static string Format(decimal? value) => value?.ToString("0.##", CultureInfo.InvariantCulture) ?? string.Empty;
}

using System.Globalization;
using Avalonia.Data.Converters;
using FusionCanvas.Domain.Stores;

namespace FusionCanvas.App.Stores;

public sealed class FulfillmentStrategyLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        FulfillmentStrategy.Manual => "Manual",
        FulfillmentStrategy.ShopifyManual => "Shopify + Manual",
        FulfillmentStrategy.ShopifyPrintify => "Shopify + Printify",
        FulfillmentStrategy.Printify => "Printify",
        _ => string.Empty
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

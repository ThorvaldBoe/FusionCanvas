using FusionCanvas.Application.Metadata;
using FusionCanvas.Application.Stores;
using FusionCanvas.Domain.Stores;

namespace FusionCanvas.Integration.Stores;

/// <summary>
/// Maps store context to the existing JSON metadata column while preserving unknown keys.
/// </summary>
public sealed class StoreContextMapper : IStoreContextMapper
{
    private const string NotesKey = "notes";
    private const string TargetMarketKey = "targetMarket";
    private const string BrandDirectionKey = "brandDirection";
    private const string PlanningContextKey = "planningContext";
    private const string UrlKey = "url";
    private const string PrintifyShopIdKey = "printifyShopId";
    private const string PrintifyShopTitleKey = "printifyShopTitle";

    public StoreContext Read(Store store)
    {
        ArgumentNullException.ThrowIfNull(store);
        var metadata = StringMetadataCodec.Parse(store.MetadataJson);
        return new StoreContext(
            store.Description,
            metadata.GetValueOrDefault(NotesKey),
            metadata.GetValueOrDefault(TargetMarketKey),
            metadata.GetValueOrDefault(BrandDirectionKey),
            metadata.GetValueOrDefault(PlanningContextKey),
            metadata.GetValueOrDefault(UrlKey),
            int.TryParse(metadata.GetValueOrDefault(PrintifyShopIdKey), out var shopId) ? shopId : null,
            metadata.GetValueOrDefault(PrintifyShopTitleKey));
    }

    public Store Apply(Store store, StoreContext context)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(context);

        var metadata = StringMetadataCodec.Parse(store.MetadataJson);
        StringMetadataCodec.SetOptional(metadata, NotesKey, context.Notes);
        StringMetadataCodec.SetOptional(metadata, TargetMarketKey, context.TargetMarket);
        StringMetadataCodec.SetOptional(metadata, BrandDirectionKey, context.BrandDirection);
        StringMetadataCodec.SetOptional(metadata, PlanningContextKey, context.PlanningContext);
        StringMetadataCodec.SetOptional(metadata, UrlKey, context.Url);
        StringMetadataCodec.SetOptional(metadata, PrintifyShopIdKey, context.PrintifyShopId?.ToString(System.Globalization.CultureInfo.InvariantCulture));
        StringMetadataCodec.SetOptional(metadata, PrintifyShopTitleKey, context.PrintifyShopTitle);

        return store with
        {
            Description = context.Description,
            MetadataJson = StringMetadataCodec.Serialize(metadata)
        };
    }
}

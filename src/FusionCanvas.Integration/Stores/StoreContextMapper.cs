using System.Text.Json;
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
        var metadata = ParseMetadata(store.MetadataJson);
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

        var metadata = ParseMetadata(store.MetadataJson);
        SetOptional(metadata, NotesKey, context.Notes);
        SetOptional(metadata, TargetMarketKey, context.TargetMarket);
        SetOptional(metadata, BrandDirectionKey, context.BrandDirection);
        SetOptional(metadata, PlanningContextKey, context.PlanningContext);
        SetOptional(metadata, UrlKey, context.Url);
        SetOptional(metadata, PrintifyShopIdKey, context.PrintifyShopId?.ToString(System.Globalization.CultureInfo.InvariantCulture));
        SetOptional(metadata, PrintifyShopTitleKey, context.PrintifyShopTitle);

        return store with
        {
            Description = context.Description,
            MetadataJson = metadata.Count == 0 ? "{}" : JsonSerializer.Serialize(metadata)
        };
    }

    private static Dictionary<string, string> ParseMetadata(string metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson) || metadataJson.Trim() == "{}")
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        using var document = JsonDocument.Parse(metadataJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        return document.RootElement
            .EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value.ToString(), StringComparer.Ordinal);
    }

    private static void SetOptional(Dictionary<string, string> metadata, string key, string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (normalized is null)
        {
            metadata.Remove(key);
            return;
        }

        metadata[key] = normalized;
    }
}

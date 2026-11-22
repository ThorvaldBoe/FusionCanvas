using System.Text.Json;
using FusionCanvas.Application.AI;
using FusionCanvas.Application.Items;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Concepts;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.DesignFiles;

public static class ArtworkPromptCompiler
{
    public static string? BuildDefault(
        WorkspaceSnapshot snapshot,
        Item item,
        Guid? designAreaId,
        ArtworkContrast contrast)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(item);

        var configuration = snapshot.ItemListingConfigurations.SingleOrDefault(value => value.ItemId == item.Id);
        var area = configuration is null || designAreaId is not Guid areaId
            ? null
            : ResolveArea(snapshot, configuration.OfferingId, areaId);
        if (area is null)
        {
            return null;
        }

        var metadata = ItemMetadataCodec.ParseMetadata(item.MetadataJson);
        var selectedStyle = Guid.TryParse(metadata.GetValueOrDefault(ItemMetadataCodec.ArtworkStyleIdKey), out var selectedStyleId)
            ? snapshot.ArtworkStyles.SingleOrDefault(value => value.Id == selectedStyleId && value.WorkspaceId == snapshot.Stores.SingleOrDefault(store => store.Id == item.StoreId)?.WorkspaceId)
            : null;
        var phrase = metadata.GetValueOrDefault(ItemMetadataCodec.PhraseKey) ?? string.Empty;
        var sll = metadata.GetValueOrDefault(ItemMetadataCodec.SllKey);
        var fingerprint = metadata.GetValueOrDefault(ItemMetadataCodec.SllSourceFingerprintKey);
        var currentFingerprint = ItemMetadataCodec.ComputeSllSourceFingerprint(
            metadata.GetValueOrDefault(ItemMetadataCodec.IdeaKey),
            metadata.GetValueOrDefault(ItemMetadataCodec.ConceptIdeaKey),
            phrase,
            metadata.GetValueOrDefault(ItemMetadataCodec.GraphicDirectionKey));
        var isSllStale = !string.IsNullOrWhiteSpace(sll)
            && !string.Equals(fingerprint, currentFingerprint, StringComparison.Ordinal);
        var composition = isSllStale ? null : ReadCompositionGuidance(sll, phrase);

        var artworkGuidance = area.Guidance is null
            ? null
            : $"Recommended format: {area.Guidance.FileFormat ?? "PNG"}; background: {area.Guidance.Background ?? "transparent when requested"}.";
        return ArtworkPromptBuilder.Build(new ArtworkPromptContext(
            metadata.GetValueOrDefault(ItemMetadataCodec.IdeaKey) ?? string.Empty,
            metadata.GetValueOrDefault(ItemMetadataCodec.ConceptIdeaKey) ?? string.Empty,
            phrase,
            metadata.GetValueOrDefault(ItemMetadataCodec.GraphicDirectionKey) ?? string.Empty,
            area.Name,
            area.Position,
            area.Size,
            area.DecorationMethod,
            artworkGuidance,
            metadata.GetValueOrDefault(ItemMetadataCodec.NotesKey),
            SllIsStale: isSllStale,
            NicheContext: BuildStoreNicheContext(snapshot, item),
            Contrast: contrast,
            CompositionGuidance: composition,
            ArtworkStyleTitle: selectedStyle?.Title,
            ArtworkStyleDescription: selectedStyle?.Description));
    }

    private static string? ReadCompositionGuidance(string? sll, string phrase)
    {
        if (string.IsNullOrWhiteSpace(sll))
        {
            return null;
        }

        try
        {
            var document = JsonSerializer.Deserialize<SllDocument>(sll);
            if (document?.Validate(phrase) != true)
            {
                return null;
            }

            var parts = new[]
            {
                document.Notes.Composition,
                document.Triangle.Relationship,
                document.AsciiSketch
            }.Where(value => !string.IsNullOrWhiteSpace(value));
            var guidance = string.Join(Environment.NewLine, parts);
            return string.IsNullOrWhiteSpace(guidance) ? null : guidance;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? BuildStoreNicheContext(WorkspaceSnapshot snapshot, Item item)
    {
        var fields = new List<(string Label, string? Value)>();
        var store = snapshot.Stores.SingleOrDefault(value => value.Id == item.StoreId);
        if (store is not null)
        {
            fields.Add(("Store", store.Name));
            fields.Add(("Store description", store.Description));
            AddJsonField(store.MetadataJson, fields, "Brand direction", "brandDirection");
            AddJsonField(store.MetadataJson, fields, "Target market", "targetMarket");
        }

        if (item.NicheId is Guid nicheId && snapshot.Niches.SingleOrDefault(value => value.Id == nicheId) is { } niche)
        {
            fields.Add(("Niche", niche.Name));
            fields.Add(("Niche description", niche.Description));
            foreach (var (label, key) in new[]
            {
                ("Audience", "audience"),
                ("Humor style", "humorStyle"),
                ("Constraints", "constraints")
            })
            {
                AddJsonField(niche.MetadataJson, fields, label, key);
            }
        }

        var values = fields.Where(field => !string.IsNullOrWhiteSpace(field.Value))
            .Select(field => $"{field.Label}: {field.Value!.Trim()}")
            .ToArray();
        return values.Length == 0 ? null : string.Join(Environment.NewLine, values);
    }

    private static void AddJsonField(string json, List<(string Label, string? Value)> fields, string label, string key)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty(key, out var property)
                && property.ValueKind == JsonValueKind.String)
            {
                fields.Add((label, property.GetString()));
            }
        }
        catch (JsonException)
        {
            // Keep other valid creative context when one metadata document is malformed.
        }
    }

    private static ResolvedArea? ResolveArea(WorkspaceSnapshot snapshot, Guid offeringId, Guid areaId)
    {
        var placeholder = snapshot.OfferingPlaceholders.SingleOrDefault(value =>
            value.Id == areaId && value.OfferingId == offeringId && !value.IsArchived);
        if (placeholder is not null)
        {
            return new(placeholder.Name, placeholder.Position, placeholder.DecorationMethod,
                new AiImageSize(placeholder.Width, placeholder.Height), placeholder.ArtworkGuidance);
        }

        var legacy = snapshot.DesignAreas.SingleOrDefault(value => value.Id == areaId && value.FulfillmentOfferingId == offeringId);
        return legacy is null
            ? null
            : new(legacy.Name, legacy.Position, legacy.DecorationMethod, new AiImageSize(legacy.Width, legacy.Height), null);
    }

    private sealed record ResolvedArea(
        string Name,
        string Position,
        string DecorationMethod,
        AiImageSize Size,
        FusionCanvas.Domain.Catalog.DesignAreaArtworkGuidance? Guidance);
}

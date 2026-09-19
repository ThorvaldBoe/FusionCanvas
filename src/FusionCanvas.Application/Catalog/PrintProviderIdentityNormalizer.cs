using System.Text.Json;
using System.Text.Json.Nodes;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Catalog;

public static class PrintProviderIdentityNormalizer
{
    private const string ExternalProviderIdsProperty = "externalProviderIds";

    public static string NormalizeName(string name) => name.Trim();

    public static (WorkspaceSnapshot Snapshot, bool Changed) NormalizeStore(
        WorkspaceSnapshot source,
        Guid storeId,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(source);

        var providers = source.PrintProviders.ToList();
        var offerings = source.BlueprintOfferings.ToList();
        var changed = false;

        var groups = providers
            .Where(value => value.StoreId == storeId)
            .GroupBy(value => NormalizeName(value.Name), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1);

        foreach (var group in groups)
        {
            var members = group
                .OrderByDescending(value => !value.IsArchived)
                .ThenBy(value => value.CreatedAt)
                .ThenBy(value => value.Id)
                .ToArray();
            var survivor = members[0];
            var mergedMetadata = MergeExternalProviderMetadata(survivor, members.SelectMany(ExternalProviderIds));
            var updatedSurvivor = survivor with
            {
                MetadataJson = mergedMetadata,
                UpdatedAt = members.Skip(1).Any(value => value.UpdatedAt > survivor.UpdatedAt)
                    ? members.Max(value => value.UpdatedAt)
                    : survivor.UpdatedAt
            };

            if (!updatedSurvivor.Equals(survivor))
            {
                Replace(providers, value => value.Id == survivor.Id, updatedSurvivor);
                changed = true;
            }

            foreach (var duplicate in members.Skip(1))
            {
                if (!duplicate.IsArchived)
                {
                    Replace(providers, value => value.Id == duplicate.Id, duplicate with { IsArchived = true, UpdatedAt = now });
                    changed = true;
                }

                for (var index = 0; index < offerings.Count; index++)
                {
                    var offering = offerings[index];
                    if (offering.StoreId == storeId && offering.PrintProviderId == duplicate.Id)
                    {
                        offerings[index] = offering with { PrintProviderId = survivor.Id, UpdatedAt = now };
                        changed = true;
                    }
                }
            }
        }

        return (source with { PrintProviders = providers, BlueprintOfferings = offerings }, changed);
    }

    public static string MergeExternalProviderMetadata(PrintProvider provider, IEnumerable<string> additionalIds)
    {
        var metadata = ParseObject(provider.MetadataJson) ?? new JsonObject();
        var aliases = ExternalProviderIds(provider)
            .Concat(additionalIds)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (aliases.Length == 0)
            return metadata.ToJsonString();

        var values = new JsonArray();
        foreach (var alias in aliases)
            values.Add(alias);
        metadata[ExternalProviderIdsProperty] = values;
        return metadata.ToJsonString();
    }

    private static IEnumerable<string> ExternalProviderIds(PrintProvider provider)
    {
        if (!string.IsNullOrWhiteSpace(provider.ExternalProviderId))
            yield return provider.ExternalProviderId;

        var metadata = ParseObject(provider.MetadataJson);
        if (metadata?[ExternalProviderIdsProperty] is JsonArray aliases)
        {
            foreach (var alias in aliases)
            {
                if (alias is JsonValue value && value.TryGetValue<string>(out var id) && !string.IsNullOrWhiteSpace(id))
                    yield return id;
            }
        }

        if (metadata?["source"] is JsonValue source
            && source.TryGetValue<string>(out var sourceName)
            && sourceName == "printify"
            && metadata["ids"] is JsonArray ids)
        {
            foreach (var id in ids)
            {
                if (id is JsonValue value && value.TryGetValue<int>(out var numericId))
                    yield return numericId.ToString();
            }
        }
    }

    private static JsonObject? ParseObject(string json)
    {
        try
        {
            return JsonNode.Parse(json) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void Replace(List<PrintProvider> values, Func<PrintProvider, bool> match, PrintProvider replacement)
    {
        var index = values.FindIndex(value => match(value));
        if (index >= 0)
            values[index] = replacement;
    }
}

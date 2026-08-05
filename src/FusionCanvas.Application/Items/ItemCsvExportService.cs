using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Tags;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Items;

public sealed class ItemCsvExportService : IItemCsvExportService
{
    public IReadOnlyList<ItemCsvRow> Project(
        WorkspaceSnapshot snapshot,
        WorkspaceEntityKind topicKind,
        Guid topicId)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var items = topicKind switch
        {
            WorkspaceEntityKind.Group => ProjectGroup(snapshot, topicId),
            WorkspaceEntityKind.Niche => ProjectNiche(snapshot, topicId),
            _ => throw new ArgumentOutOfRangeException(
                nameof(topicKind),
                topicKind,
                "Only Niche and Group topics can be exported.")
        };

        return items
            .Where(item => ItemHierarchy.IsEffectivelyActive(snapshot, item))
            .Where(item => !IsEmpty(snapshot, item))
            .OrderBy(item => item.CreatedAt)
            .ThenBy(item => item.Id)
            .Select(item => ProjectRow(snapshot, item))
            .ToArray();
    }

    private static IReadOnlyList<Item> ProjectGroup(WorkspaceSnapshot snapshot, Guid groupId)
    {
        var group = snapshot.Groups.SingleOrDefault(candidate => candidate.Id == groupId);
        if (group is null)
        {
            return [];
        }

        var groupIds = GroupHierarchy.GetDescendants(snapshot, group)
            .Append(group)
            .Select(candidate => candidate.Id)
            .ToHashSet();

        return snapshot.Items
            .Where(item => item.GroupId is Guid id && groupIds.Contains(id))
            .ToArray();
    }

    private static IReadOnlyList<Item> ProjectNiche(WorkspaceSnapshot snapshot, Guid nicheId)
    {
        var niche = snapshot.Niches.SingleOrDefault(candidate => candidate.Id == nicheId);
        if (niche is null)
        {
            return [];
        }

        return snapshot.Items
            .Where(item =>
            {
                var effectiveNiche = ItemHierarchy.GetEffectiveNiche(snapshot, item);
                return effectiveNiche.Id == nicheId;
            })
            .ToArray();
    }

    private static bool IsEmpty(WorkspaceSnapshot snapshot, Item item)
    {
        if (!string.IsNullOrWhiteSpace(item.Name))
        {
            return false;
        }

        var metadata = ItemMetadataCodec.ParseMetadata(item.MetadataJson);
        var hasText = HasText(metadata, ItemMetadataCodec.NotesKey)
            || HasText(metadata, ItemMetadataCodec.IdeaKey)
            || HasText(metadata, ItemMetadataCodec.ConceptIdeaKey)
            || HasText(metadata, ItemMetadataCodec.PhraseKey)
            || HasText(metadata, ItemMetadataCodec.GraphicDirectionKey);

        var hasTags = snapshot.ItemTags.Any(link => link.ItemId == item.Id);

        return !hasText && !hasTags;
    }

    private static bool HasText(IReadOnlyDictionary<string, string> metadata, string key) =>
        metadata.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value);

    private static ItemCsvRow ProjectRow(WorkspaceSnapshot snapshot, Item item)
    {
        var metadata = ItemMetadataCodec.ParseMetadata(item.MetadataJson);

        var tags = snapshot.ItemTags
            .Where(link => link.ItemId == item.Id)
            .Select(link => snapshot.Tags.SingleOrDefault(tag => tag.Id == link.TagId))
            .Where(tag => tag is not null)
            .Select(tag => tag!.Name)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new ItemCsvRow(
            item.Name,
            Read(metadata, ItemMetadataCodec.IdeaKey),
            Read(metadata, ItemMetadataCodec.ConceptIdeaKey),
            Read(metadata, ItemMetadataCodec.PhraseKey),
            Read(metadata, ItemMetadataCodec.GraphicDirectionKey),
            Read(metadata, ItemMetadataCodec.NotesKey),
            string.Join(", ", tags));
    }

    private static string? Read(IReadOnlyDictionary<string, string> metadata, string key) =>
        metadata.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;
}

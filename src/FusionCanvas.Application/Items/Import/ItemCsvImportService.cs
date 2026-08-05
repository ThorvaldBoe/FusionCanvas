using FusionCanvas.Application.ToolContexts;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Tags;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;

namespace FusionCanvas.Application.Items.Import;

public sealed class ItemCsvImportService : IItemCsvImportService
{
    private readonly IWorkspaceRepository _repository;
    private readonly IToolContextResolver _contextResolver;
    private readonly Func<DateTimeOffset> _clock;
    private readonly IItemIdGenerator _idGenerator;

    public ItemCsvImportService(
        IWorkspaceRepository repository,
        IToolContextResolver? contextResolver = null,
        Func<DateTimeOffset>? clock = null,
        Func<Guid>? newId = null)
        : this(repository, contextResolver, clock, newId is null ? null : new DelegateItemIdGenerator(newId))
    {
    }

    public ItemCsvImportService(
        IWorkspaceRepository repository,
        IToolContextResolver? contextResolver,
        Func<DateTimeOffset>? clock,
        IItemIdGenerator? idGenerator)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _contextResolver = contextResolver ?? new ToolContextResolver();
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _idGenerator = idGenerator ?? new GuidItemIdGenerator();
    }

    public async Task<ItemCsvImportResult> ImportAsync(
        ItemCsvImportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (!TryResolveTarget(snapshot, request.Target, out var storeId, out var nicheId, out var groupId, out var targetError))
        {
            return ItemCsvImportResult.Failure(targetError!);
        }

        var contextResolution = _contextResolver.Resolve(new ToolContextResolveRequest(
            snapshot,
            ToolContextSelectionKind.Topic,
            request.Target.Kind,
            request.Target.Id));
        var creationDefaults = contextResolution.IsAvailable
            ? _contextResolver.ResolveCreationDefaults(contextResolution)
            : null;

        var now = _clock();
        var errors = new List<string>();
        var newItems = new List<Item>();
        var newTags = new List<Tag>();
        var newItemTags = new List<ItemTag>();

        foreach (var row in request.Rows)
        {
            var name = ItemMetadataCodec.NormalizeName(row.Title);
            var nameError = ItemMetadataCodec.ValidateName(name);
            if (nameError is not null)
            {
                errors.Add($"Error on line {row.LineNumber}: {nameError}");
                continue;
            }

            var stage = ShouldCreateAtConcept(row) ? WorkflowStage.Concept : WorkflowStage.Idea;
            var itemId = _idGenerator.NewId();
            if (itemId == Guid.Empty)
            {
                errors.Add($"Error on line {row.LineNumber}: Item identity could not be generated.");
                continue;
            }

            var item = new Item(
                itemId,
                storeId,
                nicheId,
                groupId,
                name,
                null,
                ItemStatus.Draft,
                stage,
                false,
                now,
                now,
                ItemMetadataCodec.SerializeMetadata(BuildMetadata(creationDefaults, row)));
            newItems.Add(item);

            foreach (var tagId in ResolveTagIds(snapshot, storeId, creationDefaults, row.Tags, newTags))
            {
                newItemTags.Add(new ItemTag(itemId, tagId));
            }
        }

        if (errors.Count > 0)
        {
            return ItemCsvImportResult.Failure(errors);
        }

        var updated = snapshot with
        {
            Items = [.. snapshot.Items, .. newItems],
            Tags = newTags.Count > 0 ? [.. snapshot.Tags, .. newTags] : snapshot.Tags,
            ItemTags = [.. snapshot.ItemTags, .. newItemTags]
        };

        try
        {
            await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return ItemCsvImportResult.Failure($"The import could not be saved. {exception.Message}");
        }

        return ItemCsvImportResult.Success(newItems.Count);
    }

    private static bool TryResolveTarget(
        WorkspaceSnapshot snapshot,
        ItemTopicReference topic,
        out Guid storeId,
        out Guid nicheId,
        out Guid? groupId,
        out string? error)
    {
        storeId = Guid.Empty;
        nicheId = Guid.Empty;
        groupId = null;
        error = null;

        if (topic.Kind == WorkspaceEntityKind.Niche)
        {
            var niche = snapshot.Niches.SingleOrDefault(candidate => candidate.Id == topic.Id);
            if (niche is null || niche.IsArchived)
            {
                error = "The destination niche must exist and be active.";
                return false;
            }

            var store = snapshot.Stores.SingleOrDefault(candidate => candidate.Id == niche.StoreId && !candidate.IsArchived);
            if (store is null)
            {
                error = "The destination store must be active.";
                return false;
            }

            storeId = store.Id;
            nicheId = niche.Id;
            return true;
        }

        var group = snapshot.Groups.SingleOrDefault(candidate => candidate.Id == topic.Id);
        if (group is null || !GroupHierarchy.IsEffectivelyActive(snapshot, group))
        {
            error = "The destination group and its complete parent path must be active.";
            return false;
        }

        var groupStore = snapshot.Stores.SingleOrDefault(candidate => candidate.Id == group.StoreId && !candidate.IsArchived);
        if (groupStore is null)
        {
            error = "The destination store must be active.";
            return false;
        }

        storeId = groupStore.Id;
        nicheId = GroupHierarchy.GetEffectiveNiche(snapshot, group).Id;
        groupId = group.Id;
        return true;
    }

    private static Dictionary<string, string> BuildMetadata(
        ToolContextCreationDefaults? defaults,
        ItemCsvRow row)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal);
        if (defaults is not null)
        {
            foreach (var value in defaults.Metadata)
            {
                metadata[value.Key] = value.Value;
                metadata[$"{ItemMetadataCodec.InheritedFromPrefix}{value.Key}"] = $"{value.Source.EntityKind}:{value.Source.EntityId}";
            }
        }

        ApplyCsvField(metadata, ItemMetadataCodec.IdeaKey, row.BaseIdea);
        ApplyCsvField(metadata, ItemMetadataCodec.ConceptIdeaKey, row.ConceptIdea);
        ApplyCsvField(metadata, ItemMetadataCodec.PhraseKey,
            string.IsNullOrWhiteSpace(row.Phrase) ? null : ItemMetadataCodec.NormalizeSingleLine(row.Phrase));
        ApplyCsvField(metadata, ItemMetadataCodec.GraphicDirectionKey, row.Graphic);
        ApplyCsvField(metadata, ItemMetadataCodec.NotesKey, row.Notes);
        return metadata;
    }

    private static void ApplyCsvField(Dictionary<string, string> metadata, string key, string? value)
    {
        ItemMetadataCodec.SetOptional(metadata, key, value);
        metadata.Remove($"{ItemMetadataCodec.InheritedFromPrefix}{key}");
    }

    private static bool ShouldCreateAtConcept(ItemCsvRow row) =>
        !string.IsNullOrWhiteSpace(row.ConceptIdea) ||
        !string.IsNullOrWhiteSpace(row.Phrase) ||
        !string.IsNullOrWhiteSpace(row.Graphic);

    private IReadOnlyList<Guid> ResolveTagIds(
        WorkspaceSnapshot snapshot,
        Guid storeId,
        ToolContextCreationDefaults? defaults,
        IReadOnlyList<string> csvTags,
        List<Tag> createdTags)
    {
        var resolved = new HashSet<Guid>();

        if (defaults is not null)
        {
            foreach (var inherited in defaults.Tags)
            {
                var tag = snapshot.Tags
                    .Concat(createdTags)
                    .FirstOrDefault(candidate => candidate.StoreId == storeId && !candidate.IsArchived &&
                        string.Equals(candidate.Name, inherited.Value, StringComparison.OrdinalIgnoreCase));
                if (tag is not null)
                {
                    resolved.Add(tag.Id);
                }
            }
        }

        foreach (var rawName in csvTags)
        {
            var trimmed = rawName.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            var existing = snapshot.Tags
                .Concat(createdTags)
                .FirstOrDefault(candidate => candidate.StoreId == storeId && !candidate.IsArchived &&
                    string.Equals(candidate.Name, trimmed, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                resolved.Add(existing.Id);
                continue;
            }

            var created = new Tag(_idGenerator.NewId(), storeId, trimmed, null, false, _clock(), _clock(), "{}");
            createdTags.Add(created);
            resolved.Add(created.Id);
        }

        return resolved.ToArray();
    }
}

using System.Text.Json;
using FusionCanvas.Application.AI;
using FusionCanvas.Application.ConceptRefinement;
using FusionCanvas.Application.Items;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Concepts;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.SllGeneration;

public sealed class SllGenerationService : ISllGenerationService
{
    private readonly IWorkspaceRepository _repository;
    private readonly IAiTextGenerationService _ai;
    private readonly IDesignTriangleGuidanceSource _guidance;

    public SllGenerationService(
        IWorkspaceRepository repository,
        IAiTextGenerationService ai,
        IDesignTriangleGuidanceSource guidance)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _ai = ai ?? throw new ArgumentNullException(nameof(ai));
        _guidance = guidance ?? throw new ArgumentNullException(nameof(guidance));
    }

    public async Task<SllGenerationResult> GenerateAsync(
        Guid itemId,
        ConceptRefinementTriangle triangle,
        string originalIdea,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(triangle);
        ArgumentNullException.ThrowIfNull(originalIdea);

        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var context = ResolveCreativeContext(snapshot, itemId);
        var frameworkText = _guidance.Load();

        var systemMessage = new AiTextMessage(
            AiMessageRole.System,
            $"""
            You are a PoD Sketch Layout Language (SLL) generator using the Design Triangle framework.

            {frameworkText}

            Output rules:
            - Respond with exactly these labelled blocks, in order:
              ASSUMPTIONS, INTENT, TRIANGLE, ASCII_SKETCH, NOTES, VALIDATION.
            - The ASCII_SKETCH block must contain one complete plain-ASCII composition sketch,
              and it must be enclosed in a code fence.
            - Preserve the supplied Phrase exactly. If you must revise it, add a line
              "REVISED PHRASE: <revised>" inside the TRIANGLE block.
            - Output only the labelled blocks. No preamble or explanation.
            """);

        var userMessage = new AiTextMessage(
            AiMessageRole.User,
            $"""
            Create SLL for this Design Triangle.

            Original idea: {originalIdea}

            Current Concept idea: {triangle.ConceptIdea}
            Current Phrase: {triangle.Phrase}
            Current Graphic direction: {triangle.GraphicDirection}

            Creative context:
            Store: {context.StoreName}
            Store description: {context.StoreDescription}
            Niche: {context.NicheName}
            Niche description: {context.NicheDescription}
            Topic: {context.GroupName ?? "(none)"}
            Tags: {string.Join(", ", context.Tags)}

            {FormatMetadata("Store metadata", context.StoreMetadata)}
            {FormatMetadata("Niche metadata", context.NicheMetadata)}
            {FormatMetadata("Topic metadata", context.GroupMetadata)}
            """);

        var request = new AiTextRequest(
            AiRequestPurpose.Sll,
            [systemMessage, userMessage]);

        var result = await _ai.GenerateAsync(request, cancellationToken).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            return SllGenerationResult.Failure(
                result.FailureKind ?? AiTextFailureKind.ProviderFailure,
                result.Message ?? "The AI provider could not generate an SLL.");
        }

        if (string.IsNullOrWhiteSpace(result.Text))
        {
            return SllGenerationResult.Failure(
                AiTextFailureKind.InvalidProviderResponse,
                "The AI response was empty.");
        }

        return ParseResponse(result.Text, triangle.Phrase);
    }

    private static SllGenerationResult ParseResponse(string text, string suppliedPhrase)
    {
        var blocks = SplitBlocks(text);
        if (!blocks.TryGetValue("ASSUMPTIONS", out var assumptionsBlock) ||
            !blocks.TryGetValue("INTENT", out var intentBlock) ||
            !blocks.TryGetValue("TRIANGLE", out var triangleBlock) ||
            !blocks.TryGetValue("ASCII_SKETCH", out var sketchBlock) ||
            !blocks.TryGetValue("NOTES", out var notesBlock) ||
            !blocks.TryGetValue("VALIDATION", out var validationBlock))
        {
            return SllGenerationResult.Failure(
                AiTextFailureKind.InvalidProviderResponse,
                "The AI response did not contain all required SLL blocks.");
        }

        var assumptions = ParseList(assumptionsBlock);
        var communication = ParseCommunication(intentBlock);
        var triangle = ParseTriangle(triangleBlock);
        var sketch = StripFence(sketchBlock);
        var notes = ParseNotes(notesBlock);
        var validation = ParseValidation(validationBlock);

        var document = new SllDocument(assumptions, communication, triangle, sketch, notes, validation);
        if (!document.Validate(suppliedPhrase))
        {
            return SllGenerationResult.Failure(
                AiTextFailureKind.InvalidProviderResponse,
                "The SLL did not preserve the supplied phrase or contained an empty sketch.");
        }

        return SllGenerationResult.Success(document);
    }

    private static Dictionary<string, string> SplitBlocks(string text)
    {
        var markers = new[] { "ASSUMPTIONS", "INTENT", "TRIANGLE", "ASCII_SKETCH", "NOTES", "VALIDATION" };
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var current = new List<string>();
        string? currentKey = null;

        foreach (var rawLine in text.Split(['\r', '\n']))
        {
            var line = rawLine.TrimEnd();
            var marker = MatchMarker(line, markers);
            if (marker is not null)
            {
                if (currentKey is not null)
                {
                    result[currentKey] = string.Join("\n", current).Trim();
                }

                currentKey = marker;
                current = new List<string>();
            }
            else if (currentKey is not null)
            {
                current.Add(line);
            }
        }

        if (currentKey is not null)
        {
            result[currentKey] = string.Join("\n", current).Trim();
        }

        return result;
    }

    private static string? MatchMarker(string line, string[] markers)
    {
        var trimmed = line.TrimStart(' ', '-', '*', '#');
        foreach (var marker in markers)
        {
            if (trimmed.StartsWith(marker, StringComparison.OrdinalIgnoreCase))
            {
                var rest = trimmed[marker.Length..];
                if (rest.Length == 0 || rest[0] == ':')
                {
                    return marker;
                }
            }
        }

        return null;
    }

    private static string StripFence(string block)
    {
        if (string.IsNullOrWhiteSpace(block))
        {
            return block;
        }

        return string.Join("\n", block
            .Split(['\r', '\n'])
            .Where(line => !line.TrimStart().StartsWith("`"))
            .Select(line => line.TrimEnd()))
            .Trim();
    }

    private static IReadOnlyList<string> ParseList(string block) =>
        block.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.TrimStart(' ', '-', '*', '•'))
            .Where(line => line.Length > 0)
            .ToArray();

    private static SllCommunication ParseCommunication(string block)
    {
        var kv = ParseKeyValues(block);
        return new SllCommunication(
            kv.GetValueOrDefault("wearersignal"),
            kv.GetValueOrDefault("viewerinference"),
            kv.GetValueOrDefault("emotion"),
            kv.GetValueOrDefault("sharedcontext"));
    }

    private static SllTriangle ParseTriangle(string block)
    {
        var kv = ParseKeyValues(block);
        return new SllTriangle(
            kv.GetValueOrDefault("idea"),
            kv.GetValueOrDefault("phrase"),
            kv.GetValueOrDefault("graphic"),
            kv.GetValueOrDefault("relationship"),
            kv.GetValueOrDefault("revisedphrase"));
    }

    private static SllNotes ParseNotes(string block)
    {
        var kv = ParseKeyValues(block);
        return new SllNotes(
            kv.GetValueOrDefault("composition"),
            kv.GetValueOrDefault("typography"),
            kv.GetValueOrDefault("graphicstyle"),
            kv.GetValueOrDefault("colors"),
            kv.GetValueOrDefault("textureeffects"),
            kv.GetValueOrDefault("placementscale"),
            kv.GetValueOrDefault("production"));
    }

    private static SllValidation ParseValidation(string block)
    {
        var kv = ParseKeyValues(block);
        return new SllValidation(
            kv.GetValueOrDefault("readingorder"),
            kv.GetValueOrDefault("thumbnail"),
            kv.GetValueOrDefault("signal"),
            kv.GetValueOrDefault("largestrisk"));
    }

    private static Dictionary<string, string> ParseKeyValues(string block)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in block.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var colonIndex = line.IndexOf(':');
            if (colonIndex <= 0)
            {
                continue;
            }

            var key = NormalizeKey(line[..colonIndex]);
            var value = line[(colonIndex + 1)..].Trim();
            if (key.Length > 0 && value.Length > 0)
            {
                result[key] = value;
            }
        }

        return result;
    }

    private static string NormalizeKey(string key) =>
        new string(key.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

    private static CreativeContext ResolveCreativeContext(WorkspaceSnapshot snapshot, Guid itemId)
    {
        var item = snapshot.Items.SingleOrDefault(i => i.Id == itemId);
        if (item is null)
        {
            throw new InvalidOperationException($"Item {itemId} not found.");
        }

        var store = snapshot.Stores.SingleOrDefault(s => s.Id == item.StoreId);
        var niche = snapshot.Niches.SingleOrDefault(n => n.Id == item.NicheId);
        TopicGroup? group = item.GroupId is Guid gid
            ? snapshot.Groups.SingleOrDefault(g => g.Id == gid)
            : null;

        var tagIds = snapshot.ItemTags
            .Where(it => it.ItemId == itemId)
            .Select(it => it.TagId)
            .ToHashSet();

        var tags = snapshot.Tags
            .Where(t => tagIds.Contains(t.Id) && !t.IsArchived)
            .Select(t => t.Name)
            .ToList();

        return new CreativeContext(
            store?.Name ?? "",
            store?.Description ?? "",
            niche?.Name ?? "",
            niche?.Description ?? "",
            group?.Name,
            SanitizeMetadata(store?.MetadataJson),
            SanitizeMetadata(niche?.MetadataJson),
            group is not null ? SanitizeMetadata(group.MetadataJson) : new Dictionary<string, string>(),
            tags);
    }

    private static IReadOnlyDictionary<string, string> SanitizeMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson) || metadataJson.Trim() == "{}")
        {
            return new Dictionary<string, string>();
        }

        Dictionary<string, string> parsed;
        try
        {
            parsed = ItemMetadataCodec.ParseMetadata(metadataJson);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>();
        }

        return parsed
            .Where(pair => !IsOperationalKey(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }

    private static bool IsOperationalKey(string key)
    {
        var normalized = key.Trim().ToLowerInvariant();
        var compact = new string(normalized.Where(char.IsLetterOrDigit).ToArray());
        return normalized.StartsWith(ItemMetadataCodec.InheritedFromPrefix.ToLowerInvariant(), StringComparison.Ordinal) ||
               compact is "id" or "createdat" or "updatedat" or "isarchived" or "status" ||
               compact.Contains("inherited", StringComparison.Ordinal) ||
               compact.Contains("path", StringComparison.Ordinal) ||
               compact.Contains("apikey", StringComparison.Ordinal) ||
               compact.Contains("credential", StringComparison.Ordinal) ||
               compact.Contains("secret", StringComparison.Ordinal) ||
               compact.Contains("token", StringComparison.Ordinal);
    }

    private static string FormatMetadata(string label, IReadOnlyDictionary<string, string> metadata)
    {
        if (metadata.Count == 0)
        {
            return string.Empty;
        }

        var entries = string.Join("; ", metadata.Select(pair => $"{pair.Key}={pair.Value}"));
        return $"{label}: {entries}";
    }

    private sealed record CreativeContext(
        string StoreName,
        string StoreDescription,
        string NicheName,
        string NicheDescription,
        string? GroupName,
        IReadOnlyDictionary<string, string> StoreMetadata,
        IReadOnlyDictionary<string, string> NicheMetadata,
        IReadOnlyDictionary<string, string> GroupMetadata,
        IReadOnlyList<string> Tags);
}

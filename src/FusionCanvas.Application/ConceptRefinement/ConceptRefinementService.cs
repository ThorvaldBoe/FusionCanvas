using System.Text.Json;
using FusionCanvas.Application.AI;
using FusionCanvas.Application.Items;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.ConceptRefinement;

public sealed class ConceptRefinementService : IConceptRefinementService
{
    private readonly IWorkspaceRepository _repository;
    private readonly IAiTextGenerationService _ai;
    private readonly IDesignTriangleGuidanceSource _guidance;

    public ConceptRefinementService(
        IWorkspaceRepository repository,
        IAiTextGenerationService ai,
        IDesignTriangleGuidanceSource guidance)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _ai = ai ?? throw new ArgumentNullException(nameof(ai));
        _guidance = guidance ?? throw new ArgumentNullException(nameof(guidance));
    }

    public async Task<ConceptRefinementResult> InitializeAsync(
        Guid itemId,
        string originalIdea,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(originalIdea);

        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var context = ResolveCreativeContext(snapshot, itemId);
        var guidanceText = _guidance.Load();

        var systemMessage = new AiTextMessage(
            AiMessageRole.System,
            $"""
            You are a PoD concept-refinement assistant.

            {guidanceText}

            Use the framework to preserve a clear wearer signal, intended viewer inference or effect, and shared audience context. Ensure Idea, Phrase, and Graphic form a coherent triangle; the Phrase and Graphic should intentionally reinforce, complete, or contrast with each other, and the Graphic should have a semantic role rather than being decoration.

            Output rules:
            - Respond with exactly three labeled lines in this format:
              IDEA: <concept idea>
              PHRASE: <phrase>
              GRAPHIC: <graphic direction>
            - Each label is case-insensitive.
            - The phrase must be a single line.
            - All three values must be non-empty.
            """);

        var userMessage = new AiTextMessage(
            AiMessageRole.User,
            $"""
            Initialize the design triangle from the original idea.

            Original idea: {originalIdea}

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
            AiRequestPurpose.Concept,
            [systemMessage, userMessage]);

        var result = await _ai.GenerateAsync(request, cancellationToken).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            return ConceptRefinementResult.Failure(
                result.FailureKind ?? AiTextFailureKind.ProviderFailure,
                result.Message ?? "The AI provider could not generate a concept.");
        }

        if (string.IsNullOrWhiteSpace(result.Text))
        {
            return ConceptRefinementResult.Failure(
                AiTextFailureKind.InvalidProviderResponse,
                "The AI response was empty.");
        }

        return ParseInitializeResponse(result.Text);
    }

    public async Task<ConceptRefinementResult> RefineAsync(
        Guid itemId,
        ConceptRefinementActionKind action,
        ConceptRefinementCorner corner,
        ConceptRefinementTriangle current,
        string originalIdea,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(originalIdea);

        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var context = ResolveCreativeContext(snapshot, itemId);
        var guidanceText = _guidance.Load();

        var cornerLabel = corner switch
        {
            ConceptRefinementCorner.ConceptIdea => "Concept idea",
            ConceptRefinementCorner.Phrase => "Phrase",
            ConceptRefinementCorner.GraphicDirection => "Graphic direction",
            _ => throw new ArgumentOutOfRangeException(nameof(corner))
        };

        var instruction = action switch
        {
            ConceptRefinementActionKind.FineTune =>
                $"Improve the {cornerLabel} while preserving its direction, given the other two corners.",
            ConceptRefinementActionKind.Change =>
                $"Propose a materially different direction for the {cornerLabel} that still works with the other two corners.",
            _ => throw new ArgumentOutOfRangeException(nameof(action))
        };

        var cornerValue = corner switch
        {
            ConceptRefinementCorner.ConceptIdea => current.ConceptIdea,
            ConceptRefinementCorner.Phrase => current.Phrase,
            ConceptRefinementCorner.GraphicDirection => current.GraphicDirection,
            _ => ""
        };

        // FineTune is disabled for empty corners upstream; Change is allowed.
        var currentTriangleText =
            $"""
            Current Concept idea: {current.ConceptIdea}
            Current Phrase: {current.Phrase}
            Current Graphic direction: {current.GraphicDirection}
            """;

        var systemMessage = new AiTextMessage(
            AiMessageRole.System,
            $"""
            You are a PoD concept-refinement assistant.

            {guidanceText}

            Use the framework to preserve the triangle's social proposition and audience recognition. Fine tune should strengthen the requested corner without weakening the other two; Change should be materially different but still coherent. Keep the Phrase/Graphic relationship intentional and ensure the Graphic contributes meaning rather than merely illustrating a noun.

            Output rules:
            - Respond with only the new value for the requested corner.
            - Do not include any label prefix, explanation, or surrounding text.
            - If you include a label prefix (e.g., "IDEA:"), it will be stripped.
            - The phrase must be a single line.
            - The value must be non-empty.
            """);

        var userMessage = new AiTextMessage(
            AiMessageRole.User,
            $"""
            {instruction}

            {currentTriangleText}

            Target corner ({cornerLabel}): {cornerValue}

            Original idea: {originalIdea}

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
            AiRequestPurpose.Concept,
            [systemMessage, userMessage]);

        var result = await _ai.GenerateAsync(request, cancellationToken).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            return ConceptRefinementResult.Failure(
                result.FailureKind ?? AiTextFailureKind.ProviderFailure,
                result.Message ?? "The AI provider could not generate a refinement.");
        }

        if (string.IsNullOrWhiteSpace(result.Text))
        {
            return ConceptRefinementResult.Failure(
                AiTextFailureKind.InvalidProviderResponse,
                "The AI response was empty.");
        }

        return ParseRefineResponse(result.Text, corner);
    }

    private static ConceptRefinementResult ParseInitializeResponse(string text)
    {
        var idea = ExtractLabeledValue(text, "IDEA");
        var phrase = ExtractLabeledValue(text, "PHRASE");
        var graphic = ExtractLabeledValue(text, "GRAPHIC");

        if (string.IsNullOrWhiteSpace(idea) ||
            string.IsNullOrWhiteSpace(phrase) ||
            string.IsNullOrWhiteSpace(graphic))
        {
            return ConceptRefinementResult.Failure(
                AiTextFailureKind.InvalidProviderResponse,
                "The AI response did not contain all three required labeled values (IDEA, PHRASE, GRAPHIC).");
        }

        return ConceptRefinementResult.Success(idea.Trim(), NormalizePhrase(phrase), graphic.Trim());
    }

    private static ConceptRefinementResult ParseRefineResponse(string text, ConceptRefinementCorner corner)
    {
        var value = text.Trim();

        // Strip optional leading LABEL: prefix
        var colonIndex = value.IndexOf(':');
        if (colonIndex > 0 && colonIndex < 10)
        {
            var prefix = value[..colonIndex].Trim().ToUpperInvariant();
            if (prefix is "IDEA" or "PHRASE" or "GRAPHIC")
            {
                value = value[(colonIndex + 1)..].Trim();
            }
        }

        // Strip surrounding quotes
        if ((value.StartsWith('"') && value.EndsWith('"')) ||
            (value.StartsWith('\'') && value.EndsWith('\'')))
        {
            value = value[1..^1].Trim();
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return ConceptRefinementResult.Failure(
                AiTextFailureKind.InvalidProviderResponse,
                "The AI response was empty after parsing.");
        }

        if (corner == ConceptRefinementCorner.Phrase)
        {
            value = NormalizePhrase(value);
        }

        return corner switch
        {
            ConceptRefinementCorner.ConceptIdea => ConceptRefinementResult.Success(value, null, null),
            ConceptRefinementCorner.Phrase => ConceptRefinementResult.Success(null, value, null),
            ConceptRefinementCorner.GraphicDirection => ConceptRefinementResult.Success(null, null, value),
            _ => ConceptRefinementResult.Failure(
                AiTextFailureKind.InvalidProviderResponse,
                "Unknown corner.")
        };
    }

    private static string? ExtractLabeledValue(string text, string label)
    {
        foreach (var line in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            var colonIndex = trimmed.IndexOf(':');
            if (colonIndex < 0)
            {
                continue;
            }

            var candidateLabel = trimmed[..colonIndex].Trim();
            if (string.Equals(candidateLabel, label, StringComparison.OrdinalIgnoreCase))
            {
                var value = trimmed[(colonIndex + 1)..].Trim();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
        }

        return null;
    }

    private static string NormalizePhrase(string value) =>
        ItemMetadataCodec.NormalizeSingleLine(value);

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

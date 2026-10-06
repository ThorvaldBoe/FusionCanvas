using System.Text.Json;
using System.Text.Json.Nodes;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.Application.Mockups;

public sealed class MockupOutputResolutionSettingsService : IMockupOutputResolutionSettingsService
{
    private const string MaximumLongEdgeKey = "mockupMaximumLongEdgePixels";

    private readonly IWorkspaceRepository _repository;
    private readonly Func<DateTimeOffset> _clock;

    public MockupOutputResolutionSettingsService(IWorkspaceRepository repository, Func<DateTimeOffset>? clock = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public async Task<MockupOutputResolutionSettingsState> LoadAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var store = snapshot.Stores.SingleOrDefault(value => value.Id == storeId);
        return store is null
            ? new(storeId, true, MockupOutputResolutionPolicy.Default, "Store was not found.")
            : new(store.Id, store.IsArchived, ReadPolicy(store.MetadataJson));
    }

    public async Task<MockupOutputResolutionSettingsResult> SaveAsync(Guid storeId, int maximumLongEdgePixels, CancellationToken cancellationToken = default)
    {
        MockupOutputResolutionPolicy policy;
        try
        {
            policy = new MockupOutputResolutionPolicy(maximumLongEdgePixels);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            var state = await LoadAsync(storeId, cancellationToken).ConfigureAwait(false);
            return MockupOutputResolutionSettingsResult.Failure(exception.Message, state);
        }

        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var store = snapshot.Stores.SingleOrDefault(value => value.Id == storeId);
        if (store is null)
        {
            return MockupOutputResolutionSettingsResult.Failure(
                "Store was not found.",
                new(storeId, true, MockupOutputResolutionPolicy.Default, "Store was not found."));
        }

        var current = new MockupOutputResolutionSettingsState(store.Id, store.IsArchived, ReadPolicy(store.MetadataJson));
        if (store.IsArchived)
            return MockupOutputResolutionSettingsResult.Failure("Archived Stores are read-only.", current);

        var updatedStore = store with
        {
            UpdatedAt = _clock(),
            MetadataJson = WritePolicy(store.MetadataJson, policy)
        };
        var updated = snapshot with
        {
            Stores = snapshot.Stores.Select(value => value.Id == storeId ? updatedStore : value).ToArray()
        };

        try
        {
            await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return MockupOutputResolutionSettingsResult.Failure(exception.Message, current);
        }

        return MockupOutputResolutionSettingsResult.Success(new(storeId, false, policy));
    }

    public static MockupOutputResolutionPolicy ReadPolicy(string? metadataJson)
    {
        try
        {
            if (JsonNode.Parse(metadataJson ?? "{}") is not JsonObject metadata)
                return MockupOutputResolutionPolicy.Default;

            var value = metadata[MaximumLongEdgeKey]?.GetValue<int?>();
            return value is > 0
                ? new MockupOutputResolutionPolicy(value.Value)
                : MockupOutputResolutionPolicy.Default;
        }
        catch (JsonException)
        {
            return MockupOutputResolutionPolicy.Default;
        }
        catch (InvalidOperationException)
        {
            return MockupOutputResolutionPolicy.Default;
        }
        catch (FormatException)
        {
            return MockupOutputResolutionPolicy.Default;
        }
    }

    public static string WritePolicy(string? metadataJson, MockupOutputResolutionPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        var metadata = ParseObject(metadataJson);
        metadata[MaximumLongEdgeKey] = policy.MaximumLongEdgePixels;
        return metadata.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    private static JsonObject ParseObject(string? metadataJson)
    {
        try
        {
            return JsonNode.Parse(metadataJson ?? "{}") as JsonObject ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}

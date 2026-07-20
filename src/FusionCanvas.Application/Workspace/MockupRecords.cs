using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Workspace;

public sealed record MockupSummary(
    Guid Id, Guid StoreId, Guid ListingId, Guid? DesignId,
    string Name, string? SourceMethod, string? ProductType, string? VendorProduct,
    string? Template, string? ColorVariant, string? View, string? Notes,
    string? IntendedMarketplaceUse, bool IsArchived,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record MockupRecordsState(
    Guid? ActiveStoreId, Guid? ActiveListingId,
    IReadOnlyList<MockupSummary> ActiveMockups,
    IReadOnlyList<MockupSummary> ArchivedMockups)
{
    public static MockupRecordsState Empty { get; } = new(null, null, [], []);
}

public sealed record MockupRecordsResult(
    bool Succeeded, string? Error, MockupSummary? Mockup, MockupRecordsState State)
{
    public static MockupRecordsResult Success(MockupSummary? mockup, MockupRecordsState state) => new(true, null, mockup, state);
    public static MockupRecordsResult Failure(string error, MockupRecordsState state) => new(false, string.IsNullOrWhiteSpace(error) ? "Mockup operation failed." : error, null, state);
}

public sealed record MockupCreateRequest(
    Guid ListingId, string Name, Guid? DesignId = null,
    string? SourceMethod = null, string? ProductType = null,
    string? VendorProduct = null, string? Template = null,
    string? ColorVariant = null, string? View = null,
    string? Notes = null, string? IntendedMarketplaceUse = null,
    string? RegenerationMetadataJson = null);

public interface IMockupRecordsService
{
    void SetActiveWorkspace(Guid? workspaceId);
    Task<MockupRecordsState> LoadAsync(Guid? listingId, CancellationToken cancellationToken = default);
    Task<MockupRecordsResult> CreateAsync(MockupCreateRequest request, CancellationToken cancellationToken = default);
    Task<MockupRecordsResult> ArchiveAsync(Guid mockupId, CancellationToken cancellationToken = default);
}

public sealed class MockupRecordsService : IMockupRecordsService
{
    private readonly IWorkspaceRepository _repository;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Func<Guid> _newId;
    private Guid? _activeWorkspaceId;
    private Guid? _activeStoreId;
    private Guid? _activeListingId;

    public MockupRecordsService(IWorkspaceRepository repository, Func<DateTimeOffset>? clock = null, Func<Guid>? newId = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _newId = newId ?? Guid.NewGuid;
    }

    public void SetActiveWorkspace(Guid? workspaceId) => _activeWorkspaceId = workspaceId;

    public async Task<MockupRecordsState> LoadAsync(Guid? listingId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        _activeListingId = listingId;
        if (listingId is Guid lid && snapshot.Listings.SingleOrDefault(l => l.Id == lid) is { } listing)
        {
            _activeStoreId = listing.StoreId;
        }
        return BuildState(snapshot);
    }

    public async Task<MockupRecordsResult> CreateAsync(MockupCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var listing = snapshot.Listings.SingleOrDefault(l => l.Id == request.ListingId);
        if (listing is null)
        {
            return MockupRecordsResult.Failure("Listing was not found.", BuildState(snapshot));
        }

        if (request.DesignId is Guid designId)
        {
            var design = snapshot.Designs.SingleOrDefault(d => d.Id == designId);
            if (design is null || design.ListingId != listing.Id)
            {
                return MockupRecordsResult.Failure("The design must belong to the same listing.", BuildState(snapshot));
            }
        }

        var now = _clock();
        var mockup = new Mockup(
            _newId(), listing.StoreId, listing.Id, request.DesignId,
            request.Name.Trim(), null, request.SourceMethod ?? "manual",
            request.ProductType, request.VendorProduct, request.Template,
            request.ColorVariant, request.View, request.Notes,
            request.IntendedMarketplaceUse, request.RegenerationMetadataJson,
            false, now, now, "{}");

        var updated = snapshot with { Mockups = [.. snapshot.Mockups, mockup] };

        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return MockupRecordsResult.Failure(saveError, BuildState(snapshot));
        }

        _activeStoreId = listing.StoreId;
        _activeListingId = listing.Id;
        return MockupRecordsResult.Success(ToSummary(mockup), BuildState(updated));
    }

    public async Task<MockupRecordsResult> ArchiveAsync(Guid mockupId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = snapshot.Mockups.SingleOrDefault(m => m.Id == mockupId);
        if (existing is null)
        {
            return MockupRecordsResult.Failure("Mockup was not found.", BuildState(snapshot));
        }

        var changed = existing with { IsArchived = true, UpdatedAt = _clock() };
        var updated = snapshot with { Mockups = snapshot.Mockups.Select(m => m.Id == existing.Id ? changed : m).ToArray() };

        var saveError = await TrySaveAsync(updated, cancellationToken).ConfigureAwait(false);
        if (saveError is not null)
        {
            return MockupRecordsResult.Failure(saveError, BuildState(snapshot));
        }

        return MockupRecordsResult.Success(ToSummary(changed), BuildState(updated));
    }

    private static MockupSummary ToSummary(Mockup mockup) =>
        new(mockup.Id, mockup.StoreId, mockup.ListingId, mockup.DesignId,
            mockup.Name, mockup.SourceMethod, mockup.ProductType, mockup.VendorProduct,
            mockup.Template, mockup.ColorVariant, mockup.View, mockup.Notes,
            mockup.IntendedMarketplaceUse, mockup.IsArchived, mockup.CreatedAt, mockup.UpdatedAt);

    private MockupRecordsState BuildState(WorkspaceSnapshot snapshot)
    {
        var listingMockups = _activeListingId is Guid lid
            ? snapshot.Mockups.Where(m => m.ListingId == lid).ToArray()
            : [];
        var active = listingMockups.Where(m => !m.IsArchived).OrderBy(m => m.CreatedAt).Select(ToSummary).ToArray();
        var archived = listingMockups.Where(m => m.IsArchived).OrderByDescending(m => m.UpdatedAt).Select(ToSummary).ToArray();
        return new MockupRecordsState(_activeStoreId, _activeListingId, active, archived);
    }

    private async Task<string?> TrySaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken)
    {
        try
        {
            await _repository.SaveAsync(snapshot, cancellationToken).ConfigureAwait(false);
            return null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return $"The mockup change could not be saved. {exception.Message}";
        }
    }
}

using FusionCanvas.Application.Snowclones;
using FusionCanvas.Domain.Snowclones;

namespace FusionCanvas.Application.Tests.Snowclones;

public sealed class SnowcloneLibraryServiceTests
{
    private static readonly DateTimeOffset CreatedAt = DateTimeOffset.Parse("2026-01-02T03:04:05Z");
    private static readonly DateTimeOffset UpdatedAt = CreatedAt.AddHours(1);

    [Fact]
    public async Task LoadAsync_SortsAndSearchesPhraseAndGuidance()
    {
        var alpha = Snowclone("Alpha {X}", "Coffee audience");
        var beta = Snowclone("beta {Y}", "Dog audience");
        var repository = new InMemoryRepository(new SnowcloneLibrarySnapshot([beta, alpha], true));
        var service = Service(repository);

        var phraseResult = await service.LoadAsync("ALPHA");
        var guidanceResult = await service.LoadAsync("dog");

        Assert.Equal([alpha.Id, beta.Id], phraseResult.State.AllSnowclones.Select(item => item.Id));
        Assert.Equal(alpha.Id, Assert.Single(phraseResult.State.VisibleSnowclones).Id);
        Assert.Equal(beta.Id, Assert.Single(guidanceResult.State.VisibleSnowclones).Id);
    }

    [Fact]
    public async Task CreateAsync_UsesDeterministicIdentityAndTimestamp()
    {
        var id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var repository = new InMemoryRepository(SnowcloneLibrarySnapshot.Empty);
        var service = Service(repository, clock: () => CreatedAt, newId: () => id);

        var result = await service.CreateAsync(new SnowcloneCreateRequest("  Easily distracted by {X}  ", "  Replace X.  "));

        Assert.True(result.Succeeded);
        var created = Assert.Single(repository.Snapshot.Snowclones);
        Assert.Equal(id, created.Id);
        Assert.Equal("Easily distracted by {X}", created.Phrase);
        Assert.Equal("Replace X.", created.Guidance);
        Assert.Equal(CreatedAt, created.CreatedAt);
        Assert.Equal(CreatedAt, created.UpdatedAt);
        Assert.Equal(id, result.AffectedSnowclone!.Id);
    }

    [Fact]
    public async Task CreateAsync_RejectsNormalizedDuplicateWithoutSaving()
    {
        var existing = Snowclone("Easily  distracted by {X}", "Original");
        var repository = new InMemoryRepository(new SnowcloneLibrarySnapshot([existing], true));
        var service = Service(repository);

        var result = await service.CreateAsync(new SnowcloneCreateRequest(" easily distracted by {x} ", "Different"));

        Assert.False(result.Succeeded);
        Assert.Contains("already exists", result.Error);
        Assert.Equal(0, repository.SaveCalls);
        Assert.Equal(existing, Assert.Single(repository.Snapshot.Snowclones));
    }

    [Fact]
    public async Task UpdateAsync_PreservesIdentityAndCreatedAtAndAdvancesUpdatedAt()
    {
        var existing = Snowclone("Old {X}", "Old guidance");
        var repository = new InMemoryRepository(new SnowcloneLibrarySnapshot([existing], true));
        var service = Service(repository, clock: () => UpdatedAt);

        var result = await service.UpdateAsync(new SnowcloneUpdateRequest(existing.Id, "New {Person}", "New guidance"));

        Assert.True(result.Succeeded);
        var updated = Assert.Single(repository.Snapshot.Snowclones);
        Assert.Equal(existing.Id, updated.Id);
        Assert.Equal(existing.CreatedAt, updated.CreatedAt);
        Assert.Equal(UpdatedAt, updated.UpdatedAt);
        Assert.Equal("New {Person}", updated.Phrase);
    }

    [Fact]
    public async Task UpdateAsync_CollisionLeavesConfirmedRecord()
    {
        var first = Snowclone("First {X}", "First");
        var second = Snowclone("Second {Y}", "Second");
        var repository = new InMemoryRepository(new SnowcloneLibrarySnapshot([first, second], true));
        var service = Service(repository);

        var result = await service.UpdateAsync(new SnowcloneUpdateRequest(second.Id, " FIRST {x} ", "Draft guidance"));

        Assert.False(result.Succeeded);
        Assert.Equal(0, repository.SaveCalls);
        Assert.Equal(second, repository.Snapshot.Snowclones.Single(item => item.Id == second.Id));
    }

    [Fact]
    public async Task DeleteAsync_RemovesOnlyRequestedSnowclone()
    {
        var first = Snowclone("First {X}", "First");
        var second = Snowclone("Second {Y}", "Second");
        var repository = new InMemoryRepository(new SnowcloneLibrarySnapshot([first, second], true));
        var service = Service(repository);

        var result = await service.DeleteAsync(first.Id);

        Assert.True(result.Succeeded);
        Assert.Equal(second, Assert.Single(repository.Snapshot.Snowclones));
        Assert.Equal(first.Id, result.AffectedSnowclone!.Id);
    }

    [Fact]
    public async Task InitializeAsync_ImportsOnceAndPersistsMarker()
    {
        var repository = new InMemoryRepository(SnowcloneLibrarySnapshot.Empty);
        var codec = new StubCsvCodec
        {
            ReadResult = SnowcloneCsvReadResult.Success(
                [new SnowcloneCsvRow("Easily distracted by {X}", "Replace X.", 2)])
        };
        var service = Service(repository, codec);

        var first = await service.InitializeAsync();
        var second = await service.InitializeAsync();

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.True(repository.Snapshot.StarterLibraryInitialized);
        Assert.Single(repository.Snapshot.Snowclones);
        Assert.Equal(1, codec.ReadCalls);
        Assert.Equal(1, repository.SaveCalls);
    }

    [Fact]
    public async Task InitializeAsync_AfterStarterDeletionDoesNotResurrectIt()
    {
        var starter = Snowclone("Easily distracted by {X}", "Replace X.");
        var repository = new InMemoryRepository(new SnowcloneLibrarySnapshot([starter], true));
        var codec = new StubCsvCodec
        {
            ReadResult = SnowcloneCsvReadResult.Success(
                [new SnowcloneCsvRow(starter.Phrase, starter.Guidance, 2)])
        };
        var service = Service(repository, codec);

        await service.DeleteAsync(starter.Id);
        var result = await service.InitializeAsync();

        Assert.True(result.Succeeded);
        Assert.Empty(repository.Snapshot.Snowclones);
        Assert.True(repository.Snapshot.StarterLibraryInitialized);
        Assert.Equal(0, codec.ReadCalls);
    }

    [Fact]
    public async Task InitializeAsync_InvalidBundleDoesNotSaveOrSetMarker()
    {
        var repository = new InMemoryRepository(SnowcloneLibrarySnapshot.Empty);
        var codec = new StubCsvCodec
        {
            ReadResult = SnowcloneCsvReadResult.Failure("CSV header must be Phrase,Guidance.")
        };
        var service = Service(repository, codec);

        var result = await service.InitializeAsync();

        Assert.False(result.Succeeded);
        Assert.False(repository.Snapshot.StarterLibraryInitialized);
        Assert.Empty(repository.Snapshot.Snowclones);
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task ImportBundledAsync_AddsUniqueAndPreservesExistingGuidance()
    {
        var existing = Snowclone("Existing {X}", "User guidance");
        var repository = new InMemoryRepository(new SnowcloneLibrarySnapshot([existing], true));
        var codec = new StubCsvCodec
        {
            ReadResult = SnowcloneCsvReadResult.Success(
            [
                new SnowcloneCsvRow(" existing {x} ", "Bundled overwrite", 2),
                new SnowcloneCsvRow("New {Y}", "New guidance", 3)
            ])
        };
        var service = Service(repository, codec);

        var result = await service.ImportBundledAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.AddedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Equal("User guidance", repository.Snapshot.Snowclones.Single(item => item.Id == existing.Id).Guidance);
        Assert.Contains(repository.Snapshot.Snowclones, item => item.Phrase == "New {Y}");
    }

    [Fact]
    public async Task ImportAsync_InvalidSemanticRowRejectsEntireDocument()
    {
        var repository = new InMemoryRepository(SnowcloneLibrarySnapshot.Empty);
        var codec = new StubCsvCodec
        {
            ReadResult = SnowcloneCsvReadResult.Success(
            [
                new SnowcloneCsvRow("Valid {X}", "Good", 2),
                new SnowcloneCsvRow("Invalid", "Bad", 3)
            ])
        };
        var service = Service(repository, codec);

        var result = await service.ImportAsync(new MemoryStream());

        Assert.False(result.Succeeded);
        Assert.StartsWith("Row 3:", result.Error);
        Assert.Empty(repository.Snapshot.Snowclones);
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task ImportAsync_DuplicatesWithinDocumentAreSkippedAtomically()
    {
        var repository = new InMemoryRepository(SnowcloneLibrarySnapshot.Empty);
        var codec = new StubCsvCodec
        {
            ReadResult = SnowcloneCsvReadResult.Success(
            [
                new SnowcloneCsvRow("One {X}", "First", 2),
                new SnowcloneCsvRow(" one {x} ", "Second", 3)
            ])
        };
        var service = Service(repository, codec);

        var result = await service.ImportAsync(new MemoryStream());

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.AddedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Equal("First", Assert.Single(repository.Snapshot.Snowclones).Guidance);
        Assert.Equal(1, repository.SaveCalls);
    }

    [Fact]
    public async Task ImportAsync_AllRowsDuplicateExistingLibraryDoesNotSave()
    {
        var existing = Snowclone("One {X}", "Keep this guidance");
        var repository = new InMemoryRepository(
            new SnowcloneLibrarySnapshot([existing], true));
        var codec = new StubCsvCodec
        {
            ReadResult = SnowcloneCsvReadResult.Success(
                [new SnowcloneCsvRow(" one {x} ", "Imported guidance", 2)])
        };
        var service = Service(repository, codec);

        var result = await service.ImportAsync(
            new MemoryStream(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(0, result.AddedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Equal(0, repository.SaveCalls);
        Assert.Equal(existing, Assert.Single(repository.Snapshot.Snowclones));
    }

    [Fact]
    public async Task SaveFailure_ReturnsPreviousConfirmedState()
    {
        var existing = Snowclone("Existing {X}", "Confirmed");
        var repository = new InMemoryRepository(new SnowcloneLibrarySnapshot([existing], true))
        {
            SaveException = new IOException("disk full")
        };
        var service = Service(repository);

        var result = await service.CreateAsync(new SnowcloneCreateRequest("New {Y}", "Draft"));

        Assert.False(result.Succeeded);
        Assert.Contains("disk full", result.Error);
        Assert.Equal(existing.Id, Assert.Single(result.State.AllSnowclones).Id);
        Assert.Equal(existing, Assert.Single(repository.Snapshot.Snowclones));
    }

    [Fact]
    public async Task ExportAsync_WritesAlphabeticalRowsWithoutMutation()
    {
        var alpha = Snowclone("Alpha {X}", "A");
        var beta = Snowclone("beta {Y}", "B");
        var repository = new InMemoryRepository(new SnowcloneLibrarySnapshot([beta, alpha], true));
        var codec = new StubCsvCodec();
        var service = Service(repository, codec);

        var result = await service.ExportAsync(new MemoryStream());

        Assert.True(result.Succeeded);
        Assert.Equal(["Alpha {X}", "beta {Y}"], codec.WrittenRows.Select(row => row.Phrase));
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task ImportAsync_PropagatesCancellationWithoutSaving()
    {
        var repository = new InMemoryRepository(SnowcloneLibrarySnapshot.Empty);
        var codec = new StubCsvCodec { ThrowCancellationOnRead = true };
        var service = Service(repository, codec);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.ImportAsync(new MemoryStream(), cancellationToken: new CancellationToken(true)));

        Assert.Equal(0, repository.SaveCalls);
    }

    private static SnowcloneLibraryService Service(
        InMemoryRepository repository,
        StubCsvCodec? codec = null,
        Func<DateTimeOffset>? clock = null,
        Func<Guid>? newId = null) =>
        new(
            repository,
            codec ?? new StubCsvCodec(),
            new StubBundledSource(),
            clock ?? (() => UpdatedAt),
            newId ?? Guid.NewGuid);

    private static Snowclone Snowclone(string phrase, string guidance) =>
        new(Guid.NewGuid(), phrase, guidance, CreatedAt, CreatedAt);

    private sealed class InMemoryRepository(SnowcloneLibrarySnapshot snapshot) : ISnowcloneRepository
    {
        public SnowcloneLibrarySnapshot Snapshot { get; private set; } = snapshot;

        public int SaveCalls { get; private set; }

        public Exception? SaveException { get; init; }

        public Task<SnowcloneLibrarySnapshot> LoadAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Snapshot);
        }

        public Task SaveAsync(SnowcloneLibrarySnapshot snapshot, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SaveCalls++;
            if (SaveException is not null)
            {
                throw SaveException;
            }

            Snapshot = snapshot;
            return Task.CompletedTask;
        }
    }

    private sealed class StubCsvCodec : ISnowcloneCsvCodec
    {
        public SnowcloneCsvReadResult ReadResult { get; init; } = SnowcloneCsvReadResult.Success([]);

        public bool ThrowCancellationOnRead { get; init; }

        public int ReadCalls { get; private set; }

        public IReadOnlyList<SnowcloneCsvRow> WrittenRows { get; private set; } = [];

        public Task<SnowcloneCsvReadResult> ReadAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            ReadCalls++;
            if (ThrowCancellationOnRead)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(ReadResult);
        }

        public Task WriteAsync(
            Stream stream,
            IReadOnlyList<SnowcloneCsvRow> rows,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WrittenRows = rows;
            return Task.CompletedTask;
        }
    }

    private sealed class StubBundledSource : IBundledSnowcloneSource
    {
        public Task<Stream> OpenReadAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Stream>(new MemoryStream());
        }
    }
}

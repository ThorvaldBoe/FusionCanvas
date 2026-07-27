using FusionCanvas.Application.Ideation;
using FusionCanvas.Application.Snowclones;

namespace FusionCanvas.Application.Tests.Ideation;

public sealed class PersistedSnowcloneCatalogTests
{
    [Fact]
    public async Task SelectsConfirmedPhraseGuidanceAndTokensUniquelyWithinEachCycle()
    {
        var summaries = new[]
        {
            Summary("Talk to me about {subject}", "Fill {subject}."),
            Summary("Whatever {action} your {thing}", "Fill both placeholders.")
        };
        var catalog = new PersistedSnowcloneCatalog(
            new StubLibrary(SnowcloneLibraryResult.Success(
                new(summaries, summaries, true, string.Empty))),
            new Random(17));

        var result = await catalog.GetSelectionsAsync(3, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Selections.Take(2).Select(x => x.Id).Distinct().Count());
        Assert.Equal(3, result.Selections.Count);
        Assert.Contains(result.Selections, x =>
            x.Guidance == "Fill {subject}." &&
            x.PlaceholderTokens.SequenceEqual(["{subject}"]));
    }

    [Fact]
    public async Task ReportsEmptyAndLoadFailureAndObservesCancellation()
    {
        var empty = await new PersistedSnowcloneCatalog(
                new StubLibrary(SnowcloneLibraryResult.Success(SnowcloneLibraryState.Empty)))
            .GetSelectionsAsync(1, TestContext.Current.CancellationToken);
        Assert.False(empty.Succeeded);
        Assert.Contains("Add or import", empty.Error, StringComparison.Ordinal);

        var failure = await new PersistedSnowcloneCatalog(
                new StubLibrary(SnowcloneLibraryResult.Failure("database unavailable", SnowcloneLibraryState.Empty)))
            .GetSelectionsAsync(1, TestContext.Current.CancellationToken);
        Assert.False(failure.Succeeded);
        Assert.Equal("database unavailable", failure.Error);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new PersistedSnowcloneCatalog(
                    new StubLibrary(SnowcloneLibraryResult.Success(SnowcloneLibraryState.Empty)))
                .GetSelectionsAsync(0, cancellation.Token));
    }

    private static SnowcloneSummary Summary(string phrase, string guidance) =>
        new(Guid.NewGuid(), phrase, guidance, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    private sealed class StubLibrary(SnowcloneLibraryResult result) : ISnowcloneLibraryService
    {
        public Task<SnowcloneLibraryResult> LoadAsync(string? searchText = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(result);
        public Task<SnowcloneLibraryResult> InitializeAsync(string? searchText = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<SnowcloneLibraryResult> CreateAsync(SnowcloneCreateRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<SnowcloneLibraryResult> UpdateAsync(SnowcloneUpdateRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<SnowcloneLibraryResult> DeleteAsync(Guid id, string? searchText = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<SnowcloneLibraryResult> ImportAsync(Stream stream, string? searchText = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<SnowcloneLibraryResult> ImportBundledAsync(string? searchText = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<SnowcloneLibraryResult> ExportAsync(Stream stream, string? searchText = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}

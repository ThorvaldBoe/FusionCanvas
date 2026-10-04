using FusionCanvas.Application.ContentRisk;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.ContentRisk;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests.ContentRisk;

public sealed class ContentRiskReviewServiceTests
{
    [Fact]
    public async Task ReviewTextPersistsUnreviewedThenPotentialRisk()
    {
        var ownerId = Guid.NewGuid();
        var target = new ContentRiskReviewTarget(ownerId, ContentRiskOwnerKind.Item, ContentRiskContentKind.Text, "concept.phrase");
        var repository = new MemoryRepository(WorkspaceSnapshot.Empty with { Workspaces = [WorkspaceSnapshot.DefaultWorkspace(DateTimeOffset.UtcNow)] });
        var analyzer = new FakeAnalyzer(ContentRiskAnalysisResult.Success(
            [new(ContentRiskCategory.IpRisk, ContentRiskSeverity.Medium, "Possible brand reference.")],
            "fake-analyzer"));
        var service = new ContentRiskReviewService(repository, analyzer, () => new DateTimeOffset(2026, 10, 4, 12, 0, 0, TimeSpan.Zero));

        var review = await service.ReviewTextAsync(target, "Possible phrase", TestContext.Current.CancellationToken);

        Assert.Equal(ContentRiskReviewState.PotentialRisk, review.State);
        Assert.Equal(ContentRiskReviewState.PotentialRisk, Assert.Single(repository.Snapshot.ContentRiskReviews).State);
        Assert.Equal(2, repository.SaveCount);
    }

    [Fact]
    public async Task ReviewFailurePersistsUnavailableAndDoesNotChangeConfirmedContent()
    {
        var ownerId = Guid.NewGuid();
        var target = new ContentRiskReviewTarget(ownerId, ContentRiskOwnerKind.Item, ContentRiskContentKind.Text, "listing.title");
        var repository = new MemoryRepository(WorkspaceSnapshot.Empty with { Workspaces = [WorkspaceSnapshot.DefaultWorkspace(DateTimeOffset.UtcNow)] });
        var analyzer = new FakeAnalyzer(ContentRiskAnalysisResult.Unavailable(ContentRiskAnalyzerFailureKind.ProviderFailure, "provider error"));
        var service = new ContentRiskReviewService(repository, analyzer);

        var review = await service.ReviewTextAsync(target, "Existing title", TestContext.Current.CancellationToken);

        Assert.Equal(ContentRiskReviewState.ReviewUnavailable, review.State);
        Assert.Equal("Existing title", analyzer.LastRequest!.Text);
        Assert.Single(repository.Snapshot.ContentRiskReviews);
    }

    [Fact]
    public async Task NewFingerprintDoesNotReusePriorReview()
    {
        var ownerId = Guid.NewGuid();
        var target = new ContentRiskReviewTarget(ownerId, ContentRiskOwnerKind.Item, ContentRiskContentKind.Text, "concept.phrase");
        var repository = new MemoryRepository(WorkspaceSnapshot.Empty with { Workspaces = [WorkspaceSnapshot.DefaultWorkspace(DateTimeOffset.UtcNow)] });
        var analyzer = new FakeAnalyzer(ContentRiskAnalysisResult.Success([], "fake-analyzer"));
        var service = new ContentRiskReviewService(repository, analyzer);

        await service.ReviewTextAsync(target, "First phrase", TestContext.Current.CancellationToken);
        var current = await service.FindAsync(target, ContentRiskFingerprint.ForText(target, "Changed phrase"), TestContext.Current.CancellationToken);

        Assert.Null(current);
    }

    [Fact]
    public async Task ReviewCanBeRetriedAndReplacesThePriorResult()
    {
        var target = new ContentRiskReviewTarget(Guid.NewGuid(), ContentRiskOwnerKind.Item, ContentRiskContentKind.Text, "listing.title");
        var repository = new MemoryRepository(WorkspaceSnapshot.Empty with { Workspaces = [WorkspaceSnapshot.DefaultWorkspace(DateTimeOffset.UtcNow)] });
        var analyzer = new SequenceAnalyzer([
            ContentRiskAnalysisResult.Unavailable(ContentRiskAnalyzerFailureKind.ProviderFailure, "not exposed"),
            ContentRiskAnalysisResult.Success([], "fake-analyzer")]);
        var service = new ContentRiskReviewService(repository, analyzer);

        var unavailable = await service.ReviewTextAsync(target, "Title", TestContext.Current.CancellationToken);
        var retried = await service.ReviewTextAsync(target, "Title", TestContext.Current.CancellationToken);

        Assert.Equal(ContentRiskReviewState.ReviewUnavailable, unavailable.State);
        Assert.Equal(ContentRiskReviewState.NoObviousSignalDetected, retried.State);
        Assert.Equal(ContentRiskReviewState.NoObviousSignalDetected, Assert.Single(repository.Snapshot.ContentRiskReviews).State);
    }

    [Fact]
    public async Task CancelledAnalyzerProducesUnavailableReviewWithoutRollingBackContentBoundary()
    {
        var target = new ContentRiskReviewTarget(Guid.NewGuid(), ContentRiskOwnerKind.Asset, ContentRiskContentKind.Image, "design.asset");
        var repository = new MemoryRepository(WorkspaceSnapshot.Empty with { Workspaces = [WorkspaceSnapshot.DefaultWorkspace(DateTimeOffset.UtcNow)] });
        var service = new ContentRiskReviewService(repository, new CancellingAnalyzer());

        var review = await service.ReviewImageAsync(target, "image/png", [1, 2, 3], TestContext.Current.CancellationToken);

        Assert.Equal(ContentRiskReviewState.ReviewUnavailable, review.State);
        Assert.Single(repository.Snapshot.ContentRiskReviews);
    }

    private sealed class FakeAnalyzer(ContentRiskAnalysisResult result) : IContentRiskAnalyzer
    {
        public ContentRiskAnalysisRequest? LastRequest { get; private set; }

        public Task<ContentRiskAnalysisResult> AnalyzeAsync(ContentRiskAnalysisRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(result);
        }
    }

    private sealed class SequenceAnalyzer(IReadOnlyList<ContentRiskAnalysisResult> results) : IContentRiskAnalyzer
    {
        private int _index;

        public Task<ContentRiskAnalysisResult> AnalyzeAsync(ContentRiskAnalysisRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(results[Math.Min(Interlocked.Increment(ref _index) - 1, results.Count - 1)]);
    }

    private sealed class CancellingAnalyzer : IContentRiskAnalyzer
    {
        public Task<ContentRiskAnalysisResult> AnalyzeAsync(ContentRiskAnalysisRequest request, CancellationToken cancellationToken = default) =>
            Task.FromException<ContentRiskAnalysisResult>(new OperationCanceledException(cancellationToken));
    }

    private sealed class MemoryRepository(WorkspaceSnapshot initial) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; private set; } = initial;

        public int SaveCount { get; private set; }

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Snapshot);

        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            Snapshot = snapshot;
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}

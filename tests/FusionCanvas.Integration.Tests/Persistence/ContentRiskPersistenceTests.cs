using FusionCanvas.Domain.ContentRisk;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Integration.Persistence;

namespace FusionCanvas.Integration.Tests.Persistence;

public sealed class ContentRiskPersistenceTests
{
    [Fact]
    public async Task SaveAndLoadAsync_RoundTripsContentRiskReviewWithoutRawPayload()
    {
        using var temporary = new TemporaryDirectory();
        var ownerId = Guid.NewGuid();
        var target = new ContentRiskReviewTarget(ownerId, ContentRiskOwnerKind.Item, ContentRiskContentKind.Text, "concept.phrase");
        var review = ContentRiskReview.PotentialRisk(
            target,
            ContentRiskFingerprint.ForText(target, "Powered by coffee"),
            [new(ContentRiskCategory.IpRisk, ContentRiskSeverity.Medium, "Possible protected reference.", "brand-like phrase")],
            new ContentRiskReviewProvenance("test-analyzer", DateTimeOffset.UtcNow, 12, 0.01m));
        var workspace = WorkspaceSnapshot.Empty with
        {
            Workspaces = [WorkspaceSnapshot.DefaultWorkspace(DateTimeOffset.UtcNow)],
            ContentRiskReviews = [review]
        };
        var repository = new SqliteWorkspaceRepository(temporary.GetPath("workspace.db"), useConnectionPooling: false);

        await repository.SaveAsync(workspace, TestContext.Current.CancellationToken);
        var loaded = await repository.LoadAsync(TestContext.Current.CancellationToken);

        var restored = Assert.Single(loaded.ContentRiskReviews);
        Assert.Equal(review.Target, restored.Target);
        Assert.Equal(review.Fingerprint, restored.Fingerprint);
        Assert.Equal(review.State, restored.State);
        Assert.Equal("test-analyzer", restored.Provenance!.AnalyzerId);
        Assert.Equal("brand-like phrase", Assert.Single(restored.Findings).Evidence);
    }

    [Fact]
    public async Task LoadAsync_UnknownVersionRetainsUnavailableWarning()
    {
        using var temporary = new TemporaryDirectory();
        var path = temporary.GetPath("workspace.db");
        var ownerId = Guid.NewGuid();
        var target = new ContentRiskReviewTarget(ownerId, ContentRiskOwnerKind.Item, ContentRiskContentKind.Text, "concept.phrase");
        var repository = new SqliteWorkspaceRepository(path, useConnectionPooling: false);
        await repository.SaveAsync(WorkspaceSnapshot.Empty with
        {
            Workspaces = [WorkspaceSnapshot.DefaultWorkspace(DateTimeOffset.UtcNow)]
        }, TestContext.Current.CancellationToken);

        await using (var connection = new Microsoft.Data.Sqlite.SqliteConnection(new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString()))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO content_risk_reviews (owner_id, owner_kind, content_kind, role, fingerprint, state, findings_json, review_version) VALUES ($owner, $owner_kind, $content_kind, $role, $fingerprint, $state, '[]', 99);";
            command.Parameters.AddWithValue("$owner", ownerId.ToString());
            command.Parameters.AddWithValue("$owner_kind", (int)target.OwnerKind);
            command.Parameters.AddWithValue("$content_kind", (int)target.ContentKind);
            command.Parameters.AddWithValue("$role", target.Role);
            command.Parameters.AddWithValue("$fingerprint", "fingerprint");
            command.Parameters.AddWithValue("$state", (int)ContentRiskReviewState.NoObviousSignalDetected);
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        var loaded = await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ContentRiskReviewState.ReviewUnavailable, Assert.Single(loaded.ContentRiskReviews).State);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly string _path = Path.Combine(Path.GetTempPath(), "FusionCanvas.ContentRiskTests", Guid.NewGuid().ToString("N"));

        public TemporaryDirectory() => Directory.CreateDirectory(_path);

        public string GetPath(string fileName) => Path.Combine(_path, fileName);

        public void Dispose()
        {
            if (Directory.Exists(_path))
            {
                Directory.Delete(_path, recursive: true);
            }
        }
    }
}

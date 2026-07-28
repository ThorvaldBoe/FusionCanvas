using FusionCanvas.Application.Workspaces;
using FusionCanvas.Application.Workspaces.Transfer;
using FusionCanvas.Domain.Ideation;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Tests;

public class WorkspaceTransferServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 27, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ExportWorkspaceAsync_WritesFilteredPackageAndReturnsSummary()
    {
        var selected = NewWorkspace("Selected");
        var other = NewWorkspace("Other");
        var repository = new FakeRepository(new WorkspaceSnapshot([selected, other], [], [], [], [], [], [], [], [], []));
        var writer = new FakeWriter();
        var service = NewService(repository, writer: writer);

        var result = await service.ExportWorkspaceAsync(
            new WorkspaceExportRequest(selected.Id, "selected.fcworkspace"),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(selected.Id, Assert.Single(writer.Request!.Snapshot.Workspaces).Id);
        Assert.Equal(1, result.Summary!.EntityCounts["workspaces"]);
    }

    [Fact]
    public async Task ExportWorkspaceAsync_IncludesOnlySelectedWorkspaceRejectionsAndCountsThem()
    {
        var selected = NewGraph("Selected");
        var other = NewGraph("Other");
        var repository = new FakeRepository(Merge(selected.Snapshot, other.Snapshot));
        var writer = new FakeWriter();
        var service = NewService(repository, writer: writer);

        var result = await service.ExportWorkspaceAsync(
            new WorkspaceExportRequest(selected.Workspace.Id, "selected.fcworkspace"),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(selected.Rejection, Assert.Single(writer.Request!.Snapshot.IdeationRejections));
        Assert.Equal(1, result.Summary!.EntityCounts["ideationRejections"]);
    }

    [Fact]
    public async Task ImportWorkspaceAsync_RefusesIdentityCollisionBeforeOpeningFiles()
    {
        var workspace = NewWorkspace("Existing");
        var snapshot = new WorkspaceSnapshot([workspace], [], [], [], [], [], [], [], [], []);
        var opened = false;
        var session = NewSession(snapshot, [
            new WorkspacePackageReadEntry("assets/file.png", 1, _ =>
            {
                opened = true;
                return Task.FromResult<Stream>(new MemoryStream([1]));
            })
        ]);
        var repository = new FakeRepository(snapshot);
        var service = NewService(repository, reader: new FakeReader(session));

        var result = await service.ImportWorkspaceAsync(
            new WorkspaceImportRequest("duplicate.fcworkspace"),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("already exists", result.Error);
        Assert.False(opened);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task ImportWorkspaceAsync_ActivatesArchivedWorkspaceAndSuffixesActiveNameConflict()
    {
        var liveWorkspace = NewWorkspace("Brand");
        var packagedWorkspace = NewWorkspace("Brand") with { IsArchived = true };
        var package = new WorkspaceSnapshot([packagedWorkspace], [], [], [], [], [], [], [], [], []);
        var repository = new FakeRepository(new WorkspaceSnapshot([liveWorkspace], [], [], [], [], [], [], [], [], []));
        var service = NewService(repository, reader: new FakeReader(NewSession(package)));

        var result = await service.ImportWorkspaceAsync(
            new WorkspaceImportRequest("brand.fcworkspace"),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("Brand (2)", result.Summary!.FinalWorkspaceName);
        var imported = repository.Snapshot.Workspaces.Single(workspace => workspace.Id == packagedWorkspace.Id);
        Assert.False(imported.IsArchived);
        Assert.Equal("Brand (2)", imported.Name);
    }

    [Fact]
    public async Task ImportWorkspaceAsync_PreservesLiveRejectionsAndAddsPackagedRejections()
    {
        var live = NewGraph("Live");
        var package = NewGraph("Package");
        var repository = new FakeRepository(live.Snapshot);
        var service = NewService(repository, reader: new FakeReader(NewSession(package.Snapshot)));

        var result = await service.ImportWorkspaceAsync(
            new WorkspaceImportRequest("package.fcworkspace"),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(
            [live.Rejection, package.Rejection],
            repository.Snapshot.IdeationRejections);
    }

    [Fact]
    public async Task ImportWorkspaceAsync_OlderPackageWithoutRejectionsPreservesLiveHistory()
    {
        var live = NewGraph("Live");
        var packagedWorkspace = NewWorkspace("Older package");
        var package = new WorkspaceSnapshot([packagedWorkspace], [], [], [], [], [], [], [], [], []);
        var repository = new FakeRepository(live.Snapshot);
        var service = NewService(repository, reader: new FakeReader(NewSession(package)));

        var result = await service.ImportWorkspaceAsync(
            new WorkspaceImportRequest("older.fcworkspace"),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(live.Rejection, Assert.Single(repository.Snapshot.IdeationRejections));
    }

    [Fact]
    public async Task ImportWorkspaceAsync_RefusesRejectionIdentityCollisionBeforeSaving()
    {
        var live = NewGraph("Live");
        var package = NewGraph("Package");
        var collidingPackage = package.Snapshot with
        {
            IdeationRejections = [package.Rejection with { Id = live.Rejection.Id }]
        };
        var repository = new FakeRepository(live.Snapshot);
        var service = NewService(repository, reader: new FakeReader(NewSession(collidingPackage)));

        var result = await service.ImportWorkspaceAsync(
            new WorkspaceImportRequest("collision.fcworkspace"),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(0, repository.SaveCount);
        Assert.Equal(live.Rejection, Assert.Single(repository.Snapshot.IdeationRejections));
    }

    [Fact]
    public async Task ImportWorkspaceAsync_ArchivedOnlyNameConflictKeepsOriginalName()
    {
        var archivedLive = NewWorkspace("Brand") with { IsArchived = true };
        var packagedWorkspace = NewWorkspace("Brand");
        var package = new WorkspaceSnapshot([packagedWorkspace], [], [], [], [], [], [], [], [], []);
        var repository = new FakeRepository(new WorkspaceSnapshot([archivedLive], [], [], [], [], [], [], [], [], []));
        var service = NewService(repository, reader: new FakeReader(NewSession(package)));

        var result = await service.ImportWorkspaceAsync(
            new WorkspaceImportRequest("brand.fcworkspace"),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("Brand", result.Summary!.FinalWorkspaceName);
    }

    [Fact]
    public async Task ImportWorkspaceAsync_ReportsSkippedExistingAndProgress()
    {
        var packagedWorkspace = NewWorkspace("Package");
        var package = new WorkspaceSnapshot([packagedWorkspace], [], [], [], [], [], [], [], [], []);
        var fileStore = new FakeFileStore(["assets/existing.png"]);
        var progress = new List<WorkspaceTransferProgress>();
        var service = NewService(
            new FakeRepository(),
            fileStore,
            reader: new FakeReader(NewSession(package, [
                new WorkspacePackageReadEntry("assets/existing.png", 1, _ => Task.FromResult<Stream>(new MemoryStream([1])))
            ])));

        var result = await service.ImportWorkspaceAsync(
            new WorkspaceImportRequest("package.fcworkspace"),
            new InlineProgress<WorkspaceTransferProgress>(progress.Add),
            TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.Summary!.SkippedExistingFiles);
        Assert.Contains(progress, item => item.Phase == "Saving workspace" && item.Completed == 1);
    }

    [Fact]
    public async Task ImportWorkspaceAsync_SaveFailureCleansNewFilesAndLeavesRepositoryUnchanged()
    {
        var workspace = NewWorkspace("Package");
        var package = new WorkspaceSnapshot([workspace], [], [], [], [], [], [], [], [], []);
        var fileStore = new FakeFileStore();
        var repository = new FakeRepository { FailSave = true };
        var service = NewService(
            repository,
            fileStore,
            reader: new FakeReader(NewSession(package, [
                new WorkspacePackageReadEntry("assets/new.png", 1, _ => Task.FromResult<Stream>(new MemoryStream([1])))
            ])));

        var result = await service.ImportWorkspaceAsync(
            new WorkspaceImportRequest("package.fcworkspace"),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.False(fileStore.Exists("assets/new.png"));
        Assert.Empty(repository.Snapshot.Workspaces);
    }

    [Fact]
    public async Task ImportWorkspaceAsync_CancellationCleansFilesAndDoesNotSave()
    {
        var workspace = NewWorkspace("Package");
        var package = new WorkspaceSnapshot([workspace], [], [], [], [], [], [], [], [], []);
        var fileStore = new FakeFileStore();
        var repository = new FakeRepository();
        var service = NewService(
            repository,
            fileStore,
            reader: new FakeReader(NewSession(package, [
                new WorkspacePackageReadEntry("assets/first.png", 1, _ => Task.FromResult<Stream>(new MemoryStream([1]))),
                new WorkspacePackageReadEntry("assets/second.png", 1, _ => throw new OperationCanceledException())
            ])));

        var result = await service.ImportWorkspaceAsync(
            new WorkspaceImportRequest("package.fcworkspace"),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Cancelled);
        Assert.False(fileStore.Exists("assets/first.png"));
        Assert.Equal(0, repository.SaveCount);
    }

    private static WorkspaceTransferService NewService(
        FakeRepository repository,
        FakeFileStore? fileStore = null,
        FakeWriter? writer = null,
        FakeReader? reader = null) =>
        new(
            repository,
            fileStore ?? new FakeFileStore(),
            writer ?? new FakeWriter(),
            reader ?? new FakeReader(NewSession(WorkspaceSnapshot.Empty)),
            () => Now);

    private static FakeSession NewSession(
        WorkspaceSnapshot snapshot,
        IReadOnlyList<WorkspacePackageReadEntry>? files = null,
        IReadOnlyList<string>? skippedUnsupported = null)
    {
        var workspace = snapshot.Workspaces.SingleOrDefault() ?? NewWorkspace("Empty");
        return new FakeSession(
            new WorkspacePackageManifest(
                1,
                7,
                "test",
                workspace.Id,
                workspace.Name,
                Now,
                new Dictionary<string, int>(),
                files?.Select(file => new WorkspacePackageFile(file.WorkspaceRelativePath, file.Size)).ToArray() ?? [],
                [],
                0),
            snapshot,
            files ?? [],
            skippedUnsupported ?? []);
    }

    private static FusionCanvas.Domain.Workspace.Workspace NewWorkspace(string name) =>
        new(Guid.NewGuid(), name, null, false, Now, Now, "{}");

    private static Graph NewGraph(string name)
    {
        var workspace = NewWorkspace(name);
        var store = new Store(Guid.NewGuid(), workspace.Id, $"{name} store", null, false, Now, Now, "{}");
        var niche = new Niche(Guid.NewGuid(), store.Id, $"{name} niche", null, false, Now, Now, "{}");
        var rejection = new IdeationRejection(
            Guid.NewGuid(),
            store.Id,
            niche.Id,
            null,
            $"{name} rejection",
            "Not a fit",
            IdeationMode.Basic,
            Now);
        var snapshot = new WorkspaceSnapshot([workspace], [store], [niche], [], [], [], [], [], [], [])
        {
            IdeationRejections = [rejection]
        };
        return new Graph(snapshot, workspace, rejection);
    }

    private static WorkspaceSnapshot Merge(WorkspaceSnapshot left, WorkspaceSnapshot right) =>
        new(
            [.. left.Workspaces, .. right.Workspaces],
            [.. left.Stores, .. right.Stores],
            [.. left.Niches, .. right.Niches],
            [.. left.Groups, .. right.Groups],
            [.. left.Items, .. right.Items],
            [.. left.Assets, .. right.Assets],
            [.. left.Prompts, .. right.Prompts],
            [.. left.Tags, .. right.Tags],
            [.. left.ItemTags, .. right.ItemTags],
            [.. left.AssetLinks, .. right.AssetLinks])
        {
            IdeationRejections = [.. left.IdeationRejections, .. right.IdeationRejections]
        };

    private sealed record Graph(
        WorkspaceSnapshot Snapshot,
        FusionCanvas.Domain.Workspace.Workspace Workspace,
        IdeationRejection Rejection);

    private sealed class FakeRepository(WorkspaceSnapshot? snapshot = null) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; private set; } = snapshot ?? WorkspaceSnapshot.Empty;

        public int SaveCount { get; private set; }

        public bool FailSave { get; init; }

        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            SaveCount++;
            if (FailSave)
            {
                throw new InvalidOperationException("save failed");
            }

            Snapshot = snapshot;
            return Task.CompletedTask;
        }

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Snapshot);
    }

    private sealed class FakeFileStore(IEnumerable<string>? existing = null) : IWorkspaceFileStore
    {
        private readonly Dictionary<string, byte[]> _files = (existing ?? [])
            .ToDictionary(path => path, _ => Array.Empty<byte>(), StringComparer.Ordinal);

        public string WorkspaceRoot => "memory";

        public Task<ManagedWorkspaceFile> ImportAsync(string sourcePath, FusionCanvas.Domain.Assets.AssetKind kind, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public bool Exists(string workspaceRelativePath) => _files.ContainsKey(workspaceRelativePath);

        public bool TryDelete(string workspaceRelativePath) => _files.Remove(workspaceRelativePath);

        public Task<Stream> OpenReadAsync(string workspaceRelativePath, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new MemoryStream(_files[workspaceRelativePath], writable: false));

        public async Task<WorkspaceFileRestoreOutcome> RestoreAsync(string workspaceRelativePath, Stream content, CancellationToken cancellationToken = default)
        {
            if (_files.ContainsKey(workspaceRelativePath))
            {
                return WorkspaceFileRestoreOutcome.SkippedExisting;
            }

            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            _files[workspaceRelativePath] = buffer.ToArray();
            return WorkspaceFileRestoreOutcome.Created;
        }

        public Task ExportCopyAsync(string workspaceRelativePath, string destinationPath, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeWriter : IWorkspacePackageWriter
    {
        public int CurrentFormatVersion => 1;

        public int CurrentSchemaVersion => 8;

        public string AppVersion => "test";

        public WorkspacePackageWriteRequest? Request { get; private set; }

        public Task<WorkspacePackageWriteResult> WriteAsync(
            WorkspacePackageWriteRequest request,
            IProgress<WorkspaceTransferProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.FromResult(new WorkspacePackageWriteResult(request.Manifest));
        }
    }

    private sealed class FakeReader(FakeSession session) : IWorkspacePackageReader
    {
        public Task<WorkspacePackageReadResult> ReadAsync(
            string packagePath,
            IProgress<WorkspaceTransferProgress>? progress = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(WorkspacePackageReadResult.Success(session));
    }

    private sealed record FakeSession(
        WorkspacePackageManifest Manifest,
        WorkspaceSnapshot Snapshot,
        IReadOnlyList<WorkspacePackageReadEntry> Files,
        IReadOnlyList<string> SkippedUnsupportedFiles) : IWorkspacePackageReadSession
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}

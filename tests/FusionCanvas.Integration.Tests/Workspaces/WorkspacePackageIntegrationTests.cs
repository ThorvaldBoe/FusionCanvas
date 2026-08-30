using System.IO.Compression;
using System.Text.Json;
using FusionCanvas.Application.Workspaces.Transfer;
using FusionCanvas.Application.Snowclones;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Ideation;
using FusionCanvas.Domain.Mockups;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Snowclones;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Integration.Files;
using FusionCanvas.Integration.Packages;
using FusionCanvas.Integration.Persistence;
using Microsoft.Data.Sqlite;

namespace FusionCanvas.Integration.Tests;

public class WorkspacePackageIntegrationTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 27, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ExportThenImport_RoundTripsSnapshotAndManagedFileBytes()
    {
        using var temp = new TemporaryDirectory();
        var sourceRepository = new SqliteWorkspaceRepository(temp.GetPath("source.db"));
        var sourceFiles = new LocalWorkspaceFileStore(temp.GetPath("source-files"));
        var snapshot = AddNormalizedCatalog(CreateSnapshot("Round trip", "assets/design.png", archivedAsset: true));
        await sourceRepository.SaveAsync(snapshot, TestContext.Current.CancellationToken);
        await sourceFiles.RestoreAsync("assets/design.png", new MemoryStream([1, 2, 3, 4]), TestContext.Current.CancellationToken);
        var packagePath = temp.GetPath("roundtrip.fcworkspace");
        var writer = new ZipWorkspacePackageWriter();
        var reader = new ZipWorkspacePackageReader();
        var exportService = new WorkspaceTransferService(sourceRepository, sourceFiles, writer, reader, () => Now);

        var exported = await exportService.ExportWorkspaceAsync(
            new WorkspaceExportRequest(snapshot.Workspaces[0].Id, packagePath),
            cancellationToken: TestContext.Current.CancellationToken);

        var destinationRepository = new SqliteWorkspaceRepository(temp.GetPath("destination.db"));
        var destinationFiles = new LocalWorkspaceFileStore(temp.GetPath("destination-files"));
        var importService = new WorkspaceTransferService(destinationRepository, destinationFiles, writer, reader, () => Now);
        var imported = await importService.ImportWorkspaceAsync(
            new WorkspaceImportRequest(packagePath),
            cancellationToken: TestContext.Current.CancellationToken);
        var restored = await destinationRepository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(exported.Succeeded);
        Assert.True(imported.Succeeded);
        Assert.Equal(snapshot.Workspaces, restored.Workspaces);
        Assert.Equal(snapshot.Stores, restored.Stores);
        Assert.Equal(snapshot.Niches, restored.Niches);
        Assert.Equal(snapshot.Groups, restored.Groups);
        Assert.Equal(snapshot.Items, restored.Items);
        Assert.Equal(snapshot.Assets, restored.Assets);
        Assert.Equal(snapshot.Prompts, restored.Prompts);
        Assert.Equal(snapshot.Tags, restored.Tags);
        Assert.Equal(snapshot.ItemTags, restored.ItemTags);
        Assert.Equal(snapshot.AssetLinks, restored.AssetLinks);
        Assert.Equal(snapshot.Blueprints, restored.Blueprints);
        Assert.Equal(snapshot.BlueprintOfferings, restored.BlueprintOfferings);
        Assert.Equal(snapshot.OfferingVariants, restored.OfferingVariants);
        Assert.Equal(snapshot.OfferingPlaceholders, restored.OfferingPlaceholders);
        Assert.Equal(snapshot.MockupTemplates, restored.MockupTemplates);
        Assert.Equal(snapshot.MockupTemplateColorVariants, restored.MockupTemplateColorVariants);
        Assert.Equal(snapshot.MockupTemplateRevisions, restored.MockupTemplateRevisions);
        Assert.Equal(snapshot.MockupTemplateRevisionColors, restored.MockupTemplateRevisionColors);
        Assert.Equal([1, 2, 3, 4], await ReadBytesAsync(destinationFiles, "assets/design.png"));
    }

    [Fact]
    public async Task ExportThenImport_RoundTripsRejectionsPreservesDestinationHistoryAndExcludesGlobalSnowclones()
    {
        using var temp = new TemporaryDirectory();
        var sourceSnapshot = AddRejection(CreateSnapshot("Source", "assets/source.png"), "Source rejection");
        var sourceRepository = new SqliteWorkspaceRepository(temp.GetPath("source.db"));
        await sourceRepository.SaveAsync(sourceSnapshot, TestContext.Current.CancellationToken);
        var sourceSnowclones = new SqliteSnowcloneRepository(temp.GetPath("source.db"));
        var sourceSnowclone = NewSnowclone("Source {X}");
        await sourceSnowclones.SaveAsync(
            new SnowcloneLibrarySnapshot([sourceSnowclone], true),
            TestContext.Current.CancellationToken);
        var packagePath = temp.GetPath("rejections.fcworkspace");

        var exported = await NewService(
            sourceRepository,
            new LocalWorkspaceFileStore(temp.GetPath("source-files"))).ExportWorkspaceAsync(
                new WorkspaceExportRequest(sourceSnapshot.Workspaces[0].Id, packagePath),
                cancellationToken: TestContext.Current.CancellationToken);

        var embeddedPath = temp.GetPath("embedded.db");
        using (var archive = ZipFile.OpenRead(packagePath))
        await using (var input = archive.GetEntry("workspace.db")!.Open())
        await using (var output = File.Create(embeddedPath))
        {
            await input.CopyToAsync(output, TestContext.Current.CancellationToken);
        }

        var embeddedSnowclones = await new SqliteSnowcloneRepository(embeddedPath)
            .LoadAsync(TestContext.Current.CancellationToken);
        var destinationSnapshot = AddRejection(
            CreateSnapshot("Destination", "assets/destination.png"),
            "Destination rejection");
        var destinationRepository = new SqliteWorkspaceRepository(temp.GetPath("destination.db"));
        await destinationRepository.SaveAsync(destinationSnapshot, TestContext.Current.CancellationToken);
        var destinationSnowcloneRepository = new SqliteSnowcloneRepository(temp.GetPath("destination.db"));
        var destinationSnowclone = NewSnowclone("Destination {X}");
        await destinationSnowcloneRepository.SaveAsync(
            new SnowcloneLibrarySnapshot([destinationSnowclone], true),
            TestContext.Current.CancellationToken);

        var imported = await NewService(
            destinationRepository,
            new LocalWorkspaceFileStore(temp.GetPath("destination-files"))).ImportWorkspaceAsync(
                new WorkspaceImportRequest(packagePath),
                cancellationToken: TestContext.Current.CancellationToken);
        var restored = await destinationRepository.LoadAsync(TestContext.Current.CancellationToken);
        var restoredSnowclones = await destinationSnowcloneRepository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(exported.Succeeded);
        Assert.Equal(1, exported.Summary!.EntityCounts["ideationRejections"]);
        Assert.Empty(embeddedSnowclones.Snowclones);
        Assert.True(imported.Succeeded);
        Assert.Equal(2, restored.IdeationRejections.Count);
        Assert.Contains(destinationSnapshot.IdeationRejections[0], restored.IdeationRejections);
        Assert.Contains(sourceSnapshot.IdeationRejections[0], restored.IdeationRejections);
        Assert.Equal(destinationSnowclone, Assert.Single(restoredSnowclones.Snowclones));
    }

    [Fact]
    public async Task Export_MissingFileIsRecordedAndDoesNotFail()
    {
        using var temp = new TemporaryDirectory();
        var snapshot = CreateSnapshot("Missing", "assets/missing.png");
        var repository = new SqliteWorkspaceRepository(temp.GetPath("source.db"));
        await repository.SaveAsync(snapshot, TestContext.Current.CancellationToken);
        var service = NewService(repository, new LocalWorkspaceFileStore(temp.GetPath("files")));

        var result = await service.ExportWorkspaceAsync(
            new WorkspaceExportRequest(snapshot.Workspaces[0].Id, temp.GetPath("missing.fcworkspace")),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(["assets/missing.png"], result.Summary!.MissingFiles);
        Assert.Equal(0, result.Summary.WrittenFiles);

        var destinationRepository = new SqliteWorkspaceRepository(temp.GetPath("destination.db"));
        var imported = await NewService(
            destinationRepository,
            new LocalWorkspaceFileStore(temp.GetPath("destination-files"))).ImportWorkspaceAsync(
                new WorkspaceImportRequest(temp.GetPath("missing.fcworkspace")),
                cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(imported.Succeeded);
        Assert.True(Assert.Single((await destinationRepository.LoadAsync(TestContext.Current.CancellationToken)).Assets).IsMissing);
    }

    [Fact]
    public async Task ExportSuccess_ReplacesExistingDestinationOnlyWithCompletePackage()
    {
        using var temp = new TemporaryDirectory();
        var snapshot = CreateSnapshot("Replace", "assets/missing.png");
        var repository = new SqliteWorkspaceRepository(temp.GetPath("source.db"));
        await repository.SaveAsync(snapshot, TestContext.Current.CancellationToken);
        var destination = temp.GetPath("existing.fcworkspace");
        await File.WriteAllTextAsync(destination, "old content", TestContext.Current.CancellationToken);

        var result = await NewService(repository, new LocalWorkspaceFileStore(temp.GetPath("files")))
            .ExportWorkspaceAsync(
                new WorkspaceExportRequest(snapshot.Workspaces[0].Id, destination),
                cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        using var archive = ZipFile.OpenRead(destination);
        Assert.NotNull(archive.GetEntry("manifest.json"));
        Assert.NotNull(archive.GetEntry("workspace.db"));
    }

    [Fact]
    public async Task Reader_RefusesTraversalEntryAndCorruptPackage()
    {
        using var temp = new TemporaryDirectory();
        var packagePath = await CreatePackageAsync(temp, CreateSnapshot("Unsafe", "assets/file.png"));
        using (var archive = ZipFile.Open(packagePath, ZipArchiveMode.Update))
        {
            archive.CreateEntry("files/../escape.png");
        }

        var unsafeResult = await new ZipWorkspacePackageReader().ReadAsync(
            packagePath,
            cancellationToken: TestContext.Current.CancellationToken);
        var corruptPath = temp.GetPath("corrupt.fcworkspace");
        await File.WriteAllTextAsync(corruptPath, "not a zip", TestContext.Current.CancellationToken);
        var corruptResult = await new ZipWorkspacePackageReader().ReadAsync(
            corruptPath,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(unsafeResult.Succeeded);
        Assert.False(corruptResult.Succeeded);
    }

    [Fact]
    public async Task Import_UnsupportedFileIsSkippedAndAssetMarkedMissing()
    {
        using var temp = new TemporaryDirectory();
        var packagePath = await CreatePackageAsync(temp, CreateSnapshot("Unsupported", "assets/tool.exe"), [5, 6]);
        var repository = new SqliteWorkspaceRepository(temp.GetPath("destination.db"));
        var service = NewService(repository, new LocalWorkspaceFileStore(temp.GetPath("destination-files")));

        var result = await service.ImportWorkspaceAsync(
            new WorkspaceImportRequest(packagePath),
            cancellationToken: TestContext.Current.CancellationToken);
        var restored = await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(["assets/tool.exe"], result.Summary!.SkippedUnsupportedFiles);
        Assert.True(Assert.Single(restored.Assets).IsMissing);
    }

    [Fact]
    public async Task Import_ExistingManagedFileIsKeptAndCounted()
    {
        using var temp = new TemporaryDirectory();
        var packagePath = await CreatePackageAsync(temp, CreateSnapshot("Existing", "assets/file.png"), [1, 2, 3]);
        var repository = new SqliteWorkspaceRepository(temp.GetPath("destination.db"));
        var destinationFiles = new LocalWorkspaceFileStore(temp.GetPath("destination-files"));
        await destinationFiles.RestoreAsync("assets/file.png", new MemoryStream([9, 9]), TestContext.Current.CancellationToken);
        var service = NewService(repository, destinationFiles);

        var result = await service.ImportWorkspaceAsync(
            new WorkspaceImportRequest(packagePath),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.Summary!.SkippedExistingFiles);
        Assert.Equal([9, 9], await ReadBytesAsync(destinationFiles, "assets/file.png"));
    }

    [Fact]
    public async Task Reader_RefusesNewerFormatOrSchemaVersion()
    {
        using var temp = new TemporaryDirectory();
        var formatPackagePath = await CreatePackageAsync(temp, CreateSnapshot("Newer format", "assets/file.png"));
        await UpdateManifestAsync(formatPackagePath, manifest => manifest with { FormatVersion = 99 });
        var schemaPackagePath = await CreatePackageAsync(temp, CreateSnapshot("Newer schema", "assets/file.png"));
        await UpdateManifestAsync(schemaPackagePath, manifest => manifest with { SchemaVersion = 99 });

        var formatResult = await new ZipWorkspacePackageReader().ReadAsync(
            formatPackagePath,
            cancellationToken: TestContext.Current.CancellationToken);
        var schemaResult = await new ZipWorkspacePackageReader().ReadAsync(
            schemaPackagePath,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(formatResult.Succeeded);
        Assert.Contains("newer FusionCanvas", formatResult.Error);
        Assert.False(schemaResult.Succeeded);
        Assert.Contains("newer FusionCanvas", schemaResult.Error);
    }

    [Fact]
    public async Task Reader_MigratesOlderEmbeddedDatabase()
    {
        using var temp = new TemporaryDirectory();
        var snapshot = CreateSnapshot("Older", "assets/file.png");
        var packagePath = await CreatePackageAsync(temp, snapshot);
        var databasePath = temp.GetPath("workspace.db");
        using (var archive = ZipFile.Open(packagePath, ZipArchiveMode.Update))
        {
            var databaseEntry = archive.GetEntry("workspace.db")!;
            await using (var source = databaseEntry.Open())
            await using (var destination = File.Create(databasePath))
            {
                await source.CopyToAsync(destination, TestContext.Current.CancellationToken);
            }

            databaseEntry.Delete();
        }

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Pooling = false
        }.ToString();
        await using (var connection = new SqliteConnection(connectionString))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA user_version = 4;";
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        using (var archive = ZipFile.Open(packagePath, ZipArchiveMode.Update))
        {
            archive.CreateEntryFromFile(databasePath, "workspace.db");
        }

        await UpdateManifestAsync(packagePath, manifest => manifest with { SchemaVersion = 4 });
        var result = await new ZipWorkspacePackageReader().ReadAsync(
            packagePath,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        await using var session = result.Session!;
        Assert.Equal(snapshot.Workspaces, session.Snapshot.Workspaces);
        Assert.Equal(snapshot.Stores, session.Snapshot.Stores);
        Assert.Equal(snapshot.Assets, session.Snapshot.Assets);
    }

    [Fact]
    public async Task ExportCancellation_PreservesExistingDestination()
    {
        using var temp = new TemporaryDirectory();
        var snapshot = CreateSnapshot("Cancel", "assets/file.png");
        var repository = new SqliteWorkspaceRepository(temp.GetPath("source.db"));
        await repository.SaveAsync(snapshot, TestContext.Current.CancellationToken);
        var files = new LocalWorkspaceFileStore(temp.GetPath("files"));
        await files.RestoreAsync("assets/file.png", new MemoryStream(new byte[1024]), TestContext.Current.CancellationToken);
        var destination = temp.GetPath("existing.fcworkspace");
        await File.WriteAllTextAsync(destination, "original", TestContext.Current.CancellationToken);
        using var cancellation = new CancellationTokenSource();
        var progress = new InlineProgress<WorkspaceTransferProgress>(value =>
        {
            if (value.Phase == "Writing managed files")
            {
                cancellation.Cancel();
            }
        });
        var service = NewService(repository, files);

        var result = await service.ExportWorkspaceAsync(
            new WorkspaceExportRequest(snapshot.Workspaces[0].Id, destination),
            progress,
            cancellation.Token);

        Assert.True(result.Cancelled);
        Assert.Equal("original", await File.ReadAllTextAsync(destination, TestContext.Current.CancellationToken));
    }

    private static WorkspaceTransferService NewService(
        SqliteWorkspaceRepository repository,
        LocalWorkspaceFileStore files) =>
        new(repository, files, new ZipWorkspacePackageWriter(), new ZipWorkspacePackageReader(), () => Now);

    private static async Task<string> CreatePackageAsync(
        TemporaryDirectory temp,
        WorkspaceSnapshot snapshot,
        byte[]? fileBytes = null)
    {
        var repository = new SqliteWorkspaceRepository(temp.GetPath($"{Guid.NewGuid():N}.db"));
        await repository.SaveAsync(snapshot, TestContext.Current.CancellationToken);
        var files = new LocalWorkspaceFileStore(temp.GetPath($"files-{Guid.NewGuid():N}"));
        if (fileBytes is not null)
        {
            await files.RestoreAsync(snapshot.Assets[0].WorkspaceRelativePath, new MemoryStream(fileBytes), TestContext.Current.CancellationToken);
        }

        var packagePath = temp.GetPath($"{Guid.NewGuid():N}.fcworkspace");
        var result = await NewService(repository, files).ExportWorkspaceAsync(
            new WorkspaceExportRequest(snapshot.Workspaces[0].Id, packagePath),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(result.Succeeded, result.Error);
        return packagePath;
    }

    private static WorkspaceSnapshot CreateSnapshot(string name, string assetPath, bool archivedAsset = false)
    {
        var workspace = new FusionCanvas.Domain.Workspace.Workspace(Guid.NewGuid(), name, "description", false, Now, Now, "{\"workspace\":true}");
        var store = new Store(Guid.NewGuid(), workspace.Id, "Store", "description", false, Now, Now, "{\"store\":true}");
        var asset = new Asset(
            Guid.NewGuid(),
            store.Id,
            "Asset",
            "description",
            AssetKind.ExportedImage,
            assetPath,
            "original",
            false,
            archivedAsset,
            Now,
            Now,
            "{\"asset\":true}");
        return new WorkspaceSnapshot([workspace], [store], [], [], [], [asset], [], [], [], []);
    }

    private static WorkspaceSnapshot AddRejection(WorkspaceSnapshot snapshot, string text)
    {
        var store = Assert.Single(snapshot.Stores);
        var niche = new Niche(Guid.NewGuid(), store.Id, $"{text} niche", null, false, Now, Now, "{}");
        var rejection = new IdeationRejection(
            Guid.NewGuid(),
            store.Id,
            niche.Id,
            null,
            text,
            "Not suitable",
            IdeationMode.Basic,
            Now);
        return snapshot with
        {
            Niches = [niche],
            IdeationRejections = [rejection]
        };
    }

    private static WorkspaceSnapshot AddNormalizedCatalog(WorkspaceSnapshot snapshot)
    {
        var store = Assert.Single(snapshot.Stores);
        var blueprint = new Blueprint(Guid.NewGuid(), store.Id, "T-shirt", null, false, Now, Now);
        var provider = new PrintProvider(Guid.NewGuid(), store.Id, "SwiftPOD", "provider-42", false, Now, Now);
        var offering = new BlueprintOffering(Guid.NewGuid(), blueprint.Id, store.Id, "Tee", null, BlueprintOfferingKind.FixedPrintProvider, provider.Id, null, null, "offering-42", false, Now, Now);
        var colorOption = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Color, "Color", 0);
        var sizeOption = new OfferingOption(Guid.NewGuid(), offering.Id, OptionKind.Size, "Size", 1);
        var black = new OfferingOptionValue(Guid.NewGuid(), colorOption.Id, offering.Id, "Black", 0);
        var medium = new OfferingOptionValue(Guid.NewGuid(), sizeOption.Id, offering.Id, "M", 0);
        var variant = new OfferingVariant(Guid.NewGuid(), offering.Id, "Black / M", [black.Id, medium.Id], false, Now, Now);
        var area = new OfferingPlaceholder(Guid.NewGuid(), offering.Id, "Front", null, "front", "DTG", 3000, 4500, [variant.Id], false, Now, Now, providerReference: "front-area", artworkGuidance: new DesignAreaArtworkGuidance(4500, 5400, 300, "PNG", "Transparent"));
        var template = new MockupTemplate(Guid.NewGuid(), offering.Id, area.Id, "Front black", null, 1, false, Now, Now);
        var binding = new MockupTemplateColorVariant(Guid.NewGuid(), template.Id, black.Id, false, Now, Now);
        var revision = new MockupTemplateRevision(Guid.NewGuid(), template.Id, 1, area.Id, Now, providerMockupReference: "front-black", imageMapping: new MockupImageSpaceMapping(1200, 1200, 300, 200, 500, 650));
        var revisionColor = new MockupTemplateRevisionColor(Guid.NewGuid(), revision.Id, black.Id);
        var draft = new MockupTemplate(Guid.NewGuid(), offering.Id, null, "Manual draft", null, 1, false, Now, Now);
        var draftRevision = new MockupTemplateRevision(Guid.NewGuid(), draft.Id, 1, null, Now);
        return snapshot with
        {
            Blueprints = [blueprint],
            PrintProviders = [provider],
            BlueprintOfferings = [offering],
            OfferingOptions = [colorOption, sizeOption],
            OfferingOptionValues = [black, medium],
            OfferingVariants = [variant],
            OfferingPlaceholders = [area],
            MockupTemplates = [template, draft],
            MockupTemplateColorVariants = [binding],
            MockupTemplateRevisions = [revision, draftRevision],
            MockupTemplateRevisionColors = [revisionColor]
        };
    }

    private static Snowclone NewSnowclone(string phrase) =>
        new(Guid.NewGuid(), phrase, "Replace the placeholder.", Now, Now);

    private static async Task<byte[]> ReadBytesAsync(LocalWorkspaceFileStore store, string path)
    {
        await using var content = await store.OpenReadAsync(path, TestContext.Current.CancellationToken);
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, TestContext.Current.CancellationToken);
        return buffer.ToArray();
    }

    private static async Task UpdateManifestAsync(
        string packagePath,
        Func<WorkspacePackageManifest, WorkspacePackageManifest> update)
    {
        using var archive = ZipFile.Open(packagePath, ZipArchiveMode.Update);
        var entry = archive.GetEntry("manifest.json")!;
        WorkspacePackageManifest manifest;
        await using (var input = entry.Open())
        {
            manifest = (await JsonSerializer.DeserializeAsync<WorkspacePackageManifest>(
                input,
                new JsonSerializerOptions(JsonSerializerDefaults.Web),
                TestContext.Current.CancellationToken))!;
        }

        entry.Delete();
        var replacement = archive.CreateEntry("manifest.json");
        await using var output = replacement.Open();
        await JsonSerializer.SerializeAsync(
            output,
            update(manifest),
            new JsonSerializerOptions(JsonSerializerDefaults.Web),
            TestContext.Current.CancellationToken);
    }

    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory();

        public string GetPath(string path) => Path.Combine(_directory.FullName, path);

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            _directory.Delete(recursive: true);
        }
    }
}

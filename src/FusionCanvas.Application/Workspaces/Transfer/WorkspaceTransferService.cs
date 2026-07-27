using FusionCanvas.Domain.Assets;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workspace.Transfer;

namespace FusionCanvas.Application.Workspaces.Transfer;

public sealed class WorkspaceTransferService(
    IWorkspaceRepository repository,
    IWorkspaceFileStore fileStore,
    IWorkspacePackageWriter packageWriter,
    IWorkspacePackageReader packageReader,
    Func<DateTimeOffset>? clock = null) : IWorkspaceTransferService
{
    private readonly Func<DateTimeOffset> _clock = clock ?? (() => DateTimeOffset.UtcNow);

    public async Task<WorkspaceTransferResult> ExportWorkspaceAsync(
        WorkspaceExportRequest request,
        IProgress<WorkspaceTransferProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            var live = await repository.LoadAsync(cancellationToken).ConfigureAwait(false);
            var filtered = WorkspaceSnapshotFilter.ForWorkspace(live, request.WorkspaceId);
            var workspace = filtered.Snapshot.Workspaces.SingleOrDefault();
            if (workspace is null)
            {
                return WorkspaceTransferResult.Failure("Workspace was not found.");
            }

            var manifest = new WorkspacePackageManifest(
                packageWriter.CurrentFormatVersion,
                packageWriter.CurrentSchemaVersion,
                packageWriter.AppVersion,
                workspace.Id,
                workspace.Name,
                _clock(),
                CountEntities(filtered.Snapshot),
                [],
                [],
                filtered.DroppedAssetLinks.Count);
            var writeResult = await packageWriter.WriteAsync(
                new WorkspacePackageWriteRequest(request.DestinationPath, filtered.Snapshot, manifest, fileStore),
                progress,
                cancellationToken).ConfigureAwait(false);
            var finalManifest = writeResult.Manifest;
            var warnings = BuildWarnings(finalManifest.MissingFiles, [], finalManifest.DroppedLinkCount);
            return WorkspaceTransferResult.Success(
                workspace.Id,
                new WorkspaceTransferSummary(
                    finalManifest.EntityCounts,
                    finalManifest.Files.Count,
                    0,
                    0,
                    finalManifest.MissingFiles,
                    [],
                    finalManifest.DroppedLinkCount,
                    workspace.Name,
                    workspace.Name,
                    warnings));
        }
        catch (OperationCanceledException)
        {
            return WorkspaceTransferResult.CancelledResult();
        }
        catch (Exception exception)
        {
            return WorkspaceTransferResult.Failure($"Workspace export failed: {exception.Message}");
        }
    }

    public async Task<WorkspaceTransferResult> ImportWorkspaceAsync(
        WorkspaceImportRequest request,
        IProgress<WorkspaceTransferProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var createdFiles = new List<string>();
        var saved = false;
        try
        {
            var readResult = await packageReader.ReadAsync(request.PackagePath, progress, cancellationToken).ConfigureAwait(false);
            if (!readResult.Succeeded || readResult.Session is null)
            {
                return WorkspaceTransferResult.Failure(readResult.Error ?? "The workspace package could not be read.");
            }

            await using var session = readResult.Session;
            var live = await repository.LoadAsync(cancellationToken).ConfigureAwait(false);
            var collisions = WorkspaceImportPreflight.FindIdentityCollisions(live, session.Snapshot);
            if (collisions.Count > 0)
            {
                return WorkspaceTransferResult.Failure("This workspace already exists in this installation.");
            }

            var packagedWorkspace = session.Snapshot.Workspaces.Single();
            var finalName = WorkspaceImportPreflight.ResolveImportName(
                packagedWorkspace.Name,
                live.Workspaces.Where(workspace => !workspace.IsArchived).Select(workspace => workspace.Name));

            var skippedExisting = 0;
            for (var index = 0; index < session.Files.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var file = session.Files[index];
                progress?.Report(new WorkspaceTransferProgress("Restoring managed files", index, session.Files.Count));
                await using var content = await file.OpenReadAsync(cancellationToken).ConfigureAwait(false);
                var outcome = await fileStore.RestoreAsync(
                    file.WorkspaceRelativePath,
                    content,
                    cancellationToken).ConfigureAwait(false);
                if (outcome == WorkspaceFileRestoreOutcome.Created)
                {
                    createdFiles.Add(file.WorkspaceRelativePath);
                }
                else
                {
                    skippedExisting++;
                }
            }

            progress?.Report(new WorkspaceTransferProgress(
                "Restoring managed files",
                session.Files.Count,
                session.Files.Count));

            var imported = PrepareImportedSnapshot(session, finalName);
            var merged = Merge(live, imported);
            progress?.Report(new WorkspaceTransferProgress("Saving workspace", 0, 1));
            await repository.SaveAsync(merged, cancellationToken).ConfigureAwait(false);
            saved = true;
            progress?.Report(new WorkspaceTransferProgress("Saving workspace", 1, 1));

            var missingFiles = session.Manifest.MissingFiles
                .Concat(session.SkippedUnsupportedFiles)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray();
            var warnings = BuildWarnings(
                missingFiles,
                session.SkippedUnsupportedFiles,
                session.Manifest.DroppedLinkCount).ToList();
            if (!string.Equals(packagedWorkspace.Name, finalName, StringComparison.Ordinal))
            {
                warnings.Add($"Workspace renamed from '{packagedWorkspace.Name}' to '{finalName}'.");
            }

            return WorkspaceTransferResult.Success(
                packagedWorkspace.Id,
                new WorkspaceTransferSummary(
                    CountEntities(imported),
                    0,
                    createdFiles.Count,
                    skippedExisting,
                    missingFiles,
                    session.SkippedUnsupportedFiles,
                    session.Manifest.DroppedLinkCount,
                    packagedWorkspace.Name,
                    finalName,
                    warnings));
        }
        catch (OperationCanceledException)
        {
            return WorkspaceTransferResult.CancelledResult();
        }
        catch (Exception exception)
        {
            return WorkspaceTransferResult.Failure($"Workspace import failed: {exception.Message}");
        }
        finally
        {
            if (!saved)
            {
                foreach (var path in createdFiles)
                {
                    fileStore.TryDelete(path);
                }
            }
        }
    }

    private static WorkspaceSnapshot PrepareImportedSnapshot(
        IWorkspacePackageReadSession session,
        string finalName)
    {
        var missingPaths = session.Manifest.MissingFiles
            .Concat(session.SkippedUnsupportedFiles)
            .ToHashSet(StringComparer.Ordinal);
        var workspace = session.Snapshot.Workspaces.Single() with
        {
            Name = finalName,
            IsArchived = false
        };
        var assets = session.Snapshot.Assets
            .Select(asset => missingPaths.Contains(asset.WorkspaceRelativePath)
                ? CopyAsset(asset, isMissing: true)
                : asset)
            .ToArray();
        return session.Snapshot with
        {
            Workspaces = [workspace],
            Assets = assets
        };
    }

    private static Asset CopyAsset(Asset asset, bool isMissing) =>
        new(
            asset.Id,
            asset.StoreId,
            asset.Name,
            asset.Description,
            asset.Kind,
            asset.WorkspaceRelativePath,
            asset.OriginalSourcePath,
            isMissing,
            asset.IsArchived,
            asset.CreatedAt,
            asset.UpdatedAt,
            asset.MetadataJson);

    private static WorkspaceSnapshot Merge(WorkspaceSnapshot live, WorkspaceSnapshot imported) =>
        new(
            [.. live.Workspaces, .. imported.Workspaces],
            [.. live.Stores, .. imported.Stores],
            [.. live.Niches, .. imported.Niches],
            [.. live.Groups, .. imported.Groups],
            [.. live.Items, .. imported.Items],
            [.. live.Assets, .. imported.Assets],
            [.. live.Prompts, .. imported.Prompts],
            [.. live.Tags, .. imported.Tags],
            [.. live.ItemTags, .. imported.ItemTags],
            [.. live.AssetLinks, .. imported.AssetLinks]);

    internal static IReadOnlyDictionary<string, int> CountEntities(WorkspaceSnapshot snapshot) =>
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["workspaces"] = snapshot.Workspaces.Count,
            ["stores"] = snapshot.Stores.Count,
            ["niches"] = snapshot.Niches.Count,
            ["groups"] = snapshot.Groups.Count,
            ["items"] = snapshot.Items.Count,
            ["assets"] = snapshot.Assets.Count,
            ["prompts"] = snapshot.Prompts.Count,
            ["tags"] = snapshot.Tags.Count,
            ["itemTags"] = snapshot.ItemTags.Count,
            ["assetLinks"] = snapshot.AssetLinks.Count
        };

    private static IReadOnlyList<string> BuildWarnings(
        IReadOnlyList<string> missingFiles,
        IReadOnlyList<string> skippedUnsupportedFiles,
        int droppedLinkCount)
    {
        var warnings = new List<string>();
        if (missingFiles.Count > 0)
        {
            warnings.Add($"{missingFiles.Count} managed file(s) are missing.");
        }

        if (skippedUnsupportedFiles.Count > 0)
        {
            warnings.Add($"{skippedUnsupportedFiles.Count} unsupported file(s) were skipped.");
        }

        if (droppedLinkCount > 0)
        {
            warnings.Add($"{droppedLinkCount} cross-workspace asset link(s) were dropped.");
        }

        return warnings;
    }
}

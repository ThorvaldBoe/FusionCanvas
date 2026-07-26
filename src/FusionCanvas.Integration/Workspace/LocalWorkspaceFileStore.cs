using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Assets;

namespace FusionCanvas.Integration.Workspace;

public sealed class LocalWorkspaceFileStore : IWorkspaceFileStore
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".ai",
        ".afdesign",
        ".brush",
        ".eps",
        ".jpg",
        ".jpeg",
        ".otf",
        ".pdf",
        ".png",
        ".psd",
        ".svg",
        ".tif",
        ".tiff",
        ".ttf",
        ".webp"
    };

    public LocalWorkspaceFileStore(string workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
        {
            throw new ArgumentException("Workspace root must not be empty.", nameof(workspaceRoot));
        }

        WorkspaceRoot = Path.GetFullPath(workspaceRoot);
        Directory.CreateDirectory(WorkspaceRoot);
    }

    public string WorkspaceRoot { get; }

    public async Task<ManagedWorkspaceFile> ImportAsync(
        string sourcePath,
        AssetKind kind,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("The source asset file was not found.", sourcePath);
        }

        var extension = Path.GetExtension(sourcePath);
        if (extension.Length > 0 && !SupportedExtensions.Contains(extension))
        {
            throw new NotSupportedException($"The file extension '{extension}' is not a recognized creative asset type.");
        }

        var importedAt = DateTimeOffset.UtcNow;
        var assetDirectory = Path.Combine(WorkspaceRoot, "assets", importedAt.ToString("yyyy"), importedAt.ToString("MM"));
        Directory.CreateDirectory(assetDirectory);

        var fileName = $"{Path.GetFileNameWithoutExtension(sourcePath)}-{Guid.NewGuid():N}{extension}";
        var destinationPath = Path.Combine(assetDirectory, fileName);

        await using (var source = File.OpenRead(sourcePath))
        await using (var destination = File.Create(destinationPath))
        {
            await source.CopyToAsync(destination, cancellationToken);
        }

        var relativePath = WorkspaceFileReference.Normalize(Path.GetRelativePath(WorkspaceRoot, destinationPath));
        return new ManagedWorkspaceFile(
            Path.GetFileName(sourcePath),
            kind,
            relativePath,
            destinationPath,
            Path.GetFullPath(sourcePath));
    }

    public bool Exists(string workspaceRelativePath)
    {
        string normalizedReference;
        try
        {
            normalizedReference = WorkspaceFileReference.Normalize(workspaceRelativePath);
        }
        catch (ArgumentException)
        {
            return false;
        }

        var workspaceBoundary = Path.EndsInDirectorySeparator(WorkspaceRoot)
            ? WorkspaceRoot
            : WorkspaceRoot + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(WorkspaceRoot, normalizedReference));
        return fullPath.StartsWith(workspaceBoundary, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath);
    }

    public bool TryDelete(string workspaceRelativePath)
    {
        string normalizedReference;
        try
        {
            normalizedReference = WorkspaceFileReference.Normalize(workspaceRelativePath);
        }
        catch (ArgumentException)
        {
            return false;
        }

        var fullPath = ResolveWithinWorkspace(normalizedReference);
        if (fullPath is null || !File.Exists(fullPath))
        {
            return false;
        }

        try
        {
            File.Delete(fullPath);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    public Task<Stream> OpenReadAsync(string workspaceRelativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveWithinWorkspaceOrThrow(workspaceRelativePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("The managed workspace file was not found.", fullPath);
        }

        return Task.FromResult<Stream>(File.OpenRead(fullPath));
    }

    public async Task ExportCopyAsync(string workspaceRelativePath, string destinationPath, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveWithinWorkspaceOrThrow(workspaceRelativePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("The managed workspace file was not found.", fullPath);
        }

        var normalizedDestination = Path.GetFullPath(destinationPath);
        if (string.Equals(normalizedDestination, fullPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Export copy destination must differ from the managed source.");
        }

        await using var source = File.OpenRead(fullPath);
        await using var destination = File.Create(normalizedDestination);
        await source.CopyToAsync(destination, cancellationToken);
    }

    private string? ResolveWithinWorkspace(string normalizedReference)
    {
        var workspaceBoundary = Path.EndsInDirectorySeparator(WorkspaceRoot)
            ? WorkspaceRoot
            : WorkspaceRoot + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(WorkspaceRoot, normalizedReference));
        return fullPath.StartsWith(workspaceBoundary, StringComparison.OrdinalIgnoreCase) ? fullPath : null;
    }

    private string ResolveWithinWorkspaceOrThrow(string workspaceRelativePath)
    {
        string normalizedReference;
        try
        {
            normalizedReference = WorkspaceFileReference.Normalize(workspaceRelativePath);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException("The managed reference escapes the workspace boundary.", exception);
        }

        var fullPath = ResolveWithinWorkspace(normalizedReference)
            ?? throw new InvalidOperationException("The managed reference escapes the workspace boundary.");
        return fullPath;
    }
}

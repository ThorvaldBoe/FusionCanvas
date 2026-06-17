using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Integration.Workspace;

public sealed class LocalWorkspaceFileStore(string workspaceRoot) : IWorkspaceFileStore
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

    public string WorkspaceRoot { get; } = Path.GetFullPath(workspaceRoot);

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

        var relativePath = Path.GetRelativePath(WorkspaceRoot, destinationPath);
        return new ManagedWorkspaceFile(
            Path.GetFileName(sourcePath),
            kind,
            relativePath,
            destinationPath,
            Path.GetFullPath(sourcePath));
    }

    public bool Exists(string workspaceRelativePath)
    {
        var workspaceBoundary = Path.EndsInDirectorySeparator(WorkspaceRoot)
            ? WorkspaceRoot
            : WorkspaceRoot + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(WorkspaceRoot, workspaceRelativePath));
        return fullPath.StartsWith(workspaceBoundary, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath);
    }
}

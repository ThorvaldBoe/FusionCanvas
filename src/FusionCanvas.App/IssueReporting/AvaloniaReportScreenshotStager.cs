using System.Diagnostics;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;

namespace FusionCanvas.App.IssueReporting;

public sealed class AvaloniaReportScreenshotStager(IClipboard clipboard) : IReportScreenshotStager
{
    private const long MaximumImageBytes = 10 * 1024 * 1024;
    private const long MaximumPixelCount = 40_000_000;
    private static readonly string StagingDirectory = Path.Combine(Path.GetTempPath(), "FusionCanvas", "IssueReports");

    public async Task<StagedScreenshot> PasteFromClipboardAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var image = await clipboard.TryGetBitmapAsync().ConfigureAwait(false)
            ?? throw new InvalidOperationException("The clipboard does not contain an image. Take a screenshot, then try Paste screenshot again.");
        if ((long)image.PixelSize.Width * image.PixelSize.Height > MaximumPixelCount)
        {
            image.Dispose();
            throw new InvalidOperationException("The screenshot is too large to attach. Capture a smaller area and try again.");
        }

        Directory.CreateDirectory(StagingDirectory);
        var path = Path.Combine(StagingDirectory, $"report-{Guid.NewGuid():N}.png");
        try
        {
            await using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                image.Save(stream);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            if (new FileInfo(path).Length > MaximumImageBytes)
            {
                throw new InvalidOperationException("The screenshot exceeds GitHub's 10 MB image upload limit. Capture a smaller area and try again.");
            }

            return new StagedScreenshot(path, image);
        }
        catch
        {
            image.Dispose();
            Delete(path);
            throw;
        }
    }

    public void OpenLocation(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        var stagingRoot = Path.GetFullPath(StagingDirectory) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(stagingRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The screenshot is not in the report staging folder.");
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = Path.GetDirectoryName(fullPath) ?? StagingDirectory,
            UseShellExecute = true
        });
    }

    public void Delete(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            var stagingRoot = Path.GetFullPath(StagingDirectory) + Path.DirectorySeparatorChar;
            if (fullPath.StartsWith(stagingRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
        }
    }

    public static void RemoveStaleFiles()
    {
        try
        {
            if (!Directory.Exists(StagingDirectory)) return;
            foreach (var path in Directory.EnumerateFiles(StagingDirectory, "report-*.png"))
            {
                if (File.GetLastWriteTimeUtc(path) < DateTime.UtcNow.AddDays(-1))
                {
                    File.Delete(path);
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}

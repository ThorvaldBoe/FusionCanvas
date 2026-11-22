using Avalonia.Media.Imaging;

namespace FusionCanvas.App.IssueReporting;

public sealed record StagedScreenshot(string Path, Bitmap Preview);

public interface IReportScreenshotStager
{
    Task<StagedScreenshot> PasteFromClipboardAsync(CancellationToken cancellationToken = default);
    void OpenLocation(string path);
    void Delete(string path);
}

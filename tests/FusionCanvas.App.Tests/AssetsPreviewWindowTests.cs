using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using FusionCanvas.App.Assets;
using FusionCanvas.Application.Assets;
using FusionCanvas.Domain.Assets;

namespace FusionCanvas.App.Tests;

public class AssetsPreviewWindowTests
{
    [AvaloniaFact]
    public void PreviewWindow_DisplaysAssetThumbnail()
    {
        var parent = new AssetsViewModel(new StubAssetManagementService());
        var row = new AssetRowViewModel(
            new AssetSummary(
                Guid.NewGuid(), Guid.NewGuid(), "Logo", AssetKind.ExportedImage,
                "assets/logo.png", null, "logo.png", false, null,
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
                Path.GetFullPath("src/FusionCanvas.App/Assets/FusionCanvasLogo_Square.png")),
            parent.AvailablePurposes,
            parent);
        var window = new AssetPreviewWindow { DataContext = row };

        try
        {
            window.Show();
            var image = window.GetVisualDescendants().OfType<Image>().Single();
            Assert.IsType<Bitmap>(image.Source);
            Assert.True(row.CanPreview);
        }
        finally
        {
            window.Close();
            row.Dispose();
        }
    }

    private sealed class StubAssetManagementService : IAssetManagementService
    {
        public Guid? ActiveWorkspaceId => null;
        public void SetActiveWorkspace(Guid? workspaceId) { }
        public Task<AssetManagementState> LoadAsync(AssetContextReference context, CancellationToken cancellationToken = default) =>
            Task.FromResult(AssetManagementState.Empty);
        public Task<AssetManagementResult> ImportAssetAsync(AssetManagementImportRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<AssetManagementResult> RelabelAssetAsync(AssetManagementRelabelRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<AssetManagementResult> RemoveAssetAsync(AssetManagementRemoveRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}

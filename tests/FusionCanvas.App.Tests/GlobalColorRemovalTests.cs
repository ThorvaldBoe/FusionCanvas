using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using FusionCanvas.App.StageTools;
using FusionCanvas.Application.DesignFiles;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using AvaloniaImage = Avalonia.Controls.Image;

namespace FusionCanvas.App.Tests;

public sealed class GlobalColorRemovalTests
{
    [AvaloniaFact]
    public async Task ViewModel_UsesHexColorAndPreventsApplyUntilPreviewIsValid()
    {
        var service = new StubGlobalColorRemovalService();
        using var viewModel = new GlobalColorRemovalViewModel(service, Guid.NewGuid(), Guid.NewGuid(), "design.png");

        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        Assert.False(viewModel.HasPickedColor);
        Assert.False(viewModel.CanApply);
        Assert.Contains("Pick a visible color", viewModel.StatusMessage);

        viewModel.PickedColorHex = "#000000";
        await viewModel.UseHexColorAsync(TestContext.Current.CancellationToken);

        Assert.True(viewModel.HasPickedColor);
        Assert.True(viewModel.HasPreview);
        Assert.True(viewModel.CanApply);
        Assert.Contains("everywhere", viewModel.WarningMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, service.PreviewCalls);

        viewModel.TolerancePercent = 15;
        await Task.Delay(180, TestContext.Current.CancellationToken);
        Assert.Equal(0.15, service.LastTolerance, precision: 3);
    }

    [AvaloniaFact]
    public async Task Window_RendersAccessibleControlsAndApplyClosesTheWindow()
    {
        var service = new StubGlobalColorRemovalService();
        using var viewModel = new GlobalColorRemovalViewModel(service, Guid.NewGuid(), Guid.NewGuid(), "design.png");
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.PickedColorHex = "#000000";
        await viewModel.UseHexColorAsync(TestContext.Current.CancellationToken);

        var window = new GlobalColorRemovalWindow { DataContext = viewModel };
        try
        {
            window.Show();
            window.UpdateLayout();

            Assert.Equal(900, window.Width);
            Assert.Equal(720, window.Height);
            Assert.Equal(560, window.MinWidth);
            Assert.Equal(480, window.MinHeight);
            var layout = Assert.IsType<Grid>(window.Content);
            Assert.Equal(new Thickness(24), layout.Margin);
            Assert.Equal(14, layout.RowSpacing);
            Assert.NotNull(window.GetVisualDescendants().OfType<AvaloniaImage>().SingleOrDefault(image => image.Source is Bitmap));
            Assert.Single(window.GetVisualDescendants().OfType<Slider>());
            Assert.Contains(window.GetVisualDescendants().OfType<Button>(), button => Equals(button.Content, "Apply"));
            Assert.Contains(window.GetVisualDescendants().OfType<Button>(), button => Equals(button.Content, "Cancel"));

            var slider = Assert.Single(window.GetVisualDescendants().OfType<Slider>());
            slider.Value = 20;
            Assert.Equal(20, viewModel.TolerancePercent);

            var apply = Assert.Single(window.GetVisualDescendants().OfType<Button>(), button => Equals(button.Content, "Apply"));
            apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await TestSupport.HeadlessUiWait.UntilAsync(() => !window.IsVisible, "color removal window closes after apply");

            Assert.Equal(1, service.ApplyCalls);
        }
        finally
        {
            window.Close();
        }
    }

    private sealed class StubGlobalColorRemovalService : IGlobalColorRemovalService
    {
        public int PreviewCalls { get; private set; }
        public int ApplyCalls { get; private set; }
        public double LastTolerance { get; private set; }

        public Task<GlobalColorRemovalSourceResult> OpenSourcePreviewAsync(Guid itemId, Guid assetId, CancellationToken cancellationToken = default) =>
            Task.FromResult(GlobalColorRemovalSourceResult.Success(assetId, "design.png", PngBytes()));

        public Task<GlobalColorRemovalSampleResult> SampleColorAsync(Guid itemId, Guid assetId, int x, int y, CancellationToken cancellationToken = default) =>
            Task.FromResult(GlobalColorRemovalSampleResult.Success(new GlobalColorRemovalColor(0, 0, 0)));

        public Task<GlobalColorRemovalPreviewResult> PreviewAsync(Guid itemId, Guid assetId, GlobalColorRemovalParameters parameters, CancellationToken cancellationToken = default)
        {
            PreviewCalls++;
            LastTolerance = parameters.Tolerance;
            return Task.FromResult(GlobalColorRemovalPreviewResult.Success(new GlobalColorRemovalRasterPreview(PngBytes(), 1, 1, 1, 2)));
        }

        public Task<GlobalColorRemovalApplyResult> ApplyAsync(Guid itemId, Guid assetId, GlobalColorRemovalParameters parameters, CancellationToken cancellationToken = default)
        {
            ApplyCalls++;
            return Task.FromResult(GlobalColorRemovalApplyResult.Success(
                new GlobalColorRemovalDerivedAsset(Guid.NewGuid(), "derived.png", "assets/derived.png"),
                1));
        }

        private static byte[] PngBytes()
        {
            using var image = new Image<Rgba32>(1, 1, new Rgba32(0, 0, 0, 255));
            using var stream = new MemoryStream();
            image.SaveAsPng(stream);
            stream.Position = 0;
            return stream.ToArray();
        }
    }
}

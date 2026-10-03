using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FusionCanvas.App.StageTools;
using FusionCanvas.App.Tests.TestSupport;

namespace FusionCanvas.App.Tests;

public class DesignPreviewWindowTests
{
    [AvaloniaFact]
    public void PreviewWindow_ConstructsWithEmptyImageAndMinimumSize()
    {
        using var viewModel = MainWindowViewModelFactory.CreateSample();
        var window = new DesignPreviewWindow { DataContext = viewModel.DesignTool };

        try
        {
            window.Show();
            window.UpdateLayout();

            Assert.Equal("Image Preview", window.Title);
            Assert.Equal(400, window.MinWidth);
            Assert.Equal(300, window.MinHeight);
            Assert.True(window.Bounds.Width >= window.MinWidth);
            Assert.True(window.Bounds.Height >= window.MinHeight);
            Assert.Null(Assert.Single(window.GetVisualDescendants().OfType<Image>()).Source);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void PreviewWindow_BindsImageAndClearsItWhenPreviewCloses()
    {
        using var viewModel = MainWindowViewModelFactory.CreateSample();
        var window = new DesignPreviewWindow { DataContext = viewModel.DesignTool };

        try
        {
            window.Show();
            var image = Assert.Single(window.GetVisualDescendants().OfType<Image>());
            var imagePath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "../../../../../src/FusionCanvas.App/Assets/FusionCanvasLogo_Square.png"));

            viewModel.DesignTool.PreviewSupportingImage(Guid.NewGuid(), imagePath);
            Dispatcher.UIThread.RunJobs();

            Assert.NotNull(viewModel.DesignTool.PreviewBitmap);
            Assert.Same(viewModel.DesignTool.PreviewBitmap, image.Source);

            viewModel.DesignTool.ClosePreviewDialog();
            Dispatcher.UIThread.RunJobs();

            Assert.Null(image.Source);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void PreviewWindow_CloseButtonClosesWindow()
    {
        using var viewModel = MainWindowViewModelFactory.CreateSample();
        var window = new DesignPreviewWindow { DataContext = viewModel.DesignTool };
        var closed = false;
        window.Closed += (_, _) => closed = true;

        try
        {
            window.Show();
            var closeButton = Assert.Single(window.GetVisualDescendants().OfType<Button>(),
                button => Equals(button.Content, "Close"));

            closeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.True(closed);
            Assert.False(window.IsVisible);
        }
        finally
        {
            window.Close();
        }
    }
}

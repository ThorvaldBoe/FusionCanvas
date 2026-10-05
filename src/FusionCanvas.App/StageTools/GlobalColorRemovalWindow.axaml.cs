using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace FusionCanvas.App.StageTools;

public partial class GlobalColorRemovalWindow : Window
{
    public GlobalColorRemovalWindow()
    {
        InitializeComponent();
    }

    private async void OnUseHexColorClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is GlobalColorRemovalViewModel viewModel)
        {
            await viewModel.UseHexColorAsync();
        }
    }

    private async void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not GlobalColorRemovalViewModel viewModel)
        {
            return;
        }

        var result = await viewModel.ApplyAsync();
        if (result.Succeeded)
        {
            Close();
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is GlobalColorRemovalViewModel viewModel)
        {
            viewModel.Cancel();
        }

        Close();
    }

    private async void OnPreviewPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not GlobalColorRemovalViewModel viewModel
            || viewModel.IsReadOnly
            || viewModel.IsBusy
            || PreviewImage.Source is not Avalonia.Media.Imaging.Bitmap bitmap)
        {
            return;
        }

        var point = e.GetPosition(PreviewImage);
        if (!TryGetImagePixel(point, PreviewImage.Bounds.Width, PreviewImage.Bounds.Height, bitmap.PixelSize.Width, bitmap.PixelSize.Height, out var x, out var y))
        {
            return;
        }

        PreviewImage.Focus();
        await viewModel.PickColorAtAsync(x, y);
    }

    private static bool TryGetImagePixel(
        Avalonia.Point point,
        double controlWidth,
        double controlHeight,
        int imageWidth,
        int imageHeight,
        out int x,
        out int y)
    {
        x = 0;
        y = 0;
        if (controlWidth <= 0 || controlHeight <= 0 || imageWidth <= 0 || imageHeight <= 0)
        {
            return false;
        }

        var scale = Math.Min(controlWidth / imageWidth, controlHeight / imageHeight);
        var renderedWidth = imageWidth * scale;
        var renderedHeight = imageHeight * scale;
        var offsetX = (controlWidth - renderedWidth) / 2;
        var offsetY = (controlHeight - renderedHeight) / 2;
        if (point.X < offsetX || point.X >= offsetX + renderedWidth || point.Y < offsetY || point.Y >= offsetY + renderedHeight)
        {
            return false;
        }

        x = Math.Clamp((int)((point.X - offsetX) / scale), 0, imageWidth - 1);
        y = Math.Clamp((int)((point.Y - offsetY) / scale), 0, imageHeight - 1);
        return true;
    }
}

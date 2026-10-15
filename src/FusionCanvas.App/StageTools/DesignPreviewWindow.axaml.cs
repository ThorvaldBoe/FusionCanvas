using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace FusionCanvas.App.StageTools;

public partial class DesignPreviewWindow : Window
{
    private bool _allowClose;
    private bool _discardPromptOpen;

    public DesignPreviewWindow()
    {
        InitializeComponent();
        AdjustmentSurface.DraftBoundsChanged += OnDraftBoundsChanged;
        Opened += (_, _) => Dispatcher.UIThread.Post(() => AdjustmentSurface.Focus(), DispatcherPriority.Input);
        Closing += OnClosing;
        KeyDown += OnKeyDown;
    }

    private void OnDraftBoundsChanged(object? sender, ArtworkPixelRectangleEventArgs e)
    {
        if (DataContext is DesignStageToolViewModel viewModel)
            viewModel.UpdateArtworkAdjustmentBounds(e.Bounds);
    }

    private void OnAdjustClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not DesignStageToolViewModel viewModel) return;
        viewModel.BeginArtworkAdjustment();
        Dispatcher.UIThread.Post(() => AdjustmentSurface.Focus(), DispatcherPriority.Input);
    }

    private async void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not DesignStageToolViewModel viewModel) return;
        await viewModel.ApplyArtworkAdjustmentAsync();
    }

    private void OnCancelAdjustmentClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not DesignStageToolViewModel viewModel) return;
        viewModel.CancelArtworkAdjustment();
        Dispatcher.UIThread.Post(() => AdjustArtworkButton.Focus(), DispatcherPriority.Input);
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        Close();
        e.Handled = true;
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_allowClose || DataContext is not DesignStageToolViewModel { IsArtworkAdjustmentDirty: true } viewModel)
            return;

        e.Cancel = true;
        if (_discardPromptOpen) return;
        _discardPromptOpen = true;
        var confirmation = new ArtworkAdjustmentDiscardWindow();
        var discard = await confirmation.ShowDialog<bool>(this);
        _discardPromptOpen = false;
        if (discard)
        {
            viewModel.CancelArtworkAdjustment();
            _allowClose = true;
            Close();
        }
        else
        {
            Dispatcher.UIThread.Post(() => AdjustmentSurface.Focus(), DispatcherPriority.Input);
        }
    }
}

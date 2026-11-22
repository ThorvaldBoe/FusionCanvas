using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

namespace FusionCanvas.App.StageTools;

public partial class ListingColorOptionsWindow : Window
{
    public ListingColorOptionsWindow()
    {
        InitializeComponent();
        Opened += (_, _) => Dispatcher.UIThread.Post(() => SearchColorsBox.Focus(), DispatcherPriority.Input);
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        if (DataContext is PrintifyListingStageToolViewModel viewModel)
            viewModel.CancelMoreOptionsCommand.Execute(null);
        Close();
        e.Handled = true;
    }
}

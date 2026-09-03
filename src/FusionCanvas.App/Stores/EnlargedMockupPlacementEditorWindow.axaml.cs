using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace FusionCanvas.App.Stores;

public partial class EnlargedMockupPlacementEditorWindow : Window
{
    public EnlargedMockupPlacementEditorWindow()
    {
        InitializeComponent();
        Opened += (_, _) => Dispatcher.UIThread.Post(() => PlacementEditor.Focus());
        CloseButton.Click += OnCloseClicked;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => Close();
}

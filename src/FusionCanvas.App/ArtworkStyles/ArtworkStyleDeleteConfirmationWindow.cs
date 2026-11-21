using Avalonia.Controls;
using FusionCanvas.App.DocumentWindow;

namespace FusionCanvas.App.ArtworkStyles;

internal sealed class ArtworkStyleDeleteConfirmationWindow : Window
{
    public ArtworkStyleDeleteConfirmationWindow(string message = "Delete this style? Items using it will switch to General. This cannot be undone.", string confirmLabel = "Delete")
    {
        Title = "Delete artwork style";
        Width = 420;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 14,
            Children =
            {
                new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Spacing = 8,
                    Children =
                    {
                        new Button { Content = "Cancel", Command = new RelayCommand(_ => Close(false)) },
                        new Button { Content = confirmLabel, Classes = { "primary" }, Command = new RelayCommand(_ => Close(true)) }
                    }
                }
            }
        };
    }
}

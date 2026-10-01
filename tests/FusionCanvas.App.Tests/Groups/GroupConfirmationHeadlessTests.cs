using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using FusionCanvas.App.Groups;
using FusionCanvas.App.Navigation;

namespace FusionCanvas.App.Tests.Groups;

public sealed class GroupConfirmationHeadlessTests
{
    [AvaloniaFact]
    public async Task GroupActionConfirmation_CancelButtonReturnsFalse()
    {
        var owner = new Window();
        var dialog = new GroupActionConfirmationWindow(
            "Archive selected entities",
            "Archive 2 selected entities? They can be restored later.");

        try
        {
            owner.Show();
            var resultTask = dialog.ShowDialog<bool>(owner);
            PumpLayout(dialog);

            var visibleText = VisibleText(dialog);
            Assert.Contains("Archive selected entities", visibleText);
            Assert.Contains("Archive 2 selected entities? They can be restored later.", visibleText);

            FindButton(dialog, "Cancel").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.False(await resultTask);
        }
        finally
        {
            if (dialog.IsVisible)
            {
                dialog.Close();
            }

            if (owner.IsVisible)
            {
                owner.Close();
            }
        }
    }

    [AvaloniaFact]
    public async Task GroupActionConfirmation_ConfirmButtonReturnsTrue()
    {
        var owner = new Window();
        var dialog = new GroupActionConfirmationWindow(
            "Delete selected entities",
            "Permanently delete 2 selected entities and any contained descendants? This cannot be undone.");

        try
        {
            owner.Show();
            var resultTask = dialog.ShowDialog<bool>(owner);
            PumpLayout(dialog);

            FindButton(dialog, "Confirm").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.True(await resultTask);
        }
        finally
        {
            if (dialog.IsVisible)
            {
                dialog.Close();
            }

            if (owner.IsVisible)
            {
                owner.Close();
            }
        }
    }

    [AvaloniaFact]
    public void GroupDeleteConfirmation_RendersImpactAndIrreversibleWarning()
    {
        var impact = new GroupDeleteImpact(Guid.NewGuid(), "Campaign", 2, 3, new HashSet<Guid>());
        var dialog = new GroupDeleteConfirmationWindow(impact);

        try
        {
            dialog.Show();
            PumpLayout(dialog);

            var viewModel = Assert.IsType<GroupDeleteConfirmationViewModel>(dialog.DataContext);
            Assert.Equal("Delete 'Campaign'?", viewModel.Title);
            Assert.Equal(
                "The selected group, 2 subgroups, and 3 items will be permanently lost.",
                viewModel.WarningMessage);

            var visibleText = VisibleText(dialog);
            Assert.Contains(viewModel.Title, visibleText);
            Assert.Contains(viewModel.WarningMessage, visibleText);
            Assert.Contains(
                "This action cannot be undone. Reusable asset files are retained, but links from deleted groups and items are removed.",
                visibleText);
            Assert.NotNull(FindButton(dialog, "Cancel"));
            Assert.NotNull(FindButton(dialog, "Delete permanently"));
        }
        finally
        {
            if (dialog.IsVisible)
            {
                dialog.Close();
            }
        }
    }

    private static Button FindButton(Window window, string content) =>
        window.GetVisualDescendants()
            .OfType<Button>()
            .Single(button => Equals(button.Content, content));

    private static string[] VisibleText(Window window) =>
        window.GetVisualDescendants()
            .OfType<TextBlock>()
            .Where(textBlock => textBlock.IsVisible && !string.IsNullOrWhiteSpace(textBlock.Text))
            .Select(textBlock => textBlock.Text!)
            .ToArray();

    private static void PumpLayout(Window window)
    {
        window.UpdateLayout();
        window.UpdateLayout();
    }
}

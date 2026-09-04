using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using FusionCanvas.App.Groups;
using FusionCanvas.Application.Groups;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Tests.Groups;

public sealed class GroupSelectionHeadlessTests
{
    [AvaloniaFact]
    public void Window_BindsNameAndDestinationsAndSelectsDefaultDestination()
    {
        var first = CreateDestination("Niche");
        var second = CreateDestination("Niche / Existing group");
        var window = new GroupSelectionWindow([first, second], second);

        try
        {
            window.Show();
            PumpLayout(window);

            var viewModel = Assert.IsType<GroupSelectionViewModel>(window.DataContext);
            var nameBox = window.FindControl<TextBox>("NameBox");
            var destinationBox = window.FindControl<ComboBox>("DestinationBox");
            Assert.NotNull(nameBox);
            Assert.NotNull(destinationBox);

            Assert.Equal(2, viewModel.Destinations.Count);
            Assert.Same(second, viewModel.SelectedDestination);
            Assert.Equal(2, destinationBox.ItemCount);
            Assert.Same(second, destinationBox.SelectedItem);

            nameBox.Text = "Seasonal collection";
            destinationBox.SelectedItem = first;
            PumpLayout(window);

            Assert.Equal("Seasonal collection", viewModel.Name);
            Assert.Same(first, viewModel.SelectedDestination);
            Assert.True(viewModel.CanConfirm);
        }
        finally
        {
            if (window.IsVisible)
            {
                window.Close();
            }
        }
    }

    [AvaloniaFact]
    public void ConfirmWithMissingName_ShowsValidationAndKeepsWindowOpen()
    {
        var window = new GroupSelectionWindow([CreateDestination("Niche")], defaultDestination: null);

        try
        {
            window.Show();
            PumpLayout(window);

            var groupButton = FindButton(window, "Group");
            Assert.NotNull(groupButton);
            groupButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            PumpLayout(window);

            var viewModel = Assert.IsType<GroupSelectionViewModel>(window.DataContext);
            Assert.Equal("Enter a name and choose a destination.", viewModel.ErrorMessage);
            Assert.True(viewModel.HasError);
            Assert.True(window.IsVisible);
            Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(),
                textBlock => textBlock.IsVisible && textBlock.Text == viewModel.ErrorMessage);
        }
        finally
        {
            if (window.IsVisible)
            {
                window.Close();
            }
        }
    }

    [AvaloniaFact]
    public void ConfirmWithNameAndDestination_ClosesWindow()
    {
        var window = new GroupSelectionWindow([CreateDestination("Niche")], defaultDestination: null);

        try
        {
            window.Show();
            PumpLayout(window);

            var viewModel = Assert.IsType<GroupSelectionViewModel>(window.DataContext);
            viewModel.Name = "Seasonal collection";
            Assert.True(viewModel.CanConfirm);

            var groupButton = FindButton(window, "Group");
            Assert.NotNull(groupButton);
            groupButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.False(window.IsVisible);
        }
        finally
        {
            if (window.IsVisible)
            {
                window.Close();
            }
        }
    }

    private static GroupDestination CreateDestination(string displayPath)
    {
        var nicheId = Guid.NewGuid();
        return new GroupDestination(
            new GroupParentReference(WorkspaceEntityKind.Niche, nicheId),
            Guid.NewGuid(),
            nicheId,
            displayPath);
    }

    private static Button? FindButton(Control root, string content) =>
        root.GetVisualDescendants()
            .OfType<Button>()
            .FirstOrDefault(button => Equals(button.Content, content));

    private static void PumpLayout(Window window)
    {
        window.UpdateLayout();
        window.UpdateLayout();
    }
}

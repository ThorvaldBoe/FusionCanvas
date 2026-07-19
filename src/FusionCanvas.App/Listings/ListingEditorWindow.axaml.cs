using Avalonia.Controls;
using Avalonia.Input;

namespace FusionCanvas.App.Listings;

public partial class ListingEditorWindow : Window
{
    public ListingEditorWindow() => InitializeComponent();

    private void OnTagInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not ListingManagementViewModel viewModel) return;
        switch (e.Key)
        {
            case Key.Enter:
                viewModel.ApplyOrCreateTagCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Escape:
                viewModel.ClearTagInputCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Up:
                viewModel.SelectPreviousSuggestionCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Down:
                viewModel.SelectNextSuggestionCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Back when string.IsNullOrEmpty(viewModel.TagInput) && viewModel.AppliedTags.Count > 0:
                var last = viewModel.AppliedTags[^1];
                viewModel.RemoveTagCommand.Execute(last);
                e.Handled = true;
                break;
        }
    }
}

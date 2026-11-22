using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FusionCanvas.Application.ArtworkStyles;

namespace FusionCanvas.App.ArtworkStyles;

public partial class ArtworkStyleManagementWindow : Window
{
    private ArtworkStyleManagementViewModel _viewModel = null!;
    private readonly bool _closeAfterSave;

    public ArtworkStyleManagementWindow() => InitializeComponent();

    public ArtworkStyleManagementWindow(IArtworkStyleManagementService service, Guid workspaceId, bool closeAfterSave = false)
        : this()
    {
        _closeAfterSave = closeAfterSave;
        _viewModel = new ArtworkStyleManagementViewModel(service, workspaceId)
        {
            PickImagesAsync = PickImagesAsync,
            ConfirmDeleteAsync = ConfirmDeleteAsync,
            ConfirmDiscardDraftAsync = ConfirmDiscardDraftAsync
        };
        _viewModel.Saved += (_, _) =>
        {
            if (_closeAfterSave) Close(_viewModel.SavedStyleId);
        };
        DataContext = _viewModel;
        Opened += async (_, _) => await _viewModel.LoadAsync();
        var allowClose = false;
        Closing += (_, args) =>
        {
            if (allowClose || !_viewModel.HasUnsavedDraft) return;
            args.Cancel = true;
            _ = ConfirmCloseAsync();
        };
        async Task ConfirmCloseAsync()
        {
            if (!await ConfirmDiscardDraftAsync()) return;
            allowClose = true;
            Close(_viewModel.SavedStyleId);
        }
    }

    private async Task<IReadOnlyList<string>> PickImagesAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose style reference images",
            AllowMultiple = true,
            FileTypeFilter = [new FilePickerFileType("Images") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp"] }]
        });
        return files.Select(file => file.Path.LocalPath).ToArray();
    }

    private async Task<bool> ConfirmDeleteAsync(FusionCanvas.Domain.ArtworkStyles.ArtworkStyle style, int affectedItemCount)
    {
        var message = affectedItemCount == 0
            ? $"Delete {style.Title}? No Items currently use this style. This cannot be undone."
            : $"Delete {style.Title}? {affectedItemCount} Item(s) will switch to General. This cannot be undone.";
        var prompt = new ArtworkStyleDeleteConfirmationWindow(message);
        return await prompt.ShowDialog<bool>(this);
    }

    private async Task<bool> ConfirmDiscardDraftAsync()
    {
        var prompt = new ArtworkStyleDeleteConfirmationWindow("Discard unsaved changes? Your edits will be lost.", "Discard changes");
        return await prompt.ShowDialog<bool>(this);
    }

    private async void OnAddImages(object? sender, RoutedEventArgs e) => await _viewModel.AddImagesAsync();
    private void OnRemoveImage(object? sender, RoutedEventArgs e) => _viewModel.RemoveImage(_viewModel.SelectedReference);
    private void OnMoveImageUp(object? sender, RoutedEventArgs e) => _viewModel.MoveImage(_viewModel.SelectedReference, -1);
    private void OnMoveImageDown(object? sender, RoutedEventArgs e) => _viewModel.MoveImage(_viewModel.SelectedReference, 1);
    private void OnClose(object? sender, RoutedEventArgs e) => Close(_viewModel.SavedStyleId);
}

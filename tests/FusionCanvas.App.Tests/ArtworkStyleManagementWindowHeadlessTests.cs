using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using FusionCanvas.App.ArtworkStyles;
using FusionCanvas.App.Tests.TestSupport;
using FusionCanvas.Application.ArtworkStyles;
using FusionCanvas.Domain.ArtworkStyles;

namespace FusionCanvas.App.Tests;

public sealed class ArtworkStyleManagementWindowHeadlessTests
{
    [AvaloniaFact]
    public async Task ManagerRendersWorkspaceStylesAndKeepsDraftWhenSelectionOrDeletionIsCancelled()
    {
        var workspaceId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var first = new ArtworkStyle(Guid.NewGuid(), workspaceId, "Linocut", "Bold carved shapes", now, now);
        var second = new ArtworkStyle(Guid.NewGuid(), workspaceId, "Risograph", "Grain and limited colors", now, now);
        var service = new StaticArtworkStyleManagementService(new ArtworkStyleManagementState(
            workspaceId,
            [first, second],
            new Dictionary<Guid, IReadOnlyList<ArtworkStyleReferenceState>>()));
        var window = new ArtworkStyleManagementWindow(service, workspaceId);

        try
        {
            window.Show();
            var viewModel = Assert.IsType<ArtworkStyleManagementViewModel>(window.DataContext);
            await HeadlessUiWait.UntilAsync(() => viewModel.Styles.Count == 2, "workspace styles loaded");

            var list = window.GetVisualDescendants().OfType<ListBox>()
                .Single(control => AutomationProperties.GetName(control) == "Artwork styles");
            Assert.Equal(2, list.Items.Count);
            Assert.DoesNotContain(list.Items.Cast<object>(), value => value.ToString() == "General");
            list.SelectedItem = first;
            await HeadlessUiWait.UntilAsync(() => viewModel.SelectedStyle?.Id == first.Id, "first style selected");

            var title = window.GetVisualDescendants().OfType<TextBox>()
                .Single(control => AutomationProperties.GetName(control) == "Style title");
            Assert.Equal(first.Title, title.Text);
            title.Text = "Edited but unsaved";
            await HeadlessUiWait.UntilAsync(() => viewModel.HasUnsavedDraft, "style draft becomes dirty");
            var discardPrompted = false;
            viewModel.ConfirmDiscardDraftAsync = () =>
            {
                discardPrompted = true;
                return Task.FromResult(false);
            };
            list.SelectedItem = second;
            await HeadlessUiWait.UntilAsync(() => discardPrompted, "selection change asks before discarding");
            Assert.Equal(first.Id, viewModel.SelectedStyle?.Id);
            Assert.Equal("Edited but unsaved", title.Text);

            var deletePrompted = false;
            viewModel.ConfirmDeleteAsync = (_, _) =>
            {
                deletePrompted = true;
                return Task.FromResult(false);
            };
            var delete = window.GetVisualDescendants().OfType<Button>()
                .Single(control => (control.Content as string) == "Delete style");
            delete.Command!.Execute(null);
            await HeadlessUiWait.UntilAsync(() => deletePrompted, "delete confirmation is requested");
            Assert.Equal(0, service.DeleteCalls);
            Assert.Equal(2, viewModel.Styles.Count);
            Assert.Equal(first.Id, viewModel.SelectedStyle?.Id);
        }
        finally
        {
            window.Close();
        }
    }

    private sealed class StaticArtworkStyleManagementService(ArtworkStyleManagementState state) : IArtworkStyleManagementService
    {
        public int DeleteCalls { get; private set; }
        public Task<ArtworkStyleManagementState> LoadAsync(Guid workspaceId, CancellationToken cancellationToken = default) => Task.FromResult(state);
        public Task<ArtworkStyleOperationResult> SaveAsync(Guid workspaceId, Guid? styleId, string title, string description, IReadOnlyList<ArtworkStyleImageSelection> images, CancellationToken cancellationToken = default) =>
            Task.FromResult(ArtworkStyleOperationResult.Failure("Save is not used in this view test.", state));
        public Task<ArtworkStyleOperationResult> DeleteAsync(Guid workspaceId, Guid styleId, CancellationToken cancellationToken = default)
        {
            DeleteCalls++;
            return Task.FromResult(ArtworkStyleOperationResult.Failure("Delete should not run after cancellation.", state));
        }
    }
}
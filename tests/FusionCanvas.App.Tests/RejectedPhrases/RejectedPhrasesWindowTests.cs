using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using FusionCanvas.App.RejectedPhrases;
using FusionCanvas.App.Snowclones;
using FusionCanvas.Application.RejectedPhrases;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Ideation;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.App.Settings;
using FusionCanvas.App.Views;

namespace FusionCanvas.App.Tests.RejectedPhrases;

public sealed class RejectedPhrasesWindowTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 28, 9, 0, 0, TimeSpan.Zero);

    [AvaloniaFact]
    public async Task Window_ConstructsWithRequiredControlsAndPreselectedRecord()
    {
        var sample = Sample.Create();
        var first = Rejection(sample, "Alpha phrase", "Reason");
        var second = Rejection(sample, "Beta phrase", null);
        var snapshot = sample.Snapshot with { IdeationRejections = [first, second] };
        var viewModel = ViewModel(snapshot, sample);
        await viewModel.OpenAsync(
            RejectedPhraseScope.WholeWorkspaceView,
            ScopeOptions(sample),
            TestContext.Current.CancellationToken);
        var window = new RejectedPhrasesWindow { DataContext = viewModel };
        try
        {
            window.Show();
            PumpLayout(window);

            Assert.NotNull(window.FindControl<TextBox>("SearchBox"));
            Assert.NotNull(window.FindControl<ItemsControl>("RejectionList"));
            Assert.NotNull(window.FindControl<TextBox>("PhraseBox"));
            Assert.NotNull(window.FindControl<TextBox>("ReasonBox"));
            Assert.NotNull(window.FindControl<ComboBox>("ScopeSelector"));
            Assert.Equal(first.Id, viewModel.SelectedRejection!.Id);
            Assert.NotNull(FindButton(window, "New rejected phrase"));
            Assert.NotNull(FindButton(window, "Close"));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task NewCommand_FocusesPhraseAndDisablesDelete()
    {
        var sample = Sample.Create();
        var existing = Rejection(sample, "Existing", null);
        var viewModel = ViewModel(sample.Snapshot with { IdeationRejections = [existing] }, sample);
        await viewModel.OpenAsync(
            RejectedPhraseScope.WholeWorkspaceView,
            ScopeOptions(sample),
            TestContext.Current.CancellationToken);
        var window = new RejectedPhrasesWindow { DataContext = viewModel };
        try
        {
            window.Show();
            PumpLayout(window);

            FindButton(window, "New rejected phrase")!.Command!.Execute(null);
            await viewModel.WhenIdleAsync();
            PumpLayout(window);
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();

            Assert.True(viewModel.IsNewDraft);
            Assert.False(viewModel.CanDelete);
            Assert.True(window.FindControl<TextBox>("PhraseBox")!.IsFocused);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task SearchAndUnsavedConfirmation_RenderCompleteInteractionStates()
    {
        var sample = Sample.Create();
        var first = Rejection(sample, "First", "Coffee");
        var second = Rejection(sample, "Second", "Dogs");
        var viewModel = ViewModel(sample.Snapshot with { IdeationRejections = [first, second] }, sample);
        await viewModel.OpenAsync(
            RejectedPhraseScope.WholeWorkspaceView,
            ScopeOptions(sample),
            TestContext.Current.CancellationToken);
        var window = new RejectedPhrasesWindow { DataContext = viewModel };
        try
        {
            window.Show();
            PumpLayout(window);

            viewModel.SearchText = "no match";
            await viewModel.WhenIdleAsync();
            PumpLayout(window);
            Assert.True(viewModel.HasNoResults);
            Assert.NotNull(FindVisibleText(window, "No matching rejected phrases"));
            Assert.NotNull(FindButton(window, "Clear search"));

            viewModel.SearchText = string.Empty;
            await viewModel.WhenIdleAsync();
            viewModel.Reason = "Unsaved";
            viewModel.SelectRejectionCommand.Execute(
                viewModel.Rejections.Single(item => item.Id == second.Id));
            await viewModel.WhenIdleAsync();
            PumpLayout(window);

            Assert.True(viewModel.UnsavedPromptVisible);
            Assert.NotNull(FindVisibleText(window, "Save changes to this rejected phrase before continuing?"));
            Assert.True(FindButton(window, "Save", visibleOnly: true)!.Focusable);
            Assert.True(FindButton(window, "Discard")!.Focusable);
            Assert.True(FindButton(window, "Cancel", visibleOnly: true)!.Focusable);
        }
        finally
        {
            viewModel.CancelPendingCommand.Execute(null);
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task DeleteConfirmation_IsVisibleAndCancellable()
    {
        var sample = Sample.Create();
        var existing = Rejection(sample, "Existing", null);
        var viewModel = ViewModel(sample.Snapshot with { IdeationRejections = [existing] }, sample);
        await viewModel.OpenAsync(
            RejectedPhraseScope.WholeWorkspaceView,
            ScopeOptions(sample),
            TestContext.Current.CancellationToken);
        var window = new RejectedPhrasesWindow { DataContext = viewModel };
        try
        {
            window.Show();
            PumpLayout(window);

            viewModel.RequestDeleteCommand.Execute(null);
            PumpLayout(window);
            Assert.True(viewModel.DeleteConfirmationVisible);
            Assert.NotNull(FindButton(window, "Delete permanently"));

            viewModel.CancelDeleteCommand.Execute(null);
            PumpLayout(window);
            Assert.False(viewModel.DeleteConfirmationVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task ScopeFilter_NarrowsToGroupScope()
    {
        var sample = Sample.Create();
        var inGroup = new IdeationRejection(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, sample.Group.Id, "Group phrase", null, IdeationMode.Basic, Now);
        var nicheRoot = Rejection(sample, "Niche root", null);
        var viewModel = ViewModel(sample.Snapshot with { IdeationRejections = [inGroup, nicheRoot] }, sample);
        await viewModel.OpenAsync(
            RejectedPhraseScope.WholeWorkspaceView,
            ScopeOptions(sample),
            TestContext.Current.CancellationToken);
        var window = new RejectedPhrasesWindow { DataContext = viewModel };
        try
        {
            window.Show();
            PumpLayout(window);
            Assert.Equal(2, viewModel.Rejections.Count);

            viewModel.SelectScopeCommand.Execute(
                new ScopeOption(sample.Group.Name, RejectedPhraseScope.ForGroup(sample.Store.Id, sample.Niche.Id, sample.Group.Id)));
            await viewModel.WhenIdleAsync();
            PumpLayout(window);

            var visible = Assert.Single(viewModel.Rejections);
            Assert.Equal("Group phrase", visible.Text);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task RecoverableError_IsVisibleWhenSaveFails()
    {
        var sample = Sample.Create();
        var repository = new InMemoryRepository(sample.Snapshot);
        var service = new RejectedPhraseManagementService(repository, Guid.NewGuid, () => Now);
        var viewModel = new RejectedPhrasesViewModel(service);
        await viewModel.OpenAsync(
            RejectedPhraseScope.ForNiche(sample.Store.Id, sample.Niche.Id),
            ScopeOptions(sample),
            TestContext.Current.CancellationToken);
        var window = new RejectedPhrasesWindow { DataContext = viewModel };
        try
        {
            window.Show();
            PumpLayout(window);

            repository.FailSave = true;
            viewModel.NewCommand.Execute(null);
            await viewModel.WhenIdleAsync();
            viewModel.Phrase = "New phrase";
            viewModel.SaveCommand.Execute(null);
            await viewModel.WhenIdleAsync();
            PumpLayout(window);

            Assert.True(viewModel.HasError);
            Assert.NotNull(FindVisibleText(window, viewModel.ErrorMessage!));
            Assert.Equal("New phrase", viewModel.Phrase);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void CurrentMainWindowAndSettingsExposeNoRejectedPhrasesLauncher()
    {
        using var fixture = new MainWindowFixture();
        Assert.DoesNotContain(
            fixture.Window.GetVisualDescendants().OfType<Button>(),
            button => (button.Content as string)?.Contains("Rejected phrase", StringComparison.OrdinalIgnoreCase) == true);

        var settings = fixture.ViewModel.Settings;
        settings.OpenCommand.Execute(null);
        var settingsWindow = new SettingsWindow { DataContext = settings };
        try
        {
            settingsWindow.Show();
            PumpLayout(settingsWindow);
            Assert.DoesNotContain(
                settingsWindow.GetVisualDescendants().OfType<Button>(),
                button => (button.Content as string)?.Contains("Rejected phrase", StringComparison.OrdinalIgnoreCase) == true);
        }
        finally
        {
            settingsWindow.Close();
        }
    }

    private static RejectedPhrasesViewModel ViewModel(WorkspaceSnapshot snapshot, Sample sample)
    {
        var repository = new InMemoryRepository(snapshot);
        var service = new RejectedPhraseManagementService(repository, Guid.NewGuid, () => Now);
        return new RejectedPhrasesViewModel(service);
    }

    private static IdeationRejection Rejection(Sample sample, string text, string? reason) =>
        new(Guid.NewGuid(), sample.Store.Id, sample.Niche.Id, null, text, reason, IdeationMode.Basic, Now);

    private static IReadOnlyList<ScopeOption> ScopeOptions(Sample sample) =>
    [
        new ScopeOption("Whole workspace", RejectedPhraseScope.WholeWorkspaceView),
        new ScopeOption(sample.Niche.Name, RejectedPhraseScope.ForNiche(sample.Store.Id, sample.Niche.Id)),
        new ScopeOption(sample.Group.Name, RejectedPhraseScope.ForGroup(sample.Store.Id, sample.Niche.Id, sample.Group.Id))
    ];

    private static Button? FindButton(Control root, string content, bool visibleOnly = false) =>
        root.GetVisualDescendants()
            .OfType<Button>()
            .FirstOrDefault(button =>
                (!visibleOnly || button.IsVisible) && Equals(button.Content, content));

    private static TextBlock? FindVisibleText(Control root, string text) =>
        root.GetVisualDescendants()
            .OfType<TextBlock>()
            .FirstOrDefault(block => block.IsVisible && block.Text == text);

    private static void PumpLayout(Window window)
    {
        window.UpdateLayout();
        window.UpdateLayout();
    }

    private sealed class InMemoryRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        public WorkspaceSnapshot Snapshot { get; set; } = snapshot;

        public bool FailSave { get; set; }

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Snapshot);

        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            if (FailSave)
            {
                throw new InvalidOperationException("Simulated save failure.");
            }

            Snapshot = snapshot;
            return Task.CompletedTask;
        }
    }

    private sealed record Sample(WorkspaceSnapshot Snapshot, Store Store, Niche Niche, TopicGroup Group)
    {
        public static Sample Create()
        {
            var store = new Store(Guid.NewGuid(), "Dog Shop", null, false, Now, Now, "{}");
            var niche = new Niche(Guid.NewGuid(), store.Id, "Dogs", null, false, Now, Now, "{}");
            var group = new TopicGroup(Guid.NewGuid(), store.Id, niche.Id, null, "Pugs", null, false, Now, Now, "{}");
            return new Sample(
                new WorkspaceSnapshot([store], [niche], [group], [], [], [], [], [], []),
                store,
                niche,
                group);
        }
    }
}

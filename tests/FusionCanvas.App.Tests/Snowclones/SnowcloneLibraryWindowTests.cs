using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using FusionCanvas.App.Settings;
using FusionCanvas.App.Snowclones;
using FusionCanvas.Application.Snowclones;
using FusionCanvas.Domain.Snowclones;

namespace FusionCanvas.App.Tests.Snowclones;

public sealed class SnowcloneLibraryWindowTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-01-02T03:04:05Z");

    [AvaloniaFact]
    public async Task Window_ConstructsWithRequiredControlsAndPreselectedRecord()
    {
        var snowclone = Snowclone("Easily distracted by {X}", "Replace X.");
        var viewModel = ViewModel([snowclone]);
        await viewModel.OpenAsync(TestContext.Current.CancellationToken);
        var window = new SnowcloneLibraryWindow { DataContext = viewModel };
        try
        {
            window.Show();
            PumpLayout(window);

            Assert.NotNull(window.FindControl<TextBox>("SearchBox"));
            Assert.NotNull(window.FindControl<ItemsControl>("SnowcloneList"));
            Assert.NotNull(window.FindControl<TextBox>("PhraseBox"));
            Assert.NotNull(window.FindControl<TextBox>("GuidanceBox"));
            Assert.Equal(snowclone.Id, viewModel.SelectedSnowclone!.Id);
            Assert.NotNull(FindButton(window, "Import CSV\u2026"));
            Assert.NotNull(FindButton(window, "Export CSV\u2026"));
            Assert.NotNull(FindButton(window, "Import bundled library"));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task NewCommand_FocusesPhraseAndDisablesDelete()
    {
        var viewModel = ViewModel([Snowclone("Existing {X}", "Guidance")]);
        await viewModel.OpenAsync(TestContext.Current.CancellationToken);
        var window = new SnowcloneLibraryWindow { DataContext = viewModel };
        try
        {
            window.Show();
            PumpLayout(window);

            FindButton(window, "New snowclone")!.Command!.Execute(null);
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
        var first = Snowclone("First {X}", "Coffee");
        var second = Snowclone("Second {Y}", "Dogs");
        var viewModel = ViewModel([first, second]);
        await viewModel.OpenAsync(TestContext.Current.CancellationToken);
        var window = new SnowcloneLibraryWindow { DataContext = viewModel };
        try
        {
            window.Show();
            PumpLayout(window);

            viewModel.SearchText = "no match";
            await viewModel.WhenIdleAsync();
            PumpLayout(window);
            Assert.True(viewModel.HasNoResults);
            Assert.NotNull(FindVisibleText(window, "No matching snowclones"));
            Assert.NotNull(FindButton(window, "Clear search"));

            viewModel.SearchText = string.Empty;
            await viewModel.WhenIdleAsync();
            viewModel.Guidance = "Unsaved";
            viewModel.SelectSnowcloneCommand.Execute(
                viewModel.Snowclones.Single(item => item.Id == second.Id));
            PumpLayout(window);

            Assert.True(viewModel.UnsavedPromptVisible);
            Assert.NotNull(FindVisibleText(window, "Save changes to this snowclone before continuing?"));
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
    public async Task DeleteConfirmationAndRecoverableError_AreVisible()
    {
        var repository = new InMemoryRepository(
            new SnowcloneLibrarySnapshot([Snowclone("Existing {X}", "Guidance")], true));
        var viewModel = ViewModel(repository);
        await viewModel.OpenAsync(TestContext.Current.CancellationToken);
        var window = new SnowcloneLibraryWindow { DataContext = viewModel };
        try
        {
            window.Show();
            PumpLayout(window);

            viewModel.RequestDeleteCommand.Execute(null);
            PumpLayout(window);
            Assert.True(viewModel.DeleteConfirmationVisible);
            Assert.NotNull(FindButton(window, "Delete permanently"));

            viewModel.CancelDeleteCommand.Execute(null);
            repository.SaveException = new IOException("disk full");
            viewModel.Guidance = "Retry";
            viewModel.SaveCommand.Execute(null);
            await viewModel.WhenIdleAsync();
            PumpLayout(window);

            Assert.True(viewModel.HasError);
            Assert.NotNull(FindVisibleText(window, viewModel.ErrorMessage!));
            Assert.Equal("Retry", viewModel.Guidance);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void CurrentMainWindowAndSettingsExposeNoSnowcloneLauncher()
    {
        using var fixture = new MainWindowFixture();
        Assert.DoesNotContain(
            fixture.Window.GetVisualDescendants().OfType<Button>(),
            button => (button.Content as string)?.Contains("Snowclone", StringComparison.OrdinalIgnoreCase) == true);

        var settings = fixture.ViewModel.Settings;
        settings.OpenCommand.Execute(null);
        var settingsWindow = new SettingsWindow { DataContext = settings };
        try
        {
            settingsWindow.Show();
            PumpLayout(settingsWindow);
            Assert.DoesNotContain(
                settingsWindow.GetVisualDescendants().OfType<Button>(),
                button => (button.Content as string)?.Contains("Snowclone", StringComparison.OrdinalIgnoreCase) == true);
        }
        finally
        {
            settingsWindow.Close();
        }
    }

    private static SnowcloneLibraryViewModel ViewModel(IReadOnlyList<Snowclone> snowclones) =>
        ViewModel(new InMemoryRepository(new SnowcloneLibrarySnapshot(snowclones, true)));

    private static SnowcloneLibraryViewModel ViewModel(InMemoryRepository repository)
    {
        var service = new SnowcloneLibraryService(
            repository,
            new StubCodec(),
            new StubBundledSource(),
            () => Now.AddMinutes(1),
            Guid.NewGuid);
        return new SnowcloneLibraryViewModel(service);
    }

    private static Snowclone Snowclone(string phrase, string guidance) =>
        new(Guid.NewGuid(), phrase, guidance, Now, Now);

    private static Button? FindButton(
        Control root,
        string content,
        bool visibleOnly = false) =>
        root.GetVisualDescendants()
            .OfType<Button>()
            .FirstOrDefault(button =>
                (!visibleOnly || button.IsVisible) &&
                Equals(button.Content, content));

    private static TextBlock? FindVisibleText(Control root, string text) =>
        root.GetVisualDescendants()
            .OfType<TextBlock>()
            .FirstOrDefault(block => block.IsVisible && block.Text == text);

    private static void PumpLayout(Window window)
    {
        window.UpdateLayout();
        window.UpdateLayout();
    }

    private sealed class InMemoryRepository(SnowcloneLibrarySnapshot snapshot) : ISnowcloneRepository
    {
        public SnowcloneLibrarySnapshot Snapshot { get; private set; } = snapshot;

        public Exception? SaveException { get; set; }

        public Task<SnowcloneLibrarySnapshot> LoadAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Snapshot);
        }

        public Task SaveAsync(SnowcloneLibrarySnapshot snapshot, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (SaveException is not null)
            {
                throw SaveException;
            }

            Snapshot = snapshot;
            return Task.CompletedTask;
        }
    }

    private sealed class StubCodec : ISnowcloneCsvCodec
    {
        public Task<SnowcloneCsvReadResult> ReadAsync(
            Stream stream,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(SnowcloneCsvReadResult.Success([]));
        }

        public Task WriteAsync(
            Stream stream,
            IReadOnlyList<SnowcloneCsvRow> rows,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed class StubBundledSource : IBundledSnowcloneSource
    {
        public Task<Stream> OpenReadAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Stream>(new MemoryStream());
        }
    }
}

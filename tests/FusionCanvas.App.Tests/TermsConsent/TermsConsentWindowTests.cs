using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using FusionCanvas.App.Commands;
using FusionCanvas.App.TermsConsent;
using FusionCanvas.Application.Settings;
using FusionCanvas.Application.TermsConsent;

namespace FusionCanvas.App.Tests.TermsConsent;

public sealed class TermsConsentWindowTests
{
    [AvaloniaFact]
    public void Window_ShowsOfflinePolicyFourCheckboxesAndGatedActions()
    {
        var viewModel = new TermsConsentViewModel(
            ApplicationSettings.Default,
            new TermsConsentService(new RecordingStore()),
            "Bundled offline policy");
        var window = new TermsConsentWindow { DataContext = viewModel };

        try
        {
            window.Show();
            window.UpdateLayout();

            var checkBoxes = window.GetVisualDescendants().OfType<CheckBox>().ToArray();
            Assert.Equal(4, checkBoxes.Length);
            Assert.Contains(window.GetVisualDescendants().OfType<TextBox>(), box => box.Text == "Bundled offline policy");
            var agree = window.GetVisualDescendants().OfType<Button>().Single(button =>
                AutomationProperties.GetName(button) == "Agree and continue");
            Assert.False(agree.IsEffectivelyEnabled);

            foreach (var checkBox in checkBoxes)
            {
                ClickRenderedControl(window, checkBox);
            }
            window.UpdateLayout();

            Assert.True(agree.IsEffectivelyEnabled);
            Assert.All(checkBoxes, checkBox => Assert.True(checkBox.IsChecked));
            Assert.Contains(window.GetVisualDescendants().OfType<Button>(), button =>
                AutomationProperties.GetName(button) == "Quit FusionCanvas");
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Window_QuitButtonIsKeyboardReachableAndClosesThroughTheViewModelDecision()
    {
        var viewModel = new TermsConsentViewModel(
            ApplicationSettings.Default,
            new TermsConsentService(new RecordingStore()),
            "Bundled offline policy");
        var window = new TermsConsentWindow { DataContext = viewModel };
        var quitRequested = false;
        viewModel.QuitRequested += () => quitRequested = true;

        try
        {
            window.Show();
            window.UpdateLayout();

            var quit = window.GetVisualDescendants().OfType<Button>().Single(button =>
                AutomationProperties.GetName(button) == "Quit FusionCanvas");
            quit.Focus();
            Assert.True(quit.IsFocused);

            HeadlessWindowExtensions.KeyPress(
                window,
                Key.Enter,
                RawInputModifiers.None,
                PhysicalKey.Enter,
                string.Empty);

            Assert.True(quitRequested);
        }
        finally
        {
            window.AllowClose();
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task Window_RenderedAgreeActionShowsSavingProgressUntilPersistenceCompletes()
    {
        var saveStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var saveCompletion = new TaskCompletionSource<ApplicationSettingsSaveResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var viewModel = new TermsConsentViewModel(
            ApplicationSettings.Default,
            new TermsConsentService(new BlockingStore(saveStarted, saveCompletion)),
            "Bundled offline policy");
        var window = new TermsConsentWindow { DataContext = viewModel };

        try
        {
            window.Show();
            window.UpdateLayout();
            SelectAllRenderedAcknowledgements(window);
            var agree = window.GetVisualDescendants().OfType<Button>().Single(button =>
                AutomationProperties.GetName(button) == "Agree and continue");
            agree.Focus();
            HeadlessWindowExtensions.KeyPress(window, Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, string.Empty);

            await saveStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
            window.UpdateLayout();

            var progress = window.GetVisualDescendants().OfType<ProgressBar>().Single(bar =>
                AutomationProperties.GetName(bar) == "Saving acknowledgement");
            Assert.True(progress.IsVisible);
            Assert.False(agree.IsEffectivelyEnabled);

            saveCompletion.TrySetResult(ApplicationSettingsSaveResult.Success);
            await Assert.IsType<AsyncRelayCommand>(viewModel.AgreeCommand).ExecutionTask!
                .WaitAsync(TestContext.Current.CancellationToken);
        }
        finally
        {
            saveCompletion.TrySetResult(ApplicationSettingsSaveResult.Success);
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task Window_RenderedAgreeActionKeepsFormAndShowsPersistenceError()
    {
        var viewModel = new TermsConsentViewModel(
            ApplicationSettings.Default,
            new TermsConsentService(new FailingStore()),
            "Bundled offline policy");
        var window = new TermsConsentWindow { DataContext = viewModel };

        try
        {
            window.Show();
            window.UpdateLayout();
            SelectAllRenderedAcknowledgements(window);
            var agree = window.GetVisualDescendants().OfType<Button>().Single(button =>
                AutomationProperties.GetName(button) == "Agree and continue");
            agree.Focus();
            HeadlessWindowExtensions.KeyPress(window, Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, string.Empty);
            await Assert.IsType<AsyncRelayCommand>(viewModel.AgreeCommand).ExecutionTask!
                .WaitAsync(TestContext.Current.CancellationToken);
            window.UpdateLayout();

            var error = window.GetVisualDescendants().OfType<TextBlock>().Single(text =>
                AutomationProperties.GetName(text) == "Consent error");
            Assert.True(error.IsVisible);
            Assert.Contains("disk is full", error.Text, StringComparison.OrdinalIgnoreCase);
            Assert.All(window.GetVisualDescendants().OfType<CheckBox>(), checkBox => Assert.True(checkBox.IsChecked));
        }
        finally
        {
            window.Close();
        }
    }

    private static void SelectAllRenderedAcknowledgements(TermsConsentWindow window)
    {
        foreach (var checkBox in window.GetVisualDescendants().OfType<CheckBox>())
        {
            ClickRenderedControl(window, checkBox);
        }

        window.UpdateLayout();
    }

    private static void ClickRenderedControl(Window window, Control control)
    {
        var point = control.TranslatePoint(
            new Point(control.Bounds.Width / 2, control.Bounds.Height / 2),
            window);
        if (point is not { } clickPoint)
        {
            throw new InvalidOperationException("The rendered consent control must be positioned in the window.");
        }
        HeadlessWindowExtensions.MouseDown(window, clickPoint, MouseButton.Left, RawInputModifiers.None);
        HeadlessWindowExtensions.MouseUp(window, clickPoint, MouseButton.Left, RawInputModifiers.None);
    }

    private sealed class RecordingStore : IApplicationSettingsStore
    {
        public Task<ApplicationSettingsLoadResult> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplicationSettingsLoadResult.Success(ApplicationSettings.Default));

        public Task<ApplicationSettingsSaveResult> SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplicationSettingsSaveResult.Success);
    }

    private sealed class FailingStore : IApplicationSettingsStore
    {
        public Task<ApplicationSettingsLoadResult> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplicationSettingsLoadResult.Success(ApplicationSettings.Default));

        public Task<ApplicationSettingsSaveResult> SaveAsync(
            ApplicationSettings settings,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplicationSettingsSaveResult.Failed("The disk is full."));
    }

    private sealed class BlockingStore(
        TaskCompletionSource saveStarted,
        TaskCompletionSource<ApplicationSettingsSaveResult> saveCompletion) : IApplicationSettingsStore
    {
        public Task<ApplicationSettingsLoadResult> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplicationSettingsLoadResult.Success(ApplicationSettings.Default));

        public async Task<ApplicationSettingsSaveResult> SaveAsync(
            ApplicationSettings settings,
            CancellationToken cancellationToken = default)
        {
            saveStarted.TrySetResult();
            return await saveCompletion.Task.WaitAsync(cancellationToken);
        }
    }
}

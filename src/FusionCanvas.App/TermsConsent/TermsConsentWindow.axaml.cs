using Avalonia.Controls;
using Avalonia.Input;
using FusionCanvas.Application.Settings;

namespace FusionCanvas.App.TermsConsent;

public partial class TermsConsentWindow : Window
{
    private bool _allowClose;

    public TermsConsentWindow()
    {
        InitializeComponent();
        Opened += (_, _) => FocusFirstAcknowledgement();
        DataContextChanged += (_, _) => WireViewModel();
        Closing += (_, args) =>
        {
            if (!_allowClose && DataContext is TermsConsentViewModel viewModel)
            {
                args.Cancel = true;
                viewModel.RequestQuit();
            }
        };
    }

    public void AllowClose() => _allowClose = true;

    private void WireViewModel()
    {
        if (DataContext is not TermsConsentViewModel viewModel)
        {
            return;
        }

        viewModel.Accepted += OnAccepted;
        viewModel.QuitRequested += OnQuitRequested;
    }

    private void OnAccepted(ApplicationSettings settings) => CloseAfterDecision();

    private void OnQuitRequested() => CloseAfterDecision();

    private void CloseAfterDecision()
    {
        _allowClose = true;
        Close();
    }

    private void FocusFirstAcknowledgement()
    {
        var first = this.FindControl<Avalonia.Controls.Primitives.ToggleButton>("FusionCanvasTermsCheckBox");
        first?.Focus();
    }
}

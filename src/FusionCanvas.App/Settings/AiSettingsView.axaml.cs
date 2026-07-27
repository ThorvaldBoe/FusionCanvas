using Avalonia.Controls;
using Avalonia.Threading;

namespace FusionCanvas.App.Settings;

public partial class AiSettingsView : UserControl
{
    private AiSettingsViewModel? _subscribed;

    public AiSettingsView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Subscribe();
        AttachedToVisualTree += (_, _) => Subscribe();
        DetachedFromVisualTree += (_, _) => Unsubscribe();
    }

    private void Subscribe()
    {
        Unsubscribe();
        if (DataContext is AiSettingsViewModel viewModel)
        {
            _subscribed = viewModel;
            _subscribed.CredentialFocusRequested += OnCredentialFocusRequested;
        }
    }

    private void Unsubscribe()
    {
        if (_subscribed is not null)
        {
            _subscribed.CredentialFocusRequested -= OnCredentialFocusRequested;
            _subscribed = null;
        }
    }

    private void OnCredentialFocusRequested(object? sender, EventArgs args) =>
        Dispatcher.UIThread.Post(
            () =>
            {
                if (_subscribed?.IsEditingCredential == true)
                {
                    ApiKeyDraft.Focus();
                }
                else
                {
                    AddOrReplaceKeyButton.Focus();
                }
            },
            DispatcherPriority.Input);
}

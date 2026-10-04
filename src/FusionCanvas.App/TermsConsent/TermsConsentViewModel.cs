using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.Commands;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Application.Settings;
using FusionCanvas.Application.TermsConsent;

namespace FusionCanvas.App.TermsConsent;

public sealed class TermsConsentViewModel : INotifyPropertyChanged
{
    private readonly IApplicationSettingsStore _store;
    private readonly IExternalLinkLauncher _linkLauncher;
    private readonly CancellationToken _startupCancellationToken;
    private ApplicationSettings _settings;
    private bool _fusionCanvasTermsSelected;
    private bool _printifyTermsSelected;
    private bool _shopifyTermsSelected;
    private bool _intellectualPropertySelected;
    private bool _isSaving;
    private string? _errorMessage;
    private bool _completed;

    public TermsConsentViewModel(
        ApplicationSettings settings,
        IApplicationSettingsStore store,
        string policyText,
        IExternalLinkLauncher? linkLauncher = null,
        CancellationToken startupCancellationToken = default)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _startupCancellationToken = startupCancellationToken;
        PolicyText = string.IsNullOrWhiteSpace(policyText)
            ? throw new ArgumentException("The bundled policy text must not be empty.", nameof(policyText))
            : policyText;
        _linkLauncher = linkLauncher ?? new ProcessExternalLinkLauncher();

        AgreeCommand = new AsyncRelayCommand(AcceptAsync, () => CanAgree);
        QuitCommand = new RelayCommand(_ => RequestQuit(), () => CanQuit);
        OpenPrintifyTermsCommand = new RelayCommand(_ => Open(TermsConsentPolicy.PrintifyTermsUrl));
        OpenPrintifyIpPolicyCommand = new RelayCommand(_ => Open(TermsConsentPolicy.PrintifyIpPolicyUrl));
        OpenShopifyTermsCommand = new RelayCommand(_ => Open(TermsConsentPolicy.ShopifyTermsUrl));
        OpenShopifyAupCommand = new RelayCommand(_ => Open(TermsConsentPolicy.ShopifyAcceptableUsePolicyUrl));
        OpenShopifyApiTermsCommand = new RelayCommand(_ => Open(TermsConsentPolicy.ShopifyApiTermsUrl));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event Action<ApplicationSettings>? Accepted;

    public event Action? QuitRequested;

    public string PolicyText { get; }

    public string TermsVersion => TermsConsentPolicy.FusionCanvasTermsVersion;

    public string PolicyVersion => TermsConsentPolicy.AcknowledgementPolicyVersion;

    public string PrintifyTermsUrl => TermsConsentPolicy.PrintifyTermsUrl;

    public string PrintifyIpPolicyUrl => TermsConsentPolicy.PrintifyIpPolicyUrl;

    public string ShopifyTermsUrl => TermsConsentPolicy.ShopifyTermsUrl;

    public string ShopifyAcceptableUsePolicyUrl => TermsConsentPolicy.ShopifyAcceptableUsePolicyUrl;

    public string ShopifyApiTermsUrl => TermsConsentPolicy.ShopifyApiTermsUrl;

    public bool FusionCanvasTermsSelected
    {
        get => _fusionCanvasTermsSelected;
        set => SetSelection(ref _fusionCanvasTermsSelected, value);
    }

    public bool PrintifyTermsSelected
    {
        get => _printifyTermsSelected;
        set => SetSelection(ref _printifyTermsSelected, value);
    }

    public bool ShopifyTermsSelected
    {
        get => _shopifyTermsSelected;
        set => SetSelection(ref _shopifyTermsSelected, value);
    }

    public bool IntellectualPropertySelected
    {
        get => _intellectualPropertySelected;
        set => SetSelection(ref _intellectualPropertySelected, value);
    }

    public bool CanAgree =>
        !IsSaving && TermsConsentPolicy.AreAllAcknowledgementsSelected(
            FusionCanvasTermsSelected,
            PrintifyTermsSelected,
            ShopifyTermsSelected,
            IntellectualPropertySelected);

    public bool CanQuit => !_completed && !IsSaving;

    public bool IsSaving
    {
        get => _isSaving;
        private set
        {
            if (SetField(ref _isSaving, value))
            {
                OnPropertyChanged(nameof(CanAgree));
                OnPropertyChanged(nameof(CanQuit));
                (AgreeCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
                (QuitCommand as RelayCommand)?.NotifyCanExecuteChanged();
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetField(ref _errorMessage, value);
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsCompleted => _completed;

    public ICommand AgreeCommand { get; }

    public ICommand QuitCommand { get; }

    public ICommand OpenPrintifyTermsCommand { get; }

    public ICommand OpenPrintifyIpPolicyCommand { get; }

    public ICommand OpenShopifyTermsCommand { get; }

    public ICommand OpenShopifyAupCommand { get; }

    public ICommand OpenShopifyApiTermsCommand { get; }

    public void RequestQuit()
    {
        if (CanQuit)
        {
            QuitRequested?.Invoke();
        }
    }

    internal Task WaitForPendingSaveAsync() =>
        (AgreeCommand as AsyncRelayCommand)?.ExecutionTask ?? Task.CompletedTask;

    private async Task AcceptAsync()
    {
        ErrorMessage = null;
        if (!CanAgree)
        {
            return;
        }

        IsSaving = true;
        try
        {
            var updated = _settings with
            {
                TermsConsent = TermsConsentPolicy.CreateRecord(DateTimeOffset.UtcNow)
            };
            var result = await _store.SaveAsync(updated, _startupCancellationToken).ConfigureAwait(true);
            if (!result.Saved)
            {
                ErrorMessage = result.Warning ?? "The acknowledgement could not be saved. Please try again.";
                return;
            }

            _settings = updated;
            _completed = true;
            Accepted?.Invoke(updated);
        }
        catch (OperationCanceledException)
        {
            ErrorMessage = "Saving the acknowledgement was cancelled. Please try again.";
        }
        catch (Exception)
        {
            ErrorMessage = "The acknowledgement could not be saved. Check that FusionCanvas can write its application settings, then try again.";
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void Open(string url)
    {
        try
        {
            _linkLauncher.Open(new Uri(url, UriKind.Absolute));
        }
        catch (Exception)
        {
            ErrorMessage = "The policy link could not be opened. You can copy the address from the consent form and open it in a browser.";
        }
    }

    private void SetSelection(ref bool field, bool value, [CallerMemberName] string? propertyName = null)
    {
        if (!SetField(ref field, value, propertyName))
        {
            return;
        }

        ErrorMessage = null;
        OnPropertyChanged(nameof(CanAgree));
        (AgreeCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
    }

    private bool SetField<T>(ref T field, T value, string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        if (propertyName == nameof(ErrorMessage))
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasError)));
        }
    }
}

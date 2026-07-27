using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.Application.AI;

namespace FusionCanvas.App.Settings;

public sealed class AiSettingsViewModel : INotifyPropertyChanged, IAiConfigurationProvider
{
    private readonly IAiCredentialStore _credentials;
    private readonly IAiCredentialValidator _validator;
    private readonly IAiModelCatalogProvider _catalogProvider;
    private readonly IAiModelCatalogCache _catalogCache;
    private AiConfigurationSettings _settings;
    private bool _loaded;
    private bool _busy;
    private bool _hasCredential;
    private bool _isEditingCredential;
    private bool _confirmRemove;
    private bool _confirmZdrOptOut;
    private string _credentialDraft = string.Empty;
    private string _credentialStatus = "Not checked";
    private string? _message;
    private string _modelSearch = string.Empty;
    private bool _ideationUseGeneral;
    private bool _conceptUseGeneral;

    public AiSettingsViewModel(
        AiConfigurationSettings settings,
        IAiCredentialStore credentials,
        IAiCredentialValidator validator,
        IAiModelCatalogProvider catalogProvider,
        IAiModelCatalogCache catalogCache)
    {
        _settings = settings;
        _credentials = credentials;
        _validator = validator;
        _catalogProvider = catalogProvider;
        _catalogCache = catalogCache;
        _ideationUseGeneral = settings.Ideation.UseGeneral;
        _conceptUseGeneral = settings.Concept.UseGeneral;

        General = new AiProfileEditorViewModel(settings.General);
        Ideation = new AiProfileEditorViewModel(settings.Ideation.CustomProfile);
        Concept = new AiProfileEditorViewModel(settings.Concept.CustomProfile);
        General.SettingsChanged += (_, _) => ApplyProfiles();
        Ideation.SettingsChanged += (_, _) => ApplyProfiles();
        Concept.SettingsChanged += (_, _) => ApplyProfiles();

        AddOrReplaceCommand = new DocumentWindow.RelayCommand(_ => BeginCredentialEdit());
        CancelCredentialCommand = new DocumentWindow.RelayCommand(_ => CancelCredentialEdit());
        SaveCredentialCommand = new AsyncRelayCommand(SaveCredentialAsync, () => CanSaveCredential);
        ValidateCredentialCommand = new AsyncRelayCommand(ValidateCredentialAsync, () => HasCredential && !IsBusy);
        RefreshModelsCommand = new AsyncRelayCommand(RefreshModelsAsync, () => HasCredential && !IsBusy);
        RequestRemoveCommand = new DocumentWindow.RelayCommand(_ => ConfirmRemove = true);
        CancelRemoveCommand = new DocumentWindow.RelayCommand(_ => ConfirmRemove = false);
        RemoveCredentialCommand = new AsyncRelayCommand(RemoveCredentialAsync, () => HasCredential && !IsBusy);
        ConfirmZdrOptOutCommand = new DocumentWindow.RelayCommand(_ => ApplyZdrOptOut());
        CancelZdrOptOutCommand = new DocumentWindow.RelayCommand(_ => ConfirmZdrOptOut = false);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? SettingsChanged;
    public event EventHandler? CredentialFocusRequested;

    public AiConfigurationSettings Current => _settings;
    public AiProfileEditorViewModel General { get; }
    public AiProfileEditorViewModel Ideation { get; }
    public AiProfileEditorViewModel Concept { get; }
    public string GeneralReadiness => Readiness(AiRequestPurpose.General);
    public string IdeationReadiness => IdeationUseGeneral
        ? $"Using General — {Readiness(AiRequestPurpose.Ideation)}"
        : Readiness(AiRequestPurpose.Ideation);
    public string ConceptReadiness => ConceptUseGeneral
        ? $"Using General — {Readiness(AiRequestPurpose.Concept)}"
        : Readiness(AiRequestPurpose.Concept);

    public bool IsBusy
    {
        get => _busy;
        private set
        {
            if (SetField(ref _busy, value))
            {
                NotifyCommands();
            }
        }
    }

    public bool HasCredential
    {
        get => _hasCredential;
        private set
        {
            if (SetField(ref _hasCredential, value))
            {
                NotifyCommands();
            }
        }
    }

    public bool IsEditingCredential
    {
        get => _isEditingCredential;
        private set
        {
            if (SetField(ref _isEditingCredential, value))
            {
                OnPropertyChanged(nameof(HasUnsavedCredentialDraft));
            }
        }
    }

    public bool HasUnsavedCredentialDraft =>
        IsEditingCredential && !string.IsNullOrEmpty(CredentialDraft);

    public string CredentialDraft
    {
        get => _credentialDraft;
        set
        {
            if (SetField(ref _credentialDraft, value))
            {
                OnPropertyChanged(nameof(HasUnsavedCredentialDraft));
                OnPropertyChanged(nameof(CanSaveCredential));
                SaveCredentialCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool CanSaveCredential => !IsBusy && !string.IsNullOrWhiteSpace(CredentialDraft);

    public string CredentialStatus
    {
        get => _credentialStatus;
        private set => SetField(ref _credentialStatus, value);
    }

    public string? Message
    {
        get => _message;
        private set
        {
            if (SetField(ref _message, value))
            {
                OnPropertyChanged(nameof(HasMessage));
            }
        }
    }

    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

    public bool ConfirmRemove
    {
        get => _confirmRemove;
        set => SetField(ref _confirmRemove, value);
    }

    public bool ConfirmZdrOptOut
    {
        get => _confirmZdrOptOut;
        set => SetField(ref _confirmZdrOptOut, value);
    }

    public bool RequireZeroDataRetention
    {
        get => _settings.RequireZeroDataRetention;
        set
        {
            if (value == _settings.RequireZeroDataRetention)
            {
                return;
            }

            if (!value)
            {
                ConfirmZdrOptOut = true;
                OnPropertyChanged();
                return;
            }

            UpdateSettings(_settings with { RequireZeroDataRetention = true });
            _ = LoadCatalogFromCacheAsync();
        }
    }

    public bool AdvancedMode
    {
        get => _settings.AdvancedMode;
        set => UpdateSettings(_settings with { AdvancedMode = value });
    }

    public bool IdeationUseGeneral
    {
        get => _ideationUseGeneral;
        set
        {
            if (!SetField(ref _ideationUseGeneral, value))
            {
                return;
            }

            if (!value && !_settings.Ideation.HasCustomProfile)
            {
                Ideation.Replace(General.Snapshot);
            }

            ApplyProfiles();
        }
    }

    public bool ConceptUseGeneral
    {
        get => _conceptUseGeneral;
        set
        {
            if (!SetField(ref _conceptUseGeneral, value))
            {
                return;
            }

            if (!value && !_settings.Concept.HasCustomProfile)
            {
                Concept.Replace(General.Snapshot);
            }

            ApplyProfiles();
        }
    }

    public string ModelSearch
    {
        get => _modelSearch;
        set
        {
            if (SetField(ref _modelSearch, value))
            {
                ApplyModelFilter();
            }
        }
    }

    public ICommand AddOrReplaceCommand { get; }
    public ICommand CancelCredentialCommand { get; }
    public AsyncRelayCommand SaveCredentialCommand { get; }
    public AsyncRelayCommand ValidateCredentialCommand { get; }
    public AsyncRelayCommand RefreshModelsCommand { get; }
    public ICommand RequestRemoveCommand { get; }
    public ICommand CancelRemoveCommand { get; }
    public AsyncRelayCommand RemoveCredentialCommand { get; }
    public ICommand ConfirmZdrOptOutCommand { get; }
    public ICommand CancelZdrOptOutCommand { get; }

    public async Task EnsureLoadedAsync()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        IsBusy = true;
        try
        {
            var credential = await _credentials.ReadAsync().ConfigureAwait(true);
            HasCredential = credential.State == AiCredentialStateKind.Available;
            CredentialStatus = credential.State switch
            {
                AiCredentialStateKind.Available => "Saved — not verified",
                AiCredentialStateKind.NotFound => "No API key saved",
                _ => credential.Message ?? "Native credential store unavailable"
            };
            Message = credential.State is AiCredentialStateKind.Unavailable or
                AiCredentialStateKind.Locked or AiCredentialStateKind.AccessDenied
                ? credential.Message
                : null;
            await LoadCatalogFromCacheAsync().ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void DiscardCredentialDraft()
    {
        CredentialDraft = string.Empty;
        IsEditingCredential = false;
        ConfirmRemove = false;
    }

    private void BeginCredentialEdit()
    {
        CredentialDraft = string.Empty;
        IsEditingCredential = true;
        CredentialFocusRequested?.Invoke(this, EventArgs.Empty);
    }

    private void CancelCredentialEdit()
    {
        DiscardCredentialDraft();
        CredentialFocusRequested?.Invoke(this, EventArgs.Empty);
    }

    private async Task SaveCredentialAsync()
    {
        IsBusy = true;
        try
        {
            var result = await _credentials.SaveAsync(CredentialDraft).ConfigureAwait(true);
            if (!result.Succeeded)
            {
                Message = result.Message;
                return;
            }

            HasCredential = true;
            CredentialStatus = "Saved — not verified";
            Message = null;
            DiscardCredentialDraft();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ValidateCredentialAsync()
    {
        IsBusy = true;
        try
        {
            var credential = await _credentials.ReadAsync().ConfigureAwait(true);
            if (credential.State != AiCredentialStateKind.Available || credential.Secret is null)
            {
                CredentialStatus = credential.Message ?? "The saved API key is unavailable.";
                return;
            }

            var result = await _validator.ValidateAsync(credential.Secret).ConfigureAwait(true);
            CredentialStatus = result.Kind switch
            {
                AiCredentialValidationKind.Valid => "Verified for inference",
                AiCredentialValidationKind.ManagementKey => "Management-only key",
                AiCredentialValidationKind.Invalid => "Invalid or revoked",
                _ => result.Message ?? "Validation unavailable"
            };
            Message = result.Kind == AiCredentialValidationKind.Valid ? null : result.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RemoveCredentialAsync()
    {
        IsBusy = true;
        try
        {
            var result = await _credentials.RemoveAsync().ConfigureAwait(true);
            if (!result.Succeeded)
            {
                Message = result.Message;
                return;
            }

            HasCredential = false;
            ConfirmRemove = false;
            CredentialStatus = "No API key saved";
            Message = null;
            CredentialFocusRequested?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshModelsAsync()
    {
        IsBusy = true;
        try
        {
            var credential = await _credentials.ReadAsync().ConfigureAwait(true);
            if (credential.State != AiCredentialStateKind.Available || credential.Secret is null)
            {
                Message = credential.Message ?? "The saved API key is unavailable.";
                return;
            }

            var catalog = await _catalogProvider.GetModelsAsync(
                credential.Secret,
                RequireZeroDataRetention).ConfigureAwait(true);
            await _catalogCache.SaveAsync(catalog).ConfigureAwait(true);
            SetModels(catalog.Models);
            Message = catalog.Models.Count == 0 ? "No compatible text models were returned." : null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Message = "OpenRouter models could not be refreshed. A cached catalog remains available when present.";
            await LoadCatalogFromCacheAsync().ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadCatalogFromCacheAsync()
    {
        var catalog = await _catalogCache.LoadAsync(RequireZeroDataRetention).ConfigureAwait(true);
        SetModels(catalog?.Models ?? []);
        if (catalog?.IsStale == true)
        {
            Message = "The model catalog is stale. Refresh when OpenRouter is available.";
        }
    }

    private void SetModels(IReadOnlyList<AiModelDescriptor> models)
    {
        _allModels = models;
        ApplyModelFilter();
        NotifyReadiness();
    }

    private IReadOnlyList<AiModelDescriptor> _allModels = [];

    private void ApplyModelFilter()
    {
        var models = string.IsNullOrWhiteSpace(ModelSearch)
            ? _allModels
            : _allModels.Where(model =>
                model.Id.Contains(ModelSearch, StringComparison.OrdinalIgnoreCase) ||
                model.Name.Contains(ModelSearch, StringComparison.OrdinalIgnoreCase)).ToArray();
        General.Models = models;
        Ideation.Models = models;
        Concept.Models = models;
    }

    private void ApplyZdrOptOut()
    {
        ConfirmZdrOptOut = false;
        UpdateSettings(_settings with { RequireZeroDataRetention = false });
        _ = LoadCatalogFromCacheAsync();
    }

    private void ApplyProfiles()
    {
        var ideation = new AiPurposeProfileSettings(
            IdeationUseGeneral,
            _settings.Ideation.HasCustomProfile || !IdeationUseGeneral,
            Ideation.Snapshot);
        var concept = new AiPurposeProfileSettings(
            ConceptUseGeneral,
            _settings.Concept.HasCustomProfile || !ConceptUseGeneral,
            Concept.Snapshot);
        UpdateSettings(_settings with
        {
            General = General.Snapshot,
            Ideation = ideation,
            Concept = concept
        });
    }

    private void UpdateSettings(AiConfigurationSettings settings)
    {
        if (_settings == settings)
        {
            return;
        }

        _settings = settings;
        OnPropertyChanged(nameof(Current));
        OnPropertyChanged(nameof(RequireZeroDataRetention));
        OnPropertyChanged(nameof(AdvancedMode));
        SettingsChanged?.Invoke(this, EventArgs.Empty);
        NotifyReadiness();
    }

    private void NotifyCommands()
    {
        SaveCredentialCommand.NotifyCanExecuteChanged();
        ValidateCredentialCommand.NotifyCanExecuteChanged();
        RefreshModelsCommand.NotifyCanExecuteChanged();
        RemoveCredentialCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(CanSaveCredential));
    }

    private string Readiness(AiRequestPurpose purpose)
    {
        var resolution = AiConfigurationResolver.Resolve(_settings, purpose, _allModels);
        return resolution.Availability switch
        {
            AiConfigurationAvailability.Ready => $"Ready - {resolution.Model!.Name}",
            AiConfigurationAvailability.MissingModel => "Select a model",
            AiConfigurationAvailability.ModelUnavailable => "Selected model is unavailable",
            AiConfigurationAvailability.PrivacyIncompatible => "Selected model is incompatible with ZDR",
            _ => resolution.Errors.FirstOrDefault() ?? "Review profile parameters"
        };
    }

    private void NotifyReadiness()
    {
        OnPropertyChanged(nameof(GeneralReadiness));
        OnPropertyChanged(nameof(IdeationReadiness));
        OnPropertyChanged(nameof(ConceptReadiness));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Avalonia.Media.Imaging;
using FusionCanvas.Application.DesignFiles;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Application.AI;
using FusionCanvas.App.Settings;
using FusionCanvas.App.DocumentWindow;

namespace FusionCanvas.App.StageTools;




public sealed class DesignStageToolViewModel : INotifyPropertyChanged
{
    private readonly IDesignStageService _designStageService;
    private string _readOnlyReason = string.Empty;
    private bool _isReadOnly;
    private bool _isBusy;
    private bool _hasConfiguration;
    private string _configPrompt = "Select a listing configuration to begin.";
    private bool _showPreviewDialog;
    private Guid _previewAssetId;
    private Stream? _previewStream;
    private Bitmap? _previewBitmap;
    private string? _errorMessage;
    private string _noConfigMessage = "No listing configuration selected. Select a configuration from the offerings below to show the design slot grid.";
    private Guid _itemId;
    private FulfillmentOffering? _selectedOffering;
    private Guid? _selectedOfferingId;
    private string? _selectedOfferingName;
    private string? _selectedBlueprintName;
    private string? _providerNetworkWarning;
    private PendingRemovalAction? _pendingRemoval;
    private bool _isRemovalConfirmationVisible;
    private string _removalConfirmationMessage = string.Empty;
    private long _loadGeneration;
    private bool _isApplyingState;
    private readonly IArtworkGenerationService? _artworkGenerationService;
    private readonly AiSettingsViewModel? _aiSettings;
    private CancellationTokenSource? _artworkCts;
    private readonly SemaphoreSlim _artworkPreferenceSaveGate = new(1, 1);
    private bool _isArtworkBusy;
    private Guid? _selectedArtworkTargetId;
    private bool _transparentBackground;
    private IReadOnlyList<AiImageEndpointCapabilities> _artworkEndpoints = [];
    private bool _artworkCapabilitiesLoaded;
    private string? _artworkCapabilityMessage;
    private long _artworkAvailabilityGeneration;

    public DesignStageToolViewModel(IDesignStageService designStageService, IArtworkGenerationService? artworkGenerationService = null, AiSettingsViewModel? aiSettings = null)
    {
        _designStageService = designStageService ?? throw new ArgumentNullException(nameof(designStageService));
        _artworkGenerationService = artworkGenerationService;
        _aiSettings = aiSettings;
        GenerateArtworkCommand = new RelayCommand(_ => _ = GenerateArtworkAsync(), () => CanGenerateArtwork);
        CancelArtworkCommand = new RelayCommand(_ => _artworkCts?.Cancel(), () => IsArtworkBusy);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    // --- Configuration ---
    public bool HasConfiguration
    {
        get => _hasConfiguration;
        private set
        {
            if (_hasConfiguration == value) return;
            _hasConfiguration = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanGenerateArtwork));
            GenerateArtworkCommand.NotifyCanExecuteChanged();
        }
    }

    public string ConfigPrompt
    {
        get => _configPrompt;
        set { _configPrompt = value; OnPropertyChanged(); }
    }

    public FulfillmentOffering? SelectedOffering
    {
        get => _selectedOffering;
        set
        {
            if (_selectedOffering?.Id != value?.Id)
            {
                _selectedOffering = value;
                OnPropertyChanged();
                if (value is not null && !_isApplyingState)
                {
                    Interlocked.Increment(ref _loadGeneration);
                    _ = PersistSelectedOfferingAsync(value.Id);
                }
            }
        }
    }

    public Guid? SelectedOfferingId
    {
        get => _selectedOfferingId;
        private set { _selectedOfferingId = value; OnPropertyChanged(); }
    }

    public string? SelectedOfferingName
    {
        get => _selectedOfferingName;
        private set { _selectedOfferingName = value; OnPropertyChanged(); OnPropertyChanged(nameof(SelectedOfferingStatus)); }
    }

    public string? SelectedBlueprintName
    {
        get => _selectedBlueprintName;
        private set { _selectedBlueprintName = value; OnPropertyChanged(); }
    }

    public string? ProviderNetworkWarning
    {
        get => _providerNetworkWarning;
        private set { _providerNetworkWarning = value; OnPropertyChanged(); }
    }

    public string? SelectedOfferingStatus
    {
        get
        {
            if (_selectedOfferingId is null) return null;
            var offering = AvailableOfferings.SingleOrDefault(o => o.Id == _selectedOfferingId);
            if (offering is null) return null;
            return offering.Kind == FulfillmentKind.PrintifyChoiceNetwork
                ? "Printify Choice network"
                : offering.ProviderName is not null
                    ? $"Fixed provider: {offering.ProviderName}"
                    : null;
        }
    }

    public string NoConfigMessage
    {
        get => _noConfigMessage;
        set { _noConfigMessage = value; OnPropertyChanged(); }
    }

    public bool IsReadOnly
    {
        get => _isReadOnly;
        private set
        {
            if (_isReadOnly == value) return;
            _isReadOnly = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanGenerateArtwork));
            GenerateArtworkCommand.NotifyCanExecuteChanged();
        }
    }

    public string ReadOnlyReason
    {
        get => _readOnlyReason;
        set { _readOnlyReason = value; OnPropertyChanged(); }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    public bool ShowPreviewDialog
    {
        get => _showPreviewDialog;
        set { _showPreviewDialog = value; OnPropertyChanged(); }
    }

    public Guid PreviewAssetId
    {
        get => _previewAssetId;
        private set { _previewAssetId = value; OnPropertyChanged(); }
    }

    /// <summary>Bitmap for the large preview dialog. Disposed when preview is closed or replaced.</summary>
    public Bitmap? PreviewBitmap
    {
        get => _previewBitmap;
        private set { _previewBitmap = value; OnPropertyChanged(); }
    }

    public Stream? PreviewStream
    {
        get => _previewStream;
        private set { _previewStream = value; OnPropertyChanged(); }
    }

    // --- Removal confirmation ---

    public bool IsRemovalConfirmationVisible
    {
        get => _isRemovalConfirmationVisible;
        private set { _isRemovalConfirmationVisible = value; OnPropertyChanged(); }
    }

    public string RemovalConfirmationMessage
    {
        get => _removalConfirmationMessage;
        private set { _removalConfirmationMessage = value; OnPropertyChanged(); }
    }

    public void RequestRemoveSlotImage(Guid rowId, Guid designAreaId)
    {
        _pendingRemoval = new PendingRemovalAction(PendingRemovalKind.SlotImage, rowId, designAreaId, null);
        RemovalConfirmationMessage = "Remove this slot image? This cannot be undone.";
        IsRemovalConfirmationVisible = true;
    }

    public void RequestRemoveSupportingImage(Guid assetId)
    {
        _pendingRemoval = new PendingRemovalAction(PendingRemovalKind.SupportingImage, Guid.Empty, Guid.Empty, assetId);
        RemovalConfirmationMessage = "Remove this supporting image? This cannot be undone.";
        IsRemovalConfirmationVisible = true;
    }

    public void RequestRemoveSpecificRow(Guid rowId)
    {
        _pendingRemoval = new PendingRemovalAction(PendingRemovalKind.SpecificRow, rowId, Guid.Empty, null);
        RemovalConfirmationMessage = "Remove this specific row? Its colors will move back to the default row.";
        IsRemovalConfirmationVisible = true;
    }

    public async Task ConfirmPendingRemovalAsync(CancellationToken ct = default)
    {
        if (_pendingRemoval is null || IsBusy || IsReadOnly) return;
        IsBusy = true;
        try
        {
            DesignStageResult? result = null;
            switch (_pendingRemoval.Kind)
            {
                case PendingRemovalKind.SlotImage:
                    result = await _designStageService.RemoveSlotImageAsync(_itemId, _pendingRemoval.RowId, _pendingRemoval.DesignAreaId, ct).ConfigureAwait(true);
                    break;
                case PendingRemovalKind.SupportingImage:
                    result = await _designStageService.RemoveSupportingImageAsync(_itemId, _pendingRemoval.AssetId!.Value, ct).ConfigureAwait(true);
                    break;
                case PendingRemovalKind.SpecificRow:
                    result = await _designStageService.RemoveSpecificRowAsync(_itemId, _pendingRemoval.RowId, ct).ConfigureAwait(true);
                    break;
            }
            ErrorMessage = result?.Error;
            if (result?.Succeeded == true)
            {
                await LoadAsync(_itemId, !IsReadOnly, ct).ConfigureAwait(true);
            }
        }
        finally
        {
            IsBusy = false;
            _pendingRemoval = null;
            IsRemovalConfirmationVisible = false;
            RemovalConfirmationMessage = string.Empty;
        }
    }

    public void CancelPendingRemoval()
    {
        _pendingRemoval = null;
        IsRemovalConfirmationVisible = false;
        RemovalConfirmationMessage = string.Empty;
    }

    // --- Collections ---
    public ObservableCollection<FulfillmentOffering> AvailableOfferings { get; } = [];
    public ObservableCollection<DesignColorViewModel> AvailableColors { get; } = [];
    public ObservableCollection<DesignColorViewModel> SelectedColors { get; } = [];
    public ObservableCollection<DesignRowViewModel> Rows { get; } = [];
    public ObservableCollection<DesignSlotViewModel> SupportingImages { get; } = [];
    public ObservableCollection<ArtworkTargetOption> ArtworkTargets { get; } = [];

    public Guid? SelectedArtworkTargetId
    {
        get => _selectedArtworkTargetId;
        set
        {
            if (_selectedArtworkTargetId == value) return;
            _selectedArtworkTargetId = value;
            OnPropertyChanged();
            var target = ArtworkTargets.SingleOrDefault(candidate => candidate.Id == value);
            if (!_isApplyingState)
            {
                var recommendedTransparency = target?.RecommendsTransparency ?? false;
                if (_artworkCapabilitiesLoaded && !HasCompatibleArtworkEndpoint(transparentBackground: true))
                {
                    recommendedTransparency = false;
                }
                if (_transparentBackground != recommendedTransparency)
                {
                    _transparentBackground = recommendedTransparency;
                    OnPropertyChanged(nameof(TransparentBackground));
                }
                _ = PersistArtworkPreferencesAsync();
            }
            OnPropertyChanged(nameof(ArtworkGenerationGuidance));
            OnPropertyChanged(nameof(CanGenerateArtwork));
            OnPropertyChanged(nameof(CanUseTransparentBackground));
            GenerateArtworkCommand.NotifyCanExecuteChanged();
        }
    }

    public bool TransparentBackground
    {
        get => _transparentBackground;
        set
        {
            if (value && _artworkCapabilitiesLoaded && !HasCompatibleArtworkEndpoint(transparentBackground: true))
                value = false;
            if (_transparentBackground == value) return;
            _transparentBackground = value;
            OnPropertyChanged();
            if (!_isApplyingState)
            {
                _ = PersistArtworkPreferencesAsync();
            }
        }
    }

    public string ArtworkGenerationGuidance => !HasConfiguration
        ? "Select a Listing Configuration to choose an artwork target."
        : ArtworkTargets.Count == 0
            ? "Configure an active Design Area and optional primary in Store setup before generating artwork."
            : SelectedArtworkTargetId is null
                ? "Choose a Design Area before generating artwork."
                : !_artworkCapabilitiesLoaded
                    ? "Checking the selected image model's endpoint capabilities…"
                    : !HasCompatibleArtworkEndpoint(transparentBackground: false)
                        ? _artworkCapabilityMessage ?? "The selected image model has no endpoint compatible with this target and privacy policy. Review Artwork AI Settings."
                        : !CanUseTransparentBackground
                            ? "The selected image model supports opaque artwork for this target; transparent background is unavailable."
                            : "Generation uses the current Concept triangle and does not upload Supporting Images automatically.";

    public bool IsArtworkBusy
    {
        get => _isArtworkBusy;
        private set { if (_isArtworkBusy == value) return; _isArtworkBusy = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanGenerateArtwork)); GenerateArtworkCommand.NotifyCanExecuteChanged(); CancelArtworkCommand.NotifyCanExecuteChanged(); }
    }

    public bool CanGenerateArtwork => _artworkGenerationService is not null && !IsArtworkBusy && !IsReadOnly && _selectedArtworkTargetId is not null && HasConfiguration && HasCompatibleArtworkEndpoint(transparentBackground: false);
    public bool CanUseTransparentBackground => !IsReadOnly && HasCompatibleArtworkEndpoint(transparentBackground: true);
    public string ArtworkProgress => IsArtworkBusy ? "Generating artwork…" : string.Empty;
    public RelayCommand GenerateArtworkCommand { get; }
    public RelayCommand CancelArtworkCommand { get; }

    // --- Commands ---
    public async Task SelectConfigurationAsync(Guid offeringId, CancellationToken ct = default)
    {
        if (IsBusy || IsReadOnly) return;
        IsBusy = true;
        try
        {
            var result = await _designStageService.SelectConfigurationAsync(_itemId, offeringId, ct).ConfigureAwait(true);
            ErrorMessage = result.Error;
            if (result.Succeeded)
            {
                await LoadAsync(_itemId, !IsReadOnly, ct).ConfigureAwait(true);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task PersistSelectedOfferingAsync(Guid offeringId)
    {
        try
        {
            await SelectConfigurationAsync(offeringId).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = $"The listing configuration could not be persisted. {ex.Message}";
        }
    }

    private async Task PersistArtworkPreferencesAsync()
    {
        if (_isApplyingState || _itemId == Guid.Empty || IsReadOnly)
        {
            return;
        }

        try
        {
            await _artworkPreferenceSaveGate.WaitAsync().ConfigureAwait(true);
            try
            {
                var result = await _designStageService.SaveArtworkPreferencesAsync(
                    _itemId, _selectedArtworkTargetId, _transparentBackground).ConfigureAwait(true);
                if (!result.Succeeded && !string.IsNullOrWhiteSpace(result.Error))
                {
                    ErrorMessage = result.Error;
                }
            }
            finally
            {
                _artworkPreferenceSaveGate.Release();
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ErrorMessage = $"The artwork preferences could not be persisted. {exception.Message}";
        }
    }

    public async Task ToggleColorAsync(string colorValue, bool add, CancellationToken ct = default)
    {
        if (IsBusy || IsReadOnly) return;
        IsBusy = true;
        try
        {
            DesignStageResult result;
            if (add)
            {
                result = await _designStageService.AddSelectedColorAsync(_itemId, colorValue, ct).ConfigureAwait(true);
            }
            else
            {
                result = await _designStageService.RemoveSelectedColorAsync(_itemId, colorValue, ct).ConfigureAwait(true);
            }
            ErrorMessage = result.Error;
            if (result.Succeeded)
            {
                await LoadAsync(_itemId, !IsReadOnly, ct).ConfigureAwait(true);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task MakeSpecificForColorAsync(string colorValue, CancellationToken ct = default)
    {
        if (IsBusy || IsReadOnly) return;
        IsBusy = true;
        try
        {
            var result = await _designStageService.MakeSpecificForColorAsync(_itemId, colorValue, ct).ConfigureAwait(true);
            ErrorMessage = result.Error;
            if (result.Succeeded)
            {
                await LoadAsync(_itemId, !IsReadOnly, ct).ConfigureAwait(true);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RemoveSpecificRowAsync(Guid rowId, CancellationToken ct = default)
    {
        if (IsBusy || IsReadOnly) return;
        IsBusy = true;
        try
        {
            var result = await _designStageService.RemoveSpecificRowAsync(_itemId, rowId, ct).ConfigureAwait(true);
            ErrorMessage = result.Error;
            if (result.Succeeded)
            {
                await LoadAsync(_itemId, !IsReadOnly, ct).ConfigureAwait(true);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task AssignSlotImageAsync(Guid rowId, Guid designAreaId, string sourcePath, CancellationToken ct = default)
    {
        if (IsBusy || IsReadOnly) return;
        IsBusy = true;
        try
        {
            var result = await _designStageService.AssignSlotImageAsync(_itemId, rowId, designAreaId, sourcePath, ct).ConfigureAwait(true);
            ErrorMessage = result.Error;
            if (result.Succeeded)
            {
                await LoadAsync(_itemId, !IsReadOnly, ct).ConfigureAwait(true);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RemoveSlotImageAsync(Guid rowId, Guid designAreaId, CancellationToken ct = default)
    {
        if (IsBusy || IsReadOnly) return;
        IsBusy = true;
        try
        {
            var result = await _designStageService.RemoveSlotImageAsync(_itemId, rowId, designAreaId, ct).ConfigureAwait(true);
            ErrorMessage = result.Error;
            if (result.Succeeded)
            {
                await LoadAsync(_itemId, !IsReadOnly, ct).ConfigureAwait(true);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task PreviewSlotImageAsync(Guid rowId, Guid designAreaId, CancellationToken ct = default)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var stream = await _designStageService.OpenSlotPreviewAsync(rowId, designAreaId, ct).ConfigureAwait(true);
            PreviewStream?.Dispose();
            PreviewBitmap?.Dispose();
            PreviewStream = stream;
            PreviewBitmap = stream is not null ? new Bitmap(stream) : null;
            PreviewAssetId = Guid.Empty;
            ShowPreviewDialog = true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = $"Preview failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ExportSlotImageAsync(Guid rowId, Guid designAreaId, string destinationPath, CancellationToken ct = default)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await _designStageService.ExportSlotImageAsync(rowId, designAreaId, destinationPath, ct).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = $"Export failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ExportSupportingImageAsync(Guid assetId, string destinationPath, CancellationToken ct = default)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await _designStageService.ExportSupportingImageAsync(assetId, destinationPath, ct).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = $"Export failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ImportSupportingImageAsync(string sourcePath, CancellationToken ct = default)
    {
        if (IsBusy || IsReadOnly) return;
        IsBusy = true;
        try
        {
            var result = await _designStageService.ImportSupportingImageAsync(_itemId, sourcePath, ct).ConfigureAwait(true);
            ErrorMessage = result.Error;
            if (result.Succeeded)
            {
                await LoadAsync(_itemId, !IsReadOnly, ct).ConfigureAwait(true);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void PreviewSupportingImage(Guid assetId, string? thumbnailPath)
    {
        // For supporting images, load the thumbnail directly from the managed file path
        try
        {
            PreviewStream?.Dispose();
            PreviewBitmap?.Dispose();
            PreviewStream = null;

            if (thumbnailPath is not null && File.Exists(thumbnailPath))
            {
                PreviewBitmap = new Bitmap(thumbnailPath);
            }
            else
            {
                PreviewBitmap = null;
                ErrorMessage = "Could not open supporting image preview (file not found).";
            }

            PreviewAssetId = assetId;
            ShowPreviewDialog = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Preview failed: {ex.Message}";
        }
    }

    public async Task RemoveSupportingImageAsync(Guid assetId, CancellationToken ct = default)
    {
        if (IsBusy || IsReadOnly) return;
        IsBusy = true;
        try
        {
            var result = await _designStageService.RemoveSupportingImageAsync(_itemId, assetId, ct).ConfigureAwait(true);
            ErrorMessage = result.Error;
            if (result.Succeeded)
            {
                await LoadAsync(_itemId, !IsReadOnly, ct).ConfigureAwait(true);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void ClosePreviewDialog()
    {
        ShowPreviewDialog = false;
        PreviewBitmap?.Dispose();
        PreviewBitmap = null;
        PreviewStream?.Dispose();
        PreviewStream = null;
    }

    public async Task GenerateArtworkAsync(CancellationToken cancellationToken = default)
    {
        if (!CanGenerateArtwork || _aiSettings is null || _selectedArtworkTargetId is not Guid targetId)
            return;

        _artworkCts?.Dispose();
        _artworkCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        IsArtworkBusy = true;
        ErrorMessage = null;
        try
        {
            var key = await _aiSettings.ReadApiKeyAsync(_artworkCts.Token).ConfigureAwait(true);
            var profile = _aiSettings.Current.Artwork;
            var endpoints = await _aiSettings.GetArtworkEndpointsAsync(_artworkCts.Token).ConfigureAwait(true);
            ApplyArtworkEndpointCapabilities(endpoints);
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(profile.ModelId))
            {
                ErrorMessage = "Configure an Artwork image model and API key in AI Settings.";
                return;
            }
            if (!HasCompatibleArtworkEndpoint(transparentBackground: false))
            {
                ErrorMessage = _artworkCapabilityMessage ?? "The selected image model has no endpoint compatible with this target and privacy policy.";
                return;
            }

            var result = await _artworkGenerationService!.GenerateAsync(new ArtworkGenerationRequest(
                _itemId, targetId, key, profile, _aiSettings.AvailableModels, endpoints,
                TransparentBackground, _aiSettings.RequireZeroDataRetention), _artworkCts.Token).ConfigureAwait(true);
            ErrorMessage = result.Error;
            if (result.Succeeded)
                await LoadAsync(_itemId, !IsReadOnly, _artworkCts.Token).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            ErrorMessage = "Artwork generation cancelled. A dispatched provider request may still incur cost.";
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            IsArtworkBusy = false;
            _artworkCts?.Dispose();
            _artworkCts = null;
        }
    }

    // --- Load ---
    public async Task LoadAsync(Guid itemId, bool canEdit, CancellationToken cancellationToken = default)
    {
        _artworkCts?.Cancel();
        var loadGeneration = Interlocked.Increment(ref _loadGeneration);
        _artworkCapabilitiesLoaded = false;
        _artworkEndpoints = [];
        _artworkCapabilityMessage = null;
        NotifyArtworkCapabilityState();
        IsReadOnly = !canEdit;
        ReadOnlyReason = canEdit ? string.Empty : "Design stage content is read-only while the item is protected or an earlier stage is being reviewed.";
        _itemId = itemId;

        // A target change persists asynchronously from the selection setter. Serialize a
        // subsequent stage load behind that save so immediate navigation cannot reload
        // the previous preference and overwrite the in-memory selection.
        DesignStageState state;
        await _artworkPreferenceSaveGate.WaitAsync(cancellationToken).ConfigureAwait(true);
        try
        {
            state = await _designStageService.LoadDesignStageStateAsync(itemId, cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            _artworkPreferenceSaveGate.Release();
        }
        if (loadGeneration != Volatile.Read(ref _loadGeneration))
        {
            return;
        }

        _isApplyingState = true;
        try
        {
            HasConfiguration = state.SelectedOfferingId is not null;
            SelectedOfferingId = state.SelectedOfferingId;
            SelectedOfferingName = state.SelectedOfferingName;
            SelectedBlueprintName = state.SelectedBlueprintName;
            ProviderNetworkWarning = state.ProviderNetworkWarning;
            ArtworkTargets.Clear();
            var selectedBlueprint = state.SelectedOfferingId is Guid offeringId
                ? state.AvailableBlueprintOfferings.FirstOrDefault(value => value.Id == offeringId)
                : null;
            foreach (var placeholder in state.AvailablePlaceholders)
            {
                var recommendsTransparency = string.Equals(placeholder.ArtworkGuidance?.Background, "transparent", StringComparison.OrdinalIgnoreCase);
                ArtworkTargets.Add(new ArtworkTargetOption(placeholder.Id, placeholder.Name,
                    $"{placeholder.Position} · {placeholder.Width}×{placeholder.Height}px", selectedBlueprint?.PrimaryArtworkDesignAreaId == placeholder.Id, recommendsTransparency,
                    placeholder.Width, placeholder.Height));
            }

            var defaultTargetId = selectedBlueprint?.PrimaryArtworkDesignAreaId is Guid primary && ArtworkTargets.Any(value => value.Id == primary)
                ? primary
                : (Guid?)null;
            SelectedArtworkTargetId = state.HasPersistedArtworkTargetPreference
                ? state.PersistedArtworkTargetId
                : defaultTargetId;
            TransparentBackground = state.PersistedTransparentBackground
                ?? ArtworkTargets.SingleOrDefault(value => value.Id == SelectedArtworkTargetId)?.RecommendsTransparency
                ?? false;

            var selectedOffering = state.SelectedOfferingId is not null
                ? state.AvailableOfferings.SingleOrDefault(o => o.Id == state.SelectedOfferingId.Value)
                : null;
            if (state.IsReadOnly)
            {
                IsReadOnly = true;
                ReadOnlyReason = state.ReadOnlyReason;
            }

            // Available offerings
            AvailableOfferings.Clear();
            foreach (var offering in state.AvailableOfferings)
            {
                AvailableOfferings.Add(offering);
            }

            // Reapply the selected item after rebuilding the source collection. Avalonia
            // may clear SelectedItem while the old collection is being replaced; that
            // binding update must not be treated as a user selection to persist.
            _selectedOffering = selectedOffering;
            OnPropertyChanged(nameof(SelectedOffering));

            // Ensure SelectedOfferingStatus is re-evaluated now that offerings are populated
            OnPropertyChanged(nameof(SelectedOfferingStatus));

            // Available colors
            AvailableColors.Clear();
            foreach (var color in state.AvailableColors)
            {
                var isSelected = state.SelectedColors.Any(c => string.Equals(c, color, StringComparison.OrdinalIgnoreCase));
                AvailableColors.Add(new DesignColorViewModel(color, isSelected, IsReadOnly));
            }

            // Selected colors
            SelectedColors.Clear();
            foreach (var color in state.SelectedColors)
            {
                SelectedColors.Add(new DesignColorViewModel(color, true, IsReadOnly));
            }

            // Rows — dispose old slot bitmaps first
            foreach (var row in Rows)
            {
                foreach (var slot in row.Slots)
                {
                    slot.Dispose();
                }
            }
            Rows.Clear();
            foreach (var row in state.Rows)
            {
                Rows.Add(new DesignRowViewModel(row, IsReadOnly));
            }

            // Supporting images — dispose old slot bitmaps first
            foreach (var img in SupportingImages)
            {
                img.Dispose();
            }
            SupportingImages.Clear();
            foreach (var img in state.SupportingImages)
            {
                SupportingImages.Add(new DesignSlotViewModel(img, IsReadOnly));
            }
        }
        finally
        {
            _isApplyingState = false;
        }

        await RefreshArtworkAvailabilityAsync(cancellationToken).ConfigureAwait(true);
    }

    public async Task RefreshArtworkAvailabilityAsync(CancellationToken cancellationToken = default)
    {
        var generation = Interlocked.Increment(ref _artworkAvailabilityGeneration);
        if (_aiSettings is null || _itemId == Guid.Empty)
        {
            ApplyArtworkEndpointCapabilities([], "Configure an Artwork image model and API key in AI Settings.");
            return;
        }

        try
        {
            var endpoints = await _aiSettings.GetArtworkEndpointsAsync(cancellationToken).ConfigureAwait(true);
            if (generation != Volatile.Read(ref _artworkAvailabilityGeneration)) return;
            ApplyArtworkEndpointCapabilities(endpoints);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            if (generation != Volatile.Read(ref _artworkAvailabilityGeneration)) return;
            ApplyArtworkEndpointCapabilities([], "Image endpoint capabilities could not be loaded. Review Artwork AI Settings and try again.");
        }
    }

    private void ApplyArtworkEndpointCapabilities(
        IReadOnlyList<AiImageEndpointCapabilities> endpoints,
        string? unavailableMessage = null)
    {
        _artworkEndpoints = endpoints;
        _artworkCapabilitiesLoaded = true;
        _artworkCapabilityMessage = unavailableMessage;
        if (_transparentBackground && !HasCompatibleArtworkEndpoint(transparentBackground: true))
        {
            _transparentBackground = false;
            OnPropertyChanged(nameof(TransparentBackground));
        }
        NotifyArtworkCapabilityState();
    }

    private bool HasCompatibleArtworkEndpoint(bool transparentBackground)
    {
        if (!_artworkCapabilitiesLoaded || _aiSettings?.Current.Artwork.ModelId is not { Length: > 0 } modelId)
            return false;
        var target = ArtworkTargets.SingleOrDefault(candidate => candidate.Id == _selectedArtworkTargetId);
        return target is not null && AiImageEndpointPolicy.SelectEndpoint(
            _artworkEndpoints,
            modelId,
            _aiSettings.RequireZeroDataRetention,
            transparentBackground,
            new AiImageSize(target.Width, target.Height)) is not null;
    }

    private void NotifyArtworkCapabilityState()
    {
        OnPropertyChanged(nameof(ArtworkGenerationGuidance));
        OnPropertyChanged(nameof(CanGenerateArtwork));
        OnPropertyChanged(nameof(CanUseTransparentBackground));
        GenerateArtworkCommand.NotifyCanExecuteChanged();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>Kind of removal pending user confirmation.</summary>


using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.App.Settings;
using FusionCanvas.Application.Catalog;
using FusionCanvas.Application.Mockups;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.App.Stores;

public abstract class SelectableCatalogRecord(string label) : INotifyPropertyChanged
{
    private bool _isSelected;

    public event PropertyChangedEventHandler? PropertyChanged;
    public string Label { get; } = label;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }
}

public sealed class OptionValueChoiceViewModel(OfferingOptionValue value, string label) : SelectableCatalogRecord(label)
{
    public OfferingOptionValue Value { get; } = value;
}

public sealed class VariantChoiceViewModel(OfferingVariant variant) : SelectableCatalogRecord(variant.Name)
{
    public OfferingVariant Variant { get; } = variant;
}

/// <summary>Presentation state for the authoritative normalized Blueprint Offering editor.</summary>
public sealed class CatalogSetupViewModel : INotifyPropertyChanged
{
    private readonly ICatalogSetupService _catalog;
    private readonly IMockupTemplateSetupService _mockups;
    private readonly IOfferingManagementService? _offeringManagement;
    private readonly IProviderCatalogCandidateSource? _providerCatalog;
    private BlueprintOffering? _selectedOffering;
    private OfferingOption? _selectedOption;
    private OfferingPlaceholder? _selectedPlaceholder;
    private MockupTemplate? _selectedTemplate;
    private OfferingOptionValue? _selectedColor;
    private Guid? _requestedOfferingId;
    private string _offeringName = string.Empty;
    private string _offeringDescription = string.Empty;
    private string _providerNetworkCode = string.Empty;
    private string _externalOfferingId = string.Empty;
    private string _optionName = string.Empty;
    private string _optionValue = string.Empty;
    private string _variantName = string.Empty;
    private string _placeholderName = string.Empty;
    private string _placeholderDescription = string.Empty;
    private string _placeholderPosition = string.Empty;
    private string _placeholderDecorationMethod = string.Empty;
    private string _placeholderWidth = string.Empty;
    private string _placeholderHeight = string.Empty;
    private bool _placeholderUsesAllVariants = true;
    private string _placeholderProviderReference = string.Empty;
    private string _artworkWidth = string.Empty;
    private string _artworkHeight = string.Empty;
    private string _artworkDpi = string.Empty;
    private string _artworkFormat = string.Empty;
    private string _artworkBackground = string.Empty;
    private string _templateName = string.Empty;
    private string _error = string.Empty;
    private bool _isBusy;
    private bool _isAddingOption;
    private bool _isAddingOptionValue;
    private bool _isAddingVariant;
    private bool _isAddingPlaceholder;
    private bool _isAddingTemplate;
    private OptionKind _selectedOptionKind = OptionKind.Color;
    private OfferingOptionValue? _bulkColor;
    private string _bulkResultMessage = string.Empty;
    private BulkVariantPreview? _bulkPreview;
    private ProviderMockupCandidateDescriptor? _selectedProviderMockup;
    private string _providerCatalogMessage = string.Empty;
    private double _mappingX;
    private double _mappingY;
    private double _mappingWidth = 100;
    private double _mappingHeight = 100;

    public CatalogSetupViewModel(ICatalogSetupService catalog, IMockupTemplateSetupService mockups, IOfferingManagementService? offeringManagement = null, IProviderCatalogCandidateSource? providerCatalog = null)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _mockups = mockups ?? throw new ArgumentNullException(nameof(mockups));
        _offeringManagement = offeringManagement;
        _providerCatalog = providerCatalog;

        SaveOfferingCommand = new AsyncRelayCommand(SaveOfferingAsync, () => CanEdit && SelectedOffering is not null && !string.IsNullOrWhiteSpace(OfferingName));
        StartAddOptionCommand = new RelayCommand(_ => IsAddingOption = true, () => CanEdit && SelectedOffering is not null);
        CancelAddOptionCommand = new RelayCommand(_ => { IsAddingOption = false; OptionName = string.Empty; });
        CreateOptionCommand = new AsyncRelayCommand(CreateOptionAsync, () => CanEdit && IsAddingOption && SelectedOffering is not null && !string.IsNullOrWhiteSpace(OptionName));
        StartAddOptionValueCommand = new RelayCommand(_ => IsAddingOptionValue = true, () => CanEdit && SelectedOption is not null);
        CancelAddOptionValueCommand = new RelayCommand(_ => { IsAddingOptionValue = false; OptionValue = string.Empty; });
        CreateOptionValueCommand = new AsyncRelayCommand(CreateOptionValueAsync, () => CanEdit && IsAddingOptionValue && SelectedOffering is not null && SelectedOption is not null && !string.IsNullOrWhiteSpace(OptionValue));
        StartAddVariantCommand = new RelayCommand(_ => IsAddingVariant = true, () => CanEdit && AvailableOptions.Any() && VariantValueChoices.Count > 0);
        CancelAddVariantCommand = new RelayCommand(_ => ResetVariantDraft());
        CreateVariantCommand = new AsyncRelayCommand(CreateVariantAsync, () => CanEdit && IsAddingVariant && VariantValueChoices.Any(value => value.IsSelected));
        StartAddPlaceholderCommand = new RelayCommand(_ => BeginNewDesignArea(), () => CanEdit && AvailableVariants.Any());
        EditPlaceholderCommand = new RelayCommand(parameter => BeginEditDesignArea(parameter as OfferingPlaceholder), () => CanEdit);
        CancelAddPlaceholderCommand = new RelayCommand(_ => { ResetPlaceholderDraft(); SelectedPlaceholder = AvailablePlaceholders.FirstOrDefault(); });
        CreatePlaceholderCommand = new AsyncRelayCommand(CreatePlaceholderAsync, CanCreatePlaceholder);
        SetDefaultPlaceholderCommand = new AsyncRelayCommand(SetDefaultPlaceholderAsync, () => CanEdit && SelectedOffering is not null && SelectedPlaceholder is not null);
        StartAddTemplateCommand = new RelayCommand(_ => BeginNewTemplate(), () => CanEdit && AvailablePlaceholders.Any());
        EditTemplateCommand = new RelayCommand(parameter => BeginEditTemplate(parameter as MockupTemplate), () => CanEdit);
        CancelAddTemplateCommand = new RelayCommand(_ => { IsAddingTemplate = false; TemplateName = string.Empty; SelectedTemplate = AvailableTemplates.FirstOrDefault(); });
        CreateTemplateCommand = new AsyncRelayCommand(CreateTemplateAsync, CanCreateTemplate);
        AddTemplateColorCommand = new AsyncRelayCommand(AddTemplateColorAsync, () => CanEdit && SelectedTemplate is not null && SelectedColor is not null);
        ArchiveOptionCommand = new RelayCommand(parameter => RunArchive(parameter, CatalogRecordKind.Option));
        ArchiveOptionValueCommand = new RelayCommand(parameter => RunArchive(parameter, CatalogRecordKind.OptionValue));
        ArchiveVariantCommand = new RelayCommand(parameter => RunArchive(parameter, CatalogRecordKind.Variant));
        ArchivePlaceholderCommand = new RelayCommand(parameter => RunArchive(parameter, CatalogRecordKind.Placeholder));
        ArchiveTemplateCommand = new RelayCommand(parameter => _ = ArchiveTemplateAsync(parameter as MockupTemplate), () => CanEdit);
        PreviewBulkVariantsCommand = new AsyncRelayCommand(PreviewBulkVariantsAsync, CanPreviewBulkVariants);
        ConfirmBulkVariantsCommand = new AsyncRelayCommand(ConfirmBulkVariantsAsync, () => CanEdit && _bulkPreview?.CanConfirm == true);
        CancelBulkVariantsCommand = new RelayCommand(_ => ResetBulkDraft());
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<Blueprint> Blueprints { get; } = [];
    public ObservableCollection<PrintProvider> PrintProviders { get; } = [];
    public ObservableCollection<BlueprintOffering> Offerings { get; } = [];
    public ObservableCollection<OfferingOption> Options { get; } = [];
    public ObservableCollection<OfferingOptionValue> OptionValues { get; } = [];
    public ObservableCollection<OfferingVariant> Variants { get; } = [];
    public ObservableCollection<OfferingPlaceholder> Placeholders { get; } = [];
    public ObservableCollection<MockupTemplate> Templates { get; } = [];
    public ObservableCollection<MockupTemplateColorVariant> TemplateColors { get; } = [];
    public ObservableCollection<MockupTemplateRevision> TemplateRevisions { get; } = [];
    public ObservableCollection<OptionKind> OptionKinds { get; } = [OptionKind.Color, OptionKind.Size, OptionKind.Other];
    public ObservableCollection<OptionValueChoiceViewModel> VariantValueChoices { get; } = [];
    public ObservableCollection<VariantChoiceViewModel> PlaceholderVariantChoices { get; } = [];
    public ObservableCollection<BulkSizeChoiceViewModel> BulkSizeChoices { get; } = [];
    public ObservableCollection<BulkVariantCandidate> BulkPreviewCandidates { get; } = [];
    public ObservableCollection<ProviderMockupCandidateDescriptor> ProviderMockupCandidates { get; } = [];
    public ObservableCollection<OptionValueChoiceViewModel> TemplateColorChoices { get; } = [];

    public BlueprintOffering? SelectedOffering
    {
        get => _selectedOffering;
        private set
        {
            if (!SetField(ref _selectedOffering, value)) return;
            LoadOfferingFields();
            RefreshOfferingCollections();
            OnPropertyChanged(nameof(SelectedOfferingId));
            OnPropertyChanged(nameof(HasSelectedOffering));
            OnPropertyChanged(nameof(IsOfferingContextUnavailable));
            OnPropertyChanged(nameof(OfferingKindLabel));
            OnPropertyChanged(nameof(IsProviderNetworkOffering));
            OnPropertyChanged(nameof(ProviderDisplayName));
            NotifyCommands();
        }
    }

    public Guid? SelectedOfferingId => SelectedOffering?.Id;
    public bool HasSelectedOffering => SelectedOffering is not null;
    public bool IsOfferingContextUnavailable => IsAvailable && _requestedOfferingId is not null && SelectedOffering is null;
    public string OfferingKindLabel => SelectedOffering?.Kind == BlueprintOfferingKind.ProviderNetwork ? "Provider Network" : "Fixed Print Provider";
    public bool IsProviderNetworkOffering => SelectedOffering?.Kind == BlueprintOfferingKind.ProviderNetwork;
    public string ProviderDisplayName => SelectedOffering?.PrintProviderId is Guid id
        ? PrintProviders.FirstOrDefault(value => value.Id == id)?.Name ?? "Unknown Print Provider"
        : string.Empty;

    public OfferingOption? SelectedOption
    {
        get => _selectedOption;
        set
        {
            if (!SetField(ref _selectedOption, value)) return;
            IsAddingOptionValue = false;
            OptionValue = string.Empty;
            OnPropertyChanged(nameof(SelectedOptionId));
            OnPropertyChanged(nameof(AvailableValues));
            OnPropertyChanged(nameof(HasAvailableValues));
            NotifyCommands();
        }
    }

    public Guid? SelectedOptionId => SelectedOption?.Id;

    public OfferingPlaceholder? SelectedPlaceholder
    {
        get => _selectedPlaceholder;
        set
        {
            if (!SetField(ref _selectedPlaceholder, value)) return;
            OnPropertyChanged(nameof(SelectedPlaceholderId));
            NotifyCommands();
        }
    }

    public Guid? SelectedPlaceholderId => SelectedPlaceholder?.Id;

    public MockupTemplate? SelectedTemplate
    {
        get => _selectedTemplate;
        set
        {
            if (!SetField(ref _selectedTemplate, value)) return;
            OnPropertyChanged(nameof(SelectedTemplateId));
            NotifyCommands();
        }
    }

    public Guid? SelectedTemplateId => SelectedTemplate?.Id;
    public OfferingOptionValue? SelectedColor { get => _selectedColor; set { if (SetField(ref _selectedColor, value)) NotifyCommands(); } }
    public OfferingOptionValue? BulkColor { get => _bulkColor; set { if (SetField(ref _bulkColor, value)) { ResetBulkPreview(); NotifyCommands(); } } }
    public string BulkResultMessage { get => _bulkResultMessage; private set { if (SetField(ref _bulkResultMessage, value)) OnPropertyChanged(nameof(HasBulkResultMessage)); } }
    public bool HasBulkResultMessage => !string.IsNullOrWhiteSpace(BulkResultMessage);
    public bool HasBulkPreview => BulkPreviewCandidates.Count > 0;
    public ProviderMockupCandidateDescriptor? SelectedProviderMockup
    {
        get => _selectedProviderMockup;
        set
        {
            if (!SetField(ref _selectedProviderMockup, value)) return;
            if (value is not null)
            {
                MappingX = value.ImageWidth * 0.25;
                MappingY = value.ImageHeight * 0.2;
                MappingWidth = value.ImageWidth * 0.5;
                MappingHeight = value.ImageHeight * 0.6;
            }
            OnPropertyChanged(nameof(MappingImageWidth));
            OnPropertyChanged(nameof(MappingImageHeight));
            RebuildChoices();
            NotifyCommands();
        }
    }
    public string ProviderCatalogMessage { get => _providerCatalogMessage; private set { if (SetField(ref _providerCatalogMessage, value)) OnPropertyChanged(nameof(HasProviderCatalogMessage)); } }
    public bool HasProviderCatalogMessage => !string.IsNullOrWhiteSpace(ProviderCatalogMessage);
    public bool HasProviderMockupCandidates => ProviderMockupCandidates.Count > 0;
    public double MappingX { get => _mappingX; set { if (SetField(ref _mappingX, value)) NotifyCommands(); } }
    public double MappingY { get => _mappingY; set { if (SetField(ref _mappingY, value)) NotifyCommands(); } }
    public double MappingWidth { get => _mappingWidth; set { if (SetField(ref _mappingWidth, value)) NotifyCommands(); } }
    public double MappingHeight { get => _mappingHeight; set { if (SetField(ref _mappingHeight, value)) NotifyCommands(); } }
    public double MappingImageWidth => SelectedProviderMockup?.ImageWidth ?? 1;
    public double MappingImageHeight => SelectedProviderMockup?.ImageHeight ?? 1;

    public string OfferingName { get => _offeringName; set { if (SetField(ref _offeringName, value)) NotifyCommands(); } }
    public string OfferingDescription { get => _offeringDescription; set => SetField(ref _offeringDescription, value); }
    public string ProviderNetworkCode { get => _providerNetworkCode; set => SetField(ref _providerNetworkCode, value); }
    public string ExternalOfferingId { get => _externalOfferingId; set => SetField(ref _externalOfferingId, value); }
    public string OptionName { get => _optionName; set { if (SetField(ref _optionName, value)) NotifyCommands(); } }
    public string OptionValue { get => _optionValue; set { if (SetField(ref _optionValue, value)) NotifyCommands(); } }
    public string VariantName { get => _variantName; set => SetField(ref _variantName, value); }
    public string PlaceholderName { get => _placeholderName; set { if (SetField(ref _placeholderName, value)) NotifyCommands(); } }
    public string PlaceholderDescription { get => _placeholderDescription; set => SetField(ref _placeholderDescription, value); }
    public string PlaceholderPosition { get => _placeholderPosition; set { if (SetField(ref _placeholderPosition, value)) NotifyCommands(); } }
    public string PlaceholderDecorationMethod { get => _placeholderDecorationMethod; set { if (SetField(ref _placeholderDecorationMethod, value)) NotifyCommands(); } }
    public string PlaceholderWidth { get => _placeholderWidth; set { if (SetField(ref _placeholderWidth, value)) { OnPropertyChanged(nameof(PhysicalSizeSummary)); NotifyCommands(); } } }
    public string PlaceholderHeight { get => _placeholderHeight; set { if (SetField(ref _placeholderHeight, value)) { OnPropertyChanged(nameof(PhysicalSizeSummary)); NotifyCommands(); } } }
    public bool PlaceholderUsesAllVariants { get => _placeholderUsesAllVariants; set { if (SetField(ref _placeholderUsesAllVariants, value)) NotifyCommands(); } }
    public string PlaceholderProviderReference { get => _placeholderProviderReference; set => SetField(ref _placeholderProviderReference, value); }
    public string ArtworkWidth { get => _artworkWidth; set { if (SetField(ref _artworkWidth, value)) NotifyCommands(); } }
    public string ArtworkHeight { get => _artworkHeight; set { if (SetField(ref _artworkHeight, value)) NotifyCommands(); } }
    public string ArtworkDpi { get => _artworkDpi; set { if (SetField(ref _artworkDpi, value)) { OnPropertyChanged(nameof(PhysicalSizeSummary)); NotifyCommands(); } } }
    public string ArtworkFormat { get => _artworkFormat; set => SetField(ref _artworkFormat, value); }
    public string ArtworkBackground { get => _artworkBackground; set => SetField(ref _artworkBackground, value); }
    public string PhysicalSizeSummary
    {
        get
        {
            if (!int.TryParse(PlaceholderWidth, out var width) || !int.TryParse(PlaceholderHeight, out var height) || !int.TryParse(ArtworkDpi, out var dpi) || dpi <= 0)
                return "Physical size unavailable until reliable DPI is provided.";
            var size = new DesignAreaPhysicalSize(width / (double)dpi, height / (double)dpi);
            return $"{size.WidthInches:0.##} × {size.HeightInches:0.##} in · {size.WidthMillimetres:0.#} × {size.HeightMillimetres:0.#} mm";
        }
    }
    public string TemplateName { get => _templateName; set { if (SetField(ref _templateName, value)) NotifyCommands(); } }
    public OptionKind SelectedOptionKind { get => _selectedOptionKind; set => SetField(ref _selectedOptionKind, value); }

    public bool IsAddingOption { get => _isAddingOption; private set { if (SetField(ref _isAddingOption, value)) NotifyCommands(); } }
    public bool IsAddingOptionValue { get => _isAddingOptionValue; private set { if (SetField(ref _isAddingOptionValue, value)) NotifyCommands(); } }
    public bool IsAddingVariant { get => _isAddingVariant; private set { if (SetField(ref _isAddingVariant, value)) NotifyCommands(); } }
    public bool IsAddingPlaceholder { get => _isAddingPlaceholder; private set { if (SetField(ref _isAddingPlaceholder, value)) NotifyCommands(); } }
    public bool IsAddingTemplate { get => _isAddingTemplate; private set { if (SetField(ref _isAddingTemplate, value)) NotifyCommands(); } }
    public bool IsAvailable { get; private set; }
    public bool IsReadOnly { get; private set; }
    public bool CanEdit => IsAvailable && !IsReadOnly && !IsBusy;
    public bool IsBusy { get => _isBusy; private set { if (SetField(ref _isBusy, value)) { OnPropertyChanged(nameof(CanEdit)); NotifyCommands(); } } }
    public string ErrorMessage { get => _error; private set { if (SetField(ref _error, value)) OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasActiveDraft => IsAddingOption || IsAddingOptionValue || IsAddingVariant || IsAddingPlaceholder || IsAddingTemplate || _bulkPreview is not null;

    public void CancelActiveDrafts()
    {
        IsAddingOption = false;
        IsAddingOptionValue = false;
        ResetVariantDraft();
        ResetPlaceholderDraft();
        IsAddingTemplate = false;
        TemplateName = string.Empty;
        ResetBulkDraft();
    }

    public IEnumerable<OfferingOption> AvailableOptions => Options.Where(value => value.OfferingId == SelectedOffering?.Id && !value.IsArchived).OrderBy(value => value.SortOrder);
    public IEnumerable<OfferingOptionValue> AvailableValues => OptionValues.Where(value => value.OfferingId == SelectedOffering?.Id && value.OptionId == SelectedOption?.Id && !value.IsArchived).OrderBy(value => value.SortOrder);
    public IEnumerable<OfferingVariant> AvailableVariants => Variants.Where(value => value.OfferingId == SelectedOffering?.Id && !value.IsArchived);
    public IEnumerable<OfferingPlaceholder> AvailablePlaceholders => Placeholders.Where(value => value.OfferingId == SelectedOffering?.Id && !value.IsArchived);
    public IEnumerable<MockupTemplate> AvailableTemplates => Templates.Where(value => value.BlueprintOfferingId == SelectedOffering?.Id && !value.IsArchived);
    public IEnumerable<OfferingOptionValue> AvailableColors => OptionValues.Where(value => value.OfferingId == SelectedOffering?.Id && !value.IsArchived && Options.Any(option => option.Id == value.OptionId && option.OptionKind == OptionKind.Color));
    public bool HasAvailableOptions => AvailableOptions.Any();
    public bool HasAvailableValues => AvailableValues.Any();
    public bool HasAvailableVariants => AvailableVariants.Any();
    public bool HasAvailablePlaceholders => AvailablePlaceholders.Any();
    public bool HasAvailableTemplates => AvailableTemplates.Any();

    public ICommand SaveOfferingCommand { get; }
    public ICommand StartAddOptionCommand { get; }
    public ICommand CancelAddOptionCommand { get; }
    public ICommand CreateOptionCommand { get; }
    public ICommand StartAddOptionValueCommand { get; }
    public ICommand CancelAddOptionValueCommand { get; }
    public ICommand CreateOptionValueCommand { get; }
    public ICommand StartAddVariantCommand { get; }
    public ICommand CancelAddVariantCommand { get; }
    public ICommand CreateVariantCommand { get; }
    public ICommand StartAddPlaceholderCommand { get; }
    public ICommand EditPlaceholderCommand { get; }
    public ICommand CancelAddPlaceholderCommand { get; }
    public ICommand CreatePlaceholderCommand { get; }
    public ICommand SetDefaultPlaceholderCommand { get; }
    public ICommand StartAddTemplateCommand { get; }
    public ICommand EditTemplateCommand { get; }
    public ICommand CancelAddTemplateCommand { get; }
    public ICommand CreateTemplateCommand { get; }
    public ICommand AddTemplateColorCommand { get; }
    public ICommand ArchiveOptionCommand { get; }
    public ICommand ArchiveOptionValueCommand { get; }
    public ICommand ArchiveVariantCommand { get; }
    public ICommand ArchivePlaceholderCommand { get; }
    public ICommand ArchiveTemplateCommand { get; }
    public ICommand PreviewBulkVariantsCommand { get; }
    public ICommand ConfirmBulkVariantsCommand { get; }
    public ICommand CancelBulkVariantsCommand { get; }

    public async Task LoadForStoreAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var catalog = await _catalog.LoadForStoreAsync(storeId, cancellationToken).ConfigureAwait(true);
            var mockups = await _mockups.LoadForStoreAsync(storeId, cancellationToken).ConfigureAwait(true);
            IsAvailable = true;
            IsReadOnly = catalog.IsReadOnly || mockups.IsReadOnly;
            ApplyCatalog(catalog);
            Replace(Templates, mockups.Templates);
            Replace(TemplateColors, mockups.Colors);
            Replace(TemplateRevisions, mockups.Revisions);
            SelectedTemplate = Templates.FirstOrDefault(value => value.Id == SelectedTemplate?.Id) ?? AvailableTemplates.FirstOrDefault();
            await LoadProviderMockupsAsync(cancellationToken).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            IsAvailable = false;
            ErrorMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(IsAvailable));
            OnPropertyChanged(nameof(CanEdit));
            OnPropertyChanged(nameof(IsOfferingContextUnavailable));
        }
    }

    public void SelectOffering(Guid? offeringId)
    {
        _requestedOfferingId = offeringId;
        SelectedOffering = offeringId is null ? null : Offerings.FirstOrDefault(value => value.Id == offeringId.Value);
        OnPropertyChanged(nameof(IsOfferingContextUnavailable));
        _ = LoadProviderMockupsAsync();
    }

    private async Task LoadProviderMockupsAsync(CancellationToken cancellationToken = default)
    {
        Replace(ProviderMockupCandidates, []);
        SelectedProviderMockup = null;
        ProviderCatalogMessage = string.Empty;
        if (_providerCatalog is null || SelectedOffering is null)
        {
            ProviderCatalogMessage = "Provider mockup catalog data is not available.";
        }
        else
        {
            try
            {
                var descriptor = await _providerCatalog.LoadAsync(CurrentContext(), cancellationToken).ConfigureAwait(true);
                if (!descriptor.IsAvailable)
                    ProviderCatalogMessage = descriptor.UnavailableReason ?? "Provider mockup catalog data is not available.";
                else
                {
                    Replace(ProviderMockupCandidates, descriptor.AvailableMockupImages);
                    SelectedProviderMockup = ProviderMockupCandidates.FirstOrDefault();
                    if (SelectedProviderMockup is null) ProviderCatalogMessage = "This Offering has no provider mockup images.";
                }
            }
            catch (Exception exception) { ProviderCatalogMessage = exception.Message; }
        }
        OnPropertyChanged(nameof(HasProviderMockupCandidates));
        NotifyCommands();
    }

    private async Task SaveOfferingAsync()
    {
        if (SelectedOffering is null) return;
        await RunMutationAsync(() => _catalog.UpdateAsync(new UpdateCatalogRecordRequest(
            SelectedOffering.StoreId,
            CatalogRecordKind.Offering,
            SelectedOffering.Id,
            Name: OfferingName,
            Description: EmptyToNull(OfferingDescription),
            ProviderNetworkCode: IsProviderNetworkOffering ? ProviderNetworkCode : null,
            ExternalOfferingId: EmptyToNull(ExternalOfferingId)))).ConfigureAwait(true);
    }

    private async Task CreateOptionAsync()
    {
        if (SelectedOffering is null) return;
        await RunMutationAsync(() => _catalog.CreateOptionAsync(new CreateOfferingOptionRequest(SelectedOffering.Id, SelectedOptionKind, OptionName))).ConfigureAwait(true);
        if (!HasError) { IsAddingOption = false; OptionName = string.Empty; }
    }

    private async Task CreateOptionValueAsync()
    {
        if (SelectedOffering is null || SelectedOption is null) return;
        await RunMutationAsync(() => _catalog.CreateOptionValueAsync(new CreateOptionValueRequest(SelectedOffering.Id, SelectedOption.Id, OptionValue))).ConfigureAwait(true);
        if (!HasError) { IsAddingOptionValue = false; OptionValue = string.Empty; }
    }

    private async Task CreateVariantAsync()
    {
        if (SelectedOffering is null) return;
        var selected = VariantValueChoices.Where(value => value.IsSelected).Select(value => value.Value).ToArray();
        var name = string.IsNullOrWhiteSpace(VariantName)
            ? string.Join(", ", selected.Select(ValueLabel))
            : VariantName.Trim();
        await RunMutationAsync(() => _catalog.CreateVariantAsync(new CreateOfferingVariantRequest(SelectedOffering.Id, name, selected.Select(value => value.Id).ToArray()))).ConfigureAwait(true);
        if (!HasError) ResetVariantDraft();
    }

    private async Task CreatePlaceholderAsync()
    {
        if (SelectedOffering is null || !int.TryParse(PlaceholderWidth, out var width) || !int.TryParse(PlaceholderHeight, out var height)) return;
        if (_offeringManagement is not null)
        {
            int? artworkWidth = int.TryParse(ArtworkWidth, out var parsedArtworkWidth) ? parsedArtworkWidth : null;
            int? artworkHeight = int.TryParse(ArtworkHeight, out var parsedArtworkHeight) ? parsedArtworkHeight : null;
            int? dpi = int.TryParse(ArtworkDpi, out var parsedDpi) ? parsedDpi : null;
            DesignAreaArtworkGuidance? guidance = artworkWidth is not null || artworkHeight is not null || dpi is not null || !string.IsNullOrWhiteSpace(ArtworkFormat) || !string.IsNullOrWhiteSpace(ArtworkBackground)
                ? new DesignAreaArtworkGuidance(artworkWidth, artworkHeight, dpi, ArtworkFormat, ArtworkBackground)
                : null;
            IsBusy = true;
            try
            {
                var selectedVariantIds = PlaceholderVariantChoices.Where(value => value.IsSelected).Select(value => value.Variant.Id).ToArray();
                var result = SelectedPlaceholder is not null && AvailablePlaceholders.Any(value => value.Id == SelectedPlaceholder.Id)
                    ? await _offeringManagement.UpdateDesignAreaAsync(new UpdateFocusedDesignAreaRequest(
                        CurrentContext(), SelectedPlaceholder.Id, PlaceholderName, PlaceholderPosition, PlaceholderDecorationMethod,
                        width, height, selectedVariantIds, PlaceholderUsesAllVariants, EmptyToNull(PlaceholderDescription),
                        EmptyToNull(PlaceholderProviderReference), guidance)).ConfigureAwait(true)
                    : await _offeringManagement.CreateDesignAreaAsync(new CreateFocusedDesignAreaRequest(
                        CurrentContext(), PlaceholderName, PlaceholderPosition, PlaceholderDecorationMethod, width, height,
                        selectedVariantIds, PlaceholderUsesAllVariants, EmptyToNull(PlaceholderDescription), EmptyToNull(PlaceholderProviderReference), guidance)).ConfigureAwait(true);
                if (result.Succeeded) { ApplyOfferingState(result.State); ResetPlaceholderDraft(); }
                else ErrorMessage = result.Error ?? "Design Area could not be created.";
            }
            catch (Exception exception) { ErrorMessage = exception.Message; }
            finally { IsBusy = false; }
            return;
        }
        await RunMutationAsync(() => _catalog.CreatePlaceholderAsync(new CreateOfferingPlaceholderRequest(
            SelectedOffering.Id,
            PlaceholderName,
            PlaceholderPosition,
            PlaceholderDecorationMethod,
            width,
            height,
            PlaceholderVariantChoices.Where(value => value.IsSelected).Select(value => value.Variant.Id).ToArray(),
            EmptyToNull(PlaceholderDescription)))).ConfigureAwait(true);
        if (!HasError) ResetPlaceholderDraft();
    }

    private async Task SetDefaultPlaceholderAsync()
    {
        if (SelectedOffering is null || SelectedPlaceholder is null) return;
        await RunMutationAsync(() => _catalog.UpdateAsync(new UpdateCatalogRecordRequest(SelectedOffering.StoreId, CatalogRecordKind.Offering, SelectedOffering.Id, DefaultPlaceholderId: SelectedPlaceholder.Id))).ConfigureAwait(true);
    }

    private async Task CreateTemplateAsync()
    {
        if (SelectedOffering is null || SelectedPlaceholder is null) return;
        if (SelectedTemplate is not null && AvailableTemplates.Any(value => value.Id == SelectedTemplate.Id) && _offeringManagement is not null && SelectedProviderMockup is not null)
        {
            var mapping = new MockupImageSpaceMapping(SelectedProviderMockup.ImageWidth, SelectedProviderMockup.ImageHeight,
                (int)Math.Round(MappingX), (int)Math.Round(MappingY), (int)Math.Round(MappingWidth), (int)Math.Round(MappingHeight));
            await RunMockupMutationAsync(() => _mockups.UpdateTemplateAsync(new UpdateMockupTemplateRequest(
                SelectedOffering.StoreId, SelectedTemplate.Id, TemplateName, TargetPlaceholderId: SelectedPlaceholder.Id,
                ReplaceProviderImage: true, ProviderMockupReference: SelectedProviderMockup.ProviderReference,
                ImageMapping: mapping,
                ReplaceColorOptionValueIds: TemplateColorChoices.Where(value => value.IsSelected).Select(value => value.Value.Id).ToArray()))).ConfigureAwait(true);
            if (!HasError) IsAddingTemplate = false;
            return;
        }
        if (_offeringManagement is not null && SelectedProviderMockup is not null)
        {
            IsBusy = true;
            try
            {
                var mapping = new MockupImageSpaceMapping(
                    SelectedProviderMockup.ImageWidth, SelectedProviderMockup.ImageHeight,
                    (int)Math.Round(MappingX), (int)Math.Round(MappingY),
                    (int)Math.Round(MappingWidth), (int)Math.Round(MappingHeight));
                var result = await _offeringManagement.CreateMockupTemplateAsync(new CreateFocusedMockupTemplateRequest(
                    CurrentContext(), TemplateName, SelectedProviderMockup.ProviderReference, SelectedPlaceholder.Id,
                    TemplateColorChoices.Where(value => value.IsSelected).Select(value => value.Value.Id).ToArray(), mapping)).ConfigureAwait(true);
                if (result.Succeeded)
                {
                    ApplyOfferingState(result.State);
                    IsAddingTemplate = false;
                    TemplateName = string.Empty;
                    foreach (var color in TemplateColorChoices) color.IsSelected = false;
                }
                else
                {
                    ErrorMessage = result.Details is { Count: > 0 }
                        ? $"{result.Error} {string.Join(", ", result.Details)}"
                        : result.Error ?? "Mockup Template could not be created.";
                }
            }
            catch (Exception exception) { ErrorMessage = exception.Message; }
            finally { IsBusy = false; }
            return;
        }
        await RunMockupMutationAsync(() => _mockups.CreateTemplateAsync(new CreateMockupTemplateRequest(SelectedOffering.StoreId, SelectedOffering.Id, TemplateName, SelectedPlaceholder.Id))).ConfigureAwait(true);
        if (!HasError) { IsAddingTemplate = false; TemplateName = string.Empty; }
    }

    private async Task AddTemplateColorAsync()
    {
        if (SelectedOffering is null || SelectedTemplate is null || SelectedColor is null) return;
        await RunMockupMutationAsync(() => _mockups.AddColorAsync(new AddMockupTemplateColorRequest(SelectedOffering.StoreId, SelectedTemplate.Id, SelectedColor.Id))).ConfigureAwait(true);
    }

    private async Task ArchiveTemplateAsync(MockupTemplate? template)
    {
        if (template is null || SelectedOffering is null) return;
        await RunMockupMutationAsync(() => _mockups.ArchiveTemplateAsync(new ArchiveMockupTemplateRequest(SelectedOffering.StoreId, template.Id))).ConfigureAwait(true);
    }

    private void BeginEditTemplate(MockupTemplate? template)
    {
        if (template is null) return;
        SelectedTemplate = template;
        SelectedPlaceholder = AvailablePlaceholders.FirstOrDefault(value => value.Id == template.TargetPlaceholderId);
        TemplateName = template.Name;
        var revision = TemplateRevisions.SingleOrDefault(value => value.MockupTemplateId == template.Id && value.RevisionNumber == template.CurrentRevision);
        SelectedProviderMockup = ProviderMockupCandidates.FirstOrDefault(value => value.ProviderReference == revision?.ProviderMockupReference);
        if (revision?.ImageMapping is { } mapping)
        {
            MappingX = mapping.X;
            MappingY = mapping.Y;
            MappingWidth = mapping.Width;
            MappingHeight = mapping.Height;
        }
        RebuildChoices();
        var activeColorIds = TemplateColors.Where(value => value.MockupTemplateId == template.Id && !value.IsArchived).Select(value => value.ColorOptionValueId).ToHashSet();
        foreach (var color in TemplateColorChoices) color.IsSelected = activeColorIds.Contains(color.Value.Id);
        IsAddingTemplate = true;
    }

    private void BeginNewTemplate()
    {
        SelectedTemplate = null;
        SelectedPlaceholder = AvailablePlaceholders.FirstOrDefault(value => value.Id == SelectedOffering?.DefaultPlaceholderId) ?? AvailablePlaceholders.FirstOrDefault();
        TemplateName = string.Empty;
        foreach (var color in TemplateColorChoices) color.IsSelected = false;
        IsAddingTemplate = true;
    }

    private void RunArchive(object? parameter, CatalogRecordKind kind)
    {
        var id = parameter switch
        {
            OfferingOption value => value.Id,
            OfferingOptionValue value => value.Id,
            OfferingVariant value => value.Id,
            OfferingPlaceholder value => value.Id,
            _ => Guid.Empty
        };
        if (id == Guid.Empty || SelectedOffering is null) return;
        _ = RunMutationAsync(() => _catalog.ArchiveAsync(new ArchiveCatalogRecordRequest(SelectedOffering.StoreId, kind, id)));
    }

    private bool CanPreviewBulkVariants() => CanEdit && _offeringManagement is not null && BulkColor is not null && BulkSizeChoices.Any(value => value.IsSelected);

    private OfferingContext CurrentContext()
    {
        var offering = SelectedOffering ?? throw new InvalidOperationException("Select a Blueprint Offering first.");
        return new OfferingContext(offering.StoreId, offering.BlueprintId, offering.Id);
    }

    private async Task PreviewBulkVariantsAsync()
    {
        if (_offeringManagement is null || BulkColor is null) return;
        IsBusy = true;
        try
        {
            _bulkPreview = await _offeringManagement.PreviewBulkVariantsAsync(new BulkVariantRequest(
                CurrentContext(), BulkColor.Id,
                BulkSizeChoices.Where(value => value.IsSelected).Select(value => value.Value.Id).ToArray())).ConfigureAwait(true);
            Replace(BulkPreviewCandidates, _bulkPreview.Candidates);
            BulkResultMessage = _bulkPreview.Message ?? string.Empty;
            OnPropertyChanged(nameof(HasBulkPreview));
        }
        catch (Exception exception) { BulkResultMessage = exception.Message; }
        finally { IsBusy = false; NotifyCommands(); }
    }

    private async Task ConfirmBulkVariantsAsync()
    {
        if (_offeringManagement is null || _bulkPreview is null) return;
        IsBusy = true;
        try
        {
            var result = await _offeringManagement.ConfirmBulkVariantsAsync(_bulkPreview.Request).ConfigureAwait(true);
            BulkResultMessage = result.Succeeded
                ? $"Created {result.CreatedVariants.Count} sellable Variant{(result.CreatedVariants.Count == 1 ? string.Empty : "s")}."
                : result.Error ?? "No Variants were created.";
            if (result.Succeeded)
            {
                ApplyOfferingState(await _offeringManagement.LoadOfferingAsync(CurrentContext()).ConfigureAwait(true));
                _bulkPreview = null;
                Replace(BulkPreviewCandidates, []);
                OnPropertyChanged(nameof(HasBulkPreview));
            }
        }
        catch (Exception exception) { BulkResultMessage = exception.Message; }
        finally { IsBusy = false; NotifyCommands(); }
    }

    private void ApplyOfferingState(OfferingManagementState state)
    {
        Replace(Options, Options.Where(value => value.OfferingId != state.Offering.Id).Concat(state.Options));
        Replace(OptionValues, OptionValues.Where(value => value.OfferingId != state.Offering.Id).Concat(state.OptionValues));
        Replace(Variants, Variants.Where(value => value.OfferingId != state.Offering.Id).Concat(state.Variants));
        Replace(Placeholders, Placeholders.Where(value => value.OfferingId != state.Offering.Id).Concat(state.DesignAreas));
        Replace(Templates, Templates.Where(value => value.BlueprintOfferingId != state.Offering.Id).Concat(state.MockupTemplates));
        RefreshOfferingCollections();
    }

    private void ResetBulkDraft()
    {
        _bulkColor = null;
        OnPropertyChanged(nameof(BulkColor));
        foreach (var size in BulkSizeChoices) size.IsSelected = false;
        ResetBulkPreview();
    }

    private void ResetBulkPreview()
    {
        _bulkPreview = null;
        Replace(BulkPreviewCandidates, []);
        BulkResultMessage = string.Empty;
        OnPropertyChanged(nameof(HasBulkPreview));
    }

    private async Task RunMutationAsync(Func<Task<CatalogSetupResult>> mutation)
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var result = await mutation().ConfigureAwait(true);
            if (!result.Succeeded) ErrorMessage = result.Error ?? "Catalog change failed.";
            else ApplyCatalog(result.State);
        }
        catch (Exception exception) { ErrorMessage = exception.Message; }
        finally { IsBusy = false; }
    }

    private async Task RunMockupMutationAsync(Func<Task<MockupTemplateSetupResult>> mutation)
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var result = await mutation().ConfigureAwait(true);
            if (!result.Succeeded) ErrorMessage = result.Error ?? "Mockup Template change failed.";
            else ApplyMockups(result.State);
        }
        catch (Exception exception) { ErrorMessage = exception.Message; }
        finally { IsBusy = false; }
    }

    private void ApplyCatalog(CatalogSetupState state)
    {
        Replace(Blueprints, state.Blueprints);
        Replace(PrintProviders, state.PrintProviders);
        Replace(Offerings, state.Offerings);
        Replace(Options, state.Options);
        Replace(OptionValues, state.OptionValues);
        Replace(Variants, state.Variants);
        Replace(Placeholders, state.Placeholders);
        SelectedOffering = ResolveSelectedOffering();
        SelectedOption = AvailableOptions.FirstOrDefault(value => value.Id == SelectedOption?.Id) ?? AvailableOptions.FirstOrDefault();
        SelectedPlaceholder = AvailablePlaceholders.FirstOrDefault(value => value.Id == SelectedPlaceholder?.Id)
            ?? AvailablePlaceholders.FirstOrDefault(value => value.Id == SelectedOffering?.DefaultPlaceholderId)
            ?? AvailablePlaceholders.FirstOrDefault();
        RefreshOfferingCollections();
    }

    private void ApplyMockups(MockupTemplateSetupState state)
    {
        Replace(Templates, state.Templates);
        Replace(TemplateColors, state.Colors);
        Replace(TemplateRevisions, state.Revisions);
        SelectedTemplate = AvailableTemplates.FirstOrDefault(value => value.Id == SelectedTemplate?.Id) ?? AvailableTemplates.FirstOrDefault();
        RefreshOfferingCollections();
    }

    private BlueprintOffering? ResolveSelectedOffering()
    {
        if (_requestedOfferingId is Guid requestedOfferingId)
            return Offerings.FirstOrDefault(value => value.Id == requestedOfferingId);
        return Offerings.FirstOrDefault(value => value.Id == SelectedOffering?.Id) ?? Offerings.FirstOrDefault();
    }

    private void LoadOfferingFields()
    {
        OfferingName = SelectedOffering?.Name ?? string.Empty;
        OfferingDescription = SelectedOffering?.Description ?? string.Empty;
        ProviderNetworkCode = SelectedOffering?.ProviderNetworkCode ?? string.Empty;
        ExternalOfferingId = SelectedOffering?.ExternalOfferingId ?? string.Empty;
    }

    private void RefreshOfferingCollections()
    {
        OnPropertyChanged(nameof(AvailableOptions));
        OnPropertyChanged(nameof(AvailableValues));
        OnPropertyChanged(nameof(AvailableVariants));
        OnPropertyChanged(nameof(AvailablePlaceholders));
        OnPropertyChanged(nameof(AvailableTemplates));
        OnPropertyChanged(nameof(AvailableColors));
        OnPropertyChanged(nameof(HasAvailableOptions));
        OnPropertyChanged(nameof(HasAvailableValues));
        OnPropertyChanged(nameof(HasAvailableVariants));
        OnPropertyChanged(nameof(HasAvailablePlaceholders));
        OnPropertyChanged(nameof(HasAvailableTemplates));
        RebuildChoices();
    }

    private void RebuildChoices()
    {
        var selectedValueIds = VariantValueChoices.Where(value => value.IsSelected).Select(value => value.Value.Id).ToHashSet();
        var valueChoices = OptionValues
            .Where(value => value.OfferingId == SelectedOffering?.Id && !value.IsArchived)
            .Select(value => new OptionValueChoiceViewModel(value, ValueLabel(value)) { IsSelected = selectedValueIds.Contains(value.Id) })
            .ToArray();
        foreach (var choice in valueChoices) choice.PropertyChanged += ChoiceSelectionChanged;
        Replace(VariantValueChoices, valueChoices);

        var selectedVariantIds = PlaceholderVariantChoices.Where(value => value.IsSelected).Select(value => value.Variant.Id).ToHashSet();
        var variantChoices = AvailableVariants
            .Select(value => new VariantChoiceViewModel(value) { IsSelected = selectedVariantIds.Contains(value.Id) })
            .ToArray();
        foreach (var choice in variantChoices) choice.PropertyChanged += ChoiceSelectionChanged;
        Replace(PlaceholderVariantChoices, variantChoices);

        var selectedSizeIds = BulkSizeChoices.Where(value => value.IsSelected).Select(value => value.Value.Id).ToHashSet();
        var sizeOptionIds = Options.Where(value => value.OfferingId == SelectedOffering?.Id && !value.IsArchived && value.OptionKind == OptionKind.Size).Select(value => value.Id).ToHashSet();
        var sizeChoices = OptionValues
            .Where(value => value.OfferingId == SelectedOffering?.Id && !value.IsArchived && sizeOptionIds.Contains(value.OptionId))
            .Select(value => new BulkSizeChoiceViewModel(value) { IsSelected = selectedSizeIds.Contains(value.Id) })
            .ToArray();
        foreach (var choice in sizeChoices) choice.PropertyChanged += ChoiceSelectionChanged;
        Replace(BulkSizeChoices, sizeChoices);
        BulkColor = AvailableColors.FirstOrDefault(value => value.Id == BulkColor?.Id);

        var selectedTemplateColorIds = TemplateColorChoices.Where(value => value.IsSelected).Select(value => value.Value.Id).ToHashSet();
        var supportedColors = SelectedProviderMockup?.SupportedColorOptionValueIds;
        var templateColors = AvailableColors
            .Where(value => supportedColors is null || supportedColors.Contains(value.Id))
            .Select(value => new OptionValueChoiceViewModel(value, value.Value) { IsSelected = selectedTemplateColorIds.Contains(value.Id) })
            .ToArray();
        foreach (var choice in templateColors) choice.PropertyChanged += ChoiceSelectionChanged;
        Replace(TemplateColorChoices, templateColors);
    }

    private void ChoiceSelectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectableCatalogRecord.IsSelected))
        {
            ResetBulkPreview();
            NotifyCommands();
        }
    }

    private string ValueLabel(OfferingOptionValue value)
    {
        var option = Options.First(option => option.Id == value.OptionId);
        return $"{option.Name}: {value.Value}";
    }

    private bool CanCreatePlaceholder()
    {
        return CanEdit && IsAddingPlaceholder && SelectedOffering is not null
            && !string.IsNullOrWhiteSpace(PlaceholderName)
            && !string.IsNullOrWhiteSpace(PlaceholderPosition)
            && !string.IsNullOrWhiteSpace(PlaceholderDecorationMethod)
            && int.TryParse(PlaceholderWidth, out var width) && width > 0
            && int.TryParse(PlaceholderHeight, out var height) && height > 0
            && (PlaceholderUsesAllVariants ? HasAvailableVariants : PlaceholderVariantChoices.Any(value => value.IsSelected))
            && OptionalPositivePair(ArtworkWidth, ArtworkHeight)
            && (string.IsNullOrWhiteSpace(ArtworkDpi) || int.TryParse(ArtworkDpi, out var dpi) && dpi > 0);
    }

    private bool CanCreateTemplate()
    {
        var common = CanEdit && IsAddingTemplate && SelectedOffering is not null && SelectedPlaceholder is not null && !string.IsNullOrWhiteSpace(TemplateName);
        if (!common) return false;
        if (_offeringManagement is null) return true;
        return SelectedProviderMockup is not null
            && TemplateColorChoices.Any(value => value.IsSelected)
            && MappingX >= 0 && MappingY >= 0 && MappingWidth > 0 && MappingHeight > 0
            && MappingX + MappingWidth <= MappingImageWidth
            && MappingY + MappingHeight <= MappingImageHeight;
    }

    private static bool OptionalPositivePair(string width, string height) =>
        string.IsNullOrWhiteSpace(width) && string.IsNullOrWhiteSpace(height)
        || int.TryParse(width, out var parsedWidth) && parsedWidth > 0 && int.TryParse(height, out var parsedHeight) && parsedHeight > 0;

    private void ResetVariantDraft()
    {
        IsAddingVariant = false;
        VariantName = string.Empty;
        foreach (var value in VariantValueChoices) value.IsSelected = false;
        NotifyCommands();
    }

    private void ResetPlaceholderDraft()
    {
        IsAddingPlaceholder = false;
        PlaceholderName = string.Empty;
        PlaceholderDescription = string.Empty;
        PlaceholderPosition = string.Empty;
        PlaceholderDecorationMethod = string.Empty;
        PlaceholderWidth = string.Empty;
        PlaceholderHeight = string.Empty;
        PlaceholderUsesAllVariants = true;
        PlaceholderProviderReference = string.Empty;
        ArtworkWidth = string.Empty;
        ArtworkHeight = string.Empty;
        ArtworkDpi = string.Empty;
        ArtworkFormat = string.Empty;
        ArtworkBackground = string.Empty;
        foreach (var value in PlaceholderVariantChoices) value.IsSelected = false;
        NotifyCommands();
    }

    private void BeginNewDesignArea()
    {
        ResetPlaceholderDraft();
        SelectedPlaceholder = null;
        IsAddingPlaceholder = true;
    }

    private void BeginEditDesignArea(OfferingPlaceholder? area)
    {
        if (area is null) return;
        SelectedPlaceholder = area;
        PlaceholderName = area.Name;
        PlaceholderDescription = area.Description ?? string.Empty;
        PlaceholderPosition = area.Position;
        PlaceholderDecorationMethod = area.DecorationMethod;
        PlaceholderWidth = area.Width.ToString();
        PlaceholderHeight = area.Height.ToString();
        PlaceholderProviderReference = area.ProviderReference ?? string.Empty;
        ArtworkWidth = area.ArtworkGuidance?.RecommendedWidthPixels?.ToString() ?? string.Empty;
        ArtworkHeight = area.ArtworkGuidance?.RecommendedHeightPixels?.ToString() ?? string.Empty;
        ArtworkDpi = area.ArtworkGuidance?.DotsPerInch?.ToString() ?? string.Empty;
        ArtworkFormat = area.ArtworkGuidance?.FileFormat ?? string.Empty;
        ArtworkBackground = area.ArtworkGuidance?.Background ?? string.Empty;
        var activeIds = AvailableVariants.Select(value => value.Id).ToHashSet();
        PlaceholderUsesAllVariants = activeIds.SetEquals(area.VariantIds);
        foreach (var choice in PlaceholderVariantChoices) choice.IsSelected = area.VariantIds.Contains(choice.Variant.Id);
        IsAddingPlaceholder = true;
    }

    private void NotifyCommands()
    {
        foreach (var command in new ICommand[]
        {
            SaveOfferingCommand, StartAddOptionCommand, CreateOptionCommand, StartAddOptionValueCommand,
            CreateOptionValueCommand, StartAddVariantCommand, CreateVariantCommand, StartAddPlaceholderCommand,
            CreatePlaceholderCommand, SetDefaultPlaceholderCommand, StartAddTemplateCommand, CreateTemplateCommand,
            AddTemplateColorCommand, PreviewBulkVariantsCommand, ConfirmBulkVariantsCommand
        })
        {
            switch (command)
            {
                case AsyncRelayCommand asyncCommand: asyncCommand.NotifyCanExecuteChanged(); break;
                case RelayCommand relayCommand: relayCommand.NotifyCanExecuteChanged(); break;
            }
        }
    }

    private static string? EmptyToNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values) { target.Clear(); foreach (var value in values) target.Add(value); }
    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null) { if (EqualityComparer<T>.Default.Equals(field, value)) return false; field = value; OnPropertyChanged(name); return true; }
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.App.Settings;
using FusionCanvas.Application.Catalog;
using FusionCanvas.Application.Mockups;
using FusionCanvas.App.Assets;
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

public sealed class LocalMockupSourceDraftViewModel(string path, IReadOnlyList<Guid> optionValueIds, bool isManaged = false, MockupImageSpaceMapping? mapping = null, int imageWidth = 0, int imageHeight = 0, Guid? sourceImageId = null, string? previewPath = null) : INotifyPropertyChanged
{
    private static (int Width, int Height) ReadPreviewDimensions(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                using var bitmap = new Bitmap(path);
                return (bitmap.PixelSize.Width, bitmap.PixelSize.Height);
            }
        }
        catch { }
        return (0, 0);
    }

    private readonly (int Width, int Height) _previewDimensions = imageWidth > 0 && imageHeight > 0 ? (imageWidth, imageHeight) : mapping is not null ? (mapping.ImageWidth, mapping.ImageHeight) : ReadPreviewDimensions(previewPath ?? path);
    public event PropertyChangedEventHandler? PropertyChanged;
    public string Path { get; } = path;
    public string DisplayName => System.IO.Path.GetFileName(Path);
    public IReadOnlyList<Guid> OptionValueIds { get; private set; } = optionValueIds;
    public bool IsManaged { get; } = isManaged;
    public Guid? SourceImageId { get; } = sourceImageId;
    public string PreviewPath { get; } = previewPath ?? path;
    public MockupImageSpaceMapping? Mapping { get; private set; } = mapping;
    public int ImageWidth => _previewDimensions.Width;
    public int ImageHeight => _previewDimensions.Height;
    public string ApplicabilitySummary { get; set; } = string.Empty;
    public bool IsComplete => OptionValueIds.Count > 0 && Mapping is not null;
    public string StatusLabel => IsComplete ? "Complete" : "Needs setup";
    private bool _isSelected;
    public bool IsSelected { get => _isSelected; set { if (_isSelected == value) return; _isSelected = value; PropertyChanged?.Invoke(this, new(nameof(IsSelected))); } }
    public void UpdateMetadata(IReadOnlyList<Guid> optionValueIds, MockupImageSpaceMapping? mapping, string summary)
    {
        OptionValueIds = optionValueIds;
        Mapping = mapping;
        ApplicabilitySummary = summary;
    }
}

/// <summary>Presentation state for the authoritative normalized Blueprint Offering editor.</summary>
public sealed class CatalogSetupViewModel : INotifyPropertyChanged
{
    private readonly ICatalogSetupService _catalog;
    private readonly IMockupTemplateSetupService _mockups;
    private readonly IOfferingManagementService? _offeringManagement;
    private readonly IProviderCatalogCandidateSource? _providerCatalog;
    private readonly IMockupTemplateSourceImageService? _sourceImages;
    private IAssetFilePicker _filePicker;
    private BlueprintOffering? _selectedOffering;
    private OfferingOption? _selectedOption;
    private PrintProvider? _selectedPrintProvider;
    private OfferingPlaceholder? _selectedPlaceholder;
    private MockupTemplate? _selectedTemplate;
    private OfferingOptionValue? _selectedColor;
    private Guid? _requestedOfferingId;
    private string _offeringName = string.Empty;
    private string _offeringDescription = string.Empty;
    private string _providerNetworkCode = string.Empty;
    private string _newPrintProviderName = string.Empty;
    private string _externalOfferingId = string.Empty;
    private string _optionName = string.Empty;
    private string _optionValue = string.Empty;
    private OfferingOptionValue? _editingOptionValue;
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
    private string _localSourcePath = string.Empty;
    private LocalMockupSourceDraftViewModel? _selectedLocalSource;
    private LocalMockupSourceDraftViewModel? _selectedMappingSource;
    private string _error = string.Empty;
    private bool _isBusy;
    private bool _isAddingOption;
    private bool _isAddingPrintProvider;
    private bool _isAddingOptionValue;
    private bool _isEditingOptionValue;
    private bool _isManagingOptionValues;
    private bool _isAddingVariant;
    private bool _isAddingBulkVariants;
    private bool _isAddingPlaceholder;
    private bool _isAddingTemplate;
    private OptionKind _selectedOptionKind = OptionKind.Color;
    private OfferingOptionValue? _bulkColor;
    private string _bulkResultMessage = string.Empty;
    private BulkVariantPreview? _bulkPreview;
    private ProviderMockupCandidateDescriptor? _selectedProviderMockup;
    private string _providerCatalogMessage = string.Empty;
    private ProviderCatalogLoadState _providerCatalogState = ProviderCatalogLoadState.Unavailable;
    private double _mappingX;
    private double _mappingY;
    private double _mappingWidth = 100;
    private double _mappingHeight = 100;
    private string _mappingXText = string.Empty;
    private string _mappingYText = string.Empty;
    private string _mappingWidthText = string.Empty;
    private string _mappingHeightText = string.Empty;
    private Guid? _pendingDesignAreaArchiveId;
    private string _pendingDesignAreaArchiveName = string.Empty;
    private bool _isDesignAreaArchiveConfirmationVisible;
    private bool _isMockupTemplateDiscardConfirmationVisible;
    private MockupTemplateDraftState? _mockupTemplateDraftBaseline;
    private bool _isDesignAreaDiscardConfirmationVisible;
    private DesignAreaDraftState? _designAreaDraftBaseline;

    public CatalogSetupViewModel(ICatalogSetupService catalog, IMockupTemplateSetupService mockups, IOfferingManagementService? offeringManagement = null, IProviderCatalogCandidateSource? providerCatalog = null, IMockupTemplateSourceImageService? sourceImages = null, IAssetFilePicker? filePicker = null)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _mockups = mockups ?? throw new ArgumentNullException(nameof(mockups));
        _offeringManagement = offeringManagement;
        _providerCatalog = providerCatalog;
        _sourceImages = sourceImages;
        _filePicker = filePicker ?? new NullAssetFilePicker();

        SaveOfferingCommand = new AsyncRelayCommand(SaveOfferingAsync, CanSaveOffering);
        StartAddPrintProviderCommand = new RelayCommand(_ => IsAddingPrintProvider = true, () => CanEdit && SelectedOffering is not null && !IsProviderNetworkOffering);
        CancelAddPrintProviderCommand = new RelayCommand(_ => { IsAddingPrintProvider = false; NewPrintProviderName = string.Empty; });
        CreatePrintProviderCommand = new AsyncRelayCommand(CreatePrintProviderAsync, () => CanEdit && IsAddingPrintProvider && !string.IsNullOrWhiteSpace(NewPrintProviderName));
        StartAddOptionCommand = new RelayCommand(_ => IsAddingOption = true, () => CanEdit && SelectedOffering is not null);
        ManageOptionCommand = new RelayCommand(parameter => BeginManageOptionValues(parameter as OfferingOption), () => CanEdit);
        CloseOptionValueManagementCommand = new RelayCommand(_ => CloseOptionValueManagement());
        CancelAddOptionCommand = new RelayCommand(_ => { IsAddingOption = false; OptionName = string.Empty; });
        CreateOptionCommand = new AsyncRelayCommand(CreateOptionAsync, () => CanEdit && IsAddingOption && SelectedOffering is not null && !string.IsNullOrWhiteSpace(OptionName));
        StartAddOptionValueCommand = new RelayCommand(_ => BeginAddOptionValue(), () => CanEdit && SelectedOption is not null);
        CancelAddOptionValueCommand = new RelayCommand(_ => { IsAddingOptionValue = false; OptionValue = string.Empty; });
        CreateOptionValueCommand = new AsyncRelayCommand(CreateOptionValueAsync, () => CanEdit && IsAddingOptionValue && SelectedOffering is not null && SelectedOption is not null && !string.IsNullOrWhiteSpace(OptionValue));
        EditOptionValueCommand = new RelayCommand(parameter => BeginEditOptionValue(parameter as OfferingOptionValue), () => CanEdit && SelectedOption is not null);
        SaveOptionValueEditCommand = new AsyncRelayCommand(SaveOptionValueEditAsync, () => CanEdit && IsEditingOptionValue && _editingOptionValue is not null && !string.IsNullOrWhiteSpace(OptionValue));
        CancelOptionValueEditCommand = new RelayCommand(_ => CancelOptionValueEdit());
        StartAddVariantCommand = new RelayCommand(_ => BeginVariantDraft(false), () => CanEdit && AvailableOptions.Any() && VariantValueChoices.Count > 0);
        StartBulkVariantsCommand = new RelayCommand(_ => BeginVariantDraft(true), () => CanEdit && AvailableColors.Any() && BulkSizeChoices.Count > 0);
        CancelAddVariantCommand = new RelayCommand(_ => { ResetVariantDraft(); VariantActionsFocusRequested?.Invoke(this, EventArgs.Empty); });
        CreateVariantCommand = new AsyncRelayCommand(CreateVariantAsync, () => CanEdit && IsAddingVariant && VariantValueChoices.Any(value => value.IsSelected));
        StartAddPlaceholderCommand = new RelayCommand(_ => BeginNewDesignArea(), () => CanEdit && AvailableVariants.Any());
        EditPlaceholderCommand = new RelayCommand(parameter => BeginEditDesignArea(parameter switch
        {
            DesignAreaCardViewModel card => Placeholders.FirstOrDefault(value => value.Id == card.Id),
            OfferingPlaceholder area => area,
            _ => null
        }), () => CanEdit);
        CancelAddPlaceholderCommand = new RelayCommand(_ => { ResetPlaceholderDraft(); SelectedPlaceholder = AvailablePlaceholders.FirstOrDefault(); });
        RequestCancelDesignAreaCommand = new RelayCommand(_ => RequestCancelDesignArea(), () => IsAddingPlaceholder);
        ConfirmDiscardDesignAreaCommand = new RelayCommand(_ => ConfirmDiscardDesignArea(), () => IsDesignAreaDiscardConfirmationVisible);
        KeepEditingDesignAreaCommand = new RelayCommand(_ => IsDesignAreaDiscardConfirmationVisible = false, () => IsDesignAreaDiscardConfirmationVisible);
        CreatePlaceholderCommand = new AsyncRelayCommand(CreatePlaceholderAsync, CanCreatePlaceholder);
        SetDefaultPlaceholderCommand = new AsyncRelayCommand(SetDefaultPlaceholderAsync, () => CanEdit && SelectedOffering is not null && SelectedPlaceholder is not null);
        StartAddTemplateCommand = new RelayCommand(_ => BeginNewTemplate(), () => CanEdit && SelectedOffering is not null);
        EditTemplateCommand = new RelayCommand(parameter => BeginEditTemplate(parameter switch
        {
            MockupTemplateCardViewModel card => Templates.FirstOrDefault(value => value.Id == card.Id),
            MockupTemplate template => template,
            _ => null
        }), () => CanEdit);
        CancelAddTemplateCommand = new RelayCommand(_ => ResetTemplateDraft());
        RequestCancelMockupTemplateCommand = new RelayCommand(_ => RequestCancelMockupTemplate(), () => IsAddingTemplate);
        ConfirmDiscardMockupTemplateCommand = new RelayCommand(_ => ConfirmDiscardMockupTemplate(), () => IsMockupTemplateDiscardConfirmationVisible);
        KeepEditingMockupTemplateCommand = new RelayCommand(_ => IsMockupTemplateDiscardConfirmationVisible = false, () => IsMockupTemplateDiscardConfirmationVisible);
        CreateTemplateCommand = new AsyncRelayCommand(CreateTemplateAsync, CanCreateTemplate);
        BrowseLocalSourceCommand = new AsyncRelayCommand(BrowseLocalSourceAsync, () => CanEdit && IsAddingTemplate && _sourceImages is not null);
        RemoveLocalSourceCommand = new RelayCommand(parameter => RemoveLocalSource(parameter as LocalMockupSourceDraftViewModel), () => CanEdit && IsAddingTemplate);
        SelectLocalSourceCommand = new RelayCommand(parameter => SelectLocalSource(parameter as LocalMockupSourceDraftViewModel), () => CanEdit && IsAddingTemplate);
        ReuseMappingCommand = new RelayCommand(parameter => ReuseMapping(parameter as LocalMockupSourceDraftViewModel), () => CanEdit && HasSelectedLocalSource);
        SelectAllTemplateSizesCommand = new RelayCommand(_ => SelectAllTemplateSizes(), () => CanEdit && IsAddingTemplate && TemplateAdditionalOptionChoices.Any(value => IsSizeValue(value.Value)));
        AddTemplateColorCommand = new AsyncRelayCommand(AddTemplateColorAsync, () => CanEdit && SelectedTemplate is not null && SelectedColor is not null);
        ArchiveOptionCommand = new RelayCommand(parameter => RunArchive(parameter, CatalogRecordKind.Option));
        ArchiveOptionValueCommand = new RelayCommand(parameter => RunArchive(parameter, CatalogRecordKind.OptionValue));
        ArchiveVariantCommand = new RelayCommand(parameter => RunArchive(parameter, CatalogRecordKind.Variant));
        ArchivePlaceholderCommand = new RelayCommand(parameter => RequestDesignAreaArchive(parameter));
        ConfirmDesignAreaArchiveCommand = new AsyncRelayCommand(ConfirmDesignAreaArchiveAsync, () => CanEdit && _isDesignAreaArchiveConfirmationVisible);
        CancelDesignAreaArchiveCommand = new RelayCommand(_ => CancelDesignAreaArchive(), () => _isDesignAreaArchiveConfirmationVisible);
        ArchiveTemplateCommand = new RelayCommand(parameter => _ = ArchiveTemplateAsync(parameter switch
        {
            MockupTemplateCardViewModel card => Templates.FirstOrDefault(value => value.Id == card.Id),
            MockupTemplate template => template,
            _ => null
        }), () => CanEdit);
        PreviewBulkVariantsCommand = new AsyncRelayCommand(PreviewBulkVariantsAsync, CanPreviewBulkVariants);
        ConfirmBulkVariantsCommand = new AsyncRelayCommand(ConfirmBulkVariantsAsync, () => CanEdit && _bulkPreview?.CanConfirm == true);
        CancelBulkVariantsCommand = new RelayCommand(_ => { ResetBulkDraft(); IsAddingBulkVariants = false; BulkVariantActionFocusRequested?.Invoke(this, EventArgs.Empty); });
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? OptionValueEditorFocusRequested;
    public event EventHandler? OptionValueManagementRequested;
    public event EventHandler? OptionChoiceFocusRequested;
    public event EventHandler? AddVariantRequested;
    public event EventHandler? VariantActionsFocusRequested;
    public event EventHandler? BulkVariantsRequested;
    public event EventHandler? BulkVariantActionFocusRequested;
    public event EventHandler? DesignAreaArchiveConfirmationRequested;
    public event EventHandler? DesignAreaArchiveFocusRequested;
    public event EventHandler? MockupTemplateEditorRequested;
    public event EventHandler? DesignAreaEditorRequested;

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
    public ObservableCollection<OfferingChoiceGroupViewModel> AvailableChoiceGroups { get; } = [];
    public ObservableCollection<SellableVariantRowViewModel> SellableVariantRows { get; } = [];
    public ObservableCollection<DesignAreaCardViewModel> DesignAreaCards { get; } = [];
    public ObservableCollection<MockupTemplateCardViewModel> MockupTemplateCards { get; } = [];
    public ObservableCollection<OptionValueChoiceViewModel> VariantValueChoices { get; } = [];
    public ObservableCollection<VariantChoiceViewModel> PlaceholderVariantChoices { get; } = [];
    public ObservableCollection<BulkSizeChoiceViewModel> BulkSizeChoices { get; } = [];
    public ObservableCollection<BulkVariantCandidate> BulkPreviewCandidates { get; } = [];
    public ObservableCollection<ProviderMockupCandidateDescriptor> ProviderMockupCandidates { get; } = [];
    public ObservableCollection<OptionValueChoiceViewModel> TemplateColorChoices { get; } = [];
    public ObservableCollection<OptionValueChoiceViewModel> TemplateAdditionalOptionChoices { get; } = [];
    public ObservableCollection<LocalMockupSourceDraftViewModel> LocalSourceDrafts { get; } = [];
    public ObservableCollection<LocalMockupSourceDraftViewModel> MappedSourceChoices { get; } = [];
    private readonly List<LocalMockupSourceDraftViewModel> _archivedLocalSourceDrafts = [];
    public ICommand BrowseLocalSourceCommand { get; }
    public ICommand RemoveLocalSourceCommand { get; }
    public ICommand SelectLocalSourceCommand { get; }
    public ICommand ReuseMappingCommand { get; }
    public ICommand SelectAllTemplateSizesCommand { get; }
    public IAssetFilePicker FilePicker { get => _filePicker; set => _filePicker = value ?? new NullAssetFilePicker(); }
    public string LocalSourcePath { get => _localSourcePath; private set { if (SetField(ref _localSourcePath, value)) { NotifyMockupTemplateDraftChanged(); NotifyCommands(); } } }
    public LocalMockupSourceDraftViewModel? SelectedLocalSource { get => _selectedLocalSource; private set { if (SetField(ref _selectedLocalSource, value)) { OnPropertyChanged(nameof(HasSelectedLocalSource)); OnPropertyChanged(nameof(MappingImageWidth)); OnPropertyChanged(nameof(MappingImageHeight)); OnPropertyChanged(nameof(SelectedImagePreviewPath)); RebuildMappedSourceChoices(); NotifyCommands(); } } }
    public LocalMockupSourceDraftViewModel? SelectedMappingSource { get => _selectedMappingSource; set => SetField(ref _selectedMappingSource, value); }
    public string? SelectedImagePreviewPath => SelectedLocalSource?.PreviewPath;
    public bool HasSelectedLocalSource => SelectedLocalSource is not null;
    public bool HasLocalSource => LocalSourceDrafts.Count > 0 || !string.IsNullOrWhiteSpace(LocalSourcePath);

    public BlueprintOffering? SelectedOffering
    {
        get => _selectedOffering;
        private set
        {
            if (!SetField(ref _selectedOffering, value)) return;
            if (IsManagingOptionValues)
            {
                ResetOptionValueManagement();
            }
            if (IsAddingVariant || IsAddingBulkVariants)
            {
                ResetVariantCreation();
            }
            if (IsAddingTemplate)
            {
                ResetTemplateDraft();
            }
            if (IsAddingPlaceholder)
            {
                ResetPlaceholderDraft();
            }
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
    public IEnumerable<PrintProvider> AvailablePrintProviders => PrintProviders.Where(value => !value.IsArchived).OrderBy(value => value.Name, StringComparer.OrdinalIgnoreCase);
    public PrintProvider? SelectedPrintProvider
    {
        get => _selectedPrintProvider;
        set
        {
            if (!SetField(ref _selectedPrintProvider, value)) return;
            OnPropertyChanged(nameof(ProviderDisplayName));
            NotifyCommands();
        }
    }

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
            OnPropertyChanged(nameof(ManageOptionValuesDialogTitle));
            NotifyCommands();
        }
    }

    public Guid? SelectedOptionId => SelectedOption?.Id;
    public string ManageOptionValuesDialogTitle => SelectedOption is { } option ? $"Manage {option.Name} values" : "Manage values";

    public OfferingPlaceholder? SelectedPlaceholder
    {
        get => _selectedPlaceholder;
        set
        {
            if (!SetField(ref _selectedPlaceholder, value)) return;
            OnPropertyChanged(nameof(SelectedPlaceholderId));
            NotifyMockupTemplateDraftChanged();
            OnPropertyChanged(nameof(IsEditingDesignArea));
            OnPropertyChanged(nameof(DesignAreaEditorDialogTitle));
            NotifyCommands();
        }
    }

    public Guid? SelectedPlaceholderId => SelectedPlaceholder?.Id;
    public bool IsEditingDesignArea => IsAddingPlaceholder && SelectedPlaceholder is not null;
    public string DesignAreaEditorDialogTitle => IsEditingDesignArea ? "Edit Design Area" : "Add Design Area";

    public MockupTemplate? SelectedTemplate
    {
        get => _selectedTemplate;
        set
        {
            if (!SetField(ref _selectedTemplate, value)) return;
            OnPropertyChanged(nameof(SelectedTemplateId));
            OnPropertyChanged(nameof(IsEditingMockupTemplate));
            OnPropertyChanged(nameof(MockupTemplateEditorDialogTitle));
            NotifyMockupTemplateDraftChanged();
            NotifyCommands();
        }
    }

    public Guid? SelectedTemplateId => SelectedTemplate?.Id;
    public bool IsEditingMockupTemplate => IsAddingTemplate && SelectedTemplate is not null;
    public string MockupTemplateEditorDialogTitle => IsEditingMockupTemplate ? "Edit Mockup Template" : "Add Mockup Template";
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
            else
            {
                MappingXText = string.Empty;
                MappingYText = string.Empty;
                MappingWidthText = string.Empty;
                MappingHeightText = string.Empty;
            }
            OnPropertyChanged(nameof(MappingImageWidth));
            OnPropertyChanged(nameof(MappingImageHeight));
            OnPropertyChanged(nameof(HasSelectedProviderMockup));
            OnPropertyChanged(nameof(MockupPreviewUnavailableMessage));
            RebuildChoices();
            NotifyMockupTemplateDraftChanged();
            NotifyCommands();
        }
    }
    public string ProviderCatalogMessage { get => _providerCatalogMessage; private set { if (SetField(ref _providerCatalogMessage, value)) { OnPropertyChanged(nameof(HasProviderCatalogMessage)); OnPropertyChanged(nameof(MockupPreviewUnavailableMessage)); OnPropertyChanged(nameof(ProviderImageSelectionStateMessage)); } } }
    public ProviderCatalogLoadState ProviderCatalogState
    {
        get => _providerCatalogState;
        private set
        {
            if (!SetField(ref _providerCatalogState, value)) return;
            OnPropertyChanged(nameof(ProviderImageSelectionStateMessage));
            OnPropertyChanged(nameof(HasProviderImageSelectionRecovery));
            OnPropertyChanged(nameof(ProviderImageSelectionRecoveryMessage));
        }
    }
    public string ProviderImageSelectionInstructions =>
        "Optionally choose a mockup image supplied by this Offering's provider catalog. Local upload and drag/drop are not available in this editor. You can save a Draft without provider integration, an image, or placement mapping.";
    public string ProviderImageSelectionStateMessage => ProviderCatalogState switch
    {
        ProviderCatalogLoadState.Loading => "Loading provider-catalog mockup images…",
        ProviderCatalogLoadState.Available => "Choose the optional provider view that matches the target Design Area, or save a Draft without one.",
        ProviderCatalogLoadState.Empty => "The provider catalog has no mockup images. The template can still be saved as a Draft.",
        ProviderCatalogLoadState.Error => $"Provider images could not be loaded; Draft saving remains available{MessageSuffix(ProviderCatalogMessage)}",
        _ => $"Provider images are unavailable; Draft saving remains available{MessageSuffix(ProviderCatalogMessage)}"
    };
    public bool HasProviderImageSelectionRecovery => ProviderCatalogState is ProviderCatalogLoadState.Empty or ProviderCatalogLoadState.Unavailable or ProviderCatalogLoadState.Error;
    public string ProviderImageSelectionRecoveryMessage => ProviderCatalogState switch
    {
        ProviderCatalogLoadState.Empty => "Save now as a Draft; you may sync provider data later.",
        ProviderCatalogLoadState.Error => "Save now as a Draft, or retry provider loading later.",
        ProviderCatalogLoadState.Unavailable => "Save now as a Draft; provider setup is optional.",
        _ => string.Empty
    };
    public bool HasProviderCatalogMessage => !string.IsNullOrWhiteSpace(ProviderCatalogMessage);
    public bool HasProviderMockupCandidates => ProviderMockupCandidates.Count > 0;
    public bool HasSelectedProviderMockup => SelectedProviderMockup is not null;
    public string MockupPreviewUnavailableMessage =>
        !string.IsNullOrWhiteSpace(ProviderCatalogMessage)
            ? ProviderCatalogMessage
            : "Select a provider mockup image to preview and edit placement.";
    public double MappingX { get => _mappingX; set { if (SetField(ref _mappingX, value)) { _mappingXText = FormatMapping(value); OnPropertyChanged(nameof(MappingXText)); NotifyMockupTemplateDraftChanged(); NotifyCommands(); } } }
    public double MappingY { get => _mappingY; set { if (SetField(ref _mappingY, value)) { _mappingYText = FormatMapping(value); OnPropertyChanged(nameof(MappingYText)); NotifyMockupTemplateDraftChanged(); NotifyCommands(); } } }
    public double MappingWidth { get => _mappingWidth; set { if (SetField(ref _mappingWidth, value)) { _mappingWidthText = FormatMapping(value); OnPropertyChanged(nameof(MappingWidthText)); NotifyMockupTemplateDraftChanged(); NotifyCommands(); } } }
    public double MappingHeight { get => _mappingHeight; set { if (SetField(ref _mappingHeight, value)) { _mappingHeightText = FormatMapping(value); OnPropertyChanged(nameof(MappingHeightText)); NotifyMockupTemplateDraftChanged(); NotifyCommands(); } } }
    public string MappingXText { get => _mappingXText; set => SetMappingText(ref _mappingXText, value, ref _mappingX, nameof(MappingXText), nameof(MappingX)); }
    public string MappingYText { get => _mappingYText; set => SetMappingText(ref _mappingYText, value, ref _mappingY, nameof(MappingYText), nameof(MappingY)); }
    public string MappingWidthText { get => _mappingWidthText; set => SetMappingText(ref _mappingWidthText, value, ref _mappingWidth, nameof(MappingWidthText), nameof(MappingWidth)); }
    public string MappingHeightText { get => _mappingHeightText; set => SetMappingText(ref _mappingHeightText, value, ref _mappingHeight, nameof(MappingHeightText), nameof(MappingHeight)); }
    public double MappingImageWidth => SelectedProviderMockup?.ImageWidth ?? SelectedLocalSource?.ImageWidth ?? 0;
    public double MappingImageHeight => SelectedProviderMockup?.ImageHeight ?? SelectedLocalSource?.ImageHeight ?? 0;

    public string OfferingName { get => _offeringName; set { if (SetField(ref _offeringName, value)) NotifyCommands(); } }
    public string OfferingDescription { get => _offeringDescription; set => SetField(ref _offeringDescription, value); }
    public string ProviderNetworkCode { get => _providerNetworkCode; set => SetField(ref _providerNetworkCode, value); }
    public string NewPrintProviderName { get => _newPrintProviderName; set { if (SetField(ref _newPrintProviderName, value)) NotifyCommands(); } }
    public string ExternalOfferingId { get => _externalOfferingId; set => SetField(ref _externalOfferingId, value); }
    public string OptionName { get => _optionName; set { if (SetField(ref _optionName, value)) NotifyCommands(); } }
    public string OptionValue { get => _optionValue; set { if (SetField(ref _optionValue, value)) NotifyCommands(); } }
    public string VariantName { get => _variantName; set => SetField(ref _variantName, value); }
    public string PlaceholderName { get => _placeholderName; set { if (SetField(ref _placeholderName, value)) { NotifyDesignAreaDraftChanged(); NotifyCommands(); } } }
    public string PlaceholderDescription { get => _placeholderDescription; set { if (SetField(ref _placeholderDescription, value)) NotifyDesignAreaDraftChanged(); } }
    public string PlaceholderPosition { get => _placeholderPosition; set { if (SetField(ref _placeholderPosition, value)) { NotifyDesignAreaDraftChanged(); NotifyCommands(); } } }
    public string PlaceholderDecorationMethod { get => _placeholderDecorationMethod; set { if (SetField(ref _placeholderDecorationMethod, value)) { NotifyDesignAreaDraftChanged(); NotifyCommands(); } } }
    public string PlaceholderWidth { get => _placeholderWidth; set { if (SetField(ref _placeholderWidth, value)) { OnPropertyChanged(nameof(PhysicalSizeSummary)); NotifyDesignAreaDraftChanged(); NotifyCommands(); } } }
    public string PlaceholderHeight { get => _placeholderHeight; set { if (SetField(ref _placeholderHeight, value)) { OnPropertyChanged(nameof(PhysicalSizeSummary)); NotifyDesignAreaDraftChanged(); NotifyCommands(); } } }
    public bool PlaceholderUsesAllVariants { get => _placeholderUsesAllVariants; set { if (SetField(ref _placeholderUsesAllVariants, value)) { NotifyDesignAreaDraftChanged(); NotifyCommands(); } } }
    public string PlaceholderProviderReference { get => _placeholderProviderReference; set { if (SetField(ref _placeholderProviderReference, value)) NotifyDesignAreaDraftChanged(); } }
    public string ArtworkWidth { get => _artworkWidth; set { if (SetField(ref _artworkWidth, value)) { NotifyDesignAreaDraftChanged(); NotifyCommands(); } } }
    public string ArtworkHeight { get => _artworkHeight; set { if (SetField(ref _artworkHeight, value)) { NotifyDesignAreaDraftChanged(); NotifyCommands(); } } }
    public string ArtworkDpi { get => _artworkDpi; set { if (SetField(ref _artworkDpi, value)) { OnPropertyChanged(nameof(PhysicalSizeSummary)); NotifyDesignAreaDraftChanged(); NotifyCommands(); } } }
    public string ArtworkFormat { get => _artworkFormat; set { if (SetField(ref _artworkFormat, value)) NotifyDesignAreaDraftChanged(); } }
    public string ArtworkBackground { get => _artworkBackground; set { if (SetField(ref _artworkBackground, value)) NotifyDesignAreaDraftChanged(); } }
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
    public string TemplateName { get => _templateName; set { if (SetField(ref _templateName, value)) { NotifyMockupTemplateDraftChanged(); NotifyCommands(); } } }
    public OptionKind SelectedOptionKind { get => _selectedOptionKind; set => SetField(ref _selectedOptionKind, value); }

    public bool IsAddingOption { get => _isAddingOption; private set { if (SetField(ref _isAddingOption, value)) NotifyCommands(); } }
    public bool IsAddingPrintProvider { get => _isAddingPrintProvider; private set { if (SetField(ref _isAddingPrintProvider, value)) NotifyCommands(); } }
    public bool IsAddingOptionValue { get => _isAddingOptionValue; private set { if (SetField(ref _isAddingOptionValue, value)) NotifyCommands(); } }
    public bool IsEditingOptionValue { get => _isEditingOptionValue; private set { if (SetField(ref _isEditingOptionValue, value)) NotifyCommands(); } }
    public bool IsManagingOptionValues { get => _isManagingOptionValues; private set => SetField(ref _isManagingOptionValues, value); }
    public bool IsAddingVariant { get => _isAddingVariant; private set { if (SetField(ref _isAddingVariant, value)) NotifyCommands(); } }
    public bool IsAddingBulkVariants { get => _isAddingBulkVariants; private set { if (SetField(ref _isAddingBulkVariants, value)) NotifyCommands(); } }
    public bool IsAddingPlaceholder { get => _isAddingPlaceholder; private set { if (SetField(ref _isAddingPlaceholder, value)) { OnPropertyChanged(nameof(IsEditingDesignArea)); OnPropertyChanged(nameof(DesignAreaEditorDialogTitle)); NotifyDesignAreaDraftChanged(); NotifyCommands(); } } }
    public bool IsAddingTemplate { get => _isAddingTemplate; private set { if (SetField(ref _isAddingTemplate, value)) { OnPropertyChanged(nameof(IsEditingMockupTemplate)); OnPropertyChanged(nameof(MockupTemplateEditorDialogTitle)); NotifyMockupTemplateDraftChanged(); NotifyCommands(); } } }
    public bool IsAvailable { get; private set; }
    public bool IsReadOnly { get; private set; }
    public bool CanEdit => IsAvailable && !IsReadOnly && !IsBusy;
    public bool IsBusy { get => _isBusy; private set { if (SetField(ref _isBusy, value)) { OnPropertyChanged(nameof(CanEdit)); NotifyCommands(); } } }
    public string ErrorMessage { get => _error; private set { if (SetField(ref _error, value)) OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasActiveDraft => IsAddingPrintProvider || IsAddingOption || IsAddingOptionValue || IsEditingOptionValue || IsAddingVariant || IsAddingBulkVariants || IsAddingPlaceholder || IsAddingTemplate;
    public bool HasMeaningfulMockupTemplateDraft => IsAddingTemplate && _mockupTemplateDraftBaseline is not null && CurrentMockupTemplateDraftState() != _mockupTemplateDraftBaseline;
    public string MockupTemplateLifecycleLabel => CurrentMockupTemplateReadiness().Lifecycle == MockupTemplateLifecycle.ReadyForUse ? "Ready for use" : "Draft";
    public IReadOnlyList<string> MockupTemplateReadinessMessages => CurrentMockupTemplateReadiness().Blockers.Select(ReadinessMessage).ToArray();
    public string MockupTemplateSaveValidationMessage => string.IsNullOrWhiteSpace(TemplateName)
        ? "Enter a template name to save."
        : SelectedProviderMockup is not null && !TryCreateMapping(out _)
            ? "Enter whole-number, positive placement values that stay within the image."
            : string.Empty;
    public bool HasMockupTemplateSaveValidationMessage => !string.IsNullOrWhiteSpace(MockupTemplateSaveValidationMessage);

    public bool IsMockupTemplateDiscardConfirmationVisible
    {
        get => _isMockupTemplateDiscardConfirmationVisible;
        private set { if (SetField(ref _isMockupTemplateDiscardConfirmationVisible, value)) NotifyCommands(); }
    }

    public bool HasMeaningfulDesignAreaDraft => IsAddingPlaceholder && _designAreaDraftBaseline is not null && CurrentDesignAreaDraftState() != _designAreaDraftBaseline;

    public bool IsDesignAreaDiscardConfirmationVisible
    {
        get => _isDesignAreaDiscardConfirmationVisible;
        private set { if (SetField(ref _isDesignAreaDiscardConfirmationVisible, value)) NotifyCommands(); }
    }

    public bool IsDesignAreaArchiveConfirmationVisible
    {
        get => _isDesignAreaArchiveConfirmationVisible;
        private set { if (SetField(ref _isDesignAreaArchiveConfirmationVisible, value)) NotifyCommands(); }
    }

    public Guid? PendingDesignAreaArchiveId => _pendingDesignAreaArchiveId;

    public string PendingDesignAreaArchiveName => _pendingDesignAreaArchiveName;

    public string DesignAreaArchiveConfirmationMessage =>
        $"Archive the '{_pendingDesignAreaArchiveName}' design area? It will leave the active Design Area list and can be restored later.";

    public void CancelActiveDrafts()
    {
        IsAddingPrintProvider = false;
        NewPrintProviderName = string.Empty;
        IsAddingOption = false;
        ResetOptionValueManagement();
        ResetVariantCreation();
        ResetPlaceholderDraft();
        IsAddingTemplate = false;
        TemplateName = string.Empty;
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
    public int AvailableVariantCount => AvailableVariants.Count();
    public int AvailableDesignAreaCount => AvailablePlaceholders.Count();
    public int AvailableTemplateCount => AvailableTemplates.Count();
    public string OfferingReadinessStatus => SelectedOffering?.IsArchived == true
        ? "Archived"
        : AvailableVariantCount > 0 && AvailableDesignAreaCount > 0 && AvailableTemplateCount > 0
            ? "Ready"
            : "Setup incomplete";

    public ICommand SaveOfferingCommand { get; }
    public ICommand StartAddPrintProviderCommand { get; }
    public ICommand CancelAddPrintProviderCommand { get; }
    public ICommand CreatePrintProviderCommand { get; }
    public ICommand StartAddOptionCommand { get; }
    public ICommand ManageOptionCommand { get; }
    public ICommand CloseOptionValueManagementCommand { get; }
    public ICommand CancelAddOptionCommand { get; }
    public ICommand CreateOptionCommand { get; }
    public ICommand StartAddOptionValueCommand { get; }
    public ICommand CancelAddOptionValueCommand { get; }
    public ICommand CreateOptionValueCommand { get; }
    public ICommand EditOptionValueCommand { get; }
    public ICommand SaveOptionValueEditCommand { get; }
    public ICommand CancelOptionValueEditCommand { get; }
    public ICommand StartAddVariantCommand { get; }
    public ICommand StartBulkVariantsCommand { get; }
    public ICommand CancelAddVariantCommand { get; }
    public ICommand CreateVariantCommand { get; }
    public ICommand StartAddPlaceholderCommand { get; }
    public ICommand EditPlaceholderCommand { get; }
    public ICommand CancelAddPlaceholderCommand { get; }
    public ICommand RequestCancelDesignAreaCommand { get; }
    public ICommand ConfirmDiscardDesignAreaCommand { get; }
    public ICommand KeepEditingDesignAreaCommand { get; }
    public ICommand CreatePlaceholderCommand { get; }
    public ICommand SetDefaultPlaceholderCommand { get; }
    public ICommand StartAddTemplateCommand { get; }
    public ICommand EditTemplateCommand { get; }
    public ICommand CancelAddTemplateCommand { get; }
    public ICommand RequestCancelMockupTemplateCommand { get; }
    public ICommand ConfirmDiscardMockupTemplateCommand { get; }
    public ICommand KeepEditingMockupTemplateCommand { get; }
    public ICommand CreateTemplateCommand { get; }
    public ICommand AddTemplateColorCommand { get; }
    public ICommand ArchiveOptionCommand { get; }
    public ICommand ArchiveOptionValueCommand { get; }
    public ICommand ArchiveVariantCommand { get; }
    public ICommand ArchivePlaceholderCommand { get; }
    public ICommand ConfirmDesignAreaArchiveCommand { get; }
    public ICommand CancelDesignAreaArchiveCommand { get; }
    public ICommand ArchiveTemplateCommand { get; }
    public ICommand PreviewBulkVariantsCommand { get; }
    public ICommand ConfirmBulkVariantsCommand { get; }
    public ICommand CancelBulkVariantsCommand { get; }

    public async Task LoadForStoreAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        ClearDesignAreaArchiveConfirmation();
        ResetOptionValueManagement();
        ResetVariantCreation();
        ResetTemplateDraft();
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
            RefreshOfferingCollections();
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
        if (offeringId != SelectedOffering?.Id)
        {
            ClearDesignAreaArchiveConfirmation();
        }
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
        ProviderCatalogState = ProviderCatalogLoadState.Loading;
        if (_providerCatalog is null || SelectedOffering is null)
        {
            ProviderCatalogMessage = "Provider mockup catalog data is not available.";
            ProviderCatalogState = ProviderCatalogLoadState.Unavailable;
        }
        else
        {
            try
            {
                var descriptor = await _providerCatalog.LoadAsync(CurrentContext(), cancellationToken).ConfigureAwait(true);
                if (!descriptor.IsAvailable)
                {
                    ProviderCatalogMessage = descriptor.UnavailableReason ?? "Provider mockup catalog data is not available.";
                    ProviderCatalogState = ProviderCatalogLoadState.Unavailable;
                }
                else
                {
                    Replace(ProviderMockupCandidates, descriptor.AvailableMockupImages);
                    SelectedProviderMockup = ProviderMockupCandidates.FirstOrDefault();
                    if (SelectedProviderMockup is null)
                    {
                        ProviderCatalogMessage = "This Offering has no provider mockup images.";
                        ProviderCatalogState = ProviderCatalogLoadState.Empty;
                    }
                    else ProviderCatalogState = ProviderCatalogLoadState.Available;
                }
            }
            catch (Exception exception)
            {
                ProviderCatalogMessage = exception.Message;
                ProviderCatalogState = ProviderCatalogLoadState.Error;
            }
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
            ExternalOfferingId: EmptyToNull(ExternalOfferingId),
            PrintProviderId: IsProviderNetworkOffering ? null : SelectedPrintProvider?.Id))).ConfigureAwait(true);
    }

    private async Task CreatePrintProviderAsync()
    {
        if (SelectedOffering is null) return;
        var requestedName = NewPrintProviderName.Trim();
        await RunMutationAsync(() => _catalog.CreatePrintProviderAsync(new CreatePrintProviderRequest(SelectedOffering.StoreId, requestedName))).ConfigureAwait(true);
        if (HasError) return;
        SelectedPrintProvider = AvailablePrintProviders.FirstOrDefault(value => string.Equals(value.Name, requestedName, StringComparison.OrdinalIgnoreCase));
        IsAddingPrintProvider = false;
        NewPrintProviderName = string.Empty;
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

    private async Task SaveOptionValueEditAsync()
    {
        if (SelectedOffering is null || _editingOptionValue is null) return;
        await RunMutationAsync(() => _catalog.UpdateAsync(new UpdateCatalogRecordRequest(SelectedOffering.StoreId, CatalogRecordKind.OptionValue, _editingOptionValue.Id, Name: OptionValue))).ConfigureAwait(true);
        if (!HasError) CancelOptionValueEdit();
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
        if (SelectedOffering is null) return;
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            CaptureSelectedLocalSource();
            var colors = TemplateColorChoices.Where(value => value.IsSelected).Select(value => value.Value.Id).ToArray();
            if (_sourceImages is not null && HasLocalSource && SelectedProviderMockup is null)
            {
                var template = SelectedTemplate is not null && AvailableTemplates.Any(value => value.Id == SelectedTemplate.Id)
                    ? SelectedTemplate
                    : null;
                var templateWasExisting = template is not null;
                var sourceState = (MockupTemplateSetupState?)null;
                if (template is null)
                {
                    var created = await _mockups.CreateTemplateAsync(new CreateMockupTemplateRequest(SelectedOffering.StoreId, SelectedOffering.Id, TemplateName, SelectedPlaceholder?.Id)).ConfigureAwait(true);
                    if (!created.Succeeded) { ErrorMessage = created.Error ?? "Mockup Template could not be created."; return; }
                    ApplyMockups(created.State);
                    template = created.State.Templates.LastOrDefault(value => value.Name == TemplateName.Trim());
                    sourceState = created.State;
                }
                if (template is null) { ErrorMessage = "The Mockup Template could not be selected."; return; }
                if (templateWasExisting)
                {
                    var templateResult = await _mockups.UpdateTemplateAsync(new UpdateMockupTemplateRequest(
                        SelectedOffering.StoreId,
                        template.Id,
                        TemplateName,
                        TargetPlaceholderId: SelectedPlaceholder?.Id,
                        ReplaceTargetPlaceholder: true)).ConfigureAwait(true);
                    if (!templateResult.Succeeded) { ErrorMessage = templateResult.Error ?? "Mockup Template could not be updated."; return; }
                    ApplyMockups(templateResult.State);
                    template = templateResult.State.Templates.Single(value => value.Id == template.Id);
                    sourceState = templateResult.State;
                }
                foreach (var draft in LocalSourceDrafts)
                {
                    var sourceResult = draft.IsManaged
                        ? await _sourceImages.UpdateAsync(new UpdateLocalMockupTemplateSourceRequest(SelectedOffering.StoreId, template.Id, draft.SourceImageId ?? Guid.Empty, draft.OptionValueIds, draft.Mapping)).ConfigureAwait(true)
                        : await _sourceImages.AddAsync(new AddLocalMockupTemplateSourceRequest(SelectedOffering.StoreId, template.Id, draft.Path, draft.OptionValueIds, draft.Mapping)).ConfigureAwait(true);
                    if (!sourceResult.Succeeded) { ErrorMessage = sourceResult.Error ?? $"The local source image '{draft.DisplayName}' could not be added."; return; }
                    sourceState = sourceResult.State;
                }
                foreach (var draft in _archivedLocalSourceDrafts.Where(value => value.SourceImageId is not null))
                {
                    var archiveResult = await _sourceImages.UpdateAsync(new UpdateLocalMockupTemplateSourceRequest(SelectedOffering.StoreId, template.Id, draft.SourceImageId!.Value, draft.OptionValueIds, draft.Mapping, Archive: true)).ConfigureAwait(true);
                    if (!archiveResult.Succeeded) { ErrorMessage = archiveResult.Error ?? $"The local source image '{draft.DisplayName}' could not be archived."; return; }
                    sourceState = archiveResult.State;
                }
                if (sourceState is not null) ApplyMockups(sourceState);
                SelectedTemplate = template;
                EndTemplateDraft();
                TemplateName = string.Empty;
                LocalSourcePath = string.Empty;
                LocalSourceDrafts.Clear();
                _archivedLocalSourceDrafts.Clear();
                return;
            }
            _ = TryCreateMapping(out var mapping);
            var result = SelectedTemplate is not null && AvailableTemplates.Any(value => value.Id == SelectedTemplate.Id)
                ? await _mockups.UpdateTemplateAsync(new UpdateMockupTemplateRequest(
                    SelectedOffering.StoreId, SelectedTemplate.Id, TemplateName,
                    TargetPlaceholderId: SelectedPlaceholder?.Id,
                    ReplaceProviderImage: true,
                    ProviderMockupReference: SelectedProviderMockup?.ProviderReference,
                    ImageMapping: mapping,
                    ReplaceColorOptionValueIds: colors,
                    ReplaceTargetPlaceholder: true)).ConfigureAwait(true)
                : await _mockups.CreateTemplateAsync(new CreateMockupTemplateRequest(
                    SelectedOffering.StoreId, SelectedOffering.Id, TemplateName, SelectedPlaceholder?.Id,
                    ProviderMockupReference: SelectedProviderMockup?.ProviderReference,
                    ImageMapping: mapping,
                    ColorOptionValueIds: colors)).ConfigureAwait(true);
            if (!result.Succeeded)
            {
                ErrorMessage = result.Error ?? "Mockup Template could not be saved.";
                return;
            }
            var savedId = result.TemplateId ?? SelectedTemplate?.Id;
            ApplyMockups(result.State);
            SelectedTemplate = AvailableTemplates.FirstOrDefault(value => value.Id == savedId) ?? SelectedTemplate;
            EndTemplateDraft();
            TemplateName = string.Empty;
            foreach (var color in TemplateColorChoices) color.IsSelected = false;
        }
        catch (Exception exception) { ErrorMessage = exception.Message; }
        finally { IsBusy = false; }
    }

    private async Task BrowseLocalSourceAsync()
    {
        var path = await _filePicker.PickImportFileAsync().ConfigureAwait(true);
        if (!string.IsNullOrWhiteSpace(path))
        {
            var draft = new LocalMockupSourceDraftViewModel(path, []);
            LocalSourceDrafts.Add(draft);
            SelectLocalSource(draft);
            foreach (var color in TemplateColorChoices) color.IsSelected = false;
            MappingXText = MappingYText = MappingWidthText = MappingHeightText = string.Empty;
            LocalSourcePath = path;
            OnPropertyChanged(nameof(HasLocalSource));
            NotifyMockupTemplateDraftChanged();
        }
    }


    private void SelectLocalSource(LocalMockupSourceDraftViewModel? draft)
    {
        if (draft is null) return;
        CaptureSelectedLocalSource();
        SelectedLocalSource = draft;
        foreach (var row in LocalSourceDrafts) row.IsSelected = ReferenceEquals(row, draft);
        foreach (var color in TemplateColorChoices) color.IsSelected = draft.OptionValueIds.Contains(color.Value.Id);
        foreach (var option in TemplateAdditionalOptionChoices) option.IsSelected = draft.OptionValueIds.Contains(option.Value.Id);
        var mapping = draft.Mapping;
        MappingXText = mapping is null ? string.Empty : FormatMapping(mapping.X);
        MappingYText = mapping is null ? string.Empty : FormatMapping(mapping.Y);
        MappingWidthText = mapping is null ? string.Empty : FormatMapping(mapping.Width);
        MappingHeightText = mapping is null ? string.Empty : FormatMapping(mapping.Height);
        LocalSourcePath = draft.Path;
    }

    private void ReuseMapping(LocalMockupSourceDraftViewModel? source)
    {
        if (source?.Mapping is not { } mapping || SelectedLocalSource is null || ReferenceEquals(source, SelectedLocalSource)) return;
        SelectedMappingSource = source;
        MappingXText = FormatMapping(mapping.X); MappingYText = FormatMapping(mapping.Y);
        MappingWidthText = FormatMapping(mapping.Width); MappingHeightText = FormatMapping(mapping.Height);
        NotifyMockupTemplateDraftChanged();
    }

    private void SelectAllTemplateSizes()
    {
        foreach (var choice in TemplateAdditionalOptionChoices.Where(value => IsSizeValue(value.Value)))
            choice.IsSelected = true;
        NotifyMockupTemplateDraftChanged();
        NotifyCommands();
    }

    private bool IsSizeValue(OfferingOptionValue value) => Options.FirstOrDefault(option => option.Id == value.OptionId)?.OptionKind == OptionKind.Size;

    private void RebuildMappedSourceChoices()
    {
        var choices = LocalSourceDrafts.Where(value => !ReferenceEquals(value, SelectedLocalSource) && value.Mapping is not null).ToArray();
        Replace(MappedSourceChoices, choices);
        if (SelectedMappingSource is not null && !choices.Contains(SelectedMappingSource)) SelectedMappingSource = null;
    }

    private void CaptureSelectedLocalSource()
    {
        if (SelectedLocalSource is null) return;
        var ids = TemplateColorChoices.Where(value => value.IsSelected).Select(value => value.Value.Id)
            .Concat(TemplateAdditionalOptionChoices.Where(value => value.IsSelected).Select(value => value.Value.Id)).Distinct().ToArray();
        TryCreateMapping(out var mapping);
        var labels = TemplateColorChoices.Where(value => ids.Contains(value.Value.Id)).Select(value => value.Label)
            .Concat(TemplateAdditionalOptionChoices.Where(value => ids.Contains(value.Value.Id)).Select(value => value.Label));
        SelectedLocalSource.UpdateMetadata(ids, mapping, string.Join(", ", labels));
        RebuildMappedSourceChoices();
        OnPropertyChanged(nameof(HasLocalSource));
    }

    public void RemoveLocalSource(LocalMockupSourceDraftViewModel? draft)
    {
        if (draft is null) return;
        if (draft.IsManaged && draft.SourceImageId is not null && !_archivedLocalSourceDrafts.Contains(draft)) _archivedLocalSourceDrafts.Add(draft);
        if (ReferenceEquals(SelectedLocalSource, draft)) SelectedLocalSource = null;
        LocalSourceDrafts.Remove(draft);
        RebuildMappedSourceChoices();
        var next = LocalSourceDrafts.LastOrDefault();
        if (next is not null) SelectLocalSource(next);
        OnPropertyChanged(nameof(HasLocalSource));
        NotifyMockupTemplateDraftChanged();
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
        if (!CanEdit || template is null) return;
        SelectedTemplate = template;
        SelectedPlaceholder = AvailablePlaceholders.FirstOrDefault(value => value.Id == template.TargetPlaceholderId);
        TemplateName = template.Name;
        LocalSourceDrafts.Clear();
        MappedSourceChoices.Clear();
        _archivedLocalSourceDrafts.Clear();
        SelectedLocalSource = null;
        LocalSourcePath = string.Empty;
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
        _mockupTemplateDraftBaseline = CurrentMockupTemplateDraftState();
        IsAddingTemplate = true;
        _ = LoadLocalSourceDraftsAsync(template.Id);
        MockupTemplateEditorRequested?.Invoke(this, EventArgs.Empty);
    }

    private void BeginNewTemplate()
    {
        if (!CanEdit || SelectedOffering is null) return;
        SelectedTemplate = null;
        SelectedPlaceholder = null;
        TemplateName = string.Empty;
        LocalSourceDrafts.Clear();
        MappedSourceChoices.Clear();
        _archivedLocalSourceDrafts.Clear();
        SelectedLocalSource = null;
        LocalSourcePath = string.Empty;
        foreach (var color in TemplateColorChoices) color.IsSelected = false;
        foreach (var option in TemplateAdditionalOptionChoices) option.IsSelected = false;
        _mockupTemplateDraftBaseline = CurrentMockupTemplateDraftState();
        IsAddingTemplate = true;
        MockupTemplateEditorRequested?.Invoke(this, EventArgs.Empty);
    }

    private void RequestDesignAreaArchive(object? parameter)
    {
        if (_isDesignAreaArchiveConfirmationVisible || SelectedOffering is null) return;
        var id = parameter switch
        {
            OfferingPlaceholder area => area.Id,
            DesignAreaCardViewModel card => card.Id,
            _ => Guid.Empty
        };
        if (id == Guid.Empty) return;
        var currentArea = AvailablePlaceholders.FirstOrDefault(candidate => candidate.Id == id);
        if (currentArea is null) return;
        _pendingDesignAreaArchiveId = currentArea.Id;
        _pendingDesignAreaArchiveName = currentArea.Name;
        OnPropertyChanged(nameof(PendingDesignAreaArchiveId));
        OnPropertyChanged(nameof(PendingDesignAreaArchiveName));
        OnPropertyChanged(nameof(DesignAreaArchiveConfirmationMessage));
        IsDesignAreaArchiveConfirmationVisible = true;
        DesignAreaArchiveConfirmationRequested?.Invoke(this, EventArgs.Empty);
    }

    private async Task ConfirmDesignAreaArchiveAsync()
    {
        if (_pendingDesignAreaArchiveId is not Guid id || SelectedOffering is null) return;
        ClearDesignAreaArchiveConfirmation();
        await RunMutationAsync(() => _catalog.ArchiveAsync(new ArchiveCatalogRecordRequest(SelectedOffering.StoreId, CatalogRecordKind.Placeholder, id))).ConfigureAwait(true);
    }

    private void CancelDesignAreaArchive()
    {
        DesignAreaArchiveFocusRequested?.Invoke(this, EventArgs.Empty);
        ClearDesignAreaArchiveConfirmation();
    }

    private void ClearDesignAreaArchiveConfirmation()
    {
        _pendingDesignAreaArchiveId = null;
        _pendingDesignAreaArchiveName = string.Empty;
        OnPropertyChanged(nameof(PendingDesignAreaArchiveId));
        OnPropertyChanged(nameof(PendingDesignAreaArchiveName));
        OnPropertyChanged(nameof(DesignAreaArchiveConfirmationMessage));
        IsDesignAreaArchiveConfirmationVisible = false;
    }

    private void RunArchive(object? parameter, CatalogRecordKind kind)
    {
        var id = parameter switch
        {
            OfferingOption value => value.Id,
            OfferingOptionValue value => value.Id,
            OfferingVariant value => value.Id,
            SellableVariantRowViewModel value => value.Id,
            OfferingPlaceholder value => value.Id,
            DesignAreaCardViewModel value => value.Id,
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
                IsAddingBulkVariants = false;
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
        OnPropertyChanged(nameof(AvailablePrintProviders));
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
        _selectedPrintProvider = SelectedOffering?.PrintProviderId is Guid providerId
            ? PrintProviders.FirstOrDefault(value => value.Id == providerId)
            : null;
        OnPropertyChanged(nameof(SelectedPrintProvider));
        IsAddingPrintProvider = false;
        NewPrintProviderName = string.Empty;
    }

    private bool CanSaveOffering() => CanEdit && SelectedOffering is not null && !string.IsNullOrWhiteSpace(OfferingName)
        && (IsProviderNetworkOffering ? !string.IsNullOrWhiteSpace(ProviderNetworkCode) : SelectedPrintProvider is not null);

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
        OnPropertyChanged(nameof(AvailableVariantCount));
        OnPropertyChanged(nameof(AvailableDesignAreaCount));
        OnPropertyChanged(nameof(AvailableTemplateCount));
        OnPropertyChanged(nameof(OfferingReadinessStatus));
        var groups = AvailableOptions.Select(option => new OfferingChoiceGroupViewModel(
            option,
            OptionValues.Where(value => value.OptionId == option.Id && !value.IsArchived).OrderBy(value => value.SortOrder).ToArray(),
            ArchiveOptionCommand));
        Replace(AvailableChoiceGroups, groups);
        Replace(SellableVariantRows, AvailableVariants.Select(value => SellableVariantRowViewModel.From(value, Options, OptionValues)));

        var activeVariantIds = AvailableVariants.Select(value => value.Id).ToHashSet();
        var areaSummaries = AvailablePlaceholders.Select(value => new DesignAreaSetupSummary(
            value.Id,
            value.Name,
            value.Position,
            value.Width,
            value.Height,
            value.MaximumPhysicalSize,
            value.ArtworkGuidance,
            activeVariantIds.SetEquals(value.VariantIds),
            value.VariantIds.Count,
            value.ProviderReference));
        Replace(DesignAreaCards, areaSummaries.Select(DesignAreaCardViewModel.From));

        var templateSummaries = AvailableTemplates.Select(template =>
        {
            var colorIds = TemplateColors.Where(value => value.MockupTemplateId == template.Id && !value.IsArchived).Select(value => value.ColorOptionValueId).ToArray();
            var targetName = Placeholders.FirstOrDefault(value => value.Id == template.TargetPlaceholderId)?.Name;
            var revision = TemplateRevisions.FirstOrDefault(value => value.MockupTemplateId == template.Id && value.RevisionNumber == template.CurrentRevision);
            var compatibleVariantIds = AvailableVariants.Where(value => value.OptionValueIds.Any(colorIds.Contains)).Select(value => value.Id).ToArray();
            var effectiveRevision = revision ?? new MockupTemplateRevision(template.Id, template.Id, template.CurrentRevision, template.TargetPlaceholderId, template.CreatedAt);
            var readiness = MockupTemplateReadinessPolicy.Evaluate(new(template, effectiveRevision, colorIds, Options, OptionValues, Variants, Placeholders));
            return new MockupTemplateSetupSummary(template.Id, template.Name, template.TargetPlaceholderId, targetName, colorIds, compatibleVariantIds, revision?.ProviderMockupReference, template.CurrentRevision, template.IsArchived, readiness.Lifecycle, readiness.Blockers);
        });
        Replace(MockupTemplateCards, templateSummaries.Select(value => MockupTemplateCardViewModel.From(value, OptionValues)));
        RebuildChoices();
    }

    private void BeginManageOptionValues(OfferingOption? option)
    {
        if (IsManagingOptionValues || option is null) return;
        var currentOption = AvailableOptions.FirstOrDefault(value => value.Id == option.Id);
        if (currentOption is null) return;
        SelectedOption = currentOption;
        IsManagingOptionValues = true;
        OptionValueManagementRequested?.Invoke(this, EventArgs.Empty);
    }

    private void BeginAddOptionValue()
    {
        CancelOptionValueEdit();
        IsAddingOptionValue = true;
        OptionValueEditorFocusRequested?.Invoke(this, EventArgs.Empty);
    }

    private void BeginEditOptionValue(OfferingOptionValue? value)
    {
        if (value is null || IsAddingOptionValue) return;
        var current = AvailableValues.FirstOrDefault(candidate => candidate.Id == value.Id);
        if (current is null) return;
        _editingOptionValue = current;
        OptionValue = current.Value;
        IsEditingOptionValue = true;
        OptionValueEditorFocusRequested?.Invoke(this, EventArgs.Empty);
    }

    private void CancelOptionValueEdit()
    {
        _editingOptionValue = null;
        IsEditingOptionValue = false;
        if (!IsAddingOptionValue) OptionValue = string.Empty;
    }

    private void CloseOptionValueManagement()
    {
        ResetOptionValueManagement();
        OptionChoiceFocusRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ResetOptionValueManagement()
    {
        IsAddingOptionValue = false;
        CancelOptionValueEdit();
        OptionValue = string.Empty;
        IsManagingOptionValues = false;
    }

    private void BeginVariantDraft(bool bulk)
    {
        if (IsAddingVariant || IsAddingBulkVariants) return;
        CloseOptionValueManagement();
        ResetVariantCreation();
        IsAddingVariant = !bulk;
        IsAddingBulkVariants = bulk;
        if (bulk) BulkVariantsRequested?.Invoke(this, EventArgs.Empty);
        else AddVariantRequested?.Invoke(this, EventArgs.Empty);
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
        var templateColors = OptionValues
            .Where(value => value.OfferingId == SelectedOffering?.Id && !value.IsArchived)
            .Where(value => Options.FirstOrDefault(option => option.Id == value.OptionId)?.OptionKind == OptionKind.Color)
            .Where(value => supportedColors is null || supportedColors.Contains(value.Id))
            .Select(value => new OptionValueChoiceViewModel(value, ValueLabel(value)) { IsSelected = selectedTemplateColorIds.Contains(value.Id) })
            .ToArray();
        foreach (var choice in templateColors) choice.PropertyChanged += ChoiceSelectionChanged;
        Replace(TemplateColorChoices, templateColors);

        var selectedTemplateOptionIds = TemplateAdditionalOptionChoices.Where(value => value.IsSelected).Select(value => value.Value.Id).ToHashSet();
        var colorOptionIds = Options.Where(value => value.OfferingId == SelectedOffering?.Id && value.OptionKind == OptionKind.Color).Select(value => value.Id).ToHashSet();
        var additional = OptionValues
            .Where(value => value.OfferingId == SelectedOffering?.Id && !value.IsArchived && !colorOptionIds.Contains(value.OptionId))
            .Select(value => new OptionValueChoiceViewModel(value, ValueLabel(value)) { IsSelected = selectedTemplateOptionIds.Contains(value.Id) })
            .ToArray();
        foreach (var choice in additional) choice.PropertyChanged += ChoiceSelectionChanged;
        Replace(TemplateAdditionalOptionChoices, additional);
    }

    private void ChoiceSelectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectableCatalogRecord.IsSelected))
        {
            ResetBulkPreview();
            NotifyMockupTemplateDraftChanged();
            NotifyDesignAreaDraftChanged();
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
        if (!CanEdit || !IsAddingTemplate || SelectedOffering is null || string.IsNullOrWhiteSpace(TemplateName)) return false;
        if (_sourceImages is not null && HasLocalSource && SelectedProviderMockup is null)
            return true;
        return SelectedProviderMockup is null || TryCreateMapping(out _);
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

    private void ResetVariantCreation()
    {
        ResetVariantDraft();
        ResetBulkDraft();
        IsAddingBulkVariants = false;
    }

    private void ResetPlaceholderDraft()
    {
        IsDesignAreaDiscardConfirmationVisible = false;
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
        _designAreaDraftBaseline = null;
        OnPropertyChanged(nameof(HasMeaningfulDesignAreaDraft));
        NotifyCommands();
    }

    private void BeginNewDesignArea()
    {
        ResetPlaceholderDraft();
        SelectedPlaceholder = null;
        _designAreaDraftBaseline = CurrentDesignAreaDraftState();
        IsAddingPlaceholder = true;
        DesignAreaEditorRequested?.Invoke(this, EventArgs.Empty);
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
        _designAreaDraftBaseline = CurrentDesignAreaDraftState();
        IsAddingPlaceholder = true;
        DesignAreaEditorRequested?.Invoke(this, EventArgs.Empty);
    }

    private void RequestCancelMockupTemplate()
    {
        if (!IsAddingTemplate) return;
        if (HasMeaningfulMockupTemplateDraft)
        {
            IsMockupTemplateDiscardConfirmationVisible = true;
            return;
        }

        ResetTemplateDraft();
    }

    private void ConfirmDiscardMockupTemplate()
    {
        if (!IsMockupTemplateDiscardConfirmationVisible) return;
        ResetTemplateDraft();
    }

    private void ResetTemplateDraft()
    {
        EndTemplateDraft();
        TemplateName = string.Empty;
        foreach (var color in TemplateColorChoices) color.IsSelected = false;
        foreach (var option in TemplateAdditionalOptionChoices) option.IsSelected = false;
        SelectedTemplate = AvailableTemplates.FirstOrDefault();
        LocalSourceDrafts.Clear();
        SelectedLocalSource = null;
        LocalSourcePath = string.Empty;
    }

    private async Task LoadLocalSourceDraftsAsync(Guid templateId)
    {
        if (_sourceImages is null || SelectedOffering is null) return;
        var state = await _sourceImages.LoadAsync(SelectedOffering.StoreId, templateId).ConfigureAwait(true);
        if (!IsAddingTemplate || SelectedTemplate?.Id != templateId) return;
        foreach (var image in state.Images)
        {
            var labels = image.OptionValueIds.Select(id => OptionValues.FirstOrDefault(value => value.Id == id)).Where(value => value is not null).Select(value => ValueLabel(value!));
            LocalSourceDrafts.Add(new LocalMockupSourceDraftViewModel(image.WorkspaceRelativePath, image.OptionValueIds, isManaged: true, image.ImageMapping, image.Dimensions.Width, image.Dimensions.Height, image.Id, image.PreviewPath) { ApplicabilitySummary = string.Join(", ", labels) });
        }
        if (LocalSourceDrafts.Count > 0) SelectLocalSource(LocalSourceDrafts[0]);
        RebuildMappedSourceChoices();
        OnPropertyChanged(nameof(HasLocalSource));
    }

    private void EndTemplateDraft()
    {
        IsMockupTemplateDiscardConfirmationVisible = false;
        IsAddingTemplate = false;
        _mockupTemplateDraftBaseline = null;
        OnPropertyChanged(nameof(HasMeaningfulMockupTemplateDraft));
    }

    private void NotifyMockupTemplateDraftChanged()
    {
        OnPropertyChanged(nameof(HasMeaningfulMockupTemplateDraft));
        OnPropertyChanged(nameof(MockupTemplateLifecycleLabel));
        OnPropertyChanged(nameof(MockupTemplateReadinessMessages));
        OnPropertyChanged(nameof(MockupTemplateSaveValidationMessage));
        OnPropertyChanged(nameof(HasMockupTemplateSaveValidationMessage));
    }

    private MockupTemplateReadinessResult CurrentMockupTemplateReadiness()
    {
        if (SelectedOffering is null)
            return new([MockupTemplateReadinessBlocker.MissingTargetDesignArea, MockupTemplateReadinessBlocker.MissingColors, MockupTemplateReadinessBlocker.MissingImage, MockupTemplateReadinessBlocker.MissingMapping]);
        var templateId = SelectedTemplate?.Id ?? Guid.Parse("00000000-0000-0000-0000-000000000001");
        var template = new MockupTemplate(templateId, SelectedOffering.Id, SelectedPlaceholder?.Id, string.IsNullOrWhiteSpace(TemplateName) ? "Draft" : TemplateName, null, 1, false, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch);
        _ = TryCreateMapping(out var mapping);
        var revision = new MockupTemplateRevision(templateId, templateId, 1, template.TargetPlaceholderId, DateTimeOffset.UnixEpoch,
            providerMockupReference: SelectedProviderMockup?.ProviderReference, imageMapping: mapping);
        return MockupTemplateReadinessPolicy.Evaluate(new(template, revision,
            TemplateColorChoices.Where(value => value.IsSelected).Select(value => value.Value.Id).ToArray(),
            Options, OptionValues, Variants, Placeholders, SelectedProviderMockup?.SupportedColorOptionValueIds.ToHashSet()));
    }

    private static string ReadinessMessage(MockupTemplateReadinessBlocker blocker) => blocker switch
    {
        MockupTemplateReadinessBlocker.Archived => "Restore the template before use.",
        MockupTemplateReadinessBlocker.MissingTargetDesignArea => "Choose a Design Area.",
        MockupTemplateReadinessBlocker.InvalidTargetDesignArea => "Choose an active Design Area from this Offering.",
        MockupTemplateReadinessBlocker.MissingColors => "Choose at least one applicable Color.",
        MockupTemplateReadinessBlocker.InvalidColors => "Remove unavailable Colors.",
        MockupTemplateReadinessBlocker.MissingCompatibleVariants => "Add an active Variant that uses the selected Colors.",
        MockupTemplateReadinessBlocker.IncompatibleVariants => "Choose a Design Area compatible with every implied Variant.",
        MockupTemplateReadinessBlocker.MissingImage => "Choose a mockup image.",
        MockupTemplateReadinessBlocker.MissingMapping => "Add a valid design-area placement mapping.",
        MockupTemplateReadinessBlocker.KnownImageColorIncompatibility => "Choose Colors supported by the selected image.",
        _ => blocker.ToString()
    };

    private bool TryCreateMapping(out MockupImageSpaceMapping? mapping)
    {
        mapping = null;
        var imageWidth = SelectedProviderMockup?.ImageWidth ?? SelectedLocalSource?.ImageWidth ?? 0;
        var imageHeight = SelectedProviderMockup?.ImageHeight ?? SelectedLocalSource?.ImageHeight ?? 0;
        if (imageWidth <= 0 || imageHeight <= 0)
            return string.IsNullOrWhiteSpace(MappingXText) && string.IsNullOrWhiteSpace(MappingYText)
                && string.IsNullOrWhiteSpace(MappingWidthText) && string.IsNullOrWhiteSpace(MappingHeightText);
        if (!int.TryParse(MappingXText, out var x) || !int.TryParse(MappingYText, out var y)
            || !int.TryParse(MappingWidthText, out var width) || !int.TryParse(MappingHeightText, out var height))
            return false;
        try
        {
            mapping = new MockupImageSpaceMapping(imageWidth, imageHeight, x, y, width, height);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private void SetMappingText(ref string field, string value, ref double numericField, string textProperty, string numericProperty)
    {
        value ??= string.Empty;
        if (field == value) return;
        field = value;
        if (int.TryParse(value, out var parsed)) numericField = parsed;
        OnPropertyChanged(textProperty);
        OnPropertyChanged(numericProperty);
        NotifyMockupTemplateDraftChanged();
        NotifyCommands();
    }

    private static string FormatMapping(double value) => Math.Round(value).ToString(System.Globalization.CultureInfo.InvariantCulture);

    private MockupTemplateDraftState CurrentMockupTemplateDraftState() => new(
        SelectedTemplate?.Id,
        TemplateName,
        SelectedProviderMockup?.ProviderReference,
        SelectedPlaceholder?.Id,
        MappingXText,
        MappingYText,
        MappingWidthText,
        MappingHeightText,
        string.Join("|", TemplateColorChoices.Where(value => value.IsSelected).Select(value => value.Value.Id).OrderBy(value => value)));

    private void RequestCancelDesignArea()
    {
        if (!IsAddingPlaceholder) return;
        if (HasMeaningfulDesignAreaDraft)
        {
            IsDesignAreaDiscardConfirmationVisible = true;
            return;
        }
        ResetPlaceholderDraft();
        SelectedPlaceholder = AvailablePlaceholders.FirstOrDefault();
    }

    private void ConfirmDiscardDesignArea()
    {
        if (!IsDesignAreaDiscardConfirmationVisible) return;
        ResetPlaceholderDraft();
        SelectedPlaceholder = AvailablePlaceholders.FirstOrDefault();
    }

    private void NotifyDesignAreaDraftChanged() => OnPropertyChanged(nameof(HasMeaningfulDesignAreaDraft));

    private DesignAreaDraftState CurrentDesignAreaDraftState() => new(
        PlaceholderName,
        PlaceholderDescription,
        PlaceholderPosition,
        PlaceholderDecorationMethod,
        PlaceholderWidth,
        PlaceholderHeight,
        PlaceholderUsesAllVariants,
        PlaceholderProviderReference,
        ArtworkWidth,
        ArtworkHeight,
        ArtworkDpi,
        ArtworkFormat,
        ArtworkBackground,
        string.Join("|", PlaceholderVariantChoices.Where(value => value.IsSelected).Select(value => value.Variant.Id).OrderBy(value => value)));

    private void NotifyCommands()
    {
        foreach (var command in new ICommand[]
        {
            SaveOfferingCommand, StartAddPrintProviderCommand, CreatePrintProviderCommand, StartAddOptionCommand, ManageOptionCommand, CreateOptionCommand, StartAddOptionValueCommand,
            CreateOptionValueCommand, EditOptionValueCommand, SaveOptionValueEditCommand, CancelOptionValueEditCommand,
            StartAddVariantCommand, StartBulkVariantsCommand, CreateVariantCommand, StartAddPlaceholderCommand,
            CreatePlaceholderCommand, SetDefaultPlaceholderCommand, StartAddTemplateCommand, CreateTemplateCommand,
            AddTemplateColorCommand, PreviewBulkVariantsCommand, ConfirmBulkVariantsCommand,
            ConfirmDesignAreaArchiveCommand, CancelDesignAreaArchiveCommand,
            RequestCancelMockupTemplateCommand, ConfirmDiscardMockupTemplateCommand, KeepEditingMockupTemplateCommand,
            RequestCancelDesignAreaCommand, ConfirmDiscardDesignAreaCommand, KeepEditingDesignAreaCommand
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
    private static string MessageSuffix(string message) => string.IsNullOrWhiteSpace(message) ? "." : $": {message}";
    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values) { target.Clear(); foreach (var value in values) target.Add(value); }
    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null) { if (EqualityComparer<T>.Default.Equals(field, value)) return false; field = value; OnPropertyChanged(name); return true; }
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private sealed record MockupTemplateDraftState(
        Guid? TemplateId,
        string Name,
        string? ProviderReference,
        Guid? PlaceholderId,
        string X,
        string Y,
        string Width,
        string Height,
        string SelectedColorIds);

    private sealed record DesignAreaDraftState(
        string Name,
        string Description,
        string Position,
        string DecorationMethod,
        string Width,
        string Height,
        bool UsesAllVariants,
        string ProviderReference,
        string ArtworkWidth,
        string ArtworkHeight,
        string ArtworkDpi,
        string ArtworkFormat,
        string ArtworkBackground,
        string SelectedVariantIds);
}

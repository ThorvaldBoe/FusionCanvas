using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.Commands;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.App.Mockups;
using FusionCanvas.App.Settings;
using FusionCanvas.Application.Catalog;
using FusionCanvas.App;
using FusionCanvas.Application.Mockups;
using FusionCanvas.App.Assets;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.App.Stores;




public sealed class CatalogSetupViewModel : INotifyPropertyChanged
{
    private readonly ICatalogSetupService _catalog;
    private readonly IMockupTemplateSetupService _mockups;
    private readonly IOfferingManagementService? _offeringManagement;
    private readonly IProviderCatalogCandidateSource? _providerCatalog;
    private readonly IMockupTemplateSourceImageService? _sourceImages;
    private readonly IRasterImageMetadataReader? _rasterImageMetadataReader;
    private readonly IMockupSourceMetadataAssistanceService? _mockupSourceMetadataAssistance;
    private readonly IMockupPlacementPreviewReader? _mockupPlacementPreviewReader;
    private IReadOnlyList<MockupTemplateSourceImage> _templateSourceImages = [];
    private IReadOnlyList<MockupTemplateSourceImageOptionValue> _templateSourceConditions = [];
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
    private bool _placeholderPrimaryForArtworkGeneration;
    private string _placeholderProviderReference = string.Empty;
    private string _artworkWidth = string.Empty;
    private string _artworkHeight = string.Empty;
    private string _artworkDpi = string.Empty;
    private string _artworkFormat = string.Empty;
    private string _artworkBackground = string.Empty;
    private string _templateName = string.Empty;
    private string _templateColorSearchText = string.Empty;
    private string _localSourcePath = string.Empty;
    private LocalMockupSourceDraftViewModel? _selectedLocalSource;
    private LocalMockupSourceDraftViewModel? _selectedMappingSource;
    private readonly List<LocalMockupSourceDraftViewModel> _selectedLocalSources = [];
    private LocalMockupSourceDraftViewModel? _localSourceSelectionAnchor;
    private string _localSourceSortColumn = "File";
    private bool _localSourceSortAscending = true;
    private string _error = string.Empty;
    private bool _isBusy;
    private bool _isCoverageLoading;
    private bool _isReadOnly;
    private string _coverageError = string.Empty;
    private bool _isAddingOption;
    private bool _isAddingPrintProvider;
    private bool _isAddingOptionValue;
    private bool _isEditingOptionValue;
    private bool _isManagingOptionValues;
    private bool _isAddingVariant;
    private bool _isAddingBulkVariants;
    private bool _isAddingPlaceholder;
    private bool _isAddingTemplate;
    private CatalogArchivePlan? _archiveOfferingPlan;
    private bool _isArchiveOfferingConfirmationVisible;
    private CatalogOfferingDeletePlan? _deleteOfferingPlan;
    private bool _isDeleteOfferingConfirmationVisible;
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
    private bool _keepAspectRatio;
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
    private bool _storeEditorAttached = true;
    private OfferingReadinessSummary? _offeringReadiness;
    private string _offeringReadinessError = string.Empty;
    private long _readinessLoadVersion;
    private CancellationTokenSource? _mockupSourceMetadataAssistanceCts;
    private bool _isMockupSourceMetadataAssistanceBusy;
    private string _mockupSourceMetadataAssistanceStatus = string.Empty;
    private long _mockupSourceMetadataAssistanceVersion;
    private MockupTemplateCoveragePlan? _coveragePlan;
    private MockupTemplateCoverageGroupingStrategy _coverageGroupingStrategy = MockupTemplateCoverageGroupingStrategy.ColorFirst;
    private MockupTemplateCoverageRequirement? _selectedCoverageRequirement;
    private LocalMockupSourceDraftViewModel? _selectedCoverageExemplar;

    public CatalogSetupViewModel(ICatalogSetupService catalog, IMockupTemplateSetupService mockups, IOfferingManagementService? offeringManagement = null, IProviderCatalogCandidateSource? providerCatalog = null, IMockupTemplateSourceImageService? sourceImages = null, IAssetFilePicker? filePicker = null, IRasterImageMetadataReader? rasterImageMetadataReader = null, IMockupSourceMetadataAssistanceService? mockupSourceMetadataAssistance = null, IMockupPlacementPreviewReader? mockupPlacementPreviewReader = null)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _mockups = mockups ?? throw new ArgumentNullException(nameof(mockups));
        _offeringManagement = offeringManagement;
        _providerCatalog = providerCatalog;
        _sourceImages = sourceImages;
        _filePicker = filePicker ?? new NullAssetFilePicker();
        _rasterImageMetadataReader = rasterImageMetadataReader;
        _mockupSourceMetadataAssistance = mockupSourceMetadataAssistance;
        _mockupPlacementPreviewReader = mockupPlacementPreviewReader;
        TemplateColorChoices.CollectionChanged += (_, _) => RefreshFilteredTemplateColorChoices();

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
        MoveOptionValueUpCommand = new RelayCommand(parameter => _ = MoveOptionValueAsync(parameter as OfferingOptionValue, -1), () => CanEdit && IsManagingOptionValues);
        MoveOptionValueDownCommand = new RelayCommand(parameter => _ = MoveOptionValueAsync(parameter as OfferingOptionValue, 1), () => CanEdit && IsManagingOptionValues);
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
        DuplicateTemplateCommand = new RelayCommand(parameter => _ = DuplicateTemplateAsync(parameter), () => CanEdit && SelectedOffering is not null);
        CancelAddTemplateCommand = new RelayCommand(_ => ResetTemplateDraft());
        RequestCancelMockupTemplateCommand = new RelayCommand(_ => RequestCancelMockupTemplate(), () => IsAddingTemplate);
        ConfirmDiscardMockupTemplateCommand = new RelayCommand(_ => ConfirmDiscardMockupTemplate(), () => IsMockupTemplateDiscardConfirmationVisible);
        KeepEditingMockupTemplateCommand = new RelayCommand(_ => IsMockupTemplateDiscardConfirmationVisible = false, () => IsMockupTemplateDiscardConfirmationVisible);
        CreateTemplateCommand = new AsyncRelayCommand(CreateTemplateAsync, CanCreateTemplate);
        BrowseLocalSourceCommand = new AsyncRelayCommand(BrowseLocalSourceAsync, () => CanEdit && IsAddingTemplate && _sourceImages is not null);
        GenerateCoveragePlanCommand = new AsyncRelayCommand(GenerateCoveragePlanAsync, () => CanEdit && IsAddingTemplate && SelectedTemplate is not null && _sourceImages is not null);
        SelectCoverageRequirementCommand = new RelayCommand(SelectCoverageRequirement, () => CanEdit && IsAddingTemplate);
        AddCoverageRequirementImageCommand = new RelayCommand(parameter =>
        {
            if (parameter is MockupTemplateCoverageRequirement requirement) SelectedCoverageRequirement = requirement;
            if (BrowseLocalSourceCommand.CanExecute(null)) BrowseLocalSourceCommand.Execute(null);
        }, () => CanEdit && IsAddingTemplate && _sourceImages is not null && CoveragePlan is not null && !IsCoveragePlanStale);
        AssignExistingCoverageImageCommand = new RelayCommand(parameter =>
        {
            SelectCoverageRequirement(parameter);
            AssignExistingCoverageImage();
        }, () => CanEdit && IsAddingTemplate && HasSelectedLocalSource && SelectedLocalSource?.IsManaged == true && _sourceImages is not null && CoveragePlan is not null && !IsCoveragePlanStale);
        RemoveLocalSourceCommand = new RelayCommand(parameter =>
        {
            if (parameter is LocalMockupSourceDraftViewModel draft) RemoveLocalSource(draft);
            else RemoveSelectedLocalSources();
        },
            () => CanEdit && IsAddingTemplate);
        SelectLocalSourceCommand = new RelayCommand(parameter => SelectLocalSource(parameter as LocalMockupSourceDraftViewModel), () => CanEdit && IsAddingTemplate);
        SortLocalSourcesCommand = new RelayCommand(parameter => SortLocalSources(parameter as string));
        OpenEnlargedPlacementEditorCommand = new RelayCommand(_ => RequestEnlargedPlacementEditor(), () => CanEdit && IsAddingTemplate && HasSelectedLocalSource);
        ReuseMappingCommand = new RelayCommand(parameter => ReuseMapping(parameter as LocalMockupSourceDraftViewModel), () => CanEdit && HasSelectedLocalSource);
        SelectAllTemplateSizesCommand = new RelayCommand(_ => SelectAllTemplateSizes(), () => CanEdit && IsAddingTemplate && TemplateAdditionalOptionChoices.Any(value => IsSizeValue(value.Value)));
        AssistMockupSourceMetadataCommand = new AsyncRelayCommand(AssistMockupSourceMetadataAsync, CanAssistMockupSourceMetadataCore);
        CancelMockupSourceMetadataCommand = new RelayCommand(_ => _mockupSourceMetadataAssistanceCts?.Cancel(), () => IsMockupSourceMetadataAssistanceBusy);
        AddTemplateColorCommand = new AsyncRelayCommand(AddTemplateColorAsync, () => CanEdit && SelectedTemplate is not null && SelectedColor is not null);
        ArchiveOptionCommand = new RelayCommand(parameter => RunArchive(parameter, CatalogRecordKind.Option));
        ArchiveOptionValueCommand = new RelayCommand(parameter => RunArchive(parameter, CatalogRecordKind.OptionValue));
        ArchiveVariantCommand = new RelayCommand(parameter => RunArchive(parameter, CatalogRecordKind.Variant));
        RequestArchiveOfferingCommand = new AsyncRelayCommand(PreviewArchiveOfferingAsync, () => CanEdit && SelectedOffering is { IsArchived: false });
        ConfirmArchiveOfferingCommand = new AsyncRelayCommand(ConfirmArchiveOfferingAsync, () => CanEdit && IsArchiveOfferingConfirmationVisible && ArchiveOfferingPlan?.CanConfirm == true);
        CancelArchiveOfferingCommand = new RelayCommand(_ => CancelArchiveOfferingArchive(), () => IsArchiveOfferingConfirmationVisible);
        RequestRestoreOfferingCommand = new AsyncRelayCommand(RestoreOfferingAsync, CanRestoreOffering);
        RequestDeleteOfferingCommand = new AsyncRelayCommand(PreviewDeleteOfferingAsync, CanDeleteOffering);
        ConfirmDeleteOfferingCommand = new AsyncRelayCommand(ConfirmDeleteOfferingAsync, () => CanDeleteOffering() && IsDeleteOfferingConfirmationVisible && DeleteOfferingPlan?.CanConfirm == true);
        CancelDeleteOfferingCommand = new RelayCommand(_ => CancelOfferingDelete(), () => IsDeleteOfferingConfirmationVisible);
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
    public event EventHandler? EnlargedPlacementEditorRequested;
    public event EventHandler? DesignAreaEditorRequested;
    public event EventHandler? CatalogChanged;

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
    public ObservableCollection<OptionValueChoiceViewModel> FilteredTemplateColorChoices { get; } = [];
    public ObservableCollection<OptionValueChoiceViewModel> TemplateAdditionalOptionChoices { get; } = [];
    public ObservableCollection<LocalMockupSourceDraftViewModel> LocalSourceDrafts { get; } = [];
    public ObservableCollection<LocalMockupSourceDraftViewModel> MappedSourceChoices { get; } = [];
    public IReadOnlyList<MockupTemplateCoverageGroupingStrategy> CoverageGroupingStrategies { get; } = Enum.GetValues<MockupTemplateCoverageGroupingStrategy>();
    private readonly List<LocalMockupSourceDraftViewModel> _archivedLocalSourceDrafts = [];
    public ICommand BrowseLocalSourceCommand { get; }
    public ICommand GenerateCoveragePlanCommand { get; }
    public ICommand SelectCoverageRequirementCommand { get; }
    public ICommand AddCoverageRequirementImageCommand { get; }
    public ICommand AssignExistingCoverageImageCommand { get; }
    public ICommand RemoveLocalSourceCommand { get; }
    public ICommand SelectLocalSourceCommand { get; }
    public ICommand SortLocalSourcesCommand { get; }
    public string FileSortLabel => LocalSourceSortLabel("File");
    public string ApplicabilitySortLabel => LocalSourceSortLabel("Applicability");
    public string StatusSortLabel => LocalSourceSortLabel("Status");
    public string FileSortAccessibleName => LocalSourceSortAccessibleName("File");
    public string ApplicabilitySortAccessibleName => LocalSourceSortAccessibleName("Applicability");
    public string StatusSortAccessibleName => LocalSourceSortAccessibleName("Status");
    public ICommand OpenEnlargedPlacementEditorCommand { get; }
    public ICommand ReuseMappingCommand { get; }
    public ICommand SelectAllTemplateSizesCommand { get; }
    public ICommand AssistMockupSourceMetadataCommand { get; }
    public ICommand CancelMockupSourceMetadataCommand { get; }
    public IAssetFilePicker FilePicker { get => _filePicker; set => _filePicker = value ?? new NullAssetFilePicker(); }
    public string LocalSourcePath { get => _localSourcePath; private set { if (SetField(ref _localSourcePath, value)) { NotifyMockupTemplateDraftChanged(); NotifyCommands(); } } }
    public LocalMockupSourceDraftViewModel? SelectedLocalSource { get => _selectedLocalSource; private set { if (SetField(ref _selectedLocalSource, value)) { OnPropertyChanged(nameof(HasSelectedLocalSource)); OnPropertyChanged(nameof(CanAssignExistingCoverageImage)); OnPropertyChanged(nameof(MappingImageWidth)); OnPropertyChanged(nameof(MappingImageHeight)); OnPropertyChanged(nameof(SelectedImagePreviewPath)); RebuildMappedSourceChoices(); NotifyCommands(); } } }
    public IReadOnlyList<LocalMockupSourceDraftViewModel> SelectedLocalSources => _selectedLocalSources;
    public int SelectedLocalSourceCount => _selectedLocalSources.Count;
    public bool HasSelectedLocalSources => _selectedLocalSources.Count > 0;
    public string LocalSourceSelectionSummary => _selectedLocalSources.Count switch
    {
        0 => "No source images selected",
        1 => "1 source image selected",
        var count => $"{count} source images selected"
    };
    public string LocalSourceArchiveLabel => _selectedLocalSources.Count > 0
        ? $"Archive selected ({_selectedLocalSources.Count})"
        : "Archive selected";
    public LocalMockupSourceDraftViewModel? SelectedMappingSource { get => _selectedMappingSource; set => SetField(ref _selectedMappingSource, value); }
    public string? SelectedImagePreviewPath => SelectedLocalSource?.PreviewPath;

    internal Stream OpenPreviewRead(string sourcePath) =>
        _mockupPlacementPreviewReader?.OpenRead(sourcePath)
        ?? throw new InvalidOperationException("Mockup preview image access is unavailable.");
    public bool HasSelectedLocalSource => SelectedLocalSource is not null;
    public bool HasLocalSource => LocalSourceDrafts.Count > 0 || !string.IsNullOrWhiteSpace(LocalSourcePath);
    public bool IsMockupSourceMetadataAssistanceBusy => _isMockupSourceMetadataAssistanceBusy;
    public bool CanAssistMockupSourceMetadata => CanAssistMockupSourceMetadataCore();
    public string MockupSourceMetadataAssistanceStatus => _mockupSourceMetadataAssistanceStatus;
    public bool HasMockupSourceMetadataAssistanceStatus => !string.IsNullOrWhiteSpace(MockupSourceMetadataAssistanceStatus);
    public MockupTemplateCoveragePlan? CoveragePlan
    {
        get => _coveragePlan;
        private set
        {
            if (!SetField(ref _coveragePlan, value)) return;
            OnPropertyChanged(nameof(CoverageRequirements));
            OnPropertyChanged(nameof(CoverageSummary));
            OnPropertyChanged(nameof(IncompleteCoverageSummary));
            OnPropertyChanged(nameof(HasIncompleteCoverageSummary));
            OnPropertyChanged(nameof(HasCoveragePlan));
            OnPropertyChanged(nameof(HasCoverageRequirements));
            OnPropertyChanged(nameof(IsCoveragePlanComplete));
            OnPropertyChanged(nameof(IsCoveragePlanStale));
            NotifyCoveragePresentation();
            NotifyCommands();
        }
    }
    public IReadOnlyList<MockupTemplateCoverageRequirement> CoverageRequirements => CoveragePlan?.Requirements ?? [];
    public bool HasCoveragePlan => CoveragePlan is not null;
    public bool HasCoverageRequirements => CoverageRequirements.Count > 0;
    public bool CanAssignExistingCoverageImage => HasSelectedLocalSource && SelectedLocalSource?.IsManaged == true;
    public IEnumerable<LocalMockupSourceDraftViewModel> CoverageExemplarChoices => LocalSourceDrafts.Where(value => value.IsManaged);
    public LocalMockupSourceDraftViewModel? SelectedCoverageExemplar
    {
        get => _selectedCoverageExemplar;
        set
        {
            if (!SetField(ref _selectedCoverageExemplar, value)) return;
            OnPropertyChanged(nameof(ExemplarMappingSummary));
            OnPropertyChanged(nameof(HasExemplarMappingSummary));
            if (SelectedCoverageRequirement is { } requirement && SelectedLocalSource is { IsManaged: false } draft)
                ApplyCoverageApplicability(requirement, draft, reuseExemplarMapping: true);
            NotifyCommands();
        }
    }
    public string ExemplarMappingSummary => SelectedCoverageExemplar is null
        ? "Optional: choose a managed source image to reuse safe applicability and placement defaults."
        : SelectedCoverageExemplar.Mapping is { } mapping
            ? $"Mapping reuse is offered only for matching {mapping.ImageWidth} × {mapping.ImageHeight} pixel images."
            : "This exemplar has no placement mapping; uploaded images will need explicit placement.";
    public bool HasExemplarMappingSummary => SelectedCoverageExemplar is not null;
    public bool IsCoveragePlanComplete => CoveragePlan?.IsComplete == true && !IsCoveragePlanStale;
    public bool IsCoveragePlanStale => CoveragePlan is not null && CoveragePlan.IsStaleAgainst(CurrentCoverageContext());
    public bool IsCoverageLoading
    {
        get => _isCoverageLoading;
        private set
        {
            if (!SetField(ref _isCoverageLoading, value)) return;
            NotifyCoveragePresentation();
            NotifyCommands();
        }
    }
    public string CoverageError
    {
        get => _coverageError;
        private set
        {
            if (!SetField(ref _coverageError, value)) return;
            OnPropertyChanged(nameof(HasCoverageError));
            NotifyCoveragePresentation();
        }
    }
    public bool HasCoverageError => !string.IsNullOrWhiteSpace(CoverageError);
    public bool HasCoveragePanel => HasCoveragePlan || IsCoverageLoading || HasCoverageError;
    public MockupTemplateCoverageViewState CoverageState
    {
        get
        {
            if (!IsAddingTemplate || _sourceImages is null || SelectedTemplate is null) return MockupTemplateCoverageViewState.Unavailable;
            if (IsCoverageLoading) return MockupTemplateCoverageViewState.Loading;
            if (HasCoverageError) return MockupTemplateCoverageViewState.Error;
            if (IsReadOnly || SelectedOffering?.IsArchived == true) return MockupTemplateCoverageViewState.ReadOnly;
            if (CoveragePlan is null) return MockupTemplateCoverageViewState.Unavailable;
            if (!CoveragePlan.HasTargetDesignArea) return MockupTemplateCoverageViewState.NoTargetDesignArea;
            if (IsCoveragePlanStale) return MockupTemplateCoverageViewState.Stale;
            if (CoveragePlan.IsComplete) return MockupTemplateCoverageViewState.Complete;
            if (CoveragePlan.AmbiguousCount > 0) return MockupTemplateCoverageViewState.Ambiguous;
            if (CoveragePlan.IncompleteCount > 0) return MockupTemplateCoverageViewState.Incomplete;
            return MockupTemplateCoverageViewState.Missing;
        }
    }
    public bool HasCoverageStatus => CoverageState != MockupTemplateCoverageViewState.Unavailable;
    public string CoverageStateLabel => CoverageState switch
    {
        MockupTemplateCoverageViewState.Loading => "Loading coverage plan",
        MockupTemplateCoverageViewState.NoTargetDesignArea => "No target Design Area",
        MockupTemplateCoverageViewState.Complete => "Coverage complete",
        MockupTemplateCoverageViewState.Missing => "Coverage needs images",
        MockupTemplateCoverageViewState.Ambiguous => "Coverage needs disambiguation",
        MockupTemplateCoverageViewState.Incomplete => "Coverage needs setup",
        MockupTemplateCoverageViewState.Stale => "Coverage plan is stale",
        MockupTemplateCoverageViewState.ReadOnly => "Coverage is read-only",
        MockupTemplateCoverageViewState.Error => "Coverage plan could not be loaded",
        _ => "Coverage planning is unavailable"
    };
    public string CoverageStateHelp => CoverageState switch
    {
        MockupTemplateCoverageViewState.Loading => "Loading current source images and compatible Variants.",
        MockupTemplateCoverageViewState.NoTargetDesignArea => "Choose an active target Design Area before planning coverage.",
        MockupTemplateCoverageViewState.Complete => "Every compatible Variant resolves to exactly one usable source image.",
        MockupTemplateCoverageViewState.Missing => "Assign or add a source image for each missing requirement.",
        MockupTemplateCoverageViewState.Ambiguous => "Some Variants match more than one source image. Narrow their applicability or archive the extra row.",
        MockupTemplateCoverageViewState.Incomplete => "Complete applicability and placement mapping for the affected source rows.",
        MockupTemplateCoverageViewState.Stale => "The catalog context changed. Refresh the plan before applying new requirement defaults.",
        MockupTemplateCoverageViewState.ReadOnly => "This Store or Mockup Template is archived, so coverage changes are disabled.",
        MockupTemplateCoverageViewState.Error => CoverageError,
        _ => "Generate a coverage plan to see which compatible Variants still need mockup images."
    };
    public string CoverageSummary => CoveragePlan is null
        ? "Generate a coverage plan to see which compatible Variants still need mockup images."
        : IsCoveragePlanStale
            ? $"{CoveragePlan.Summary} Refresh required because the catalog context changed."
            : CoveragePlan.Summary;
    public string IncompleteCoverageSummary => CoveragePlan?.IncompleteSourceImageIds.Count > 0
        ? $"{CoveragePlan.IncompleteSourceImageIds.Count} active source row{(CoveragePlan.IncompleteSourceImageIds.Count == 1 ? string.Empty : "s")} still need applicability or mapping setup."
        : string.Empty;
    public bool HasIncompleteCoverageSummary => !string.IsNullOrWhiteSpace(IncompleteCoverageSummary);
    public MockupTemplateCoverageGroupingStrategy CoverageGroupingStrategy
    {
        get => _coverageGroupingStrategy;
        set { if (SetField(ref _coverageGroupingStrategy, value)) OnPropertyChanged(nameof(CoverageSummary)); }
    }
    public MockupTemplateCoverageRequirement? SelectedCoverageRequirement
    {
        get => _selectedCoverageRequirement;
        private set => SetField(ref _selectedCoverageRequirement, value);
    }

    private void RequestEnlargedPlacementEditor()
    {
        if (!CanEdit || !IsAddingTemplate || !HasSelectedLocalSource) return;
        EnlargedPlacementEditorRequested?.Invoke(this, EventArgs.Empty);
    }

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
            _offeringReadiness = null;
            OnPropertyChanged(nameof(ReadyMockupTemplateCount));
            OnPropertyChanged(nameof(HasOfferingReadinessGuidance));
            OnPropertyChanged(nameof(OfferingReadinessGuidance));
            OnPropertyChanged(nameof(OfferingReadinessSummary));
            OnPropertyChanged(nameof(OfferingReadinessStatus));
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
    public IEnumerable<PrintProvider> AvailablePrintProviders => PrintProviders
        .Where(value => !value.IsArchived)
        .GroupBy(value => value.Name.Trim(), StringComparer.OrdinalIgnoreCase)
        .Select(group => group.FirstOrDefault(value => value.Id == SelectedOffering?.PrintProviderId) ?? group.First())
        .OrderBy(value => value.Name, StringComparer.OrdinalIgnoreCase);
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
            OnPropertyChanged(nameof(PlacementAspectRatio));
            OnPropertyChanged(nameof(IsKeepAspectRatioAvailable));
            OnPropertyChanged(nameof(CanKeepAspectRatio));
            KeepAspectRatio = PlacementAspectRatio > 0;
            NotifyMockupTemplateDraftChanged();
            OnPropertyChanged(nameof(IsEditingDesignArea));
            OnPropertyChanged(nameof(DesignAreaEditorDialogTitle));
            OnPropertyChanged(nameof(IsCoveragePlanStale));
            OnPropertyChanged(nameof(CoverageSummary));
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
    public string MappingWidthText { get => _mappingWidthText; set => SetMappingText(ref _mappingWidthText, value, ref _mappingWidth, nameof(MappingWidthText), nameof(MappingWidth), true); }
    public string MappingHeightText { get => _mappingHeightText; set => SetMappingText(ref _mappingHeightText, value, ref _mappingHeight, nameof(MappingHeightText), nameof(MappingHeight), false); }
    public double MappingImageWidth => SelectedProviderMockup?.ImageWidth ?? SelectedLocalSource?.ImageWidth ?? 0;
    public double MappingImageHeight => SelectedProviderMockup?.ImageHeight ?? SelectedLocalSource?.ImageHeight ?? 0;
    public double PlacementAspectRatio => SelectedPlaceholder is { Width: > 0, Height: > 0 }
        ? SelectedPlaceholder.Width / (double)SelectedPlaceholder.Height
        : 0;
    public bool IsKeepAspectRatioAvailable => double.IsFinite(PlacementAspectRatio) && PlacementAspectRatio > 0;
    public bool CanKeepAspectRatio => CanEdit && IsKeepAspectRatioAvailable;
    public bool KeepAspectRatio
    {
        get => _keepAspectRatio;
        set
        {
            var enabled = value && IsKeepAspectRatioAvailable;
            if (SetField(ref _keepAspectRatio, enabled))
            {
                OnPropertyChanged(nameof(CanKeepAspectRatio));
                NotifyMockupTemplateDraftChanged();
                NotifyCommands();
            }
        }
    }

    public string OfferingName { get => _offeringName; set { if (SetField(ref _offeringName, value)) NotifyCommands(); } }
    public string OfferingDescription { get => _offeringDescription; set { if (SetField(ref _offeringDescription, value)) NotifyCommands(); } }
    public string ProviderNetworkCode { get => _providerNetworkCode; set { if (SetField(ref _providerNetworkCode, value)) NotifyCommands(); } }
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
    public bool PlaceholderPrimaryForArtworkGeneration { get => _placeholderPrimaryForArtworkGeneration; set { if (SetField(ref _placeholderPrimaryForArtworkGeneration, value)) { NotifyDesignAreaDraftChanged(); NotifyCommands(); } } }
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
    public string TemplateColorSearchText
    {
        get => _templateColorSearchText;
        set
        {
            if (!SetField(ref _templateColorSearchText, value ?? string.Empty)) return;
            RefreshFilteredTemplateColorChoices();
        }
    }
    public bool HasNoMatchingTemplateColors => !string.IsNullOrWhiteSpace(TemplateColorSearchText) && FilteredTemplateColorChoices.Count == 0;
    public OptionKind SelectedOptionKind { get => _selectedOptionKind; set => SetField(ref _selectedOptionKind, value); }

    public bool IsAddingOption { get => _isAddingOption; private set { if (SetField(ref _isAddingOption, value)) NotifyCommands(); } }
    public bool IsAddingPrintProvider { get => _isAddingPrintProvider; private set { if (SetField(ref _isAddingPrintProvider, value)) NotifyCommands(); } }
    public bool IsAddingOptionValue { get => _isAddingOptionValue; private set { if (SetField(ref _isAddingOptionValue, value)) NotifyCommands(); } }
    public bool IsEditingOptionValue { get => _isEditingOptionValue; private set { if (SetField(ref _isEditingOptionValue, value)) NotifyCommands(); } }
    public bool IsManagingOptionValues { get => _isManagingOptionValues; private set => SetField(ref _isManagingOptionValues, value); }
    public bool IsAddingVariant { get => _isAddingVariant; private set { if (SetField(ref _isAddingVariant, value)) NotifyCommands(); } }
    public bool IsAddingBulkVariants { get => _isAddingBulkVariants; private set { if (SetField(ref _isAddingBulkVariants, value)) NotifyCommands(); } }
    public bool IsAddingPlaceholder { get => _isAddingPlaceholder; private set { if (SetField(ref _isAddingPlaceholder, value)) { OnPropertyChanged(nameof(IsEditingDesignArea)); OnPropertyChanged(nameof(DesignAreaEditorDialogTitle)); NotifyDesignAreaDraftChanged(); NotifyCommands(); } } }
    public bool IsAddingTemplate { get => _isAddingTemplate; private set { if (SetField(ref _isAddingTemplate, value)) { OnPropertyChanged(nameof(IsEditingMockupTemplate)); OnPropertyChanged(nameof(MockupTemplateEditorDialogTitle)); NotifyMockupTemplateDraftChanged(); NotifyCoveragePresentation(); NotifyCommands(); } } }
    public bool IsAvailable { get; private set; }
    public bool IsReadOnly { get => _isReadOnly; private set { if (SetField(ref _isReadOnly, value)) { OnPropertyChanged(nameof(CanEdit)); NotifyCoveragePresentation(); NotifyCommands(); } } }
    public bool CanEdit => _storeEditorAttached && IsAvailable && !IsReadOnly && !IsBusy && SelectedOffering?.IsArchived != true;
    public bool IsBusy { get => _isBusy; private set { if (SetField(ref _isBusy, value)) { OnPropertyChanged(nameof(CanEdit)); OnPropertyChanged(nameof(CanKeepAspectRatio)); NotifyCommands(); } } }
    public string ErrorMessage { get => _error; private set { if (SetField(ref _error, value)) OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasActiveDraft => IsAddingPrintProvider || IsAddingOption || IsAddingOptionValue || IsEditingOptionValue || IsAddingVariant || IsAddingBulkVariants || IsAddingPlaceholder || IsAddingTemplate;
    public CatalogArchivePlan? ArchiveOfferingPlan { get => _archiveOfferingPlan; private set { if (SetField(ref _archiveOfferingPlan, value)) { OnPropertyChanged(nameof(HasArchiveOfferingPlan)); NotifyCommands(); } } }
    public CatalogOfferingDeletePlan? DeleteOfferingPlan { get => _deleteOfferingPlan; private set { if (SetField(ref _deleteOfferingPlan, value)) { OnPropertyChanged(nameof(HasDeleteOfferingPlan)); NotifyCommands(); } } }
    public bool HasDeleteOfferingPlan => DeleteOfferingPlan is not null;
    public bool IsDeleteOfferingConfirmationVisible { get => _isDeleteOfferingConfirmationVisible; private set { if (SetField(ref _isDeleteOfferingConfirmationVisible, value)) NotifyCommands(); } }
    public bool HasArchiveOfferingPlan => ArchiveOfferingPlan is not null;
    public bool IsArchiveOfferingConfirmationVisible { get => _isArchiveOfferingConfirmationVisible; private set { if (SetField(ref _isArchiveOfferingConfirmationVisible, value)) NotifyCommands(); } }
    public bool HasMeaningfulMockupTemplateDraft => IsAddingTemplate && _mockupTemplateDraftBaseline is not null && CurrentMockupTemplateDraftState() != _mockupTemplateDraftBaseline;
    public string MockupTemplateLifecycleLabel => CurrentMockupTemplateReadiness().Lifecycle == MockupTemplateLifecycle.ReadyForUse ? "Ready for use" : "Draft";
    public IReadOnlyList<string> MockupTemplateReadinessMessages => CurrentMockupTemplateReadiness().Blockers.Select(MockupTemplateReadinessMessageTranslator.Translate).ToArray();
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

    public IEnumerable<OfferingOption> AvailableOptions => CatalogSetupQueries.ActiveOptions(Options, SelectedOffering?.Id);
    public IEnumerable<OfferingOptionValue> AvailableValues => CatalogSetupQueries.ActiveValues(OptionValues, SelectedOffering?.Id, SelectedOption?.Id);
    public IEnumerable<OfferingVariant> AvailableVariants => CatalogSetupQueries.ActiveVariants(Variants, SelectedOffering?.Id);
    public IEnumerable<OfferingPlaceholder> AvailablePlaceholders => CatalogSetupQueries.ActiveDesignAreas(Placeholders, SelectedOffering?.Id);
    public IEnumerable<MockupTemplate> AvailableTemplates => CatalogSetupQueries.ActiveTemplates(Templates, SelectedOffering?.Id);
    public IEnumerable<OfferingOptionValue> AvailableColors => CatalogSetupQueries.ActiveColors(OptionValues, Options, SelectedOffering?.Id);
    public bool HasAvailableOptions => AvailableOptions.Any();
    public bool HasAvailableValues => AvailableValues.Any();
    public bool HasAvailableVariants => AvailableVariants.Any();
    public bool HasAvailablePlaceholders => AvailablePlaceholders.Any();
    public bool HasAvailableTemplates => AvailableTemplates.Any();
    public int AvailableVariantCount => AvailableVariants.Count();
    public int AvailableDesignAreaCount => AvailablePlaceholders.Count();
    public int AvailableTemplateCount => AvailableTemplates.Count();
    public int ReadyMockupTemplateCount => _offeringReadiness?.ReadyMockupTemplateCount ?? 0;
    public IReadOnlyList<OfferingReadinessIssue> OfferingReadinessIssues => _offeringReadiness?.Issues ?? [];
    public IReadOnlyList<string> OfferingReadinessGuidance => _offeringReadiness?.Issues.Select(OfferingReadinessMessageTranslator.Translate).ToArray() ?? [];
    public bool HasOfferingReadinessGuidance => OfferingReadinessGuidance.Count > 0;
    public string OfferingReadinessSummary => _offeringReadiness is null
        ? "Catalog readiness is loading."
        : _offeringReadiness.Status switch
        {
            FusionCanvas.Application.Catalog.OfferingReadinessStatus.ReadyForMockupGeneration => $"{ReadyMockupTemplateCount} Mockup Template{(ReadyMockupTemplateCount == 1 ? string.Empty : "s")} ready for mockup generation. Item Colors and Design artwork are still configured per Item.",
            FusionCanvas.Application.Catalog.OfferingReadinessStatus.NeedsAttention => "Mockup Templates need attention before they can be used.",
            _ => "Catalog setup is incomplete. Complete the named prerequisites before using mockups."
        };
    public string OfferingReadinessError => _offeringReadinessError;
    public bool HasOfferingReadinessError => !string.IsNullOrWhiteSpace(OfferingReadinessError);
    public string OfferingReadinessStatus
    {
        get
        {
            if (SelectedOffering?.IsArchived == true)
            {
                return "Archived";
            }

            return _offeringReadiness?.Status switch
            {
                FusionCanvas.Application.Catalog.OfferingReadinessStatus.ReadyForMockupGeneration => "Ready for mockup generation",
                FusionCanvas.Application.Catalog.OfferingReadinessStatus.NeedsAttention => "Needs attention",
                FusionCanvas.Application.Catalog.OfferingReadinessStatus.Incomplete => "Setup incomplete",
                _ => "Catalog readiness is loading"
            };
        }
    }

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
    public ICommand MoveOptionValueUpCommand { get; }
    public ICommand MoveOptionValueDownCommand { get; }
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
    public ICommand DuplicateTemplateCommand { get; }
    public ICommand CancelAddTemplateCommand { get; }
    public ICommand RequestCancelMockupTemplateCommand { get; }
    public ICommand ConfirmDiscardMockupTemplateCommand { get; }
    public ICommand KeepEditingMockupTemplateCommand { get; }
    public ICommand CreateTemplateCommand { get; }
    public ICommand AddTemplateColorCommand { get; }
    public ICommand ArchiveOptionCommand { get; }
    public ICommand ArchiveOptionValueCommand { get; }
    public ICommand ArchiveVariantCommand { get; }
    public ICommand RequestArchiveOfferingCommand { get; }
    public ICommand ConfirmArchiveOfferingCommand { get; }
    public ICommand CancelArchiveOfferingCommand { get; }
    public ICommand RequestRestoreOfferingCommand { get; }
    public ICommand RequestDeleteOfferingCommand { get; }
    public ICommand ConfirmDeleteOfferingCommand { get; }
    public ICommand CancelDeleteOfferingCommand { get; }
    public ICommand ArchivePlaceholderCommand { get; }
    public ICommand ConfirmDesignAreaArchiveCommand { get; }
    public ICommand CancelDesignAreaArchiveCommand { get; }
    public ICommand ArchiveTemplateCommand { get; }
    public ICommand PreviewBulkVariantsCommand { get; }
    public ICommand ConfirmBulkVariantsCommand { get; }
    public ICommand CancelBulkVariantsCommand { get; }

    public async Task LoadForStoreAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        CancelArchiveOfferingArchive();
        CancelOfferingDelete();
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
            _templateSourceImages = mockups.SourceImages ?? [];
            _templateSourceConditions = mockups.SourceImageOptionValues ?? [];
            ApplyCatalog(catalog);
            Replace(Templates, mockups.Templates);
            Replace(TemplateColors, mockups.Colors);
            Replace(TemplateRevisions, mockups.Revisions);
            SelectedTemplate = Templates.FirstOrDefault(value => value.Id == SelectedTemplate?.Id) ?? AvailableTemplates.FirstOrDefault();
            RefreshOfferingCollections();
            await LoadOfferingReadinessAsync(cancellationToken).ConfigureAwait(true);
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
            CancelArchiveOfferingArchive();
            CancelOfferingDelete();
            ClearDesignAreaArchiveConfirmation();
        }
        _requestedOfferingId = offeringId;
        SelectedOffering = offeringId is null ? null : Offerings.FirstOrDefault(value => value.Id == offeringId.Value);
        OnPropertyChanged(nameof(IsOfferingContextUnavailable));
        _ = LoadOfferingReadinessAsync();
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

    private async Task MoveOptionValueAsync(OfferingOptionValue? value, int offset)
    {
        if (value is null || SelectedOffering is null || SelectedOption is null) return;
        var values = AvailableValues.ToArray();
        var index = Array.FindIndex(values, candidate => candidate.Id == value.Id);
        var target = index + offset;
        if (index < 0 || target < 0 || target >= values.Length) return;
        (values[index], values[target]) = (values[target], values[index]);
        await RunMutationAsync(() => _catalog.ReorderOptionValuesAsync(new ReorderOptionValuesRequest(SelectedOffering.StoreId, SelectedOption.Id, values.Select(candidate => candidate.Id).ToArray()))).ConfigureAwait(true);
    }

    public async Task ReorderOptionValuesAsync(OfferingOptionValue source, OfferingOptionValue target)
    {
        if (SelectedOption is null || source.OptionId != SelectedOption.Id || target.OptionId != SelectedOption.Id || source.Id == target.Id) return;
        var values = AvailableValues.ToList();
        var sourceIndex = values.FindIndex(value => value.Id == source.Id);
        var targetIndex = values.FindIndex(value => value.Id == target.Id);
        if (sourceIndex < 0 || targetIndex < 0) return;
        var moved = values[sourceIndex];
        values.RemoveAt(sourceIndex);
        values.Insert(targetIndex, moved);
        await RunMutationAsync(() => _catalog.ReorderOptionValuesAsync(new ReorderOptionValuesRequest(SelectedOffering!.StoreId, SelectedOption.Id, values.Select(value => value.Id).ToArray()))).ConfigureAwait(true);
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
                        EmptyToNull(PlaceholderProviderReference), guidance, PlaceholderPrimaryForArtworkGeneration)).ConfigureAwait(true)
                    : await _offeringManagement.CreateDesignAreaAsync(new CreateFocusedDesignAreaRequest(
                        CurrentContext(), PlaceholderName, PlaceholderPosition, PlaceholderDecorationMethod, width, height,
                        selectedVariantIds, PlaceholderUsesAllVariants, EmptyToNull(PlaceholderDescription), EmptyToNull(PlaceholderProviderReference), guidance, PlaceholderPrimaryForArtworkGeneration)).ConfigureAwait(true);
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
            if (_sourceImages is not null && HasPendingLocalSourceChanges && SelectedProviderMockup is null)
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
                    template = created.TemplateId is Guid createdTemplateId
                        ? created.State.Templates.SingleOrDefault(value => value.Id == createdTemplateId)
                        : created.State.Templates.LastOrDefault(value => value.Name == TemplateName.Trim());
                    sourceState = created.State;
                }
                if (template is null) { ErrorMessage = "The Mockup Template could not be selected."; return; }
                SelectedTemplate = template;
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
                var sourceChangesTotal = LocalSourceDrafts.Count + _archivedLocalSourceDrafts.Count;
                var sourceChangesSaved = 0;
                foreach (var draft in LocalSourceDrafts)
                {
                    var existingSourceImageIds = sourceState?.SourceImages?.Select(image => image.Id).ToHashSet() ?? [];
                    var sourceResult = draft.IsManaged
                        ? await _sourceImages.UpdateAsync(new UpdateLocalMockupTemplateSourceRequest(SelectedOffering.StoreId, template.Id, draft.SourceImageId ?? Guid.Empty, draft.OptionValueIds, draft.Mapping)).ConfigureAwait(true)
                        : await _sourceImages.AddAsync(new AddLocalMockupTemplateSourceRequest(SelectedOffering.StoreId, template.Id, draft.Path, draft.OptionValueIds, draft.Mapping)).ConfigureAwait(true);
                    sourceState = sourceResult.State;
                    if (!sourceResult.Succeeded)
                    {
                        ApplyMockups(sourceState);
                        ErrorMessage = FormatPartialLocalSourceSaveError(
                            sourceResult.Error ?? $"The local source image '{draft.DisplayName}' could not be added.",
                            sourceChangesSaved,
                            sourceChangesTotal);
                        return;
                    }
                    sourceChangesSaved++;
                    if (!draft.IsManaged && sourceResult.State.SourceImages is { } savedImages)
                    {
                        var addedImage = savedImages.SingleOrDefault(image => !existingSourceImageIds.Contains(image.Id));
                        if (addedImage is not null) draft.MarkManaged(addedImage.Id);
                    }
                }
                foreach (var draft in _archivedLocalSourceDrafts.Where(value => value.SourceImageId is not null))
                {
                    var archiveResult = await _sourceImages.UpdateAsync(new UpdateLocalMockupTemplateSourceRequest(SelectedOffering.StoreId, template.Id, draft.SourceImageId!.Value, draft.OptionValueIds, draft.Mapping, Archive: true)).ConfigureAwait(true);
                    sourceState = archiveResult.State;
                    if (!archiveResult.Succeeded)
                    {
                        ApplyMockups(sourceState);
                        ErrorMessage = FormatPartialLocalSourceSaveError(
                            archiveResult.Error ?? $"The local source image '{draft.DisplayName}' could not be archived.",
                            sourceChangesSaved,
                            sourceChangesTotal);
                        return;
                    }
                    sourceChangesSaved++;
                }
                if (sourceState is not null) ApplyMockups(sourceState);
                SelectedTemplate = template;
                EndTemplateDraft();
                TemplateName = string.Empty;
                LocalSourcePath = string.Empty;
                ClearLocalSourceSelectionState();
                LocalSourceDrafts.Clear();
                RefreshLocalSourceRowPresentation();
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
        var paths = await _filePicker.PickImportFilesAsync().ConfigureAwait(true);
        if (paths.Count > 0)
        {
            LocalMockupSourceDraftViewModel? firstDraft = null;
            foreach (var path in paths.Where(path => !string.IsNullOrWhiteSpace(path)))
            {
                var dimensions = (Width: 0, Height: 0);
                string? previewReadError = null;
                if (_rasterImageMetadataReader is not null)
                {
                    try
                    {
                        var image = await _rasterImageMetadataReader.ReadAsync(path).ConfigureAwait(true);
                        dimensions = (image.Width, image.Height);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        // Keep zero dimensions as a fallback while preserving the failure for presentation.
                        dimensions = (0, 0);
                        previewReadError = string.IsNullOrWhiteSpace(exception.Message)
                            ? $"Preview dimensions could not be read ({exception.GetType().Name})."
                            : $"Preview dimensions could not be read: {exception.Message}";
                    }
                }
                else
                {
                    previewReadError = "Preview dimensions could not be read because the image metadata reader is unavailable.";
                }

                var draft = new LocalMockupSourceDraftViewModel(path, [], imageWidth: dimensions.Width, imageHeight: dimensions.Height, previewReadError: previewReadError);
                LocalSourceDrafts.Add(draft);
                firstDraft ??= draft;
            }

            if (firstDraft is null)
            {
                return;
            }

            RefreshLocalSourceRowPresentation();
            ApplyLocalSourceSort();
            SelectLocalSource(firstDraft);
            if (SelectedCoverageRequirement is { } requirement)
            {
                ApplyCoverageApplicability(requirement, firstDraft, reuseExemplarMapping: true);
                var mapping = firstDraft.Mapping;
                MappingXText = mapping is null ? string.Empty : FormatMapping(mapping.X);
                MappingYText = mapping is null ? string.Empty : FormatMapping(mapping.Y);
                MappingWidthText = mapping is null ? string.Empty : FormatMapping(mapping.Width);
                MappingHeightText = mapping is null ? string.Empty : FormatMapping(mapping.Height);
            }
            OnPropertyChanged(nameof(HasLocalSource));
            OnPropertyChanged(nameof(CoverageExemplarChoices));
            NotifyMockupTemplateDraftChanged();
        }
    }


    private void SelectLocalSource(LocalMockupSourceDraftViewModel? draft)
    {
        if (draft is null || !LocalSourceDrafts.Contains(draft)) return;
        CaptureSelectedLocalSource();
        SelectedLocalSource = draft;
        _selectedLocalSources.Clear();
        _selectedLocalSources.Add(draft);
        _localSourceSelectionAnchor = draft;
        foreach (var row in LocalSourceDrafts) row.IsSelected = ReferenceEquals(row, draft);
        foreach (var color in TemplateColorChoices) color.IsSelected = draft.OptionValueIds.Contains(color.Value.Id);
        foreach (var option in TemplateAdditionalOptionChoices) option.IsSelected = draft.OptionValueIds.Contains(option.Value.Id);
        var mapping = draft.Mapping;
        MappingXText = mapping is null ? string.Empty : FormatMapping(mapping.X);
        MappingYText = mapping is null ? string.Empty : FormatMapping(mapping.Y);
        MappingWidthText = mapping is null ? string.Empty : FormatMapping(mapping.Width);
        MappingHeightText = mapping is null ? string.Empty : FormatMapping(mapping.Height);
        LocalSourcePath = draft.Path;
        NotifyLocalSourceSelectionChanged();
    }

    private void SelectCoverageRequirement(object? parameter)
    {
        if (parameter is not MockupTemplateCoverageRequirement requirement || IsCoveragePlanStale) return;
        SelectedCoverageRequirement = requirement;
        ApplyCoverageApplicability(requirement, SelectedLocalSource);
        NotifyCommands();
    }

    private void ApplyCoverageApplicability(MockupTemplateCoverageRequirement requirement, LocalMockupSourceDraftViewModel? draft, bool reuseExemplarMapping = false)
    {
        var ids = requirement.Applicability.Select(value => value.Id).ToHashSet();
        if (SelectedCoverageExemplar is { } exemplar)
        {
            var variants = requirement.VariantIds
                .Select(id => AvailableVariants.FirstOrDefault(value => value.Id == id))
                .Where(value => value is not null)
                .ToArray();
            foreach (var optionValueId in exemplar.OptionValueIds)
            {
                var optionValue = OptionValues.FirstOrDefault(value => value.Id == optionValueId);
                if (optionValue is not null
                    && optionValue.OptionId is var optionId
                    && AvailableOptions.FirstOrDefault(value => value.Id == optionId)?.OptionKind != OptionKind.Color
                    && variants.Length > 0
                    && variants.All(value => value!.OptionValueIds.Contains(optionValueId)))
                {
                    ids.Add(optionValueId);
                }
            }
        }
        var mapping = draft?.Mapping;
        if (reuseExemplarMapping && draft is not null && mapping is null && SelectedCoverageExemplar?.Mapping is { } exemplarMapping)
        {
            if (draft.ImageWidth == exemplarMapping.ImageWidth && draft.ImageHeight == exemplarMapping.ImageHeight)
            {
                mapping = exemplarMapping;
                draft.SetAssistanceStatus("Mapping reused from exemplar", null);
            }
            else
            {
                draft.SetAssistanceStatus("Needs mapping review", null);
            }
        }
        var selectedIds = ids.ToArray();
        foreach (var choice in TemplateColorChoices) choice.IsSelected = ids.Contains(choice.Value.Id);
        foreach (var choice in TemplateAdditionalOptionChoices) choice.IsSelected = ids.Contains(choice.Value.Id);
        if (draft is not null)
        {
            draft.UpdateMetadata(selectedIds, mapping, string.Join(", ", selectedIds.Select(id => OptionValues.FirstOrDefault(value => value.Id == id)).Where(value => value is not null).Select(value => ValueLabel(value!))));
        }
        NotifyMockupTemplateDraftChanged();
    }

    private void AssignExistingCoverageImage()
    {
        if (SelectedCoverageRequirement is not { } requirement || SelectedLocalSource is not { IsManaged: true } draft) return;
        ApplyCoverageApplicability(requirement, draft);
        ErrorMessage = string.Empty;
        SelectLocalSource(draft);
    }

    private async Task GenerateCoveragePlanAsync()
    {
        if (_sourceImages is null || SelectedOffering is null || SelectedTemplate is null) return;
        IsBusy = true;
        IsCoverageLoading = true;
        CoverageError = string.Empty;
        ErrorMessage = string.Empty;
        try
        {
            CoveragePlan = await _sourceImages.PlanAsync(SelectedOffering.StoreId, SelectedTemplate.Id, CoverageGroupingStrategy).ConfigureAwait(true);
            if (CoveragePlan is null)
            {
                CoverageError = "Coverage planning is unavailable for this Mockup Template.";
                ErrorMessage = CoverageError;
            }
        }
        catch (Exception exception)
        {
            CoverageError = exception.Message;
            ErrorMessage = exception.Message;
        }
        finally
        {
            IsCoverageLoading = false;
            IsBusy = false;
        }
    }

    private MockupTemplateCoverageContext CurrentCoverageContext()
    {
        var variantIds = SelectedPlaceholder?.VariantIds.Where(id => AvailableVariants.Any(value => value.Id == id)).ToArray() ?? [];
        var optionValueIds = AvailableVariants
            .Where(value => variantIds.Contains(value.Id))
            .SelectMany(value => value.OptionValueIds)
            .Distinct()
            .ToArray();
        return new(SelectedTemplate?.Id ?? Guid.Empty, SelectedPlaceholder?.Id, variantIds, optionValueIds);
    }

    public void SelectLocalSourceWithModifiers(LocalMockupSourceDraftViewModel? draft, bool toggle, bool range)
    {
        if (draft is null || !LocalSourceDrafts.Contains(draft)) return;

        if (!toggle && !range)
        {
            SelectLocalSource(draft);
            return;
        }

        CaptureSelectedLocalSource();
        var visibleRows = LocalSourceDrafts.ToArray();
        if (range && _localSourceSelectionAnchor is not null && visibleRows.Contains(_localSourceSelectionAnchor))
        {
            var anchorIndex = Array.IndexOf(visibleRows, _localSourceSelectionAnchor);
            var clickedIndex = Array.IndexOf(visibleRows, draft);
            var start = Math.Min(anchorIndex, clickedIndex);
            var end = Math.Max(anchorIndex, clickedIndex);
            _selectedLocalSources.Clear();
            _selectedLocalSources.AddRange(visibleRows.Skip(start).Take(end - start + 1));
        }
        else if (toggle)
        {
            if (!_selectedLocalSources.Remove(draft)) _selectedLocalSources.Add(draft);
            _localSourceSelectionAnchor ??= draft;
        }
        else
        {
            _selectedLocalSources.Clear();
            _selectedLocalSources.Add(draft);
            _localSourceSelectionAnchor = draft;
        }

        RefreshLocalSourceSelectionPresentation();
        var active = _selectedLocalSources.Contains(draft) ? draft : _selectedLocalSources.LastOrDefault();
        if (active is null) ClearActiveLocalSource();
        else ActivateLocalSource(active);
        NotifyLocalSourceSelectionChanged();
    }

    private void ActivateLocalSource(LocalMockupSourceDraftViewModel draft)
    {
        SelectedLocalSource = draft;
        foreach (var color in TemplateColorChoices) color.IsSelected = draft.OptionValueIds.Contains(color.Value.Id);
        foreach (var option in TemplateAdditionalOptionChoices) option.IsSelected = draft.OptionValueIds.Contains(option.Value.Id);
        var mapping = draft.Mapping;
        MappingXText = mapping is null ? string.Empty : FormatMapping(mapping.X);
        MappingYText = mapping is null ? string.Empty : FormatMapping(mapping.Y);
        MappingWidthText = mapping is null ? string.Empty : FormatMapping(mapping.Width);
        MappingHeightText = mapping is null ? string.Empty : FormatMapping(mapping.Height);
        LocalSourcePath = draft.Path;
    }

    private void ClearActiveLocalSource()
    {
        SelectedLocalSource = null;
        SelectedMappingSource = null;
        LocalSourcePath = string.Empty;
        MappingXText = string.Empty;
        MappingYText = string.Empty;
        MappingWidthText = string.Empty;
        MappingHeightText = string.Empty;
        foreach (var color in TemplateColorChoices) color.IsSelected = false;
        foreach (var option in TemplateAdditionalOptionChoices) option.IsSelected = false;
    }

    private void RefreshLocalSourceSelectionPresentation()
    {
        var selected = _selectedLocalSources.ToHashSet();
        foreach (var row in LocalSourceDrafts) row.IsSelected = selected.Contains(row);
    }

    private void NotifyLocalSourceSelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedLocalSources));
        OnPropertyChanged(nameof(SelectedLocalSourceCount));
        OnPropertyChanged(nameof(HasSelectedLocalSources));
        OnPropertyChanged(nameof(LocalSourceSelectionSummary));
        OnPropertyChanged(nameof(LocalSourceArchiveLabel));
        NotifyCommands();
    }

    private void SortLocalSources(string? column)
    {
        if (column is not ("File" or "Applicability" or "Status")) return;
        CaptureSelectedLocalSource();
        _localSourceSortAscending = column == _localSourceSortColumn ? !_localSourceSortAscending : true;
        _localSourceSortColumn = column;
        ApplyLocalSourceSort();
        OnPropertyChanged(nameof(FileSortLabel));
        OnPropertyChanged(nameof(ApplicabilitySortLabel));
        OnPropertyChanged(nameof(StatusSortLabel));
        OnPropertyChanged(nameof(FileSortAccessibleName));
        OnPropertyChanged(nameof(ApplicabilitySortAccessibleName));
        OnPropertyChanged(nameof(StatusSortAccessibleName));
    }

    private string LocalSourceSortLabel(string column) =>
        column == _localSourceSortColumn ? $"{column} {(_localSourceSortAscending ? "↑" : "↓")}" : column;

    private string LocalSourceSortAccessibleName(string column) =>
        column == _localSourceSortColumn
            ? $"{column}, sorted {(_localSourceSortAscending ? "ascending" : "descending")}"
            : $"Sort by {column}";

    private void ApplyLocalSourceSort()
    {
        Func<LocalMockupSourceDraftViewModel, string> key = _localSourceSortColumn switch
        {
            "Applicability" => draft => draft.ApplicabilitySummary,
            "Status" => draft => draft.StatusLabel,
            _ => draft => draft.DisplayName
        };
        var ordered = (_localSourceSortAscending
                ? LocalSourceDrafts.OrderBy(key, StringComparer.OrdinalIgnoreCase)
                : LocalSourceDrafts.OrderByDescending(key, StringComparer.OrdinalIgnoreCase))
            .ThenBy(draft => draft.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        for (var index = 0; index < ordered.Length; index++)
        {
            var currentIndex = LocalSourceDrafts.IndexOf(ordered[index]);
            if (currentIndex != index) LocalSourceDrafts.Move(currentIndex, index);
        }
        RefreshLocalSourceRowPresentation();
    }

    private void RefreshLocalSourceRowPresentation()
    {
        for (var index = 0; index < LocalSourceDrafts.Count; index++)
            LocalSourceDrafts[index].SetRowPresentationIndex(index);
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

    private bool CanAssistMockupSourceMetadataCore() =>
        CanEdit && IsAddingTemplate && HasSelectedLocalSources && _mockupSourceMetadataAssistance is not null && !IsMockupSourceMetadataAssistanceBusy;

    private async Task AssistMockupSourceMetadataAsync()
    {
        if (!CanAssistMockupSourceMetadataCore() || _mockupSourceMetadataAssistance is null) return;
        CaptureSelectedLocalSource();
        var version = ++_mockupSourceMetadataAssistanceVersion;
        _mockupSourceMetadataAssistanceCts?.Cancel();
        _mockupSourceMetadataAssistanceCts?.Dispose();
        _mockupSourceMetadataAssistanceCts = new CancellationTokenSource();
        _isMockupSourceMetadataAssistanceBusy = true;
        OnPropertyChanged(nameof(IsMockupSourceMetadataAssistanceBusy));
        SetMockupSourceMetadataAssistanceStatus("Checking General AI settings…");
        NotifyCommands();
        try
        {
            var availability = await _mockupSourceMetadataAssistance.GetAvailabilityAsync(_mockupSourceMetadataAssistanceCts.Token).ConfigureAwait(true);
            if (!availability.IsReady)
            {
                SetMockupSourceMetadataAssistanceStatus(availability.Message);
                return;
            }

            SetMockupSourceMetadataAssistanceStatus("Setting up metadata for selected source images…");
            var result = await _mockupSourceMetadataAssistance.AssistAsync(BuildMockupSourceMetadataRequest(), _mockupSourceMetadataAssistanceCts.Token).ConfigureAwait(true);
            if (version != _mockupSourceMetadataAssistanceVersion) return;
            ApplyMockupSourceMetadataAssistance(result);
        }
        catch (OperationCanceledException) when (_mockupSourceMetadataAssistanceCts?.IsCancellationRequested == true)
        {
            SetMockupSourceMetadataAssistanceStatus("AI metadata setup cancelled. Existing draft values were kept where no result arrived.");
        }
        catch (Exception exception)
        {
            SetMockupSourceMetadataAssistanceStatus(exception.Message);
        }
        finally
        {
            if (version == _mockupSourceMetadataAssistanceVersion)
            {
                _isMockupSourceMetadataAssistanceBusy = false;
                OnPropertyChanged(nameof(IsMockupSourceMetadataAssistanceBusy));
                NotifyCommands();
            }
        }
    }

    private MockupSourceMetadataAssistanceRequest BuildMockupSourceMetadataRequest()
    {
        var values = TemplateColorChoices.Concat(TemplateAdditionalOptionChoices)
            .Select(choice => new MockupSourceMetadataValue(
                choice.Value.Id,
                choice.Label,
                Options.FirstOrDefault(option => option.Id == choice.Value.OptionId)?.OptionKind ?? OptionKind.Other))
            .DistinctBy(value => value.Id)
            .ToArray();
        var selected = _selectedLocalSources.ToArray();
        var images = selected.Select(draft => new MockupSourceMetadataImage(
            SourceToken(draft),
            draft.DisplayName,
            draft.Path,
            draft.ImageWidth,
            draft.ImageHeight,
            draft.OptionValueIds,
            draft.Mapping)).ToArray();
        var references = LocalSourceDrafts.Select(draft => new MockupSourceMetadataPlacementReference(
            SourceToken(draft), draft.OptionValueIds, draft.Mapping)).ToArray();
        return new(
            images,
            values,
            references,
            SelectedPlaceholder?.Name,
            SelectedPlaceholder?.Width,
            SelectedPlaceholder?.Height);
    }

    private void ApplyMockupSourceMetadataAssistance(MockupSourceMetadataAssistanceResult result)
    {
        var values = TemplateColorChoices.Concat(TemplateAdditionalOptionChoices)
            .Select(choice => new MockupSourceMetadataValue(
                choice.Value.Id,
                choice.Label,
                Options.FirstOrDefault(option => option.Id == choice.Value.OptionId)?.OptionKind ?? OptionKind.Other))
            .DistinctBy(value => value.Id)
            .ToDictionary(value => value.Id);
        foreach (var item in result.Items)
        {
            var draft = LocalSourceDrafts.FirstOrDefault(value => string.Equals(SourceToken(value), item.Token, StringComparison.Ordinal));
            if (draft is null) continue;
            if (item.Applied)
            {
                var labels = item.OptionValueIds.Where(values.ContainsKey).Select(id => values[id].Label).ToArray();
                draft.UpdateMetadata(item.OptionValueIds, item.Mapping, string.Join(", ", labels));
            }
            draft.SetAssistanceStatus(item.Status + (string.IsNullOrWhiteSpace(item.Message) ? string.Empty : $": {item.Message}"), item.Confidence);
        }
        RebuildMappedSourceChoices();
        if (SelectedLocalSource is not null) ActivateLocalSource(SelectedLocalSource);
        SetMockupSourceMetadataAssistanceStatus(result.Message ?? $"AI metadata setup finished for {result.Items.Count} selected source image{(result.Items.Count == 1 ? string.Empty : "s")}.");
        NotifyMockupTemplateDraftChanged();
    }

    private void SetMockupSourceMetadataAssistanceStatus(string status)
    {
        _mockupSourceMetadataAssistanceStatus = status;
        OnPropertyChanged(nameof(MockupSourceMetadataAssistanceStatus));
        OnPropertyChanged(nameof(HasMockupSourceMetadataAssistanceStatus));
    }

    private static string SourceToken(LocalMockupSourceDraftViewModel draft) =>
        draft.SourceImageId?.ToString("N") ?? draft.Path;

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
        ApplyLocalSourceSort();
        RebuildMappedSourceChoices();
        OnPropertyChanged(nameof(HasLocalSource));
    }

    public void RemoveLocalSource(LocalMockupSourceDraftViewModel? draft)
    {
        if (draft is null || !LocalSourceDrafts.Contains(draft)) return;
        if (ReferenceEquals(SelectedLocalSource, draft)) CaptureSelectedLocalSource();
        RemoveLocalSourceCore(draft);
        ReconcileLocalSourceSelection();
        RefreshLocalSourceRowPresentation();
        RebuildMappedSourceChoices();
        OnPropertyChanged(nameof(HasLocalSource));
        NotifyLocalSourceSelectionChanged();
        NotifyMockupTemplateDraftChanged();
    }

    private void RemoveSelectedLocalSources()
    {
        if (_selectedLocalSources.Count == 0) return;
        CaptureSelectedLocalSource();
        foreach (var draft in _selectedLocalSources.ToArray()) RemoveLocalSourceCore(draft);
        ReconcileLocalSourceSelection();
        RefreshLocalSourceRowPresentation();
        RebuildMappedSourceChoices();
        OnPropertyChanged(nameof(HasLocalSource));
        NotifyLocalSourceSelectionChanged();
        NotifyMockupTemplateDraftChanged();
    }

    private void RemoveLocalSourceCore(LocalMockupSourceDraftViewModel draft)
    {
        if (draft.IsManaged && draft.SourceImageId is not null && !_archivedLocalSourceDrafts.Contains(draft))
            _archivedLocalSourceDrafts.Add(draft);
        _selectedLocalSources.Remove(draft);
        if (ReferenceEquals(_localSourceSelectionAnchor, draft)) _localSourceSelectionAnchor = null;
        draft.IsSelected = false;
        LocalSourceDrafts.Remove(draft);
    }

    private void ReconcileLocalSourceSelection()
    {
        _selectedLocalSources.RemoveAll(draft => !LocalSourceDrafts.Contains(draft));
        if (_localSourceSelectionAnchor is not null && !LocalSourceDrafts.Contains(_localSourceSelectionAnchor))
            _localSourceSelectionAnchor = _selectedLocalSources.LastOrDefault();

        var next = _selectedLocalSources.LastOrDefault() ?? LocalSourceDrafts.LastOrDefault();
        if (next is null)
        {
            _localSourceSelectionAnchor = null;
            ClearActiveLocalSource();
            return;
        }

        if (!_selectedLocalSources.Contains(next))
        {
            _selectedLocalSources.Add(next);
            _localSourceSelectionAnchor = next;
        }
        RefreshLocalSourceSelectionPresentation();
        ActivateLocalSource(next);
    }

    private void ClearLocalSourceSelectionState()
    {
        _selectedLocalSources.Clear();
        _localSourceSelectionAnchor = null;
        RefreshLocalSourceSelectionPresentation();
        if (SelectedLocalSource is not null) ClearActiveLocalSource();
        NotifyLocalSourceSelectionChanged();
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

    private async Task DuplicateTemplateAsync(object? parameter)
    {
        if (SelectedOffering is null || !CanEdit) return;
        var template = parameter switch
        {
            MockupTemplateCardViewModel card => Templates.FirstOrDefault(value => value.Id == card.Id),
            MockupTemplate value => value,
            _ => null
        };
        if (template is null || template.IsArchived) return;

        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _mockups.DuplicateTemplateAsync(new DuplicateMockupTemplateRequest(SelectedOffering.StoreId, template.Id)).ConfigureAwait(true);
            if (!result.Succeeded)
            {
                ErrorMessage = result.Error ?? "Mockup Template could not be duplicated.";
                return;
            }

            ApplyMockups(result.State);
            var duplicate = result.State.Templates.FirstOrDefault(value => value.Id == result.TemplateId);
            if (duplicate is not null)
            {
                IsBusy = false;
                BeginEditTemplate(duplicate);
            }
        }
        catch (Exception exception) { ErrorMessage = exception.Message; }
        finally { IsBusy = false; }
    }

    private void BeginEditTemplate(MockupTemplate? template)
    {
        if (!CanEdit || template is null) return;
        SelectedTemplate = template;
        SelectedPlaceholder = AvailablePlaceholders.FirstOrDefault(value => value.Id == template.TargetPlaceholderId);
        TemplateName = template.Name;
        TemplateColorSearchText = string.Empty;
        ClearLocalSourceSelectionState();
        LocalSourceDrafts.Clear();
        RefreshLocalSourceRowPresentation();
        MappedSourceChoices.Clear();
        _archivedLocalSourceDrafts.Clear();
        SelectedLocalSource = null;
        LocalSourcePath = string.Empty;
        CoveragePlan = null;
        CoverageError = string.Empty;
        IsCoverageLoading = false;
        SelectedCoverageRequirement = null;
        SelectedCoverageExemplar = null;
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
        _mockupTemplateDraftBaseline = null;
        IsAddingTemplate = true;
        if (_sourceImages is null)
        {
            _mockupTemplateDraftBaseline = CurrentMockupTemplateDraftState();
        }
        else
        {
            ObserveLocalSourceDrafts(template.Id);
        }
        MockupTemplateEditorRequested?.Invoke(this, EventArgs.Empty);
    }

    private void BeginNewTemplate()
    {
        if (!CanEdit || SelectedOffering is null) return;
        SelectedTemplate = null;
        SelectedPlaceholder = null;
        TemplateName = string.Empty;
        TemplateColorSearchText = string.Empty;
        ClearLocalSourceSelectionState();
        LocalSourceDrafts.Clear();
        RefreshLocalSourceRowPresentation();
        MappedSourceChoices.Clear();
        _archivedLocalSourceDrafts.Clear();
        SelectedLocalSource = null;
        LocalSourcePath = string.Empty;
        CoveragePlan = null;
        CoverageError = string.Empty;
        IsCoverageLoading = false;
        SelectedCoverageRequirement = null;
        SelectedCoverageExemplar = null;
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
        if (SelectedOffering is null)
        {
            ErrorMessage = "Select an offering before archiving a catalog record.";
            return;
        }

        if (id == Guid.Empty)
        {
            ErrorMessage = "The selected catalog record could not be identified. Refresh the offering and try again.";
            return;
        }

        if (kind == CatalogRecordKind.Variant && AvailableVariants.All(value => value.Id != id))
        {
            ErrorMessage = "The selected Variant is no longer active in this offering. Refresh the offering and try again.";
            return;
        }

        _ = RunMutationAsync(() => _catalog.ArchiveAsync(new ArchiveCatalogRecordRequest(SelectedOffering.StoreId, kind, id)));
    }

    private async Task PreviewArchiveOfferingAsync()
    {
        if (SelectedOffering is null) return;
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            ArchiveOfferingPlan = await _catalog.PreviewArchiveOfferingAsync(new ArchiveOfferingCascadeRequest(SelectedOffering.StoreId, SelectedOffering.Id)).ConfigureAwait(true);
            IsArchiveOfferingConfirmationVisible = true;
        }
        catch (Exception exception) { ErrorMessage = exception.Message; }
        finally { IsBusy = false; }
    }

    private async Task ConfirmArchiveOfferingAsync()
    {
        if (SelectedOffering is null || ArchiveOfferingPlan is not { CanConfirm: true }) return;
        await RunMutationAsync(() => _catalog.ArchiveOfferingCascadeAsync(new ArchiveOfferingCascadeRequest(SelectedOffering.StoreId, SelectedOffering.Id))).ConfigureAwait(true);
        if (!HasError)
        {
            IsArchiveOfferingConfirmationVisible = false;
            ArchiveOfferingPlan = null;
        }
    }

    private void CancelArchiveOfferingArchive()
    {
        IsArchiveOfferingConfirmationVisible = false;
        ArchiveOfferingPlan = null;
    }

    private bool CanRestoreOffering() => IsAvailable && !IsReadOnly && !IsBusy && SelectedOffering?.IsArchived == true;
    private bool CanDeleteOffering() => IsAvailable && !IsReadOnly && !IsBusy && SelectedOffering?.IsArchived == true;

    private async Task RestoreOfferingAsync()
    {
        if (SelectedOffering is null) return;
        await RunMutationAsync(() => _catalog.RestoreOfferingCascadeAsync(new RestoreOfferingCascadeRequest(SelectedOffering.StoreId, SelectedOffering.Id, true))).ConfigureAwait(true);
    }

    private async Task PreviewDeleteOfferingAsync()
    {
        if (SelectedOffering is null) return;
        IsBusy = true; ErrorMessage = string.Empty;
        try
        {
            DeleteOfferingPlan = await _catalog.PreviewDeleteOfferingPermanentlyAsync(new DeleteOfferingPermanentlyRequest(SelectedOffering.StoreId, SelectedOffering.Id, false)).ConfigureAwait(true);
            IsDeleteOfferingConfirmationVisible = true;
        }
        catch (Exception exception) { ErrorMessage = exception.Message; }
        finally { IsBusy = false; }
    }

    private async Task ConfirmDeleteOfferingAsync()
    {
        if (SelectedOffering is null || DeleteOfferingPlan is not { CanConfirm: true }) return;
        await RunMutationAsync(() => _catalog.DeleteOfferingPermanentlyAsync(new DeleteOfferingPermanentlyRequest(SelectedOffering.StoreId, SelectedOffering.Id, true))).ConfigureAwait(true);
        if (!HasError) { IsDeleteOfferingConfirmationVisible = false; DeleteOfferingPlan = null; SelectOffering(null); }
    }

    private void CancelOfferingDelete() { IsDeleteOfferingConfirmationVisible = false; DeleteOfferingPlan = null; }
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
        _offeringReadiness = state.Summary.Readiness;
        OnPropertyChanged(nameof(ReadyMockupTemplateCount));
        OnPropertyChanged(nameof(OfferingReadinessIssues));
        OnPropertyChanged(nameof(HasOfferingReadinessGuidance));
        OnPropertyChanged(nameof(OfferingReadinessGuidance));
        OnPropertyChanged(nameof(OfferingReadinessSummary));
        OnPropertyChanged(nameof(OfferingReadinessStatus));
        var selectedPlaceholderId = SelectedPlaceholder?.Id;
        Replace(Options, Options.Where(value => value.OfferingId != state.Offering.Id).Concat(state.Options));
        Replace(OptionValues, OptionValues.Where(value => value.OfferingId != state.Offering.Id).Concat(state.OptionValues));
        Replace(Variants, Variants.Where(value => value.OfferingId != state.Offering.Id).Concat(state.Variants));
        Replace(Placeholders, Placeholders.Where(value => value.OfferingId != state.Offering.Id).Concat(state.DesignAreas));
        Replace(Templates, Templates.Where(value => value.BlueprintOfferingId != state.Offering.Id).Concat(state.MockupTemplates));
        if (SelectedOffering?.Id == state.Offering.Id && SetField(ref _selectedOffering, state.Offering, nameof(SelectedOffering)))
        {
            LoadOfferingFields();
            OnPropertyChanged(nameof(SelectedOfferingId));
            OnPropertyChanged(nameof(HasSelectedOffering));
            OnPropertyChanged(nameof(IsOfferingContextUnavailable));
            OnPropertyChanged(nameof(OfferingKindLabel));
            OnPropertyChanged(nameof(IsProviderNetworkOffering));
            OnPropertyChanged(nameof(ProviderDisplayName));
        }
        SelectedPlaceholder = state.DesignAreas.FirstOrDefault(value => value.Id == selectedPlaceholderId)
            ?? state.DesignAreas.FirstOrDefault(value => value.Id == state.Offering.DefaultPlaceholderId)
            ?? state.DesignAreas.FirstOrDefault();
        RefreshOfferingCollections();
    }

    private async Task LoadOfferingReadinessAsync(CancellationToken cancellationToken = default)
    {
        if (_offeringManagement is null || SelectedOffering is not { } offering)
        {
            _offeringReadiness = null;
            _offeringReadinessError = string.Empty;
            OnPropertyChanged(nameof(ReadyMockupTemplateCount));
            OnPropertyChanged(nameof(OfferingReadinessIssues));
            OnPropertyChanged(nameof(HasOfferingReadinessGuidance));
            OnPropertyChanged(nameof(OfferingReadinessGuidance));
            OnPropertyChanged(nameof(OfferingReadinessSummary));
            OnPropertyChanged(nameof(OfferingReadinessStatus));
            OnPropertyChanged(nameof(OfferingReadinessError));
            OnPropertyChanged(nameof(HasOfferingReadinessError));
            return;
        }

        var version = Interlocked.Increment(ref _readinessLoadVersion);
        _offeringReadiness = null;
        OnPropertyChanged(nameof(ReadyMockupTemplateCount));
        OnPropertyChanged(nameof(HasOfferingReadinessGuidance));
        OnPropertyChanged(nameof(OfferingReadinessGuidance));
        OnPropertyChanged(nameof(OfferingReadinessSummary));
        OnPropertyChanged(nameof(OfferingReadinessStatus));
        try
        {
            var state = await _offeringManagement.LoadOfferingAsync(
                new OfferingContext(offering.StoreId, offering.BlueprintId, offering.Id), cancellationToken).ConfigureAwait(true);
            if (version != Volatile.Read(ref _readinessLoadVersion) || SelectedOffering?.Id != offering.Id) return;
            _offeringReadiness = state.Summary.Readiness;
            _offeringReadinessError = string.Empty;
            OnPropertyChanged(nameof(ReadyMockupTemplateCount));
            OnPropertyChanged(nameof(OfferingReadinessIssues));
            OnPropertyChanged(nameof(HasOfferingReadinessGuidance));
            OnPropertyChanged(nameof(OfferingReadinessGuidance));
            OnPropertyChanged(nameof(OfferingReadinessSummary));
            OnPropertyChanged(nameof(OfferingReadinessStatus));
            OnPropertyChanged(nameof(OfferingReadinessError));
            OnPropertyChanged(nameof(HasOfferingReadinessError));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (version != Volatile.Read(ref _readinessLoadVersion) || SelectedOffering?.Id != offering.Id) return;
            _offeringReadiness = null;
            _offeringReadinessError = $"Offering readiness could not be evaluated. {exception.Message} Try reloading the Offering or checking its catalog records.";
            OnPropertyChanged(nameof(ReadyMockupTemplateCount));
            OnPropertyChanged(nameof(OfferingReadinessIssues));
            OnPropertyChanged(nameof(HasOfferingReadinessGuidance));
            OnPropertyChanged(nameof(OfferingReadinessGuidance));
            OnPropertyChanged(nameof(OfferingReadinessSummary));
            OnPropertyChanged(nameof(OfferingReadinessStatus));
            OnPropertyChanged(nameof(OfferingReadinessError));
            OnPropertyChanged(nameof(HasOfferingReadinessError));
        }
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
            else
            {
                ApplyCatalog(result.State);
                CatalogChanged?.Invoke(this, EventArgs.Empty);
            }
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
        _templateSourceImages = state.SourceImages ?? [];
        _templateSourceConditions = state.SourceImageOptionValues ?? [];
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
        OnPropertyChanged(nameof(IsCoveragePlanStale));
        OnPropertyChanged(nameof(CoverageSummary));
        OnPropertyChanged(nameof(ReadyMockupTemplateCount));
        OnPropertyChanged(nameof(HasOfferingReadinessGuidance));
        OnPropertyChanged(nameof(OfferingReadinessGuidance));
        OnPropertyChanged(nameof(OfferingReadinessSummary));
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
            value.ProviderReference)
        {
            IsPrimaryForArtworkGeneration = SelectedOffering?.PrimaryArtworkDesignAreaId == value.Id
        });
        Replace(DesignAreaCards, areaSummaries.Select(DesignAreaCardViewModel.From));

        var templateSummaries = AvailableTemplates.Select(template =>
        {
            var templateSourceImages = _templateSourceImages
                .Where(value => value.MockupTemplateId == template.Id)
                .ToArray();
            var activeSourceImageIds = templateSourceImages
                .Where(value => !value.IsArchived)
                .Select(value => value.Id)
                .ToHashSet();
            var colorIds = templateSourceImages.Length > 0
                ? _templateSourceConditions
                    .Where(value => activeSourceImageIds.Contains(value.SourceImageId))
                    .Select(value => value.OptionValueId)
                    .Where(value => OptionValues.Any(optionValue => optionValue.Id == value
                        && optionValue.OfferingId == template.BlueprintOfferingId
                        && Options.Any(option => option.Id == optionValue.OptionId
                            && option.OfferingId == template.BlueprintOfferingId
                            && option.OptionKind == OptionKind.Color)))
                    .Distinct()
                    .ToArray()
                : TemplateColors
                    .Where(value => value.MockupTemplateId == template.Id && !value.IsArchived)
                    .Select(value => value.ColorOptionValueId)
                    .ToArray();
            var targetName = Placeholders.FirstOrDefault(value => value.Id == template.TargetPlaceholderId)?.Name;
            var revision = TemplateRevisions.FirstOrDefault(value => value.MockupTemplateId == template.Id && value.RevisionNumber == template.CurrentRevision);
            var compatibleVariantIds = templateSourceImages.Length > 0
                ? AvailableVariants
                    .Where(value => template.TargetPlaceholderId is Guid targetId
                        && AvailablePlaceholders.FirstOrDefault(area => area.Id == targetId)?.VariantIds.Contains(value.Id) == true)
                    .Select(value => value.Id)
                    .ToArray()
                : AvailableVariants
                    .Where(value => value.OptionValueIds.Any(colorIds.Contains))
                    .Select(value => value.Id)
                    .ToArray();
            var effectiveRevision = revision ?? new MockupTemplateRevision(template.Id, template.Id, template.CurrentRevision, template.TargetPlaceholderId, template.CreatedAt);
            var readiness = MockupTemplateReadinessPolicy.Evaluate(new(template, effectiveRevision, colorIds, Options, OptionValues, Variants, Placeholders,
                SourceImages: _templateSourceImages, SourceImageOptionValues: _templateSourceConditions));
            return new MockupTemplateSetupSummary(template.Id, template.Name, template.TargetPlaceholderId, targetName, colorIds, compatibleVariantIds, revision?.ProviderMockupReference, template.CurrentRevision, template.IsArchived, readiness.Lifecycle, readiness.Blockers);
        });
        Replace(MockupTemplateCards, templateSummaries.Select(value => MockupTemplateCardViewModel.From(value, OptionValues)));
        RebuildChoices();
    }

    private void BeginManageOptionValues(OfferingOption? option)
    {
        if (!CanEdit || IsManagingOptionValues || option is null) return;
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
        RefreshFilteredTemplateColorChoices();

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

    private void RefreshFilteredTemplateColorChoices()
    {
        var query = TemplateColorSearchText.Trim();
        var filtered = string.IsNullOrEmpty(query)
            ? TemplateColorChoices
            : TemplateColorChoices.Where(value => value.Label.Contains(query, StringComparison.OrdinalIgnoreCase));
        Replace(FilteredTemplateColorChoices, filtered);
        OnPropertyChanged(nameof(HasNoMatchingTemplateColors));
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
        if (_sourceImages is not null && HasPendingLocalSourceChanges && SelectedProviderMockup is null)
            return true;
        return SelectedProviderMockup is null || TryCreateMapping(out _);
    }

    private bool HasPendingLocalSourceChanges => HasLocalSource || _archivedLocalSourceDrafts.Count > 0;

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
        PlaceholderPrimaryForArtworkGeneration = false;
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
        if (!CanEdit) return;
        ResetPlaceholderDraft();
        SelectedPlaceholder = null;
        _designAreaDraftBaseline = CurrentDesignAreaDraftState();
        IsAddingPlaceholder = true;
        DesignAreaEditorRequested?.Invoke(this, EventArgs.Empty);
    }

    internal void AttachStoreEditor()
    {
        if (_storeEditorAttached) return;
        _storeEditorAttached = true;
        OnPropertyChanged(nameof(CanEdit));
        NotifyCommands();
    }

    internal void DetachStoreEditor()
    {
        if (!_storeEditorAttached) return;
        _storeEditorAttached = false;
        OnPropertyChanged(nameof(CanEdit));
        NotifyCommands();
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
        PlaceholderPrimaryForArtworkGeneration = SelectedOffering?.PrimaryArtworkDesignAreaId == area.Id;
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
        _mockupSourceMetadataAssistanceCts?.Cancel();
        _mockupSourceMetadataAssistanceVersion++;
        _isMockupSourceMetadataAssistanceBusy = false;
        OnPropertyChanged(nameof(IsMockupSourceMetadataAssistanceBusy));
        EndTemplateDraft();
        TemplateName = string.Empty;
        foreach (var color in TemplateColorChoices) color.IsSelected = false;
        foreach (var option in TemplateAdditionalOptionChoices) option.IsSelected = false;
        SelectedTemplate = AvailableTemplates.FirstOrDefault();
        ClearLocalSourceSelectionState();
        LocalSourceDrafts.Clear();
        LocalSourcePath = string.Empty;
        CoveragePlan = null;
        SelectedCoverageRequirement = null;
        SelectedCoverageExemplar = null;
    }

    private async Task LoadLocalSourceDraftsAsync(Guid templateId)
    {
        if (_sourceImages is null || SelectedOffering is null) return;
        IsCoverageLoading = true;
        CoverageError = string.Empty;
        try
        {
            var configuredTemplateColorIds = TemplateColorChoices
                .Where(value => value.IsSelected)
                .Select(value => value.Value.Id)
                .ToHashSet();
            var state = await _sourceImages.LoadAsync(SelectedOffering.StoreId, templateId).ConfigureAwait(true);
            if (!IsAddingTemplate || SelectedTemplate?.Id != templateId) return;
            CoveragePlan = state.CoveragePlan;
            if (!string.IsNullOrWhiteSpace(state.Error))
            {
                CoverageError = state.Error;
                ErrorMessage = state.Error;
            }
            foreach (var image in state.Images)
            {
                var labels = image.OptionValueIds.Select(id => OptionValues.FirstOrDefault(value => value.Id == id)).Where(value => value is not null).Select(value => ValueLabel(value!));
                LocalSourceDrafts.Add(new LocalMockupSourceDraftViewModel(image.WorkspaceRelativePath, image.OptionValueIds, isManaged: true, image.ImageMapping, image.Dimensions.Width, image.Dimensions.Height, image.Id, image.PreviewPath) { ApplicabilitySummary = string.Join(", ", labels) });
            }
            ApplyLocalSourceSort();
            OnPropertyChanged(nameof(CoverageExemplarChoices));
            if (LocalSourceDrafts.Count > 0) SelectLocalSource(LocalSourceDrafts[0]);
            if (configuredTemplateColorIds.Count > 0)
            {
                foreach (var color in TemplateColorChoices)
                    color.IsSelected = configuredTemplateColorIds.Contains(color.Value.Id);
            }
            RebuildMappedSourceChoices();
            OnPropertyChanged(nameof(HasLocalSource));
            if (IsAddingTemplate && SelectedTemplate?.Id == templateId)
            {
                _mockupTemplateDraftBaseline = CurrentMockupTemplateDraftState();
                OnPropertyChanged(nameof(HasMeaningfulMockupTemplateDraft));
            }
        }
        finally
        {
            if (IsAddingTemplate && SelectedTemplate?.Id == templateId)
                IsCoverageLoading = false;
        }
    }

    private void ObserveLocalSourceDrafts(Guid templateId) => _ = ObserveLocalSourceDraftsAsync(templateId);

    private async Task ObserveLocalSourceDraftsAsync(Guid templateId)
    {
        try
        {
            await LoadLocalSourceDraftsAsync(templateId).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            if (IsAddingTemplate && SelectedTemplate?.Id == templateId)
            {
                CoverageError = exception.Message;
                ErrorMessage = exception.Message;
                IsCoverageLoading = false;
            }
        }
    }

    private void EndTemplateDraft()
    {
        IsMockupTemplateDiscardConfirmationVisible = false;
        IsAddingTemplate = false;
        TemplateColorSearchText = string.Empty;
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
            Options, OptionValues, Variants, Placeholders, SelectedProviderMockup?.SupportedColorOptionValueIds.ToHashSet(),
            SourceImages: SelectedTemplate is null ? null : _templateSourceImages,
            SourceImageOptionValues: SelectedTemplate is null ? null : _templateSourceConditions));
    }

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

    private void SetMappingText(ref string field, string value, ref double numericField, string textProperty, string numericProperty, bool? widthDimension = null)
    {
        value ??= string.Empty;
        if (field == value) return;
        field = value;
        if (int.TryParse(value, out var parsed))
        {
            numericField = parsed;
            if (widthDimension is not null && KeepAspectRatio && IsKeepAspectRatioAvailable)
            {
                var paired = widthDimension.Value ? parsed / PlacementAspectRatio : parsed * PlacementAspectRatio;
                if (widthDimension.Value)
                {
                    _mappingHeight = paired;
                    _mappingHeightText = FormatMapping(paired);
                    OnPropertyChanged(nameof(MappingHeight));
                    OnPropertyChanged(nameof(MappingHeightText));
                }
                else
                {
                    _mappingWidth = paired;
                    _mappingWidthText = FormatMapping(paired);
                    OnPropertyChanged(nameof(MappingWidth));
                    OnPropertyChanged(nameof(MappingWidthText));
                }
            }
        }
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
        string.Join("|", TemplateColorChoices.Where(value => value.IsSelected).Select(value => value.Value.Id).OrderBy(value => value)),
        LocalSourcePath,
        string.Join("|", LocalSourceDrafts.Concat(_archivedLocalSourceDrafts).Select(CurrentLocalSourceDraftState).OrderBy(value => value)));

    private string CurrentLocalSourceDraftState(LocalMockupSourceDraftViewModel draft)
    {
        var optionValueIds = draft.OptionValueIds;
        MockupImageSpaceMapping? mapping = draft.Mapping;
        var isArchived = _archivedLocalSourceDrafts.Contains(draft);
        if (ReferenceEquals(SelectedLocalSource, draft))
        {
            optionValueIds = TemplateColorChoices.Where(value => value.IsSelected).Select(value => value.Value.Id)
                .Concat(TemplateAdditionalOptionChoices.Where(value => value.IsSelected).Select(value => value.Value.Id))
                .Distinct()
                .ToArray();
            TryCreateMapping(out mapping);
        }

        var optionIds = string.Join(",", optionValueIds.OrderBy(value => value));
        var mappingValue = mapping is null
            ? string.Empty
            : string.Join(",", mapping.ImageWidth, mapping.ImageHeight, mapping.X, mapping.Y, mapping.Width, mapping.Height);
        return string.Join(";", isArchived, draft.SourceImageId, draft.Path, optionIds, mappingValue);
    }

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
        PlaceholderPrimaryForArtworkGeneration,
        PlaceholderProviderReference,
        ArtworkWidth,
        ArtworkHeight,
        ArtworkDpi,
        ArtworkFormat,
        ArtworkBackground,
        string.Join("|", PlaceholderVariantChoices.Where(value => value.IsSelected).Select(value => value.Variant.Id).OrderBy(value => value)));

    private void NotifyCommands()
    {
        OnPropertyChanged(nameof(CanAssistMockupSourceMetadata));
        foreach (var command in new ICommand[]
        {
            SaveOfferingCommand, StartAddPrintProviderCommand, CreatePrintProviderCommand, StartAddOptionCommand, ManageOptionCommand, CreateOptionCommand, StartAddOptionValueCommand,
            CreateOptionValueCommand, EditOptionValueCommand, SaveOptionValueEditCommand, CancelOptionValueEditCommand,
            MoveOptionValueUpCommand, MoveOptionValueDownCommand,
            StartAddVariantCommand, StartBulkVariantsCommand, CreateVariantCommand, StartAddPlaceholderCommand,
            CreatePlaceholderCommand, SetDefaultPlaceholderCommand, StartAddTemplateCommand, CreateTemplateCommand,
            DuplicateTemplateCommand,
            GenerateCoveragePlanCommand, SelectCoverageRequirementCommand, AddCoverageRequirementImageCommand, AssignExistingCoverageImageCommand,
            AddTemplateColorCommand, PreviewBulkVariantsCommand, ConfirmBulkVariantsCommand,
            OpenEnlargedPlacementEditorCommand,
            AssistMockupSourceMetadataCommand, CancelMockupSourceMetadataCommand,
            ConfirmDesignAreaArchiveCommand, CancelDesignAreaArchiveCommand,
            RequestCancelMockupTemplateCommand, ConfirmDiscardMockupTemplateCommand, KeepEditingMockupTemplateCommand,
            RequestCancelDesignAreaCommand, ConfirmDiscardDesignAreaCommand, KeepEditingDesignAreaCommand
            ,RequestArchiveOfferingCommand, ConfirmArchiveOfferingCommand, CancelArchiveOfferingCommand
        })
        {
            switch (command)
            {
                case AsyncRelayCommand asyncCommand: asyncCommand.NotifyCanExecuteChanged(); break;
                case RelayCommand relayCommand: relayCommand.NotifyCanExecuteChanged(); break;
            }
        }
    }

    private void NotifyCoveragePresentation()
    {
        OnPropertyChanged(nameof(CoverageState));
        OnPropertyChanged(nameof(CoverageStateLabel));
        OnPropertyChanged(nameof(CoverageStateHelp));
        OnPropertyChanged(nameof(HasCoverageStatus));
        OnPropertyChanged(nameof(HasCoveragePanel));
    }

    private static string? EmptyToNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string MessageSuffix(string message) => string.IsNullOrWhiteSpace(message) ? "." : $": {message}";
    private static string FormatPartialLocalSourceSaveError(string error, int saved, int total)
    {
        var sourceChangeLabel = total == 1 ? "source image change" : "source image changes";
        return $"Mockup Template save partially completed: the template and {saved} of {total} {sourceChangeLabel} were saved. {error}";
    }
    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values) { target.Clear(); foreach (var value in values) target.Add(value); }
    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        if (name is nameof(IsAddingPrintProvider)
            or nameof(IsAddingOption)
            or nameof(IsAddingOptionValue)
            or nameof(IsEditingOptionValue)
            or nameof(IsAddingVariant)
            or nameof(IsAddingBulkVariants)
            or nameof(IsAddingPlaceholder)
            or nameof(IsAddingTemplate))
            OnPropertyChanged(nameof(HasActiveDraft));
        return true;
    }
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
        string SelectedColorIds,
        string LocalSourcePath,
        string LocalSourceDrafts);

    private sealed record DesignAreaDraftState(
        string Name,
        string Description,
        string Position,
        string DecorationMethod,
        string Width,
        string Height,
        bool UsesAllVariants,
        bool PrimaryForArtworkGeneration,
        string ProviderReference,
        string ArtworkWidth,
        string ArtworkHeight,
        string ArtworkDpi,
        string ArtworkFormat,
        string ArtworkBackground,
        string SelectedVariantIds);
}

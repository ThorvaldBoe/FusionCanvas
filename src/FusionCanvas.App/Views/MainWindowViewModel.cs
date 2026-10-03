using FusionCanvas.App.Assets;
using FusionCanvas.App.Commands;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.App.Groups;
using FusionCanvas.App.Ideation;
using FusionCanvas.App.Items;
using FusionCanvas.App.Navigation;
using FusionCanvas.App.Settings;
using FusionCanvas.App.Stores;
using FusionCanvas.App.StageTools;
using FusionCanvas.App.SllGeneration;
using FusionCanvas.App.Workspace;
using FusionCanvas.App.Workflow;
using FusionCanvas.Application.Settings;
using FusionCanvas.Application.SllGeneration;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Groups;
using FusionCanvas.Domain.Stores;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.Application.Items;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Application.Stores;
using FusionCanvas.Application.WorkspaceTree;
using FusionCanvas.Application.Groups;
using FusionCanvas.Application.ToolContexts;
using FusionCanvas.Application.StageTools;
using FusionCanvas.Application.Assets;
using FusionCanvas.Application.Tags;
using FusionCanvas.Application.WorkflowNavigation;
using FusionCanvas.Application.Niches;
using FusionCanvas.Application.DesignFiles;
using FusionCanvas.Application.Ideation;
using FusionCanvas.Application.Workspaces.Transfer;
using FusionCanvas.Application.AI;
using FusionCanvas.Application.Telemetry;
using FusionCanvas.Application.ConceptRefinement;
using FusionCanvas.Application.Products;
using FusionCanvas.Application.Catalog;
using FusionCanvas.Application.Mockups;
using FusionCanvas.Application.Items.Import;
using FusionCanvas.Application.TitleOptimization;
using FusionCanvas.App.ConceptRefinement;

namespace FusionCanvas.App.Views;

public sealed class MainWindowViewModel : INotifyPropertyChanged, IDisposable, IAsyncDisposable
{
    private static readonly IReadOnlyDictionary<ItemStatus, ItemStatusOptionViewModel> StatusOptions =
        ItemStatuses.Ordered.ToDictionary(
            status => status,
            status => new ItemStatusOptionViewModel(status, status.ToString()));

    private readonly IToolContextResolver _toolContextResolver;
    private readonly IStageToolHostService _stageToolHostService;
    private readonly IStageToolContentResolver _stageToolContentResolver;
    private readonly IWorkspaceRepository? _workspaceRepository;
    private readonly IGroupManagementService _groupManagementService;
    private readonly IItemManagementService _itemManagementService;
    private readonly IAssetManagementService _assetManagementService;
    private readonly IItemInspectorService _itemInspectorService;
    private readonly ITagManagementService _tagManagementService;
    private readonly IIdeationService _ideationService;
    private readonly IIdeationAccessStatus _ideationAccessStatus;
    private readonly IConceptRefinementService _conceptRefinementService;
    private readonly IConceptRefinementAccessStatus _conceptRefinementAccessStatus;
    private readonly IItemCsvImportService _itemCsvImportService;
    private readonly ISllGenerationService _sllGenerationService;
    private readonly ISllAccessStatus _sllAccessStatus;
    private readonly ISllDocumentCodec _sllDocumentCodec;
    private ItemStatus? _pendingStatus;
    private bool _isStatusConfirmationVisible;
    private WorkspaceSnapshot _workspaceSnapshot;
    private IReadOnlyList<NavigationDocumentContext> _navigationContexts = [];
    private int _isInitializingWorkspace = 1;
    private long _workspaceSwitchGeneration;
    private readonly CommandTaskCoordinator _commandTasks;
    private int _activeOperationCount;
    private bool _disposed;

    public static MainWindowViewModel CreateForDefaultWorkspace(
        SettingsViewModel settings,
        IAiTextGenerationService ai,
        AppWorkspaceRuntime? workspace = null,
        IAiImageGenerationProvider? artworkProvider = null,
        ITelemetryRecorder? telemetry = null,
        CancellationToken cancellationToken = default) =>
        new(
            new WorkflowStageNavigatorViewModel(new WorkflowStageNavigatorService()),
            new DocumentWindowViewModel(),
            new ToolContextResolver(),
            new StageToolHostService(BuiltInStageTools.CreateDefaultRegistry(), new ToolContextResolver()),
            workspace ?? AppWorkspaceFactory.CreateDefault(
                ai,
                artworkProvider,
                telemetry,
                settings.ActiveWorkspaceId,
                settings.ActiveStoreId,
                cancellationToken),
            settings,
            cancellationToken: cancellationToken);

    private MainWindowViewModel(
        WorkflowStageNavigatorViewModel workflowNavigator,
        DocumentWindowViewModel documentWindow,
        IToolContextResolver toolContextResolver,
        IStageToolHostService stageToolHostService,
        AppWorkspaceRuntime runtime,
        SettingsViewModel? settings,
        CancellationToken cancellationToken = default,
        IStageToolContentResolver? stageToolContentResolver = null)
        : this(
            workflowNavigator,
            documentWindow,
            toolContextResolver,
            stageToolHostService,
            runtime.Repository,
            runtime.WorkspaceManagement,
            runtime.Snapshot,
            runtime.MainWindowServices,
            runtime.WorkspaceTransfer,
            settings,
            runtime.IdeationAccess,
            runtime.SnowcloneLibrary,
            runtime.RejectedPhrases,
            runtime.ConceptRefinement,
            runtime.ConceptRefinementAccess,
            runtime.SllGeneration,
            runtime.SllGenerationAccess,
            runtime.TitleOptimization,
            mockupGenerationService: runtime.MockupGeneration,
            artworkGenerationService: runtime.ArtworkGeneration,
            cancellationToken: cancellationToken,
            stageToolContentResolver: stageToolContentResolver)
    {
    }

    public MainWindowViewModel(
        WorkflowStageNavigatorViewModel workflowNavigator,
        DocumentWindowViewModel documentWindow,
        IToolContextResolver toolContextResolver,
        IStageToolHostService stageToolHostService,
        IWorkspaceRepository workspaceRepository,
        IWorkspaceManagementService workspaceManagementService,
        WorkspaceSnapshot workspaceSnapshot,
        MainWindowApplicationServices applicationServices,
        IWorkspaceTransferService? workspaceTransferService = null,
        SettingsViewModel? settings = null,
        IIdeationAccessStatus? ideationAccessStatus = null,
        FusionCanvas.Application.Snowclones.ISnowcloneLibraryService? snowcloneLibrary = null,
        FusionCanvas.Application.RejectedPhrases.IRejectedPhraseManagementService? rejectedPhrases = null,
        IConceptRefinementService? conceptRefinementService = null,
        IConceptRefinementAccessStatus? conceptRefinementAccessStatus = null,
        ISllGenerationService? sllGenerationService = null,
        ISllAccessStatus? sllAccessStatus = null,
        ITitleOptimizationService? titleOptimizationService = null,
        IMockupGenerationService? mockupGenerationService = null,
        IArtworkGenerationService? artworkGenerationService = null,
        CancellationToken cancellationToken = default,
        IStageToolContentResolver? stageToolContentResolver = null)
    {
        WorkflowNavigator = workflowNavigator;
        DocumentWindow = documentWindow;
        ArgumentNullException.ThrowIfNull(applicationServices);
        WorkspaceManagement = new WorkspaceManagementViewModel(
            workspaceManagementService,
            workspaceTransferService ?? NullWorkspaceTransferService.Instance);
        Settings = CreateSettings(settings);
        _commandTasks = new CommandTaskCoordinator(exception => Settings.Telemetry.RecordAsync(
            new FusionCanvas.Application.Telemetry.TelemetryEventRequest(
                "Workspace", "CommandFailed", "Error", "Failed", exception.GetType().Name)));
        StoreManagement = new StoreManagementViewModel(
            applicationServices.StoreManagement,
            applicationServices.NicheManagement,
            applicationServices.TagManagement,
            applicationServices.LegacyCatalogCompatibility,
            applicationServices.CatalogSetup,
            applicationServices.MockupTemplateSetup,
            applicationServices.OfferingManagement,
            applicationServices.ProviderCatalog,
            applicationServices.MockupTemplateSourceImages,
            new NullAssetFilePicker(),
            workspaceRepository,
            applicationServices.NichePopulation,
            applicationServices.RasterImageMetadataReader,
            applicationServices.MockupSourceMetadataAssistance,
            applicationServices.MockupSourceImageContentReader);
        StoreManagement.ActiveStoreChanged += (_, store) => Settings.UpdateActiveStore(store?.Id);
        _groupManagementService = applicationServices.GroupManagement;
        _itemManagementService = applicationServices.ItemManagement;
        _itemCsvImportService = applicationServices.ItemCsvImport;
        _tagManagementService = applicationServices.TagManagement;
        _assetManagementService = applicationServices.AssetManagement;
        _itemInspectorService = applicationServices.ItemInspector;
        _ideationAccessStatus = ideationAccessStatus ?? DisabledIdeationAccessStatus.Instance;
        _conceptRefinementService = conceptRefinementService ?? DisabledConceptRefinementService.Instance;
        _conceptRefinementAccessStatus = conceptRefinementAccessStatus ?? DisabledConceptRefinementAccessStatus.Instance;
        _sllGenerationService = sllGenerationService ?? DisabledSllGenerationService.Instance;
        _sllAccessStatus = sllAccessStatus ?? DisabledSllAccessStatus.Instance;
        _sllDocumentCodec = applicationServices.SllDocumentCodec;
        _ideationService = applicationServices.Ideation;
        GroupDetails = new GroupDetailsViewModel(_groupManagementService);
        AssetsManagement = new AssetsViewModel(_assetManagementService);
        ItemInspector = new ItemInspectorViewModel(_itemInspectorService, _itemManagementService, _tagManagementService, titleOptimizationService);
        DesignTool = new DesignStageToolViewModel(
            applicationServices.DesignStage,
            artworkGenerationService,
            Settings.Ai);
        ListingTool = new ListingStageToolViewModel(mockupGenerationService);
        Ideation = new IdeationViewModel(_ideationService, _ideationAccessStatus, snowcloneLibrary, rejectedPhrases);
        ConceptRefinement = new ConceptRefinementSessionViewModel(
            _conceptRefinementService,
            _conceptRefinementAccessStatus,
            ItemInspector);
        SllGeneration = new SllGenerationSessionViewModel(
            _sllGenerationService,
            _sllAccessStatus,
            _sllDocumentCodec,
            ItemInspector);
        _ideationAccessStatus.AvailabilityChanged += (_, _) =>
            Avalonia.Threading.Dispatcher.UIThread.Post(RaiseIdeationProperties);
        Settings.Ai.SettingsChanged += OnAiConfigurationChanged;
        Settings.Ai.AvailabilityChanged += OnAiConfigurationChanged;
        Run(cancellationToken => _ideationAccessStatus.RefreshAsync(cancellationToken));
        Run(cancellationToken => StoreManagement.RefreshNichePopulationAvailabilityAsync(cancellationToken));
        _toolContextResolver = toolContextResolver;
        _stageToolHostService = stageToolHostService;
        _stageToolContentResolver = stageToolContentResolver ?? new BuiltInStageToolContentResolver();
        _workspaceRepository = workspaceRepository;
        _workspaceSnapshot = workspaceSnapshot;
        NavigationState = new NavigationTreePresentationState();
        WorkspaceTree = new WorkspaceTreeViewModel(
            workspaceRepository,
            _groupManagementService,
            workspaceSnapshot,
            _itemManagementService,
            applicationServices.ItemCsvExport,
            applicationServices.WorkspaceBatchRollback);
        OpenNavigationContextCommand = new RelayCommand(parameter =>
        {
            if (parameter is NavigationDocumentContext navigationContext)
            {
                OpenFromNavigation(navigationContext);
            }
        });
        SelectWorkflowStageCommand = new RelayCommand(parameter =>
        {
            if (parameter is WorkflowStage stage)
            {
                SelectWorkflowStage(stage);
            }
        });
        RequestSelectTabCommand = new RelayCommand(parameter =>
        {
            if (parameter is DocumentTabViewModel tab)
            {
                HandleSelectTabRequest(tab);
            }
        });
        RequestCloseTabCommand = new RelayCommand(parameter =>
        {
            if (parameter is DocumentTabViewModel tab)
            {
                HandleCloseTabRequest(tab);
            }
        });
        OpenIdeationCommand = new RelayCommand(_ => OpenIdeation());
        InitializeItemCommands();
        InitializeGroupIntegration();
        StoreManagement.ActiveStoreChanged += (_, store) => RebuildNavigationContexts(store);
        StoreManagement.WorkspaceStructureChanged += (_, _) => RefreshWorkspaceSnapshot();
        WorkspaceManagement.ActiveWorkspaceChanged += (_, workspace) => SwitchWorkspace(workspace);
        SubscribeToWorkspacePromptState();
        // Load through the application service so the startup bridge waits for
        // the data operation only. WorkspaceManagementViewModel.LoadAsync also
        // marshals back to the UI thread, which would deadlock this constructor
        // because it already runs on that thread.
        var workspaceState = StartupTaskRunner.Run(
            token => workspaceManagementService.LoadAsync(token),
            cancellationToken);
        WorkspaceManagement.ApplyInitialState(workspaceState);
        Run(cancellationToken => InitializeStoreManagementAsync(cancellationToken));
        AssetsManagement.WorkspaceStructureChanged += (_, _) => RefreshWorkspaceSnapshot();
        WorkspaceTree.ManageAssetsRequested += (_, selection) => Run(cancellationToken => OpenManageAssetsAsync(selection, cancellationToken));
        DocumentWindow.ActiveContextChanged += (_, context) => CoordinateActiveContext(context);
        DocumentWindow.ToolScopeChangeRequested += (_, scope) => ResolveActiveToolContext(scope);
        DocumentWindow.StageToolSelectionRequested += (_, toolId) => SelectStageTool(toolId);
        ItemInspector.Saved += (_, _) => HandleInspectorSaved();
        Ideation.WorkspaceChanged += (_, _) => RefreshWorkspaceSnapshotAndInspector();
        Ideation.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(IdeationViewModel.IsOpen))
            {
                RaiseIdeationProperties();
            }
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Disposes synchronously after asynchronous command work has been drained.
    /// Use <see cref="DisposeAsync"/> for application shutdown while commands may be in flight.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _commandTasks.Dispose();
        Settings.Ai.SettingsChanged -= OnAiConfigurationChanged;
        Settings.Ai.AvailabilityChanged -= OnAiConfigurationChanged;
        try
        {
            AssetsManagement.Dispose();
        }
        finally
        {
            try
            {
                DesignTool.Dispose();
            }
            finally
            {
                try
                {
                    ConceptRefinement.Dispose();
                }
                finally
                {
                    SllGeneration.Dispose();
                }
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _commandTasks.CloseAdmission();
        _commandTasks.CancelPending();
        await _commandTasks.WaitForStartsAsync().ConfigureAwait(true);

        try
        {
            Dispose();
        }
        finally
        {
            await Task.WhenAll(
                ConceptRefinement.PendingOperation,
                SllGeneration.PendingOperation,
                _commandTasks.WaitForTasksAsync())
                .ConfigureAwait(true);
        }
    }

    public WorkflowStageNavigatorViewModel WorkflowNavigator { get; }

    public DocumentWindowViewModel DocumentWindow { get; }

    public WorkspaceManagementViewModel WorkspaceManagement { get; }

    public SettingsViewModel Settings { get; private set; } = null!;

    public StoreManagementViewModel StoreManagement { get; }

    public GroupDetailsViewModel GroupDetails { get; }

    public AssetsViewModel AssetsManagement { get; }

    public ItemInspectorViewModel ItemInspector { get; }

    public DesignStageToolViewModel DesignTool { get; }

    public ListingStageToolViewModel ListingTool { get; }

    public StageToolContentViewModel? ActiveStageToolContent { get; private set; }

    public string ActiveStageToolContentKey => ActiveStageToolContent?.DetailViewKey ?? "No hosted content";

    public IdeationViewModel Ideation { get; }

    public ConceptRefinementSessionViewModel ConceptRefinement { get; }

    public SllGenerationSessionViewModel SllGeneration { get; }

    public WorkspaceTreeViewModel WorkspaceTree { get; }

    public IItemCsvImportService ItemCsvImport => _itemCsvImportService;

    public NavigationTreePresentationState NavigationState { get; }

    public IReadOnlyList<NavigationDocumentContext> NavigationContexts
    {
        get => _navigationContexts;
        private set
        {
            _navigationContexts = value;
            OnPropertyChanged();
        }
    }

    public ICommand OpenNavigationContextCommand { get; }

    public ICommand SelectWorkflowStageCommand { get; }

    public ICommand OpenIdeationCommand { get; }

    public ICommand MoveStageForwardCommand { get; private set; } = null!;

    public ICommand MoveStageBackCommand { get; private set; } = null!;

    public ICommand SetItemStatusCommand { get; private set; } = null!;

    public bool HasActiveItem => ActiveItem is not null;

    public bool CanMoveStageForward => ActiveItem is { } item
        && ItemWorkflowPolicy.CanPerformOperation(item, ItemOperationKind.StageMovement).IsAllowed
        && item.Stage != WorkflowStage.Listing;

    public bool CanMoveStageBack => ActiveItem is { } item
        && ItemWorkflowPolicy.CanPerformOperation(item, ItemOperationKind.StageMovement).IsAllowed
        && item.Stage != WorkflowStage.Idea;

    public string? StageMoveForwardLabel => ActiveItem is { } item && item.Stage < WorkflowStage.Listing
        ? $"Move to {WorkflowStages.GetDisplayName(item.Stage + 1)} \u25B8"
        : null;

    public string? StageMoveBackLabel => ActiveItem is { } item && item.Stage > WorkflowStage.Idea
        ? $"\u25C2 Move to {WorkflowStages.GetDisplayName(item.Stage - 1)}"
        : null;

    public IReadOnlyList<ItemStatus> AvailableItemStatuses
    {
        get
        {
            if (ActiveItem is not { } item)
            {
                return [];
            }

            return ItemStatuses.Ordered
                .Where(status => status == item.Status
                    || ItemWorkflowPolicy.DecideTransition(item.Status, item.Stage, status).IsAllowed)
                .ToArray();
        }
    }

    public IReadOnlyList<string> AvailableItemStatusLabels =>
        AvailableItemStatuses.Select(status => status.ToString()).ToArray();

    public IReadOnlyList<ItemStatusOptionViewModel> AvailableItemStatusOptions =>
        AvailableItemStatuses.Select(status => StatusOptions[status]).ToArray();

    public ItemStatus? ActiveItemStatus => ActiveItem?.Status;

    public ItemStatus? SelectedItemStatus
    {
        get => ActiveItemStatus;
        set
        {
            if (value is ItemStatus status && status != ActiveItemStatus)
            {
                RequestStatusChange(status);
            }
        }
    }

    public string? SelectedItemStatusLabel
    {
        get => ActiveItemStatus?.ToString();
        set
        {
            if (Enum.TryParse<ItemStatus>(value, out var status) && status != ActiveItemStatus)
            {
                RequestStatusChange(status);
            }
        }
    }

    public ItemStatusOptionViewModel? SelectedItemStatusOption
    {
        get => ActiveItemStatus is { } status ? StatusOptions[status] : null;
        set
        {
            if (value is { Status: var status } && status != ActiveItemStatus)
            {
                RequestStatusChange(status);
            }
        }
    }

    public bool IsStatusConfirmationVisible
    {
        get => _isStatusConfirmationVisible;
        private set
        {
            if (_isStatusConfirmationVisible == value)
            {
                return;
            }

            _isStatusConfirmationVisible = value;
            OnPropertyChanged();
        }
    }

    public string StatusConfirmationMessage { get; private set; } = string.Empty;

    public bool ShowsIdeaStageTool => ActiveStageToolContent?.Kind == StageToolContentKind.Idea;
    public bool ShowsConceptStageTool => ActiveStageToolContent?.Kind == StageToolContentKind.Concept;
    public bool ShowsDesignStageTool => ActiveStageToolContent?.Kind == StageToolContentKind.Design;
    public bool ShowsListingStageTool => ActiveStageToolContent?.Kind == StageToolContentKind.Listing;

    public string ActiveStageToolContentUnavailableMessage =>
        DocumentWindow.StageToolHostState?.SelectedTool is { } selected && ActiveStageToolContent is null
            ? $"The selected stage tool has no hosted content for '{selected.Tool.DetailViewKey}'."
            : string.Empty;

    public bool HasActiveStageToolContentUnavailableMessage =>
        !string.IsNullOrWhiteSpace(ActiveStageToolContentUnavailableMessage);

    public bool IsIdeationActionVisible =>
        DocumentWindow.ActiveContext is
        {
            WorkflowStage: WorkflowStage.Idea,
            EntityKind: WorkspaceEntityKind.Niche or WorkspaceEntityKind.Group or WorkspaceEntityKind.Item
        };

    public bool CanOpenIdeation =>
        IsIdeationActionVisible
        && !Ideation.IsOpen
        && _ideationAccessStatus.GetAvailability().IsAvailable
        && ResolveIdeationScope().IsAvailable;

    public string? IdeationUnavailableMessage
    {
        get
        {
            if (!IsIdeationActionVisible)
            {
                return null;
            }

            var access = _ideationAccessStatus.GetAvailability();
            return access.IsAvailable ? ResolveIdeationScope().Error : access.UnavailableReason;
        }
    }

    public ICommand ConfirmStatusChangeCommand { get; private set; } = null!;
    public ICommand CancelStatusChangeCommand { get; private set; } = null!;
    public ICommand RequestArchiveItemCommand { get; private set; } = null!;
    public ICommand RestoreItemCommand { get; private set; } = null!;
    public ICommand RequestDeleteItemCommand { get; private set; } = null!;

    public string? StageMoveError { get; private set; }

    public string? StatusChangeError { get; private set; }

    private Item? ActiveItem => DocumentWindow.ActiveContext is { EntityKind: WorkspaceEntityKind.Item } context
        ? _workspaceSnapshot.Items.SingleOrDefault(candidate => candidate.Id == context.Id)
        : null;

    public ICommand CreateGroupCommand { get; private set; } = null!;

    public bool CanCreateGroup => StoreManagement.SelectedStore is not null;

    public bool CanManageGroup => WorkspaceTree.CanManageSelection;

    public string GroupActionStatus => CanCreateGroup
        ? CanManageGroup
            ? "Create a child group, rename with F2, or edit details in the pane."
            : "Create under the selected topic or the store's default niche."
        : "Select an active store before creating groups.";

    public bool ShowSelectionSummary =>
        WorkspaceTree.SelectedNode is { EntityKind: WorkspaceEntityKind.Store or WorkspaceEntityKind.Niche };

    public bool ShowStageToolHost =>
        DocumentWindow.HasActiveDocument && !ItemInspector.HasState && !GroupDetails.HasState;

    public bool IsBusy => Volatile.Read(ref _activeOperationCount) > 0;

    public bool ShouldShowFirstStorePrompt =>
        !WorkspaceManagement.IsWorkspaceManagementOpen &&
        !WorkspaceManagement.ShouldShowNoWorkspaceState &&
        StoreManagement.ShouldShowFirstStorePrompt;

    public void OpenFromNavigation(NavigationDocumentContext navigationContext)
    {
        ArgumentNullException.ThrowIfNull(navigationContext);

        _ = Settings.Telemetry.RecordAsync(new FusionCanvas.Application.Telemetry.TelemetryEventRequest(
            "Workspace", "OpenNavigationContext", "Information", "Started",
            $"Opening {navigationContext.Context.EntityKind} in the document window."));
        GuardActiveItemInspectorLeave(() => DocumentWindow.Open(navigationContext.Context));
    }

    public void SelectWorkflowStage(WorkflowStage stage)
    {
        _ = Settings.Telemetry.RecordAsync(new FusionCanvas.Application.Telemetry.TelemetryEventRequest(
            stage switch
            {
                WorkflowStage.Idea => "Ideation",
                WorkflowStage.Concept => "Concept",
                WorkflowStage.Design => "Design",
                WorkflowStage.Listing => "Listing",
                _ => "Workspace"
            }, "SelectWorkflowStage", "Information", "Started", $"Selected workflow stage {stage}."));
        GuardActiveItemInspectorLeave(() => ApplyActiveViewStage(stage));
    }

    private void ApplyActiveViewStage(WorkflowStage stage)
    {
        DocumentWindow.ChangeActiveWorkflowStage(stage);
        WorkflowNavigator.SelectStage(stage);
    }

    private async Task MoveStageForwardAsync()
    {
        var item = ActiveItem;
        if (item is null || !CanMoveStageForward)
        {
            return;
        }

        GuardActiveItemInspectorLeave(() => Run(token => MoveStageAsync(item.Id, item.Stage + 1, token)));
    }

    private async Task MoveStageBackAsync()
    {
        var item = ActiveItem;
        if (item is null || !CanMoveStageBack)
        {
            return;
        }

        GuardActiveItemInspectorLeave(() => Run(token => MoveStageAsync(item.Id, item.Stage - 1, token)));
    }

    private async Task MoveStageAsync(Guid itemId, WorkflowStage destination, CancellationToken cancellationToken)
    {
        var result = await _itemManagementService.MoveItemStageAsync(
            new ItemManagementMoveStageRequest(itemId, destination), cancellationToken).ConfigureAwait(true);
        ApplyLifecycleResult(result, nameof(StageMoveError));
    }

    private void RequestStatusChange(ItemStatus status)
    {
        var item = ActiveItem;
        if (item is null)
        {
            return;
        }

        var decision = ItemWorkflowPolicy.DecideTransition(item.Status, item.Stage, status);
        if (!decision.IsAllowed)
        {
            StatusChangeError = decision.Reason;
            OnPropertyChanged(nameof(StatusChangeError));
            OnPropertyChanged(nameof(SelectedItemStatus));
            OnPropertyChanged(nameof(SelectedItemStatusLabel));
            OnPropertyChanged(nameof(SelectedItemStatusOption));
            return;
        }

        GuardActiveItemInspectorLeave(() =>
        {
            if (decision.RequiresConfirmation)
            {
                _pendingStatus = status;
                StatusConfirmationMessage = decision.Reason;
                OnPropertyChanged(nameof(StatusConfirmationMessage));
                IsStatusConfirmationVisible = true;
                OnPropertyChanged(nameof(SelectedItemStatus));
                OnPropertyChanged(nameof(SelectedItemStatusLabel));
                OnPropertyChanged(nameof(SelectedItemStatusOption));
                return;
            }

            Run(cancellationToken => SetItemStatusAsync(status, confirmed: false, cancellationToken));
        });
    }

    private async Task SetItemStatusAsync(ItemStatus status, bool confirmed, CancellationToken cancellationToken)
    {
        var item = ActiveItem;
        if (item is null)
        {
            return;
        }

        var result = await _itemManagementService.SetItemStatusAsync(
            new ItemManagementSetStatusRequest(item.Id, status, ConfirmProtectedTransition: confirmed), cancellationToken).ConfigureAwait(true);
        ApplyLifecycleResult(result, nameof(StatusChangeError));
    }

    private void InitializeItemCommands()
    {
        MoveStageForwardCommand = new RelayCommand(_ => Run(_ => MoveStageForwardAsync()));
        MoveStageBackCommand = new RelayCommand(_ => Run(_ => MoveStageBackAsync()));
        SetItemStatusCommand = new RelayCommand(parameter =>
        {
            if (parameter is ItemStatus status)
            {
                RequestStatusChange(status);
            }
        });
        ConfirmStatusChangeCommand = new RelayCommand(_ => ConfirmStatusChange());
        CancelStatusChangeCommand = new RelayCommand(_ => CancelStatusChange());
        RequestArchiveItemCommand = new RelayCommand(_ =>
            GuardActiveItemInspectorLeave(() => ItemInspector.RequestArchiveCommand.Execute(null)));
        RestoreItemCommand = new RelayCommand(_ =>
            GuardActiveItemInspectorLeave(() => ItemInspector.RestoreCommand.Execute(null)));
        RequestDeleteItemCommand = new RelayCommand(_ =>
            GuardActiveItemInspectorLeave(() => ItemInspector.RequestDeleteCommand.Execute(null)));
    }

    private void ApplyLifecycleResult(ItemManagementResult result, string errorProperty)
    {
        if (result.Succeeded)
        {
            StageMoveError = null;
            StatusChangeError = null;
            RefreshWorkspaceSnapshot();
            ReopenActiveItem(result.Item?.Id);
        }
        else
        {
            SetError(errorProperty, result.Error);
        }

        RaiseLifecycleProperties();
    }

    private void ReopenActiveItem(Guid? itemId)
    {
        var targetId = itemId ?? (DocumentWindow.ActiveContext is { EntityKind: WorkspaceEntityKind.Item } activeContext ? activeContext.Id : (Guid?)null);
        if (targetId is not { } id)
        {
            return;
        }

        var documentContext = NavigationContexts.SingleOrDefault(candidate => candidate.Context.Id == id && candidate.Context.EntityKind == WorkspaceEntityKind.Item);
        if (documentContext is not null)
        {
            DocumentWindow.RefreshContexts(id, WorkspaceEntityKind.Item, documentContext.Context);
            Run(cancellationToken => ReloadItemInspectorAsync(id, documentContext.Context.WorkflowStage, cancellationToken));
        }
    }

    private async Task ReloadItemInspectorAsync(
        Guid itemId,
        WorkflowStage activeViewStage,
        CancellationToken cancellationToken)
    {
        await ItemInspector.LoadAsync(itemId, cancellationToken).ConfigureAwait(true);
        ItemInspector.ApplyStage(activeViewStage);
        RefreshStageToolState();
    }

    private void RaiseLifecycleProperties()
    {
        OnPropertyChanged(nameof(HasActiveItem));
        OnPropertyChanged(nameof(CanMoveStageForward));
        OnPropertyChanged(nameof(CanMoveStageBack));
        OnPropertyChanged(nameof(StageMoveForwardLabel));
        OnPropertyChanged(nameof(StageMoveBackLabel));
        OnPropertyChanged(nameof(ActiveItemStatus));
        OnPropertyChanged(nameof(SelectedItemStatus));
        OnPropertyChanged(nameof(SelectedItemStatusLabel));
        OnPropertyChanged(nameof(SelectedItemStatusOption));
        OnPropertyChanged(nameof(AvailableItemStatuses));
        OnPropertyChanged(nameof(AvailableItemStatusLabels));
        OnPropertyChanged(nameof(AvailableItemStatusOptions));
        OnPropertyChanged(nameof(StageMoveError));
        OnPropertyChanged(nameof(StatusChangeError));
    }

    private void SetError(string propertyName, string? value)
    {
        if (propertyName == nameof(StageMoveError))
        {
            StageMoveError = value;
            OnPropertyChanged(nameof(StageMoveError));
        }
        else
        {
            StatusChangeError = value;
            OnPropertyChanged(nameof(StatusChangeError));
        }
    }

    private async Task InitializeStoreManagementAsync(CancellationToken cancellationToken)
    {
        var activeWorkspaceId = WorkspaceManagement.SelectedWorkspace?.Id;
        try
        {
            await StoreManagement.SetActiveWorkspaceAsync(activeWorkspaceId, cancellationToken).ConfigureAwait(true);
            GroupManagementServiceSetWorkspace(activeWorkspaceId);
            await StoreManagement.LoadAsync(cancellationToken).ConfigureAwait(true);
            if (StoreManagement.SelectedStore is null && StoreManagement.ActiveStores.Count > 0)
            {
                await StoreManagement.SelectStoreAsync(StoreManagement.ActiveStores[0], cancellationToken).ConfigureAwait(true);
                return;
            }

            RebuildNavigationContexts(StoreManagement.SelectedStore);
        }
        finally
        {
            Volatile.Write(ref _isInitializingWorkspace, 0);
            var selectedWorkspace = WorkspaceManagement.SelectedWorkspace;
            if (selectedWorkspace?.Id != activeWorkspaceId)
            {
                SwitchWorkspace(selectedWorkspace);
            }
        }
    }

    private void SwitchWorkspace(WorkspaceSummary? workspace)
    {
        if (Volatile.Read(ref _isInitializingWorkspace) != 0)
        {
            return;
        }

        var generation = Interlocked.Increment(ref _workspaceSwitchGeneration);
        Run(cancellationToken => SwitchWorkspaceAsync(workspace, generation, cancellationToken));
    }

    private async Task SwitchWorkspaceAsync(
        WorkspaceSummary? workspace,
        long generation,
        CancellationToken cancellationToken)
    {
        await StoreManagement.SetActiveWorkspaceAsync(workspace?.Id, cancellationToken).ConfigureAwait(true);
        if (generation != Interlocked.Read(ref _workspaceSwitchGeneration))
        {
            return;
        }

        GroupManagementServiceSetWorkspace(workspace?.Id);
        if (!await RefreshWorkspaceSnapshotAsync(generation, cancellationToken).ConfigureAwait(true))
        {
            return;
        }

        if (generation != Interlocked.Read(ref _workspaceSwitchGeneration))
        {
            return;
        }

        _ = Settings.Telemetry.RecordAsync(new FusionCanvas.Application.Telemetry.TelemetryEventRequest(
            "Workspace", "ActivateWorkspace", "Information", "Succeeded",
            workspace is null ? "No active workspace." : "Activated workspace.",
            MetadataJson: workspace is null ? null : System.Text.Json.JsonSerializer.Serialize(new { workspaceId = workspace.Id })));
    }

    private void SubscribeToWorkspacePromptState()
    {
        WorkspaceManagement.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(WorkspaceManagementViewModel.ShouldShowNoWorkspaceState) or
                nameof(WorkspaceManagementViewModel.IsWorkspaceManagementOpen))
            {
                OnPropertyChanged(nameof(ShouldShowFirstStorePrompt));
            }
        };
        StoreManagement.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(StoreManagementViewModel.ShouldShowFirstStorePrompt))
            {
                OnPropertyChanged(nameof(ShouldShowFirstStorePrompt));
            }
        };
    }

    private void RebuildNavigationContexts(StoreSummary? selectedStore)
    {
        NavigationContexts = NavigationContextFactory.Create(_workspaceSnapshot, selectedStore);
        WorkspaceTree.SetStore(selectedStore?.Id, _workspaceSnapshot);
        RaiseGroupActionProperties();
    }

    private void RefreshWorkspaceSnapshot()
    {
        if (_workspaceRepository is not null)
        {
            _workspaceSnapshot = _workspaceRepository.LoadAsync().GetAwaiter().GetResult();
        }

        RebuildNavigationContexts(StoreManagement.SelectedStore);
        RaiseLifecycleProperties();
    }

    private async Task<bool> RefreshWorkspaceSnapshotAsync(long generation, CancellationToken cancellationToken)
    {
        var snapshot = _workspaceRepository is not null
            ? await _workspaceRepository.LoadAsync(cancellationToken).ConfigureAwait(true)
            : _workspaceSnapshot;
        if (generation != Interlocked.Read(ref _workspaceSwitchGeneration))
        {
            return false;
        }

        _workspaceSnapshot = snapshot;
        RebuildNavigationContexts(StoreManagement.SelectedStore);
        RaiseLifecycleProperties();
        return true;
    }

    private void RefreshWorkspaceSnapshotAndInspector()
    {
        RefreshWorkspaceSnapshot();
        RefreshActiveItemInspector();
    }

    public async Task RefreshWorkspaceAfterImportAsync(CancellationToken cancellationToken = default)
    {
        RefreshWorkspaceSnapshot();
        await WorkspaceTree.ExecuteTrackedCommandAsync(WorkspaceTree.ReloadAsync, cancellationToken).ConfigureAwait(true);
        RefreshActiveItemInspector();
    }

    private void RefreshActiveItemInspector()
    {
        if (DocumentWindow.ActiveContext is { EntityKind: WorkspaceEntityKind.Item, Kind: DocumentContextKind.Item, Id: var itemId }
            && ItemInspector.HasState
            && !ItemInspector.HasUnsavedChanges)
        {
            Run(cancellationToken => ItemInspector.LoadAsync(itemId, cancellationToken));
        }
    }

    private void CoordinateActiveContext(DocumentContext? context)
    {
        RaiseGroupActionProperties();
        if (context is null)
        {
            WorkflowNavigator.SetActiveItem(null);
            DocumentWindow.ApplyToolContext(null);
            DocumentWindow.ApplyStageToolHostState(null);
            ApplyStageToolContent(null);
            ItemInspector.Clear();
            GroupDetails.Clear();
            RaiseLifecycleProperties();
            OnPropertyChanged(nameof(ShowStageToolHost));
            RefreshStageToolState();
            RaiseIdeationProperties();
            return;
        }

        if (context.EntityKind is WorkspaceEntityKind.Niche or WorkspaceEntityKind.Group or WorkspaceEntityKind.Item)
        {
            WorkspaceTree.SelectEntity(
                context.Id,
                notifySelectionChanged: false,
                replaceMultiSelection: false);
        }

        if (context.NavigationLocation is not null)
        {
            NavigationState.RevealPath(context.NavigationLocation.NodePath);
        }

        WorkflowNavigator.SetActiveTab(new DocumentTabWorkflowContext(
            DocumentWindow.ActiveTab?.TabId ?? Guid.Empty,
            context.Workflow));
        ResolveActiveToolContext();
        CoordinateItemInspector(context);
        CoordinateGroupDetails(context);
        RaiseLifecycleProperties();
        OnPropertyChanged(nameof(ShowStageToolHost));
        RefreshStageToolState();
        RaiseIdeationProperties();
    }

    private void CoordinateGroupDetails(DocumentContext context)
    {
        if (context.EntityKind == WorkspaceEntityKind.Group
            && _workspaceSnapshot.Groups.SingleOrDefault(candidate => candidate.Id == context.Id) is { } group)
        {
            if (GroupDetails.Group?.Id != group.Id)
            {
                var nicheId = WorkspaceContextResolver.ResolveEffectiveNicheId(_workspaceSnapshot, group.Id);
                if (nicheId is Guid effectiveNicheId)
                {
                    Run(cancellationToken => GroupDetails.LoadAsync(group.Id, group.StoreId, effectiveNicheId, cancellationToken));
                }
            }

            return;
        }

        if (GroupDetails.HasState)
        {
            GroupDetails.Clear();
        }
    }

    private void CoordinateItemInspector(DocumentContext context)
    {
        if (context.Kind != DocumentContextKind.Item || context.EntityKind != WorkspaceEntityKind.Item)
        {
            if (ItemInspector.LoadedItemId is not null)
            {
                ItemInspector.Clear();
            }
            return;
        }

        if (ItemInspector.LoadedItemId == context.Id)
        {
            ItemInspector.ApplyStage(context.WorkflowStage);
            return;
        }

        Run(cancellationToken => ItemInspector.LoadAsync(context.Id, cancellationToken));
    }

    private void InitializeGroupIntegration()
    {
        CreateGroupCommand = new RelayCommand(_ => WorkspaceTree.BeginCreateCommand.Execute(null));
        WorkspaceTree.OpenInTabRequested += (_, selection) => OpenTreeSelectionInTab(selection);
        WorkspaceTree.OpenSelectedInTabsRequested += (_, selections) =>
        {
            foreach (var selection in selections)
            {
                OpenTreeSelectionInTab(selection);
            }
        };
        WorkspaceTree.SelectionChanged += (_, selection) => OpenTreeSelectionInCurrentTab(selection);
        WorkspaceTree.EntitiesDeleted += (_, entityIds) => CloseDeletedEntityTabs(entityIds);
        WorkspaceTree.StructureChanged += (_, _) => RefreshWorkspaceSnapshotAndInspector();
        WorkspaceTree.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(WorkspaceTreeViewModel.SelectedNode) or nameof(WorkspaceTreeViewModel.CanManageSelection))
            {
                RaiseGroupActionProperties();
                OnPropertyChanged(nameof(ShowSelectionSummary));
            }
        };
        GroupDetails.StructureChanged += (_, group) => HandleGroupStructureChanged(group);
        GroupDetails.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(GroupDetailsViewModel.HasState))
            {
                OnPropertyChanged(nameof(ShowStageToolHost));
            }
        };
        ItemInspector.LifecycleChanged += (_, args) => HandleItemLifecycleChanged(args);
        ItemInspector.TagsChanged += (_, _) => RefreshWorkspaceSnapshot();
        ItemInspector.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(ItemInspectorViewModel.HasState))
            {
                OnPropertyChanged(nameof(ShowStageToolHost));
            }

            if (args.PropertyName is nameof(ItemInspectorViewModel.State))
            {
                RefreshStageToolState();
            }
        };
    }

    private void GroupManagementServiceSetWorkspace(Guid? workspaceId)
    {
        _groupManagementService.SetActiveWorkspace(workspaceId);
        _itemManagementService.SetActiveWorkspace(workspaceId);
        AssetsManagement.SetActiveWorkspace(workspaceId);
    }

    private async Task OpenManageAssetsAsync(
        WorkspaceTreeSelection selection,
        CancellationToken cancellationToken)
    {
        RefreshWorkspaceSnapshot();
        var storeId = ResolveContextStoreId(selection);
        if (storeId is null)
        {
            return;
        }

        await AssetsManagement.OpenForContextAsync(
            new AssetContextReference(selection.Kind, selection.Id),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task OpenManageStoreAssetsAsync()
    {
        RefreshWorkspaceSnapshot();
        if (StoreManagement.SelectedStore is not { } store)
        {
            return;
        }

        await AssetsManagement.OpenForContextAsync(new AssetContextReference(WorkspaceEntityKind.Store, store.Id)).ConfigureAwait(false);
    }

    private Guid? ResolveContextStoreId(WorkspaceTreeSelection selection) =>
        WorkspaceContextResolver.ResolveStoreId(_workspaceSnapshot, selection);

    private void OpenTreeSelectionInTab(WorkspaceTreeSelection selection)
    {
        var context = NavigationContexts.SingleOrDefault(candidate =>
            candidate.Context.EntityKind == selection.Kind && candidate.Context.Id == selection.Id);
        if (context is not null)
        {
            GuardActiveItemInspectorLeave(() => DocumentWindow.OpenAdditional(context.Context));
        }
    }

    private void OpenTreeSelectionInCurrentTab(WorkspaceTreeSelection selection)
    {
        var context = NavigationContexts.SingleOrDefault(candidate =>
            candidate.Context.EntityKind == selection.Kind && candidate.Context.Id == selection.Id);
        if (context is not null)
        {
            GuardActiveItemInspectorLeave(() => DocumentWindow.OpenOrReplaceActive(context.Context));
        }
    }

    private void HandleSelectTabRequest(DocumentTabViewModel tab)
    {
        ArgumentNullException.ThrowIfNull(tab);
        if (ReferenceEquals(DocumentWindow.ActiveTab, tab))
        {
            return;
        }

        GuardActiveItemInspectorLeave(() => DocumentWindow.SelectTab(tab));
    }

    private void HandleCloseTabRequest(DocumentTabViewModel tab)
    {
        ArgumentNullException.ThrowIfNull(tab);
        if (ReferenceEquals(DocumentWindow.ActiveTab, tab))
        {
            GuardActiveItemInspectorLeave(() => DocumentWindow.CloseTab(tab));
            return;
        }

        DocumentWindow.CloseTab(tab);
    }

    public ICommand RequestSelectTabCommand { get; private set; } = null!;

    public ICommand RequestCloseTabCommand { get; private set; } = null!;

    private void GuardActiveItemInspectorLeave(Action proceed)
    {
        ArgumentNullException.ThrowIfNull(proceed);
        var commits = new List<Func<CancellationToken, Task>>(2);
        if (DocumentWindow.ActiveContext is { EntityKind: WorkspaceEntityKind.Item }
            && ItemInspector.HasState
            && !ItemInspector.IsReadOnly
            && (ItemInspector.HasUnsavedChanges || ConceptRefinement.HasPendingWorkingEdits))
        {
            commits.Add(CommitActiveItemDetailsAsync);
        }

        if (DocumentWindow.ActiveContext is { EntityKind: WorkspaceEntityKind.Group }
            && GroupDetails.HasState
            && GroupDetails.HasUnsavedChanges)
        {
            commits.Add(GroupDetails.CommitEditsAsync);
        }

        if (commits.Count == 0)
        {
            proceed();
            return;
        }

        Run(cancellationToken => CommitAndProceedAsync(commits, proceed, cancellationToken));
    }

    private async Task CommitAndProceedAsync(
        IReadOnlyCollection<Func<CancellationToken, Task>> commits,
        Action proceed,
        CancellationToken cancellationToken)
    {
        await Task.WhenAll(commits.Select(commit => commit(cancellationToken))).ConfigureAwait(true);
        if (DocumentWindow.ActiveContext is { EntityKind: WorkspaceEntityKind.Item }
            && ItemInspector.HasState
            && (ItemInspector.HasUnsavedChanges || ConceptRefinement.HasPendingWorkingEdits))
        {
            RevertStatusSelector();
            return;
        }

        proceed();
    }

    private void RevertStatusSelector()
    {
        OnPropertyChanged(nameof(SelectedItemStatus));
        OnPropertyChanged(nameof(SelectedItemStatusLabel));
        OnPropertyChanged(nameof(SelectedItemStatusOption));
    }

    private void ConfirmStatusChange()
    {
        if (_pendingStatus is not { } status)
        {
            return;
        }

        _pendingStatus = null;
        IsStatusConfirmationVisible = false;
        Run(cancellationToken => SetItemStatusAsync(status, confirmed: true, cancellationToken));
    }

    private void CancelStatusChange()
    {
        _pendingStatus = null;
        IsStatusConfirmationVisible = false;
        OnPropertyChanged(nameof(SelectedItemStatus));
        OnPropertyChanged(nameof(SelectedItemStatusLabel));
        OnPropertyChanged(nameof(SelectedItemStatusOption));
    }

    public void CommitActiveDetailsEdits()
    {
        if (ItemInspector.HasState && !ItemInspector.IsReadOnly)
        {
            Run(cancellationToken => CommitActiveItemDetailsAsync(cancellationToken));
        }

        if (GroupDetails.HasState && !GroupDetails.IsReadOnly)
        {
            Run(cancellationToken => GroupDetails.CommitEditsAsync(cancellationToken));
        }
    }

    private async Task CommitActiveItemDetailsAsync(CancellationToken cancellationToken = default)
    {
        await ConceptRefinement.CommitPendingWorkingEditsAsync(cancellationToken).ConfigureAwait(true);
        await ItemInspector.CommitEditsAsync(cancellationToken).ConfigureAwait(true);
    }

    private void HandleItemLifecycleChanged(ItemInspectorLifecycleEventArgs args)
    {
        if (_disposed)
        {
            return;
        }

        var result = args.Result;
        RefreshWorkspaceSnapshot();
        var changed = result.Item;
        var replacement = args.Deleted || changed is { IsEffectivelyActive: false }
            ? result.State.ActiveItems.FirstOrDefault() ?? changed
            : changed;
        WorkspaceTree.SelectEntity(replacement is null
            ? null
            : replacement.IsEffectivelyActive ? replacement.Id : replacement.Topic.Id);
        RaiseLifecycleProperties();
    }

    private void HandleInspectorSaved()
    {
        if (_disposed)
        {
            return;
        }

        RefreshWorkspaceSnapshot();
        if (DocumentWindow.ActiveContext is { } context && context.EntityKind == WorkspaceEntityKind.Item)
        {
            var refreshed = NavigationContexts.SingleOrDefault(candidate => candidate.Context.Id == context.Id);
            if (refreshed is not null)
            {
                DocumentWindow.RefreshContexts(context.Id, WorkspaceEntityKind.Item, refreshed.Context);
            }
        }

        if (DocumentWindow.ActiveContext is { EntityKind: WorkspaceEntityKind.Item, Id: var itemId }
            && !ItemInspector.HasUnsavedChanges)
        {
            Run(cancellationToken => ItemInspector.LoadAsync(itemId, cancellationToken));
        }

        RefreshStageToolState();
    }

    private void OnAiConfigurationChanged(object? sender, EventArgs args)
    {
        if (_disposed)
        {
            return;
        }

        Run(cancellationToken => _ideationAccessStatus.RefreshAsync(cancellationToken));
        Run(cancellationToken => ConceptRefinement.RefreshAvailabilityAsync(cancellationToken));
        Run(cancellationToken => SllGeneration.RefreshAvailabilityAsync(cancellationToken));
        Run(cancellationToken => ItemInspector.RefreshTitleOptimizationAvailabilityAsync(cancellationToken));
        Run(cancellationToken => DesignTool.RefreshArtworkAvailabilityAsync(cancellationToken));
        Run(cancellationToken => StoreManagement.RefreshNichePopulationAvailabilityAsync(cancellationToken));
    }

    private void RefreshStageToolState()
    {
        if (_disposed)
        {
            return;
        }

        OnPropertyChanged(nameof(ShowsIdeaStageTool));
        OnPropertyChanged(nameof(ShowsConceptStageTool));
        OnPropertyChanged(nameof(ShowsDesignStageTool));
        OnPropertyChanged(nameof(ShowsListingStageTool));
        RaiseIdeationProperties();

        if (ActiveItem is not { } item)
        {
            return;
        }

        var activeStage = DocumentWindow.ActiveContext?.WorkflowStage ?? item.Stage;
        var canEdit = ItemWorkflowPolicy.CanEditStage(item, activeStage).IsAllowed;
        Run(cancellationToken => ListingTool.LoadAsync(item.Id, item.Status, canEdit, cancellationToken));
        if (activeStage == WorkflowStage.Design)
        {
            Run(cancellationToken => DesignTool.LoadAsync(item.Id, canEdit, cancellationToken));
        }

        // Refresh concept refinement availability when Concept surface loads (D8)
        if (activeStage == WorkflowStage.Concept)
        {
            Run(cancellationToken => ConceptRefinement.RefreshAvailabilityAsync(cancellationToken));
            Run(cancellationToken => SllGeneration.RefreshAvailabilityAsync(cancellationToken));
        }
    }

    private void CloseDeletedEntityTabs(IReadOnlySet<Guid> entityIds)
    {
        foreach (var tab in DocumentWindow.Tabs.Where(tab => entityIds.Contains(tab.Context.Id)).ToArray())
        {
            DocumentWindow.CloseTab(tab);
        }
    }

    private void HandleGroupStructureChanged(GroupSummary? group)
    {
        if (_disposed)
        {
            return;
        }

        RefreshWorkspaceSnapshot();
        if (group is null || StoreManagement.SelectedStore?.Id != group.StoreId)
        {
            return;
        }

        var nodePath = group.IsArchived
            ? new[] { group.StoreId, group.NicheId }
            : new[] { group.StoreId }.Concat(group.Path).ToArray();
        NavigationState.RevealPath(nodePath);
        RaiseGroupActionProperties();
    }

    private void RaiseGroupActionProperties()
    {
        OnPropertyChanged(nameof(CanCreateGroup));
        OnPropertyChanged(nameof(CanManageGroup));
        OnPropertyChanged(nameof(GroupActionStatus));
    }

    private void Run(Func<CancellationToken, Task> operation)
    {
        _commandTasks.Run(async cancellationToken =>
        {
            Interlocked.Increment(ref _activeOperationCount);
            OnPropertyChanged(nameof(IsBusy));
            try
            {
                await operation(cancellationToken).ConfigureAwait(true);
            }
            finally
            {
                Interlocked.Decrement(ref _activeOperationCount);
                OnPropertyChanged(nameof(IsBusy));
            }
        });
    }

    private SettingsViewModel CreateSettings(SettingsViewModel? provided)
    {
        var settings = provided ?? new SettingsViewModel(
            new InMemoryApplicationSettingsStore(),
            new AvaloniaApplicationThemeController(),
            ApplicationSettings.Default,
            loadWarning: null);
        settings.AttachWorkspaceManagement(WorkspaceManagement);
        return settings;
    }

    private void ResolveActiveToolContext(ToolContextScopeKind? scopeOverride = null)
    {
        var context = DocumentWindow.ActiveContext;
        if (context is null)
        {
            DocumentWindow.ApplyToolContext(null);
            DocumentWindow.ApplyStageToolHostState(null);
            ApplyStageToolContent(null);
            return;
        }

        var selectionKind = context.Kind switch
        {
            DocumentContextKind.Item => ToolContextSelectionKind.Item,
            DocumentContextKind.Topic => ToolContextSelectionKind.Topic,
            _ => ToolContextSelectionKind.Store
        };

        var request = new ToolContextResolveRequest(
            _workspaceSnapshot,
            selectionKind,
            context.EntityKind,
            context.Id,
            context.WorkflowStage,
            scopeOverride);
        var resolution = _toolContextResolver.Resolve(request);
        var hostState = _stageToolHostService.Build(new StageToolHostRequest(
            _workspaceSnapshot,
            selectionKind,
            context.EntityKind,
            context.Id,
            context.WorkflowStage,
            ScopeOverride: scopeOverride));

        DocumentWindow.ApplyToolContext(resolution);
        DocumentWindow.ApplyStageToolHostState(hostState);
        ApplyStageToolContent(hostState);
    }

    private void ApplyStageToolContent(StageToolHostState? state)
    {
        ActiveStageToolContent = state?.SelectedTool is { } selected
            ? _stageToolContentResolver.Resolve(selected.Tool, this)
            : null;

        OnPropertyChanged(nameof(ActiveStageToolContent));
        OnPropertyChanged(nameof(ActiveStageToolContentKey));
        OnPropertyChanged(nameof(ShowsIdeaStageTool));
        OnPropertyChanged(nameof(ShowsConceptStageTool));
        OnPropertyChanged(nameof(ShowsDesignStageTool));
        OnPropertyChanged(nameof(ShowsListingStageTool));
        OnPropertyChanged(nameof(ActiveStageToolContentUnavailableMessage));
        OnPropertyChanged(nameof(HasActiveStageToolContentUnavailableMessage));
    }

    private void SelectStageTool(string toolId)
    {
        var state = DocumentWindow.StageToolHostState;
        if (state is null)
        {
            return;
        }

        _stageToolHostService.SelectTool(new StageToolSelectionKey(state.WorkflowStage, state.ContextKind), toolId);
        ResolveActiveToolContext();
    }

    private void OpenIdeation()
    {
        if (!CanOpenIdeation)
        {
            return;
        }

        var resolution = ResolveIdeationScope();
        if (resolution.Scope is { } scope)
        {
            Ideation.Open(scope);
            _ = Settings.Telemetry.RecordAsync(new FusionCanvas.Application.Telemetry.TelemetryEventRequest(
                "Ideation", "OpenIdeation", "Information", "Succeeded", "Opened ideation for the active workspace context."));
            RaiseIdeationProperties();
        }
    }

    private IdeationScopeResult ResolveIdeationScope()
    {
        var context = DocumentWindow.ActiveContext;
        return context is null
            ? IdeationScopeResult.Unavailable("Select an active niche, group, or Item.")
            : _ideationService.ResolveScope(_workspaceSnapshot, context.EntityKind, context.Id);
    }

    private void RaiseIdeationProperties()
    {
        OnPropertyChanged(nameof(IsIdeationActionVisible));
        OnPropertyChanged(nameof(CanOpenIdeation));
        OnPropertyChanged(nameof(IdeationUnavailableMessage));
    }

    private sealed class DisabledIdeationAccessStatus : IIdeationAccessStatus
    {
        public static DisabledIdeationAccessStatus Instance { get; } = new();

        public IdeationAccessAvailability GetAvailability() =>
            IdeationAccessAvailability.Unavailable("AI services were not supplied.");
    }

    private sealed class DisabledConceptRefinementService : IConceptRefinementService
    {
        public static DisabledConceptRefinementService Instance { get; } = new();

        public Task<ConceptRefinementResult> InitializeAsync(
            Guid itemId,
            ConceptRefinementTriangle current,
            string originalIdea,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ConceptRefinementResult.Failure(
                AiTextFailureKind.NotConfigured,
                "AI services were not supplied."));

        public Task<ConceptRefinementResult> RefineAsync(
            Guid itemId,
            ConceptRefinementActionKind action,
            ConceptRefinementCorner corner,
            ConceptRefinementTriangle current,
            string originalIdea,
            string? instruction,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ConceptRefinementResult.Failure(
                AiTextFailureKind.NotConfigured,
                "AI services were not supplied."));
    }

    private sealed class DisabledConceptRefinementAccessStatus : IConceptRefinementAccessStatus
    {
        public static DisabledConceptRefinementAccessStatus Instance { get; } = new();

        public ConceptRefinementAccessAvailability GetAvailability() =>
            ConceptRefinementAccessAvailability.Unavailable("AI services were not supplied.");
    }

    private sealed class DisabledSllGenerationService : ISllGenerationService
    {
        public static DisabledSllGenerationService Instance { get; } = new();

        public Task<SllGenerationResult> GenerateAsync(
            Guid itemId,
            ConceptRefinementTriangle triangle,
            string originalIdea,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(SllGenerationResult.Failure(
                AiTextFailureKind.NotConfigured,
                "AI services were not supplied."));
    }

    private sealed class DisabledSllAccessStatus : ISllAccessStatus
    {
        public static DisabledSllAccessStatus Instance { get; } = new();

        public SllAccessAvailability GetAvailability() =>
            SllAccessAvailability.Unavailable("AI services were not supplied.");
    }


    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

}

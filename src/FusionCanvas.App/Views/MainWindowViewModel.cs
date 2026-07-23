using FusionCanvas.App.Assets;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.App.Groups;
using FusionCanvas.App.Items;
using FusionCanvas.App.Navigation;
using FusionCanvas.App.Settings;
using FusionCanvas.App.Stores;
using FusionCanvas.App.StageTools;
using FusionCanvas.App.Workspace;
using FusionCanvas.App.Workflow;
using FusionCanvas.Application.Settings;
using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace FusionCanvas.App.Views;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private static readonly IReadOnlyDictionary<ItemStatus, ItemStatusOptionViewModel> StatusOptions =
        ItemStatuses.Ordered.ToDictionary(
            status => status,
            status => new ItemStatusOptionViewModel(status, status.ToString()));
    private static readonly Guid StoreNodeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid NicheNodeId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid TopicNodeId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private static readonly Guid IdeaNodeId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid DesignNodeId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid ListingNodeId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    private readonly IToolContextResolver _toolContextResolver;
    private readonly IStageToolHostService _stageToolHostService;
    private readonly IWorkspaceRepository? _workspaceRepository;
    private readonly IGroupManagementService _groupManagementService;
    private readonly IItemManagementService _itemManagementService;
    private readonly IAssetManagementService _assetManagementService;
    private readonly IItemInspectorService _itemInspectorService;
    private readonly ITagManagementService _tagManagementService;
    private ItemStatus? _pendingStatus;
    private bool _isStatusConfirmationVisible;
    private bool _isDesignRemoveConfirmationVisible;
    private WorkspaceSnapshot _workspaceSnapshot;
    private IReadOnlyList<NavigationDocumentContext> _navigationContexts = [];

    public MainWindowViewModel()
        : this(CreateSampleWorkspace())
    {
    }

    private MainWindowViewModel(WorkspaceSnapshot sampleWorkspace)
        : this(
            new WorkflowStageNavigatorViewModel(new WorkflowStageNavigatorService()),
            new DocumentWindowViewModel(),
            new ToolContextResolver(),
            new StageToolHostService(BuiltInStageTools.CreateDefaultRegistry(), new ToolContextResolver()),
            new InMemoryWorkspaceRepository(sampleWorkspace),
            sampleWorkspace)
    {
    }

    public static MainWindowViewModel CreateForDefaultWorkspace(SettingsViewModel? settings = null) =>
        new(
            new WorkflowStageNavigatorViewModel(new WorkflowStageNavigatorService()),
            new DocumentWindowViewModel(),
            new ToolContextResolver(),
            new StageToolHostService(BuiltInStageTools.CreateDefaultRegistry(), new ToolContextResolver()),
            AppWorkspaceFactory.CreateDefault(),
            settings);

    private MainWindowViewModel(
        WorkflowStageNavigatorViewModel workflowNavigator,
        DocumentWindowViewModel documentWindow,
        IToolContextResolver toolContextResolver,
        IStageToolHostService stageToolHostService,
        AppWorkspaceRuntime runtime,
        SettingsViewModel? settings)
        : this(
            workflowNavigator,
            documentWindow,
            toolContextResolver,
            stageToolHostService,
            runtime.Repository,
            runtime.Snapshot,
            runtime.GroupManagement,
            runtime.ItemManagement,
            runtime.AssetManagement,
            runtime.FileStore,
            runtime.ItemInspector,
            runtime.TagManagement,
            settings)
    {
    }

    public MainWindowViewModel(
        WorkflowStageNavigatorViewModel workflowNavigator,
        DocumentWindowViewModel documentWindow)
        : this(
            workflowNavigator,
            documentWindow,
            new ToolContextResolver(),
            new StageToolHostService(BuiltInStageTools.CreateDefaultRegistry(), new ToolContextResolver()),
            new InMemoryWorkspaceRepository(CreateSampleWorkspace()),
            CreateSampleWorkspace())
    {
    }

    public MainWindowViewModel(
        WorkflowStageNavigatorViewModel workflowNavigator,
        DocumentWindowViewModel documentWindow,
        IToolContextResolver toolContextResolver,
        IStageToolHostService stageToolHostService,
        IStoreManagementService storeManagementService,
        WorkspaceSnapshot workspaceSnapshot)
        : this(
            workflowNavigator,
            documentWindow,
            toolContextResolver,
            stageToolHostService,
            storeManagementService,
            null,
            workspaceSnapshot)
    {
    }

    public MainWindowViewModel(
        WorkflowStageNavigatorViewModel workflowNavigator,
        DocumentWindowViewModel documentWindow,
        IToolContextResolver toolContextResolver,
        IStageToolHostService stageToolHostService,
        IStoreManagementService storeManagementService,
        INicheManagementService? nicheManagementService,
        WorkspaceSnapshot workspaceSnapshot)
    {
        var treeRepository = new InMemoryWorkspaceRepository(workspaceSnapshot);
        WorkflowNavigator = workflowNavigator;
        DocumentWindow = documentWindow;
        WorkspaceManagement = new WorkspaceManagementViewModel(new WorkspaceManagementService(new InMemoryWorkspaceRepository(workspaceSnapshot)));
        Settings = CreateSettings(null);
        StoreManagement = new StoreManagementViewModel(storeManagementService, nicheManagementService);
        _workspaceRepository = treeRepository;
        _groupManagementService = new GroupManagementService(treeRepository);
        _itemManagementService = new ItemManagementService(treeRepository);
        _tagManagementService = new TagManagementService(treeRepository);
        var fileStore = new InMemoryWorkspaceFileStore();
        _assetManagementService = new AssetManagementService(treeRepository, fileStore);
        _itemInspectorService = new ItemInspectorService(treeRepository);
        GroupDetails = new GroupDetailsViewModel(_groupManagementService);
        AssetsManagement = new AssetsViewModel(_assetManagementService);
        ItemInspector = new ItemInspectorViewModel(_itemInspectorService, _itemManagementService, _tagManagementService);
        DesignTool = new DesignStageToolViewModel(new DesignFileService(treeRepository, fileStore));
        ListingTool = new ListingStageToolViewModel();
        _toolContextResolver = toolContextResolver;
        _stageToolHostService = stageToolHostService;
        _workspaceSnapshot = workspaceSnapshot;
        NavigationState = new NavigationTreePresentationState();
        WorkspaceTree = new WorkspaceTreeViewModel(treeRepository, _groupManagementService, workspaceSnapshot, items: _itemManagementService);
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
        InitializeItemCommands();
        InitializeGroupIntegration();
        StoreManagement.ActiveStoreChanged += (_, store) => RebuildNavigationContexts(store);
        StoreManagement.WorkspaceStructureChanged += (_, _) => RefreshWorkspaceSnapshot();
        WorkspaceManagement.ActiveWorkspaceChanged += (_, workspace) => SwitchWorkspace(workspace);
        SubscribeToWorkspacePromptState();
        InitializeWorkspaces();
        InitializeStores();
        AssetsManagement.WorkspaceStructureChanged += (_, _) => RefreshWorkspaceSnapshot();
        WorkspaceTree.ManageAssetsRequested += (_, selection) => Run(OpenManageAssetsAsync(selection));
        DocumentWindow.ActiveContextChanged += (_, context) => CoordinateActiveContext(context);
        DocumentWindow.ToolScopeChangeRequested += (_, scope) => ResolveActiveToolContext(scope);
        DocumentWindow.StageToolSelectionRequested += (_, toolId) => SelectStageTool(toolId);
        ItemInspector.Saved += (_, _) => HandleInspectorSaved();
    }

    public MainWindowViewModel(
        WorkflowStageNavigatorViewModel workflowNavigator,
        DocumentWindowViewModel documentWindow,
        IToolContextResolver toolContextResolver,
        IStageToolHostService stageToolHostService,
        IWorkspaceRepository workspaceRepository,
        WorkspaceSnapshot workspaceSnapshot,
        IGroupManagementService? groupManagementService = null,
        IItemManagementService? itemManagementService = null,
        IAssetManagementService? assetManagementService = null,
        IWorkspaceFileStore? workspaceFileStore = null,
        IItemInspectorService? itemInspectorService = null,
        ITagManagementService? tagManagementService = null,
        SettingsViewModel? settings = null)
    {
        WorkflowNavigator = workflowNavigator;
        DocumentWindow = documentWindow;
        WorkspaceManagement = new WorkspaceManagementViewModel(new WorkspaceManagementService(workspaceRepository));
        Settings = CreateSettings(settings);
        StoreManagement = new StoreManagementViewModel(
            new StoreManagementService(workspaceRepository),
            new NicheManagementService(workspaceRepository),
            new TagManagementService(workspaceRepository));
        _groupManagementService = groupManagementService ?? new GroupManagementService(workspaceRepository);
        _itemManagementService = itemManagementService ?? new ItemManagementService(workspaceRepository);
        _tagManagementService = tagManagementService ?? new TagManagementService(workspaceRepository);
        var fileStore = workspaceFileStore ?? new InMemoryWorkspaceFileStore();
        _assetManagementService = assetManagementService ?? new AssetManagementService(workspaceRepository, fileStore);
        _itemInspectorService = itemInspectorService ?? new ItemInspectorService(workspaceRepository);
        GroupDetails = new GroupDetailsViewModel(_groupManagementService);
        AssetsManagement = new AssetsViewModel(_assetManagementService);
        ItemInspector = new ItemInspectorViewModel(_itemInspectorService, _itemManagementService, _tagManagementService);
        DesignTool = new DesignStageToolViewModel(new DesignFileService(workspaceRepository, fileStore));
        ListingTool = new ListingStageToolViewModel();
        _toolContextResolver = toolContextResolver;
        _stageToolHostService = stageToolHostService;
        _workspaceRepository = workspaceRepository;
        _workspaceSnapshot = workspaceSnapshot;
        NavigationState = new NavigationTreePresentationState();
        WorkspaceTree = new WorkspaceTreeViewModel(workspaceRepository, _groupManagementService, workspaceSnapshot, items: _itemManagementService);
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
        InitializeItemCommands();
        InitializeGroupIntegration();
        StoreManagement.ActiveStoreChanged += (_, store) => RebuildNavigationContexts(store);
        StoreManagement.WorkspaceStructureChanged += (_, _) => RefreshWorkspaceSnapshot();
        WorkspaceManagement.ActiveWorkspaceChanged += (_, workspace) => SwitchWorkspace(workspace);
        SubscribeToWorkspacePromptState();
        InitializeWorkspaces();
        InitializeStores();
        AssetsManagement.WorkspaceStructureChanged += (_, _) => RefreshWorkspaceSnapshot();
        WorkspaceTree.ManageAssetsRequested += (_, selection) => Run(OpenManageAssetsAsync(selection));
        DocumentWindow.ActiveContextChanged += (_, context) => CoordinateActiveContext(context);
        DocumentWindow.ToolScopeChangeRequested += (_, scope) => ResolveActiveToolContext(scope);
        DocumentWindow.StageToolSelectionRequested += (_, toolId) => SelectStageTool(toolId);
        ItemInspector.Saved += (_, _) => HandleInspectorSaved();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

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

    public WorkspaceTreeViewModel WorkspaceTree { get; }

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

    public ICommand MoveStageForwardCommand { get; private set; } = null!;

    public ICommand MoveStageBackCommand { get; private set; } = null!;

    public ICommand SetItemStatusCommand { get; private set; } = null!;

    public bool HasActiveItem => ActiveItem is not null;

    public bool CanMoveStageForward => ActiveItem is { } item && !IsInactive(item) && item.Stage != WorkflowStage.Listing;

    public bool CanMoveStageBack => ActiveItem is { } item && !IsInactive(item) && item.Stage != WorkflowStage.Idea;

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
        private set => SetField(ref _isStatusConfirmationVisible, value);
    }

    public string StatusConfirmationMessage { get; private set; } = string.Empty;

    public bool IsDesignRemoveConfirmationVisible
    {
        get => _isDesignRemoveConfirmationVisible;
        set => SetField(ref _isDesignRemoveConfirmationVisible, value);
    }

    public bool ShowsIdeaStageTool => DocumentWindow.ActiveContext?.WorkflowStage == WorkflowStage.Idea;
    public bool ShowsConceptStageTool => DocumentWindow.ActiveContext?.WorkflowStage == WorkflowStage.Concept;
    public bool ShowsDesignStageTool => DocumentWindow.ActiveContext?.WorkflowStage == WorkflowStage.Design;
    public bool ShowsListingStageTool => DocumentWindow.ActiveContext?.WorkflowStage == WorkflowStage.Listing;

    public ICommand ConfirmStatusChangeCommand { get; private set; } = null!;
    public ICommand CancelStatusChangeCommand { get; private set; } = null!;
    public ICommand ConfirmDesignRemoveCommand { get; private set; } = null!;
    public ICommand CancelDesignRemoveCommand { get; private set; } = null!;
    public ICommand RequestArchiveItemCommand { get; private set; } = null!;
    public ICommand RestoreItemCommand { get; private set; } = null!;
    public ICommand RequestDeleteItemCommand { get; private set; } = null!;

    public string? StageMoveError { get; private set; }

    public string? StatusChangeError { get; private set; }

    private Item? ActiveItem => DocumentWindow.ActiveContext is { EntityKind: WorkspaceEntityKind.Item } context
        ? _workspaceSnapshot.Items.SingleOrDefault(candidate => candidate.Id == context.Id)
        : null;

    private static bool IsInactive(Item item) =>
        item.IsArchived || item.Status is ItemStatus.Published or ItemStatus.Rejected;

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

    public bool ShouldShowFirstStorePrompt =>
        !WorkspaceManagement.IsWorkspaceManagementOpen &&
        !WorkspaceManagement.ShouldShowNoWorkspaceState &&
        StoreManagement.ShouldShowFirstStorePrompt;

    public void OpenFromNavigation(NavigationDocumentContext navigationContext)
    {
        ArgumentNullException.ThrowIfNull(navigationContext);

        GuardActiveItemInspectorLeave(() => DocumentWindow.Open(navigationContext.Context));
    }

    public void SelectWorkflowStage(WorkflowStage stage)
    {
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
        if (item is null || item.Stage == WorkflowStage.Listing)
        {
            return;
        }

        GuardActiveItemInspectorLeave(() => Run(MoveStageAsync(item.Id, item.Stage + 1)));
    }

    private async Task MoveStageBackAsync()
    {
        var item = ActiveItem;
        if (item is null || item.Stage == WorkflowStage.Idea)
        {
            return;
        }

        GuardActiveItemInspectorLeave(() => Run(MoveStageAsync(item.Id, item.Stage - 1)));
    }

    private async Task MoveStageAsync(Guid itemId, WorkflowStage destination)
    {
        var result = await _itemManagementService.MoveItemStageAsync(
            new ItemManagementMoveStageRequest(itemId, destination)).ConfigureAwait(true);
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

            Run(SetItemStatusAsync(status, confirmed: false));
        });
    }

    private async Task SetItemStatusAsync(ItemStatus status, bool confirmed)
    {
        var item = ActiveItem;
        if (item is null)
        {
            return;
        }

        var result = await _itemManagementService.SetItemStatusAsync(
            new ItemManagementSetStatusRequest(item.Id, status, ConfirmProtectedTransition: confirmed)).ConfigureAwait(true);
        ApplyLifecycleResult(result, nameof(StatusChangeError));
    }

    private void InitializeItemCommands()
    {
        MoveStageForwardCommand = new RelayCommand(_ => Run(MoveStageForwardAsync()));
        MoveStageBackCommand = new RelayCommand(_ => Run(MoveStageBackAsync()));
        SetItemStatusCommand = new RelayCommand(parameter =>
        {
            if (parameter is ItemStatus status)
            {
                RequestStatusChange(status);
            }
        });
        ConfirmStatusChangeCommand = new RelayCommand(_ => ConfirmStatusChange());
        CancelStatusChangeCommand = new RelayCommand(_ => CancelStatusChange());
        ConfirmDesignRemoveCommand = new RelayCommand(_ => Run(ConfirmDesignRemoveAsync()));
        CancelDesignRemoveCommand = new RelayCommand(_ => IsDesignRemoveConfirmationVisible = false);
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
            Run(ReloadItemInspectorAsync(id, documentContext.Context.WorkflowStage));
        }
    }

    private async Task ReloadItemInspectorAsync(Guid itemId, WorkflowStage activeViewStage)
    {
        await ItemInspector.LoadAsync(itemId).ConfigureAwait(true);
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

    private void InitializeWorkspaces()
    {
        WorkspaceManagement.LoadAsync().GetAwaiter().GetResult();
        StoreManagement.SetActiveWorkspaceAsync(WorkspaceManagement.SelectedWorkspace?.Id).GetAwaiter().GetResult();
        GroupManagementServiceSetWorkspace(WorkspaceManagement.SelectedWorkspace?.Id);
    }

    private void InitializeStores()
    {
        StoreManagement.LoadAsync().GetAwaiter().GetResult();
        if (StoreManagement.SelectedStore is null && StoreManagement.ActiveStores.Count > 0)
        {
            StoreManagement.SelectStoreAsync(StoreManagement.ActiveStores[0]).GetAwaiter().GetResult();
            return;
        }

        RebuildNavigationContexts(StoreManagement.SelectedStore);
    }

    private void SwitchWorkspace(WorkspaceSummary? workspace)
    {
        StoreManagement.SetActiveWorkspaceAsync(workspace?.Id).GetAwaiter().GetResult();
        GroupManagementServiceSetWorkspace(workspace?.Id);
        RefreshWorkspaceSnapshot();
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
        NavigationContexts = CreateNavigationContexts(_workspaceSnapshot, selectedStore);
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

    private void RefreshWorkspaceSnapshotAndInspector()
    {
        RefreshWorkspaceSnapshot();
        RefreshActiveItemInspector();
    }

    private void RefreshActiveItemInspector()
    {
        if (DocumentWindow.ActiveContext is { EntityKind: WorkspaceEntityKind.Item, Kind: DocumentContextKind.Item, Id: var itemId }
            && ItemInspector.HasState
            && !ItemInspector.HasUnsavedChanges)
        {
            Run(ItemInspector.LoadAsync(itemId));
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
            ItemInspector.Clear();
            GroupDetails.Clear();
            RaiseLifecycleProperties();
            OnPropertyChanged(nameof(ShowStageToolHost));
            RefreshStageToolState();
            return;
        }

        if (context.EntityKind is WorkspaceEntityKind.Niche or WorkspaceEntityKind.Group or WorkspaceEntityKind.Item)
        {
            WorkspaceTree.SelectEntity(context.Id, notifySelectionChanged: false);
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
    }

    private void CoordinateGroupDetails(DocumentContext context)
    {
        if (context.EntityKind == WorkspaceEntityKind.Group
            && _workspaceSnapshot.Groups.SingleOrDefault(candidate => candidate.Id == context.Id) is { } group)
        {
            if (GroupDetails.Group?.Id != group.Id)
            {
                var nicheId = GroupHierarchy.GetEffectiveNiche(_workspaceSnapshot, group).Id;
                Run(GroupDetails.LoadAsync(group.Id, group.StoreId, nicheId));
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

        Run(ItemInspector.LoadAsync(context.Id));
    }

    private void InitializeGroupIntegration()
    {
        CreateGroupCommand = new RelayCommand(_ => Run(WorkspaceTree.BeginCreateAsync()));
        WorkspaceTree.OpenInTabRequested += (_, selection) => OpenTreeSelectionInTab(selection);
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

    private async Task OpenManageAssetsAsync(WorkspaceTreeSelection selection)
    {
        RefreshWorkspaceSnapshot();
        var storeId = ResolveContextStoreId(selection);
        if (storeId is null)
        {
            return;
        }

        await AssetsManagement.OpenForContextAsync(new AssetContextReference(selection.Kind, selection.Id)).ConfigureAwait(false);
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

    private Guid? ResolveContextStoreId(WorkspaceTreeSelection selection)
    {
        return selection.Kind switch
        {
            WorkspaceEntityKind.Store => selection.Id,
            WorkspaceEntityKind.Niche => _workspaceSnapshot.Niches.SingleOrDefault(niche => niche.Id == selection.Id)?.StoreId,
            WorkspaceEntityKind.Group => _workspaceSnapshot.Groups.SingleOrDefault(group => group.Id == selection.Id)?.StoreId,
            WorkspaceEntityKind.Item => _workspaceSnapshot.Items.SingleOrDefault(item => item.Id == selection.Id)?.StoreId,
            _ => null
        };
    }

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
        var commits = new List<Task>(2);
        if (DocumentWindow.ActiveContext is { EntityKind: WorkspaceEntityKind.Item }
            && ItemInspector.HasState
            && ItemInspector.HasUnsavedChanges)
        {
            commits.Add(ItemInspector.CommitEditsAsync());
        }

        if (DocumentWindow.ActiveContext is { EntityKind: WorkspaceEntityKind.Group }
            && GroupDetails.HasState
            && GroupDetails.HasUnsavedChanges)
        {
            commits.Add(GroupDetails.CommitEditsAsync());
        }

        if (commits.Count == 0)
        {
            proceed();
            return;
        }

        Run(CommitAndProceedAsync(commits, proceed));
    }

    private async Task CommitAndProceedAsync(IReadOnlyCollection<Task> commits, Action proceed)
    {
        await Task.WhenAll(commits).ConfigureAwait(true);
        if (DocumentWindow.ActiveContext is { EntityKind: WorkspaceEntityKind.Item }
            && ItemInspector.HasState
            && ItemInspector.HasUnsavedChanges)
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
        Run(SetItemStatusAsync(status, confirmed: true));
    }

    private void CancelStatusChange()
    {
        _pendingStatus = null;
        IsStatusConfirmationVisible = false;
        OnPropertyChanged(nameof(SelectedItemStatus));
        OnPropertyChanged(nameof(SelectedItemStatusLabel));
        OnPropertyChanged(nameof(SelectedItemStatusOption));
    }

    private async Task ConfirmDesignRemoveAsync()
    {
        if (ActiveItem is not { } item)
        {
            return;
        }

        IsDesignRemoveConfirmationVisible = false;
        if (await DesignTool.RemoveAsync(item.Id).ConfigureAwait(true))
        {
            RefreshWorkspaceSnapshot();
            RefreshActiveItemInspector();
        }
    }

    public void CommitActiveDetailsEdits()
    {
        if (ItemInspector.HasState && !ItemInspector.IsReadOnly)
        {
            Run(ItemInspector.CommitEditsAsync());
        }

        if (GroupDetails.HasState && !GroupDetails.IsReadOnly)
        {
            Run(GroupDetails.CommitEditsAsync());
        }
    }

    private void HandleItemLifecycleChanged(ItemInspectorLifecycleEventArgs args)
    {
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
            Run(ItemInspector.LoadAsync(itemId));
        }

        RefreshStageToolState();
    }

    private void RefreshStageToolState()
    {
        OnPropertyChanged(nameof(ShowsIdeaStageTool));
        OnPropertyChanged(nameof(ShowsConceptStageTool));
        OnPropertyChanged(nameof(ShowsDesignStageTool));
        OnPropertyChanged(nameof(ShowsListingStageTool));

        if (ActiveItem is not { } item)
        {
            return;
        }

        var activeStage = DocumentWindow.ActiveContext?.WorkflowStage ?? item.Stage;
        var canEdit = ItemWorkflowPolicy.CanEditStage(item, activeStage).IsAllowed;
        ListingTool.Load(item.Status, canEdit);
        if (activeStage == WorkflowStage.Design)
        {
            Run(DesignTool.LoadAsync(item.Id, canEdit));
        }
    }

    private void CloseDeletedEntityTabs(IReadOnlySet<Guid> entityIds)
    {
        foreach (var tab in DocumentWindow.Tabs.Where(tab => entityIds.Contains(tab.Context.Id)).ToArray())
        {
            DocumentWindow.CloseTab(tab);
        }
    }

    private GroupParentReference? ResolveGroupParent()
    {
        var context = DocumentWindow.ActiveContext;
        if (context?.EntityKind == WorkspaceEntityKind.Niche &&
            _workspaceSnapshot.Niches.Any(niche => niche.Id == context.Id && !niche.IsArchived))
        {
            return new GroupParentReference(WorkspaceEntityKind.Niche, context.Id);
        }

        if (context?.EntityKind == WorkspaceEntityKind.Group && ActiveGroupContext() is not null)
        {
            return new GroupParentReference(WorkspaceEntityKind.Group, context.Id);
        }

        if (context?.EntityKind == WorkspaceEntityKind.Item)
        {
            var item = _workspaceSnapshot.Items.SingleOrDefault(candidate => candidate.Id == context.Id && !candidate.IsArchived);
            if (item?.GroupId is Guid groupId && _workspaceSnapshot.Groups.SingleOrDefault(group => group.Id == groupId) is { } group && GroupHierarchy.IsEffectivelyActive(_workspaceSnapshot, group))
            {
                return new GroupParentReference(WorkspaceEntityKind.Group, groupId);
            }

            if (item?.NicheId is Guid nicheId)
            {
                return new GroupParentReference(WorkspaceEntityKind.Niche, nicheId);
            }
        }

        return StoreManagement.SelectedNiche is { IsArchived: false } niche
            ? new GroupParentReference(WorkspaceEntityKind.Niche, niche.Id)
            : null;
    }

    private TopicGroup? ActiveGroupContext()
    {
        var context = DocumentWindow.ActiveContext;
        if (context?.EntityKind != WorkspaceEntityKind.Group)
        {
            return null;
        }

        var group = _workspaceSnapshot.Groups.SingleOrDefault(candidate => candidate.Id == context.Id);
        return group is not null && GroupHierarchy.IsEffectivelyActive(_workspaceSnapshot, group) ? group : null;
    }

    private Guid? ResolveParentNicheId(GroupParentReference parent) =>
        parent.Kind == WorkspaceEntityKind.Niche
            ? parent.Id
            : _workspaceSnapshot.Groups.SingleOrDefault(group => group.Id == parent.Id) is { } group
                ? GroupHierarchy.GetEffectiveNiche(_workspaceSnapshot, group).Id
                : null;

    private void HandleGroupStructureChanged(GroupSummary? group)
    {
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

    private static void Run(Task task) => _ = task;

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

    private static IReadOnlyList<NavigationDocumentContext> CreateNavigationContexts(
        WorkspaceSnapshot snapshot,
        StoreSummary? selectedStore)
    {
        if (selectedStore is null)
        {
            return [];
        }

        var store = snapshot.Stores.SingleOrDefault(candidate => candidate.Id == selectedStore.Id)
            ?? new Store(
                selectedStore.Id,
                selectedStore.Name,
                selectedStore.Context.Description,
                selectedStore.IsArchived,
                selectedStore.CreatedAt,
                selectedStore.UpdatedAt,
                "{}");
        var contexts = new List<NavigationDocumentContext>();

        foreach (var niche in snapshot.Niches.Where(niche => niche.StoreId == store.Id && !niche.IsArchived))
        {
            contexts.Add(NewNavigationContext(
                niche.Id,
                niche.Name,
                WorkflowStage.Idea,
                DocumentContextKind.Topic,
                WorkspaceEntityKind.Niche,
                [store.Id, niche.Id],
                $"{store.Name} / {niche.Name}"));

            foreach (var group in snapshot.Groups.Where(group => group.StoreId == store.Id && group.NicheId == niche.Id && group.ParentGroupId is null))
            {
                AddGroupContexts(snapshot, contexts, group, [store.Id, niche.Id, group.Id], $"{store.Name} / {niche.Name} / {group.Name}");
            }

            foreach (var item in snapshot.Items.Where(item => item.StoreId == store.Id && item.NicheId == niche.Id && item.GroupId is null))
            {
                contexts.Add(NewItemContext(item, [store.Id, niche.Id, item.Id], $"{store.Name} / {niche.Name} / {item.Name}"));
            }
        }

        return contexts;
    }

    private static void AddGroupContexts(
        WorkspaceSnapshot snapshot,
        List<NavigationDocumentContext> contexts,
        TopicGroup group,
        IReadOnlyList<Guid> nodePath,
        string displayPath)
    {
        contexts.Add(NewNavigationContext(
            group.Id,
            group.Name,
            WorkflowStage.Idea,
            DocumentContextKind.Topic,
            WorkspaceEntityKind.Group,
            nodePath,
            displayPath));

        foreach (var childGroup in snapshot.Groups.Where(candidate => candidate.ParentGroupId == group.Id))
        {
            AddGroupContexts(snapshot, contexts, childGroup, [.. nodePath, childGroup.Id], $"{displayPath} / {childGroup.Name}");
        }

        foreach (var item in snapshot.Items.Where(item => item.GroupId == group.Id))
        {
            contexts.Add(NewItemContext(item, [.. nodePath, item.Id], $"{displayPath} / {item.Name}"));
        }
    }

    private static NavigationDocumentContext NewItemContext(
        Item item,
        IReadOnlyList<Guid> nodePath,
        string displayPath) =>
        NewNavigationContext(
            item.Id,
            item.Name,
            item.Stage,
            DocumentContextKind.Item,
            WorkspaceEntityKind.Item,
            nodePath,
            displayPath,
            item);

    private static IReadOnlyList<WorkflowStage> ReachedStages(WorkflowStage stage) =>
        WorkflowStages.Ordered.Where(reached => reached <= stage).ToArray();

    private static (bool IsInactive, string? InactiveLabel) ResolveInactive(Item item)
    {
        if (item.IsArchived)
        {
            return (true, "Archived");
        }

        return item.Status == ItemStatus.Rejected
            ? (true, "Rejected")
            : (false, null);
    }

    private static NavigationDocumentContext NewNavigationContext(
        Guid contextId,
        string title,
        WorkflowStage stage,
        DocumentContextKind kind,
        WorkspaceEntityKind entityKind,
        IReadOnlyList<Guid> nodePath,
        string displayPath,
        Item? item = null)
    {
        ActiveItemWorkflowContext? workflow = null;
        if (kind == DocumentContextKind.Item && item is not null)
        {
            var (isInactive, inactiveLabel) = ResolveInactive(item);
            workflow = new ActiveItemWorkflowContext(
                contextId,
                stage,
                ReachedStages(stage),
                isInactive,
                inactiveLabel);
        }

        return new NavigationDocumentContext(
            title,
            new DocumentContext(
                contextId,
                title,
                kind,
                new DocumentNavigationLocation(nodePath, displayPath),
                workflow,
                stage,
                DocumentWindowViewModel.GetDefaultDetailViewKey(stage),
                entityKind));
    }

    private static WorkspaceSnapshot CreateSampleWorkspace()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new Store(StoreNodeId, "North Star Studio", null, false, now, now, """{"brand":"North Star"}""");
        var niche = new Niche(NicheNodeId, store.Id, "Coffee", null, false, now, now, """{"tone":"warm"}""");
        var topic = new TopicGroup(TopicNodeId, store.Id, niche.Id, null, "Dogs and coffee", null, false, now, now, """{"humor":"gentle"}""");
        var idea = new Item(IdeaNodeId, store.Id, niche.Id, topic.Id, "Morning coffee idea", null, ItemStatus.Draft, WorkflowStage.Idea, false, now, now, """{"phrase":"But first, walkies"}""");
        var design = new Item(DesignNodeId, store.Id, niche.Id, topic.Id, "Retro mug design", null, ItemStatus.Draft, WorkflowStage.Design, false, now, now, "{}");
        var item = new Item(ListingNodeId, store.Id, niche.Id, topic.Id, "Espresso listing draft", null, ItemStatus.Draft, WorkflowStage.Listing, false, now, now, "{}");

        return new WorkspaceSnapshot(
            [store],
            [niche],
            [topic],
            [idea, design, item],
            [],
            [],
            [],
            [],
            []);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private sealed class InMemoryWorkspaceRepository(WorkspaceSnapshot snapshot) : IWorkspaceRepository
    {
        private WorkspaceSnapshot _snapshot = snapshot;

        public Task SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            _snapshot = snapshot;
            return Task.CompletedTask;
        }

        public Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_snapshot);
    }

    private sealed class InMemoryWorkspaceFileStore : IWorkspaceFileStore
    {
        private readonly HashSet<string> _existing = [];

        public string WorkspaceRoot => string.Empty;

        public Task<ManagedWorkspaceFile> ImportAsync(string sourcePath, AssetKind kind, CancellationToken cancellationToken = default)
        {
            var relativePath = $"assets/{Path.GetFileName(sourcePath)}";
            _existing.Add(relativePath);
            return Task.FromResult(new ManagedWorkspaceFile(
                Path.GetFileName(sourcePath),
                kind,
                relativePath,
                Path.Combine("workspace", relativePath),
                sourcePath));
        }

        public bool Exists(string workspaceRelativePath) => _existing.Contains(workspaceRelativePath.Replace('\\', '/'));

        public bool TryDelete(string workspaceRelativePath) => _existing.Remove(workspaceRelativePath.Replace('\\', '/'));

        public Task<Stream> OpenReadAsync(string workspaceRelativePath, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new MemoryStream());

        public Task ExportCopyAsync(string workspaceRelativePath, string destinationPath, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}

public sealed record ItemStatusOptionViewModel(ItemStatus Status, string Label);

public sealed record NavigationDocumentContext(string Label, DocumentContext Context);

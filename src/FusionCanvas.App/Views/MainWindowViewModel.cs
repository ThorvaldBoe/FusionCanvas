using FusionCanvas.App.DocumentWindow;
using FusionCanvas.App.Groups;
using FusionCanvas.App.Listings;
using FusionCanvas.App.Navigation;
using FusionCanvas.App.Stores;
using FusionCanvas.App.Workspace;
using FusionCanvas.App.Workflow;
using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace FusionCanvas.App.Views;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
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
    private readonly IListingManagementService _listingManagementService;
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

    public static MainWindowViewModel CreateForDefaultWorkspace() =>
        new(
            new WorkflowStageNavigatorViewModel(new WorkflowStageNavigatorService()),
            new DocumentWindowViewModel(),
            new ToolContextResolver(),
            new StageToolHostService(BuiltInStageTools.CreateDefaultRegistry(), new ToolContextResolver()),
            AppWorkspaceFactory.CreateDefault());

    private MainWindowViewModel(
        WorkflowStageNavigatorViewModel workflowNavigator,
        DocumentWindowViewModel documentWindow,
        IToolContextResolver toolContextResolver,
        IStageToolHostService stageToolHostService,
        AppWorkspaceRuntime runtime)
        : this(
            workflowNavigator,
            documentWindow,
            toolContextResolver,
            stageToolHostService,
            runtime.Repository,
            runtime.Snapshot,
            runtime.GroupManagement,
            runtime.ListingManagement)
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
        StoreManagement = new StoreManagementViewModel(storeManagementService, nicheManagementService);
        _workspaceRepository = treeRepository;
        _groupManagementService = new GroupManagementService(treeRepository);
        _listingManagementService = new ListingManagementService(treeRepository);
        GroupManagement = new GroupManagementViewModel(_groupManagementService);
        ListingManagement = new ListingManagementViewModel(_listingManagementService);
        _toolContextResolver = toolContextResolver;
        _stageToolHostService = stageToolHostService;
        _workspaceSnapshot = workspaceSnapshot;
        NavigationState = new NavigationTreePresentationState();
        WorkspaceTree = new WorkspaceTreeViewModel(treeRepository, _groupManagementService, workspaceSnapshot, listings: _listingManagementService);
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
        InitializeGroupIntegration();
        StoreManagement.ActiveStoreChanged += (_, store) => RebuildNavigationContexts(store);
        StoreManagement.WorkspaceStructureChanged += (_, _) => RefreshWorkspaceSnapshot();
        WorkspaceManagement.ActiveWorkspaceChanged += (_, workspace) => SwitchWorkspace(workspace);
        SubscribeToWorkspacePromptState();
        InitializeWorkspaces();
        InitializeStores();
        DocumentWindow.ActiveContextChanged += (_, context) => CoordinateActiveContext(context);
        DocumentWindow.ToolScopeChangeRequested += (_, scope) => ResolveActiveToolContext(scope);
        DocumentWindow.StageToolSelectionRequested += (_, toolId) => SelectStageTool(toolId);
    }

    public MainWindowViewModel(
        WorkflowStageNavigatorViewModel workflowNavigator,
        DocumentWindowViewModel documentWindow,
        IToolContextResolver toolContextResolver,
        IStageToolHostService stageToolHostService,
        IWorkspaceRepository workspaceRepository,
        WorkspaceSnapshot workspaceSnapshot,
        IGroupManagementService? groupManagementService = null,
        IListingManagementService? listingManagementService = null)
    {
        WorkflowNavigator = workflowNavigator;
        DocumentWindow = documentWindow;
        WorkspaceManagement = new WorkspaceManagementViewModel(new WorkspaceManagementService(workspaceRepository));
        StoreManagement = new StoreManagementViewModel(
            new StoreManagementService(workspaceRepository),
            new NicheManagementService(workspaceRepository));
        _groupManagementService = groupManagementService ?? new GroupManagementService(workspaceRepository);
        _listingManagementService = listingManagementService ?? new ListingManagementService(workspaceRepository);
        GroupManagement = new GroupManagementViewModel(_groupManagementService);
        ListingManagement = new ListingManagementViewModel(_listingManagementService);
        _toolContextResolver = toolContextResolver;
        _stageToolHostService = stageToolHostService;
        _workspaceRepository = workspaceRepository;
        _workspaceSnapshot = workspaceSnapshot;
        NavigationState = new NavigationTreePresentationState();
        WorkspaceTree = new WorkspaceTreeViewModel(workspaceRepository, _groupManagementService, workspaceSnapshot, listings: _listingManagementService);
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
        InitializeGroupIntegration();
        StoreManagement.ActiveStoreChanged += (_, store) => RebuildNavigationContexts(store);
        StoreManagement.WorkspaceStructureChanged += (_, _) => RefreshWorkspaceSnapshot();
        WorkspaceManagement.ActiveWorkspaceChanged += (_, workspace) => SwitchWorkspace(workspace);
        SubscribeToWorkspacePromptState();
        InitializeWorkspaces();
        InitializeStores();
        DocumentWindow.ActiveContextChanged += (_, context) => CoordinateActiveContext(context);
        DocumentWindow.ToolScopeChangeRequested += (_, scope) => ResolveActiveToolContext(scope);
        DocumentWindow.StageToolSelectionRequested += (_, toolId) => SelectStageTool(toolId);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public WorkflowStageNavigatorViewModel WorkflowNavigator { get; }

    public DocumentWindowViewModel DocumentWindow { get; }

    public WorkspaceManagementViewModel WorkspaceManagement { get; }

    public StoreManagementViewModel StoreManagement { get; }

    public GroupManagementViewModel GroupManagement { get; }

    public ListingManagementViewModel ListingManagement { get; }

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

    public ICommand CreateGroupCommand { get; private set; } = null!;

    public ICommand ManageGroupCommand { get; private set; } = null!;

    public bool CanCreateGroup => StoreManagement.SelectedStore is not null;

    public bool CanManageGroup => WorkspaceTree.CanManageSelection;

    public string GroupActionStatus => CanCreateGroup
        ? CanManageGroup
            ? "Create a child group, rename with F2, or edit properties."
            : "Create under the selected topic or the store's default niche."
        : "Select an active store before creating groups.";

    public bool ShouldShowFirstStorePrompt =>
        !WorkspaceManagement.IsWorkspaceManagementOpen &&
        !WorkspaceManagement.ShouldShowNoWorkspaceState &&
        StoreManagement.ShouldShowFirstStorePrompt;

    public void OpenFromNavigation(NavigationDocumentContext navigationContext)
    {
        ArgumentNullException.ThrowIfNull(navigationContext);

        DocumentWindow.Open(navigationContext.Context);
    }

    public void SelectWorkflowStage(WorkflowStage stage)
    {
        DocumentWindow.ChangeActiveWorkflowStage(stage);
        WorkflowNavigator.SelectStage(stage);
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
    }

    private void CoordinateActiveContext(DocumentContext? context)
    {
        RaiseGroupActionProperties();
        if (context is null)
        {
            WorkflowNavigator.SetActiveItem(null);
            DocumentWindow.ApplyToolContext(null);
            DocumentWindow.ApplyStageToolHostState(null);
            return;
        }

        if (context.EntityKind is WorkspaceEntityKind.Niche or WorkspaceEntityKind.Group or WorkspaceEntityKind.Listing)
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
    }

    private void InitializeGroupIntegration()
    {
        CreateGroupCommand = new RelayCommand(_ => Run(WorkspaceTree.BeginCreateAsync()));
        ManageGroupCommand = new RelayCommand(_ => Run(OpenManageGroupAsync()));
        WorkspaceTree.OpenInTabRequested += (_, selection) => OpenTreeSelectionInTab(selection);
        WorkspaceTree.SelectionChanged += (_, selection) => OpenTreeSelectionInCurrentTab(selection);
        WorkspaceTree.EditPropertiesRequested += (_, groupId) => Run(OpenManageGroupAsync(groupId));
        WorkspaceTree.EditListingPropertiesRequested += (_, listingId) => Run(OpenManageListingAsync(listingId));
        WorkspaceTree.EntitiesDeleted += (_, entityIds) => CloseDeletedEntityTabs(entityIds);
        WorkspaceTree.StructureChanged += (_, _) => RefreshWorkspaceSnapshot();
        WorkspaceTree.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(WorkspaceTreeViewModel.SelectedNode) or nameof(WorkspaceTreeViewModel.CanManageSelection))
            {
                RaiseGroupActionProperties();
            }
        };
        GroupManagement.WorkspaceStructureChanged += (_, group) => HandleGroupStructureChanged(group);
        ListingManagement.WorkspaceStructureChanged += (_, replacement) =>
        {
            RefreshWorkspaceSnapshot();
            WorkspaceTree.SelectEntity(replacement is null
                ? null
                : replacement.IsEffectivelyActive ? replacement.Id : replacement.Topic.Id);
        };
        GroupManagement.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(GroupManagementViewModel.IsOpen))
            {
                OnPropertyChanged(nameof(GroupManagement));
            }
        };
    }

    private void GroupManagementServiceSetWorkspace(Guid? workspaceId)
    {
        GroupManagement.SetActiveWorkspace(workspaceId);
        ListingManagement.SetActiveWorkspace(workspaceId);
    }

    private async Task OpenManageListingAsync(Guid listingId)
    {
        RefreshWorkspaceSnapshot();
        var listing = _workspaceSnapshot.Listings.SingleOrDefault(candidate => candidate.Id == listingId);
        if (listing is null)
        {
            return;
        }

        await ListingManagement.OpenForEditAsync(listing.StoreId, listing.Id).ConfigureAwait(false);
    }

    private async Task OpenCreateGroupAsync()
    {
        RefreshWorkspaceSnapshot();
        var parent = ResolveGroupParent();
        var store = StoreManagement.SelectedStore;
        if (parent is null || store is null)
        {
            RaiseGroupActionProperties();
            return;
        }

        var nicheId = ResolveParentNicheId(parent);
        if (nicheId is null)
        {
            return;
        }

        await GroupManagement.OpenForCreateAsync(store.Id, nicheId.Value, parent).ConfigureAwait(false);
    }

    private async Task OpenManageGroupAsync()
    {
        if (WorkspaceTree.SelectedNode is not { EntityKind: WorkspaceEntityKind.Group } selected)
        {
            RaiseGroupActionProperties();
            return;
        }

        await OpenManageGroupAsync(selected.EntityId).ConfigureAwait(false);
    }

    private async Task OpenManageGroupAsync(Guid groupId)
    {
        RefreshWorkspaceSnapshot();
        var group = _workspaceSnapshot.Groups.SingleOrDefault(candidate => candidate.Id == groupId && GroupHierarchy.IsEffectivelyActive(_workspaceSnapshot, candidate));
        if (group is null)
        {
            return;
        }

        var nicheId = GroupHierarchy.GetEffectiveNiche(_workspaceSnapshot, group).Id;
        await GroupManagement.OpenForEditAsync(group.StoreId, nicheId, group.Id).ConfigureAwait(false);
    }

    private void OpenTreeSelectionInTab(WorkspaceTreeSelection selection)
    {
        var context = NavigationContexts.SingleOrDefault(candidate =>
            candidate.Context.EntityKind == selection.Kind && candidate.Context.Id == selection.Id);
        if (context is not null)
        {
            OpenFromNavigation(context);
        }
    }

    private void OpenTreeSelectionInCurrentTab(WorkspaceTreeSelection selection)
    {
        var context = NavigationContexts.SingleOrDefault(candidate =>
            candidate.Context.EntityKind == selection.Kind && candidate.Context.Id == selection.Id);
        if (context is not null)
        {
            DocumentWindow.OpenOrReplaceActive(context.Context);
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

        if (context?.EntityKind == WorkspaceEntityKind.Listing)
        {
            var listing = _workspaceSnapshot.Listings.SingleOrDefault(candidate => candidate.Id == context.Id && !candidate.IsArchived);
            if (listing?.GroupId is Guid groupId && _workspaceSnapshot.Groups.SingleOrDefault(group => group.Id == groupId) is { } group && GroupHierarchy.IsEffectivelyActive(_workspaceSnapshot, group))
            {
                return new GroupParentReference(WorkspaceEntityKind.Group, groupId);
            }

            if (listing?.NicheId is Guid nicheId)
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

            foreach (var group in snapshot.Groups.Where(group => group.StoreId == store.Id && group.NicheId == niche.Id && group.ParentGroupId is null && !group.IsArchived))
            {
                AddGroupContexts(snapshot, contexts, group, [store.Id, niche.Id, group.Id], $"{store.Name} / {niche.Name} / {group.Name}");
            }

            foreach (var listing in snapshot.Listings.Where(listing => listing.StoreId == store.Id && listing.NicheId == niche.Id && listing.GroupId is null && !listing.IsArchived))
            {
                contexts.Add(NewListingContext(listing, [store.Id, niche.Id, listing.Id], $"{store.Name} / {niche.Name} / {listing.Name}"));
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

        foreach (var childGroup in snapshot.Groups.Where(candidate => candidate.ParentGroupId == group.Id && !candidate.IsArchived))
        {
            AddGroupContexts(snapshot, contexts, childGroup, [.. nodePath, childGroup.Id], $"{displayPath} / {childGroup.Name}");
        }

        foreach (var listing in snapshot.Listings.Where(listing => listing.GroupId == group.Id && !listing.IsArchived))
        {
            contexts.Add(NewListingContext(listing, [.. nodePath, listing.Id], $"{displayPath} / {listing.Name}"));
        }
    }

    private static NavigationDocumentContext NewListingContext(
        Listing listing,
        IReadOnlyList<Guid> nodePath,
        string displayPath) =>
        NewNavigationContext(
            listing.Id,
            listing.Name,
            WorkflowStageForListing(listing),
            DocumentContextKind.Item,
            WorkspaceEntityKind.Listing,
            nodePath,
            displayPath);

    private static WorkflowStage WorkflowStageForListing(Listing listing) =>
        listing.Status switch
        {
            ListingStatus.Ready => WorkflowStage.Design,
            ListingStatus.Active => WorkflowStage.Listing,
            _ => WorkflowStage.Idea
        };

    private static NavigationDocumentContext NewNavigationContext(
        Guid contextId,
        string title,
        WorkflowStage stage,
        DocumentContextKind kind,
        WorkspaceEntityKind entityKind,
        IReadOnlyList<Guid> nodePath,
        string displayPath)
    {
        var workflow = kind == DocumentContextKind.Item
            ? new ActiveItemWorkflowContext(contextId, stage, WorkflowStages.Ordered)
            : null;

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
        var idea = new Listing(IdeaNodeId, store.Id, niche.Id, topic.Id, "Morning coffee idea", null, ListingStatus.Draft, false, now, now, """{"phrase":"But first, walkies"}""");
        var design = new Listing(DesignNodeId, store.Id, niche.Id, topic.Id, "Retro mug design", null, ListingStatus.Ready, false, now, now, "{}");
        var listing = new Listing(ListingNodeId, store.Id, niche.Id, topic.Id, "Espresso listing draft", null, ListingStatus.Active, false, now, now, "{}");

        return new WorkspaceSnapshot(
            [store],
            [niche],
            [topic],
            [idea, design, listing],
            [],
            [],
            [],
            [],
            []);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

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
}

public sealed record NavigationDocumentContext(string Label, DocumentContext Context);

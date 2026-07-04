using FusionCanvas.App.DocumentWindow;
using FusionCanvas.App.Navigation;
using FusionCanvas.App.Workflow;
using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;
using System.Windows.Input;

namespace FusionCanvas.App.Views;

public sealed class MainWindowViewModel
{
    private static readonly Guid StoreNodeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid NicheNodeId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid TopicNodeId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private static readonly Guid IdeaNodeId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid DesignNodeId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid ListingNodeId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    private readonly IToolContextResolver _toolContextResolver;
    private readonly WorkspaceSnapshot _workspaceSnapshot;

    public MainWindowViewModel()
        : this(
            new WorkflowStageNavigatorViewModel(new WorkflowStageNavigatorService()),
            new DocumentWindowViewModel(),
            new ToolContextResolver(),
            CreateSampleWorkspace())
    {
    }

    public MainWindowViewModel(
        WorkflowStageNavigatorViewModel workflowNavigator,
        DocumentWindowViewModel documentWindow)
        : this(workflowNavigator, documentWindow, new ToolContextResolver(), CreateSampleWorkspace())
    {
    }

    public MainWindowViewModel(
        WorkflowStageNavigatorViewModel workflowNavigator,
        DocumentWindowViewModel documentWindow,
        IToolContextResolver toolContextResolver,
        WorkspaceSnapshot workspaceSnapshot)
    {
        WorkflowNavigator = workflowNavigator;
        DocumentWindow = documentWindow;
        _toolContextResolver = toolContextResolver;
        _workspaceSnapshot = workspaceSnapshot;
        NavigationState = new NavigationTreePresentationState();
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
        NavigationContexts = CreateNavigationContexts();
        DocumentWindow.ActiveContextChanged += (_, context) => CoordinateActiveContext(context);
        DocumentWindow.ToolScopeChangeRequested += (_, scope) => ResolveActiveToolContext(scope);
    }

    public WorkflowStageNavigatorViewModel WorkflowNavigator { get; }

    public DocumentWindowViewModel DocumentWindow { get; }

    public NavigationTreePresentationState NavigationState { get; }

    public IReadOnlyList<NavigationDocumentContext> NavigationContexts { get; }

    public ICommand OpenNavigationContextCommand { get; }

    public ICommand SelectWorkflowStageCommand { get; }

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

    private void CoordinateActiveContext(DocumentContext? context)
    {
        if (context is null)
        {
            WorkflowNavigator.SetActiveItem(null);
            DocumentWindow.ApplyToolContext(null);
            return;
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

    private void ResolveActiveToolContext(ToolContextScopeKind? scopeOverride = null)
    {
        var context = DocumentWindow.ActiveContext;
        if (context is null)
        {
            DocumentWindow.ApplyToolContext(null);
            return;
        }

        var selectionKind = context.Kind switch
        {
            DocumentContextKind.Item => ToolContextSelectionKind.Item,
            DocumentContextKind.Topic => ToolContextSelectionKind.Topic,
            _ => ToolContextSelectionKind.Store
        };

        var resolution = _toolContextResolver.Resolve(new ToolContextResolveRequest(
            _workspaceSnapshot,
            selectionKind,
            context.EntityKind,
            context.Id,
            context.WorkflowStage,
            scopeOverride));

        DocumentWindow.ApplyToolContext(resolution);
    }

    private static IReadOnlyList<NavigationDocumentContext> CreateNavigationContexts()
    {
        return
        [
            NewNavigationContext(
                TopicNodeId,
                "Dogs and coffee",
                WorkflowStage.Idea,
                DocumentContextKind.Topic,
                WorkspaceEntityKind.Group,
                [StoreNodeId, NicheNodeId, TopicNodeId],
                "North Star Studio / Coffee / Dogs and coffee"),
            NewNavigationContext(
                IdeaNodeId,
                "Morning coffee idea",
                WorkflowStage.Idea,
                DocumentContextKind.Item,
                WorkspaceEntityKind.Listing,
                [StoreNodeId, NicheNodeId, TopicNodeId, IdeaNodeId],
                "North Star Studio / Coffee / Morning coffee idea"),
            NewNavigationContext(
                DesignNodeId,
                "Retro mug design",
                WorkflowStage.Design,
                DocumentContextKind.Item,
                WorkspaceEntityKind.Listing,
                [StoreNodeId, NicheNodeId, TopicNodeId, DesignNodeId],
                "North Star Studio / Coffee / Retro mug design"),
            NewNavigationContext(
                ListingNodeId,
                "Espresso listing draft",
                WorkflowStage.Listing,
                DocumentContextKind.Item,
                WorkspaceEntityKind.Listing,
                [StoreNodeId, NicheNodeId, TopicNodeId, ListingNodeId],
                "North Star Studio / Coffee / Espresso listing draft")
        ];
    }

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
}

public sealed record NavigationDocumentContext(string Label, DocumentContext Context);

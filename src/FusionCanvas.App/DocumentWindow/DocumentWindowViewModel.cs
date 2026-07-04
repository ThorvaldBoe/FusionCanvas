using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace FusionCanvas.App.DocumentWindow;

public sealed class DocumentWindowViewModel : INotifyPropertyChanged
{
    private DocumentTabViewModel? _activeTab;
    private ToolContextResolution? _activeToolContext;

    public DocumentWindowViewModel()
    {
        SelectTabCommand = new RelayCommand(parameter =>
        {
            if (parameter is DocumentTabViewModel tab)
            {
                SelectTab(tab);
            }
        });
        CloseTabCommand = new RelayCommand(parameter =>
        {
            if (parameter is DocumentTabViewModel tab)
            {
                CloseTab(tab);
            }
        });
        ChangeToolScopeCommand = new RelayCommand(parameter =>
        {
            if (parameter is ToolContextScopeKind scope)
            {
                ToolScopeChangeRequested?.Invoke(this, scope);
            }
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler<DocumentContext?>? ActiveContextChanged;

    public event EventHandler<ToolContextScopeKind>? ToolScopeChangeRequested;

    public ObservableCollection<DocumentTabViewModel> Tabs { get; } = [];

    public DocumentTabViewModel? ActiveTab
    {
        get => _activeTab;
        private set
        {
            if (ReferenceEquals(_activeTab, value))
            {
                return;
            }

            if (_activeTab is not null)
            {
                _activeTab.IsActive = false;
            }

            _activeTab = value;

            if (_activeTab is not null)
            {
                _activeTab.IsActive = true;
            }

            OnActiveTabChanged();
        }
    }

    public DocumentContext? ActiveContext => ActiveTab?.Context;

    public bool HasOpenTabs => Tabs.Count > 0;

    public bool HasActiveDocument => ActiveContext is not null;

    public string EmptyStateTitle => "FusionCanvas";

    public string EmptyStateMessage => "Open an item, topic, or working view to begin.";

    public string ActiveWorkflowStageLabel => ActiveContext?.WorkflowStageLabel ?? "No workflow stage";

    public string ActiveDetailTitle => ActiveContext is null
        ? EmptyStateTitle
        : $"{ActiveContext.WorkflowStageLabel}: {ActiveContext.Title}";

    public string ActiveDetailViewKey => ActiveContext?.DetailViewKey ?? "empty";

    public string ActiveNavigationPath => ActiveContext?.NavigationLocation?.DisplayPath ?? "No navigation context";

    public ToolContextResolution? ActiveToolContext
    {
        get => _activeToolContext;
        private set
        {
            if (Equals(_activeToolContext, value))
            {
                return;
            }

            _activeToolContext = value;
            OnPropertyChanged(nameof(ActiveToolContext));
            OnPropertyChanged(nameof(ActiveToolScopeLabel));
            OnPropertyChanged(nameof(ActiveToolScopeDescription));
            OnPropertyChanged(nameof(ActiveToolUnavailableMessage));
            OnPropertyChanged(nameof(CanRunActiveTool));
        }
    }

    public string ActiveToolScopeLabel => ActiveToolContext?.ScopeSummary.Label ?? "No tool scope";

    public string ActiveToolScopeDescription => ActiveToolContext?.ScopeSummary.Description ?? "Open an item or topic to resolve tool context.";

    public string ActiveToolUnavailableMessage => ActiveToolContext?.UnavailableReason ?? string.Empty;

    public bool CanRunActiveTool => ActiveToolContext?.IsAvailable == true;

    public IReadOnlyList<ToolContextScopeKind> SupportedToolScopes { get; } =
    [
        ToolContextScopeKind.CurrentItem,
        ToolContextScopeKind.CurrentTopic,
        ToolContextScopeKind.TopicPath,
        ToolContextScopeKind.Niche,
        ToolContextScopeKind.Store,
        ToolContextScopeKind.CurrentSubtree
    ];

    public ICommand SelectTabCommand { get; }

    public ICommand CloseTabCommand { get; }

    public ICommand ChangeToolScopeCommand { get; }

    public DocumentTabViewModel Open(DocumentContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var existing = Tabs.SingleOrDefault(tab => tab.Context.Id == context.Id);

        if (existing is not null)
        {
            SelectTab(existing);
            return existing;
        }

        var tab = new DocumentTabViewModel(Guid.NewGuid(), context);
        Tabs.Add(tab);
        SelectTab(tab);
        OnPropertyChanged(nameof(HasOpenTabs));
        return tab;
    }

    public void SelectTab(DocumentTabViewModel tab)
    {
        ArgumentNullException.ThrowIfNull(tab);

        if (!Tabs.Contains(tab))
        {
            throw new ArgumentException("The selected tab is not open in this document window.", nameof(tab));
        }

        ActiveTab = tab;
    }

    public void CloseTab(DocumentTabViewModel tab)
    {
        ArgumentNullException.ThrowIfNull(tab);

        var removedIndex = Tabs.IndexOf(tab);

        if (removedIndex < 0)
        {
            return;
        }

        var wasActive = ReferenceEquals(ActiveTab, tab);
        Tabs.RemoveAt(removedIndex);
        tab.IsActive = false;

        if (wasActive)
        {
            ActiveTab = Tabs.Count switch
            {
                0 => null,
                _ => Tabs[Math.Min(removedIndex, Tabs.Count - 1)]
            };
        }

        OnPropertyChanged(nameof(HasOpenTabs));
    }

    public void ChangeActiveWorkflowStage(WorkflowStage stage)
    {
        if (ActiveTab?.Context is not DocumentContext context)
        {
            return;
        }

        var workflow = context.Workflow is null
            ? null
            : context.Workflow with { CurrentStage = stage };

        ActiveTab.ChangeWorkflowStage(context with
        {
            Workflow = workflow,
            WorkflowStage = stage,
            DetailViewKey = GetDefaultDetailViewKey(stage)
        });

        OnActiveTabChanged();
    }

    public void ApplyToolContext(ToolContextResolution? resolution) =>
        ActiveToolContext = resolution;

    public static string GetDefaultDetailViewKey(WorkflowStage stage) =>
        stage switch
        {
            WorkflowStage.Idea => "idea-stage-tool",
            WorkflowStage.Concept => "concept-stage-tool",
            WorkflowStage.Design => "design-stage-tool",
            WorkflowStage.Listing => "listing-stage-tool",
            _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unsupported workflow stage.")
        };

    private void OnActiveTabChanged()
    {
        OnPropertyChanged(nameof(ActiveTab));
        OnPropertyChanged(nameof(ActiveContext));
        OnPropertyChanged(nameof(HasActiveDocument));
        OnPropertyChanged(nameof(ActiveWorkflowStageLabel));
        OnPropertyChanged(nameof(ActiveDetailTitle));
        OnPropertyChanged(nameof(ActiveDetailViewKey));
        OnPropertyChanged(nameof(ActiveNavigationPath));
        ActiveContextChanged?.Invoke(this, ActiveContext);
    }

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

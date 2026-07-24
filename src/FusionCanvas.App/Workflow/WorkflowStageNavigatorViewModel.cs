using FusionCanvas.Domain.Workflow;
using System.ComponentModel;
using System.Windows.Input;
using FusionCanvas.Application.WorkflowNavigation;

namespace FusionCanvas.App.Workflow;

public sealed record DocumentTabWorkflowContext(Guid TabId, ActiveItemWorkflowContext? ActiveItem)
{
    public Guid TabId { get; } = TabId == Guid.Empty
        ? throw new ArgumentException("Identifier must not be empty.", nameof(TabId))
        : TabId;
}

public sealed class WorkflowStageNavigatorViewModel : INotifyPropertyChanged
{
    private readonly IWorkflowStageNavigatorService _navigatorService;
    private ActiveItemWorkflowContext? _activeItem;

    public WorkflowStageNavigatorViewModel(IWorkflowStageNavigatorService navigatorService)
    {
        _navigatorService = navigatorService;
        State = WorkflowStageNavigatorState.Empty;
        SelectStageCommand = new RelayCommand(parameter =>
        {
            if (parameter is WorkflowStage stage)
            {
                SelectStage(stage);
            }
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid? ActiveTabId { get; private set; }

    public WorkflowStageNavigatorState State { get; private set; }

    public bool HasActiveItem => State.HasActiveItem;

    public IReadOnlyList<WorkflowStageNavigatorEntry> Stages => State.Stages;

    public string? InactiveLabel => State.InactiveLabel;

    public bool ShowsInactiveState => State.IsInactive;

    public WorkflowStage? ActiveViewStage => State.ActiveViewStage;

    public ICommand SelectStageCommand { get; }

    public void SetActiveItem(ActiveItemWorkflowContext? activeItem)
    {
        _activeItem = activeItem;
        SetState(_navigatorService.Build(_activeItem));
    }

    public void SetActiveTab(DocumentTabWorkflowContext activeTab)
    {
        ArgumentNullException.ThrowIfNull(activeTab);

        ActiveTabId = activeTab.TabId;
        SetActiveItem(activeTab.ActiveItem);
    }

    public void ChangeActiveStage(WorkflowStage stage)
    {
        if (_activeItem is null)
        {
            return;
        }

        _activeItem = _activeItem with { CurrentStage = stage };
        SetState(_navigatorService.Build(_activeItem, stage));
    }

    public void SelectStage(WorkflowStage stage)
    {
        SetState(_navigatorService.Navigate(_activeItem, stage, State.ActiveViewStage));
    }

    private void SetState(WorkflowStageNavigatorState state)
    {
        State = state;
        OnPropertyChanged(nameof(State));
        OnPropertyChanged(nameof(HasActiveItem));
        OnPropertyChanged(nameof(Stages));
        OnPropertyChanged(nameof(InactiveLabel));
        OnPropertyChanged(nameof(ShowsInactiveState));
        OnPropertyChanged(nameof(ActiveViewStage));
    }

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private sealed class RelayCommand(Action<object?> execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => execute(parameter);
    }
}

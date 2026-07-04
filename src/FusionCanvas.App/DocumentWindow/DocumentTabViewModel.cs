using System.ComponentModel;

namespace FusionCanvas.App.DocumentWindow;

public sealed class DocumentTabViewModel : INotifyPropertyChanged
{
    private bool _isActive;

    public DocumentTabViewModel(Guid tabId, DocumentContext context)
    {
        TabId = tabId == Guid.Empty
            ? throw new ArgumentException("Identifier must not be empty.", nameof(tabId))
            : tabId;
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid TabId { get; }

    public DocumentContext Context { get; private set; }

    public string Title => Context.Title;

    public string WorkflowStageLabel => Context.WorkflowStageLabel;

    public string DetailViewKey => Context.DetailViewKey;

    public bool IsActive
    {
        get => _isActive;
        internal set
        {
            if (_isActive == value)
            {
                return;
            }

            _isActive = value;
            OnPropertyChanged(nameof(IsActive));
        }
    }

    public void ChangeWorkflowStage(DocumentContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Id != Context.Id)
        {
            throw new ArgumentException("Updated context must describe the same document.", nameof(context));
        }

        Context = context;
        OnPropertyChanged(nameof(Context));
        OnPropertyChanged(nameof(WorkflowStageLabel));
        OnPropertyChanged(nameof(DetailViewKey));
    }

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

using System.ComponentModel;
using System.Runtime.CompilerServices;
using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.StageTools;

public sealed class IdeaStageToolViewModel : INotifyPropertyChanged
{
    private string _idea = string.Empty;
    private string _readOnlyReason = string.Empty;
    private bool _isReadOnly;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Idea
    {
        get => _idea;
        set { _idea = value; OnPropertyChanged(); }
    }

    public bool IsReadOnly
    {
        get => _isReadOnly;
        set { _isReadOnly = value; OnPropertyChanged(); }
    }

    public string ReadOnlyReason
    {
        get => _readOnlyReason;
        set { _readOnlyReason = value; OnPropertyChanged(); }
    }

    public void LoadFromMetadata(ItemInspectorCreativeFields creative, bool canEdit)
    {
        Idea = creative.Idea ?? string.Empty;
        IsReadOnly = !canEdit;
        ReadOnlyReason = canEdit ? string.Empty : "Idea is read-only while reviewing an earlier stage or while the item is protected.";
    }

    public ItemStageSavePayload ToStagePayload() =>
        new(WorkflowStage.Idea, Idea, ConceptIdea: null, Phrase: null, GraphicDirection: null);

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class ConceptStageToolViewModel : INotifyPropertyChanged
{
    private string _conceptIdea = string.Empty;
    private string _phrase = string.Empty;
    private string _graphicDirection = string.Empty;
    private string _readOnlyReason = string.Empty;
    private bool _isReadOnly;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ConceptIdea
    {
        get => _conceptIdea;
        set { _conceptIdea = value; OnPropertyChanged(); }
    }

    public string Phrase
    {
        get => _phrase;
        set { _phrase = value; OnPropertyChanged(); }
    }

    public string GraphicDirection
    {
        get => _graphicDirection;
        set { _graphicDirection = value; OnPropertyChanged(); }
    }

    public bool IsReadOnly
    {
        get => _isReadOnly;
        set { _isReadOnly = value; OnPropertyChanged(); }
    }

    public string ReadOnlyReason
    {
        get => _readOnlyReason;
        set { _readOnlyReason = value; OnPropertyChanged(); }
    }

    public void LoadFromMetadata(ItemInspectorCreativeFields creative, bool canEdit)
    {
        ConceptIdea = creative.ConceptIdea ?? string.Empty;
        Phrase = creative.Phrase ?? string.Empty;
        GraphicDirection = creative.GraphicDirection ?? string.Empty;
        IsReadOnly = !canEdit;
        ReadOnlyReason = canEdit ? string.Empty : "Concept is read-only while reviewing an earlier stage or while the item is protected.";
    }

    public ItemStageSavePayload ToStagePayload() =>
        new(WorkflowStage.Concept, Idea: null, ConceptIdea, Phrase, GraphicDirection);

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class ListingStageToolViewModel : INotifyPropertyChanged
{
    private string _statusSummary = string.Empty;
    private string _readOnlyReason = string.Empty;
    private bool _isReadOnly;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string StatusSummary
    {
        get => _statusSummary;
        set { _statusSummary = value; OnPropertyChanged(); }
    }

    public bool IsReadOnly
    {
        get => _isReadOnly;
        set { _isReadOnly = value; OnPropertyChanged(); }
    }

    public string ReadOnlyReason
    {
        get => _readOnlyReason;
        set { _readOnlyReason = value; OnPropertyChanged(); }
    }

    public void Load(ItemStatus status, bool canEdit)
    {
        StatusSummary = $"This item is currently {ItemStatuses.GetDisplayName(status)}. Use the header status selector to manage publication lifecycle.";
        IsReadOnly = true;
        ReadOnlyReason = canEdit ? string.Empty : "Listing-stage content is protected while the item is Published or Rejected.";
    }

    public ItemStageSavePayload ToStagePayload() =>
        new(WorkflowStage.Listing, Idea: null, ConceptIdea: null, Phrase: null, GraphicDirection: null);

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

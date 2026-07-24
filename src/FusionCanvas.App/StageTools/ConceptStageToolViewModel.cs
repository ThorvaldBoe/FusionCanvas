using System.ComponentModel;
using System.Runtime.CompilerServices;
using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Items;

namespace FusionCanvas.App.StageTools;

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

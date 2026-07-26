using System.ComponentModel;
using System.Runtime.CompilerServices;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Application.Items;

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

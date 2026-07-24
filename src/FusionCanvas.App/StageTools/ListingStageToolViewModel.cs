using System.ComponentModel;
using System.Runtime.CompilerServices;
using FusionCanvas.Application.Workspace;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Items;

namespace FusionCanvas.App.StageTools;

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

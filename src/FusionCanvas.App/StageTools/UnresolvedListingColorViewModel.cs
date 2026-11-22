using System.ComponentModel;
using FusionCanvas.Application.Listings;

namespace FusionCanvas.App.StageTools;

public sealed class UnresolvedListingColorViewModel(ListingUnresolvedColorChoice choice, IReadOnlyList<ListingColorChoiceViewModel> candidates) : INotifyPropertyChanged
{
    private ListingColorChoiceViewModel? _selectedCandidate;
    public event PropertyChangedEventHandler? PropertyChanged;
    public string Label => choice.Label;
    public int SortOrder => choice.SortOrder;
    public IReadOnlyList<ListingColorChoiceViewModel> Candidates { get; } = candidates;
    public ListingColorChoiceViewModel? SelectedCandidate
    {
        get => _selectedCandidate;
        set
        {
            if (ReferenceEquals(_selectedCandidate, value)) return;
            _selectedCandidate = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedCandidate)));
            if (value is not null) CandidateSelected?.Invoke(this, value);
        }
    }
    public event Action<UnresolvedListingColorViewModel, ListingColorChoiceViewModel>? CandidateSelected;
}

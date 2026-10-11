using System.ComponentModel;
using FusionCanvas.Application.Stores.Printify;

namespace FusionCanvas.App.Stores;

public sealed class PrintifyListingImportRowViewModel(PrintifyListingImportPreview preview) : INotifyPropertyChanged
{
    private bool _isSelected;
    private Guid? _connectToItemId;
    private PrintifyListingImportCandidate? _selectedCandidate;
    private string? _importOutcome;
    private IReadOnlyList<PrintifyListingImportCandidate> _candidates = [];
    public event PropertyChangedEventHandler? PropertyChanged;
    public string ProductId => preview.Product.ProductId;
    public string Title => preview.Product.Title;
    public string Visibility => preview.Product.IsVisible ? "Published" : "Hidden";
    public bool IsLinked => preview.IsLinked;
    public bool IsSelected { get => _isSelected; set { _isSelected = value; PropertyChanged?.Invoke(this, new(nameof(IsSelected))); } }
    public IReadOnlyList<PrintifyListingImportCandidate> Candidates
    {
        get => _candidates;
        set
        {
            _candidates = value;
            PropertyChanged?.Invoke(this, new(nameof(Candidates)));
            PropertyChanged?.Invoke(this, new(nameof(HasCandidates)));
            PropertyChanged?.Invoke(this, new(nameof(ConnectableCandidates)));
            PropertyChanged?.Invoke(this, new(nameof(CandidateSummary)));
        }
    }
    public bool HasCandidates => Candidates.Count > 0;
    public IReadOnlyList<PrintifyListingImportCandidate> ConnectableCandidates => Candidates.Where(candidate => candidate.CanConnect).ToArray();
    public string CandidateSummary => string.Join("; ", Candidates.Select(candidate => $"{candidate.Name} ({candidate.Location}){(candidate.CanConnect ? string.Empty : " — already linked")}"));
    public PrintifyListingImportCandidate? SelectedCandidate
    {
        get => _selectedCandidate;
        set { _selectedCandidate = value; ConnectToItemId = value is { CanConnect: true } ? value.ItemId : null; PropertyChanged?.Invoke(this, new(nameof(SelectedCandidate))); }
    }
    public Guid? ConnectToItemId { get => _connectToItemId; set { _connectToItemId = value; PropertyChanged?.Invoke(this, new(nameof(ConnectToItemId))); PropertyChanged?.Invoke(this, new(nameof(ConnectLabel))); } }
    public string ConnectLabel => ConnectToItemId is Guid id ? Candidates.FirstOrDefault(candidate => candidate.ItemId == id)?.Name ?? "Import as new" : "Import as new";
    public string? ImportOutcome { get => _importOutcome; set { _importOutcome = value; PropertyChanged?.Invoke(this, new(nameof(ImportOutcome))); PropertyChanged?.Invoke(this, new(nameof(HasImportOutcome))); } }
    public bool HasImportOutcome => !string.IsNullOrWhiteSpace(ImportOutcome);
}

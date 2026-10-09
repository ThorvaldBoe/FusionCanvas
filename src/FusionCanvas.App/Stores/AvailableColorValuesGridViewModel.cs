using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using AvaloniaVirtualDataGrid.Core;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Domain.Catalog;

namespace FusionCanvas.App.Stores;

public sealed class AvailableColorValuesGridViewModel : INotifyPropertyChanged
{
    private static readonly IReadOnlyList<int> AvailablePageSizes = Array.AsReadOnly([10, 25, 50]);
    private static readonly IReadOnlyList<string> AvailableSortOptions = Array.AsReadOnly(
        ["Configured order", "Name A–Z", "Name Z–A"]);

    private OfferingOptionValue[] _values = [];
    private Guid? _offeringId;
    private string _searchText = string.Empty;
    private int _pageSize = 10;
    private int _currentPage = 1;
    private string _sortOption = AvailableSortOptions[0];

    public AvailableColorValuesGridViewModel()
    {
        PreviousPageCommand = new RelayCommand(_ => CurrentPage--, () => CanGoToPreviousPage);
        NextPageCommand = new RelayCommand(_ => CurrentPage++, () => CanGoToNextPage);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<int> PageSizes => AvailablePageSizes;

    public IReadOnlyList<string> SortOptions => AvailableSortOptions;

    public InMemoryDataProvider<AvailableColorGridRowViewModel> Rows { get; } = new();

    public ICommand PreviousPageCommand { get; }

    public ICommand NextPageCommand { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            var searchText = value ?? string.Empty;
            if (!SetField(ref _searchText, searchText)) return;
            ResetToFirstPage();
            RefreshRows();
        }
    }

    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (!AvailablePageSizes.Contains(value) || !SetField(ref _pageSize, value)) return;
            ResetToFirstPage();
            RefreshRows();
        }
    }

    public int CurrentPage
    {
        get => _currentPage;
        private set
        {
            var pageCount = PageCount;
            var newPage = Math.Clamp(value, 1, Math.Max(1, pageCount));
            if (!SetField(ref _currentPage, newPage)) return;
            RefreshRows();
        }
    }

    public string SortOption
    {
        get => _sortOption;
        set
        {
            if (!AvailableSortOptions.Contains(value, StringComparer.Ordinal) || !SetField(ref _sortOption, value)) return;
            ResetToFirstPage();
            RefreshRows();
        }
    }

    public bool HasColors => _values.Length > 0;

    public bool HasNoColors => !HasColors;

    public bool HasSearchResults => MatchingValues.Count > 0;

    public bool HasNoSearchResults => HasColors && !HasSearchResults;

    public int MatchingColorCount => MatchingValues.Count;

    public int PageCount => (MatchingColorCount + PageSize - 1) / PageSize;

    public bool CanGoToPreviousPage => CurrentPage > 1 && PageCount > 0;

    public bool CanGoToNextPage => CurrentPage < PageCount;

    public string PageSummary => MatchingColorCount == 0
        ? "Showing 0 colors"
        : $"Showing {((CurrentPage - 1) * PageSize) + 1}–{Math.Min(CurrentPage * PageSize, MatchingColorCount)} of {MatchingColorCount} colors";

    public void SetValues(Guid? offeringId, IEnumerable<OfferingOptionValue> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (_offeringId != offeringId)
        {
            _offeringId = offeringId;
            SetField(ref _searchText, string.Empty, nameof(SearchText));
            SetField(ref _pageSize, AvailablePageSizes[0], nameof(PageSize));
            SetField(ref _sortOption, AvailableSortOptions[0], nameof(SortOption));
            SetField(ref _currentPage, 1, nameof(CurrentPage));
        }

        _values = values.Where(value => !value.IsArchived).ToArray();
        ClampCurrentPage();
        RefreshRows();
    }

    private IReadOnlyList<OfferingOptionValue> MatchingValues
    {
        get
        {
            IEnumerable<OfferingOptionValue> values = _values;
            var searchTerm = SearchText.Trim();
            if (searchTerm.Length > 0)
            {
                values = values.Where(value => value.Value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            values = SortOption switch
            {
                "Name A–Z" => values
                    .OrderBy(value => value.Value, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(value => value.SortOrder)
                    .ThenBy(value => value.Id),
                "Name Z–A" => values
                    .OrderByDescending(value => value.Value, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(value => value.SortOrder)
                    .ThenBy(value => value.Id),
                _ => values.OrderBy(value => value.SortOrder).ThenBy(value => value.Id)
            };

            return values.ToArray();
        }
    }

    private void ClampCurrentPage()
    {
        var clampedPage = Math.Clamp(CurrentPage, 1, Math.Max(1, PageCount));
        if (clampedPage == _currentPage) return;
        _currentPage = clampedPage;
        OnPropertyChanged(nameof(CurrentPage));
    }

    private void ResetToFirstPage() => SetField(ref _currentPage, 1, nameof(CurrentPage));

    private void RefreshRows()
    {
        var matchingValues = MatchingValues;
        var pageStart = (CurrentPage - 1) * PageSize;
        var rows = matchingValues
            .Skip(pageStart)
            .Take(PageSize)
            .Select((value, index) => new AvailableColorGridRowViewModel(value, (pageStart + index) % 2 == 1))
            .ToArray();

        Rows.Reset(rows);
        OnPropertyChanged(nameof(HasColors));
        OnPropertyChanged(nameof(HasNoColors));
        OnPropertyChanged(nameof(HasSearchResults));
        OnPropertyChanged(nameof(HasNoSearchResults));
        OnPropertyChanged(nameof(MatchingColorCount));
        OnPropertyChanged(nameof(PageCount));
        OnPropertyChanged(nameof(CanGoToPreviousPage));
        OnPropertyChanged(nameof(CanGoToNextPage));
        OnPropertyChanged(nameof(PageSummary));
        (PreviousPageCommand as RelayCommand)?.NotifyCanExecuteChanged();
        (NextPageCommand as RelayCommand)?.NotifyCanExecuteChanged();
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed record AvailableColorGridRowViewModel(OfferingOptionValue Value, bool IsAlternate)
{
    public string ColorName => Value.Value;
}

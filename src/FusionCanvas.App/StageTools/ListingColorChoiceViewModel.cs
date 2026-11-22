using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using FusionCanvas.Application.Listings;

namespace FusionCanvas.App.StageTools;

public sealed class ListingColorChoiceViewModel(ListingColorChoice choice, IReadOnlyList<ListingArtworkVariantChoice> artworkVariants) : INotifyPropertyChanged
{
    private bool _isDraftSelected = choice.IsSelected;
    private ListingArtworkVariantChoice? _selectedArtworkVariant = artworkVariants.FirstOrDefault(value => value.Id == choice.ArtworkVariantId);

    public event PropertyChangedEventHandler? PropertyChanged;
    public Guid OptionValueId => choice.OptionValueId;
    public string Name => choice.Name;
    public string SwatchHex => choice.ApproximateHex ?? "#808080";
    public IBrush SwatchBrush
    {
        get
        {
            try { return new SolidColorBrush(Color.Parse(SwatchHex)); }
            catch (FormatException) { return new SolidColorBrush(Color.Parse("#808080")); }
        }
    }
    public bool IsFavorite => choice.IsFavorite;
    public bool IsSelected => choice.IsSelected;
    public bool CanSelect => ActiveVariantCount > 0;
    public int ActiveVariantCount => choice.ActiveVariantCount;
    public int OutOfStockVariantCount => choice.OutOfStockVariantCount;
    public int AvailabilityUnknownVariantCount => choice.AvailabilityUnknownVariantCount;
    public IReadOnlyList<ListingArtworkVariantChoice> ArtworkVariants { get; } = artworkVariants;
    public ListingArtworkVariantChoice? SelectedArtworkVariant
    {
        get => _selectedArtworkVariant;
        set
        {
            if (_selectedArtworkVariant?.Id == value?.Id) return;
            _selectedArtworkVariant = value;
            OnPropertyChanged();
            ArtworkVariantChanged?.Invoke(this, value);
        }
    }
    public string AvailabilityLabel => ActiveVariantCount == 0
        ? "No configured Sizes"
        : string.Join(" · ", new[]
        {
            ActiveVariantCount - OutOfStockVariantCount - AvailabilityUnknownVariantCount is var available && available > 0 ? $"{available} available" : null,
            OutOfStockVariantCount > 0 ? $"{OutOfStockVariantCount} out of stock · still selectable" : null,
            AvailabilityUnknownVariantCount > 0 ? $"{AvailabilityUnknownVariantCount} availability not verified" : null
        }.Where(value => value is not null));
    public bool IsDraftSelected
    {
        get => _isDraftSelected;
        set
        {
            if (_isDraftSelected == value) return;
            _isDraftSelected = value;
            OnPropertyChanged();
        }
    }

    public event EventHandler? PreviewRequested;
    public void RequestPreview() => PreviewRequested?.Invoke(this, EventArgs.Empty);
    public event EventHandler<ListingArtworkVariantChoice?>? ArtworkVariantChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

namespace FusionCanvas.Application.Stores.Printify;

public sealed record PrintifyListingPrintArea(string Position, IReadOnlyList<int> VariantIds, IReadOnlyList<PrintifyListingArtworkImage> Images, bool HasUnsupportedLayers);

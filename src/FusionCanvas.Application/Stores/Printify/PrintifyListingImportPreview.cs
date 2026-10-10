namespace FusionCanvas.Application.Stores.Printify;

public sealed record PrintifyListingImportPreview(PrintifyListingProductSummary Product, bool IsLinked, IReadOnlyList<PrintifyListingImportCandidate> Candidates);

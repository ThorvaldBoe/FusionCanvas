namespace FusionCanvas.Application.Stores.Printify;

public sealed record PrintifyListingImportCandidate(Guid ItemId, string Name, string? Description, string Location, bool CanConnect, double TitleSimilarity, double DescriptionSimilarity);

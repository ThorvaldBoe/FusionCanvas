namespace FusionCanvas.Application.Stores.Printify;

public sealed record PrintifyListingArtworkImage(string ImageId, string SourceUrl, string? Name, string? MediaType, string Position, IReadOnlyList<int> VariantIds);

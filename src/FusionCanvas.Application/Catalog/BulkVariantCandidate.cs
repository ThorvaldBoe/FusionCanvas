namespace FusionCanvas.Application.Catalog;

public sealed record BulkVariantCandidate(Guid SizeOptionValueId, string SizeName, bool WillCreate, string? ExclusionReason);

namespace FusionCanvas.Application.Catalog;

public sealed record ProviderMockupCandidateDescriptor(string ProviderReference, string DisplayName, int ImageWidth, int ImageHeight, IReadOnlySet<Guid> SupportedColorOptionValueIds);

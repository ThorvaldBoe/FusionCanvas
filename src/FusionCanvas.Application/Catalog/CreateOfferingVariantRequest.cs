using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Catalog;

public sealed record CreateOfferingVariantRequest(Guid OfferingId, string Name, IReadOnlyList<Guid> OptionValueIds);

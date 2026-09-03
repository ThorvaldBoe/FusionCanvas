using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Catalog;

public sealed record CreateOfferingOptionRequest(Guid OfferingId, OptionKind OptionKind, string Name, int SortOrder = 0);

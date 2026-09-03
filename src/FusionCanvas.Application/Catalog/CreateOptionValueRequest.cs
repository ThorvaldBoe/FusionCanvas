using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.Application.Catalog;

public sealed record CreateOptionValueRequest(Guid OfferingId, Guid OptionId, string Value, int SortOrder = 0);

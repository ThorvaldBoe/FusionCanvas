using FusionCanvas.Domain.Ideation;
using FusionCanvas.Domain.Items;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Application.AI;

namespace FusionCanvas.Application.Ideation;

public interface ISnowcloneCatalog
{
    Task<SnowcloneCatalogResult> GetSelectionsAsync(
        int count,
        CancellationToken cancellationToken = default);
}


namespace FusionCanvas.Application.Items;

public sealed class GuidItemIdGenerator : IItemIdGenerator
{
    public Guid NewId() => Guid.NewGuid();
}

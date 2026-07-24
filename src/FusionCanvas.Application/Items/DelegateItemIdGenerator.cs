
namespace FusionCanvas.Application.Items;

internal sealed class DelegateItemIdGenerator(Func<Guid> factory) : IItemIdGenerator
{
    public Guid NewId() => factory();
}

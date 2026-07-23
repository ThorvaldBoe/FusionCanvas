namespace FusionCanvas.Application.Workspace;

public interface IItemIdGenerator
{
    Guid NewId();
}

public sealed class GuidItemIdGenerator : IItemIdGenerator
{
    public Guid NewId() => Guid.NewGuid();
}

internal sealed class DelegateItemIdGenerator(Func<Guid> factory) : IItemIdGenerator
{
    public Guid NewId() => factory();
}

namespace FusionCanvas.Application.StageTools;

public sealed class InMemoryStageToolRegistry : IStageToolRegistry
{
    public InMemoryStageToolRegistry(IEnumerable<StageToolDescriptor> tools)
    {
        ArgumentNullException.ThrowIfNull(tools);
        Tools = tools.ToArray();
    }

    public IReadOnlyList<StageToolDescriptor> Tools { get; }
}

namespace FusionCanvas.Application.StageTools;

public interface IStageToolRegistry
{
    IReadOnlyList<StageToolDescriptor> Tools { get; }
}

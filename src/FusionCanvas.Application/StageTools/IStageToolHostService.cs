namespace FusionCanvas.Application.StageTools;

public interface IStageToolHostService
{
    StageToolHostState Build(StageToolHostRequest request);

    void SelectTool(StageToolSelectionKey key, string toolId);
}

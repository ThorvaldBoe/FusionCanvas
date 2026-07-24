using FusionCanvas.Application.ToolContexts;

namespace FusionCanvas.Application.StageTools;

public interface IStageToolCommandGateway
{
    Task ExecuteAsync(string commandName, ToolContext context, CancellationToken cancellationToken = default);
}

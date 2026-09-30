using FusionCanvas.Application.StageTools;
using FusionCanvas.App.Views;

namespace FusionCanvas.App.StageTools;

public interface IStageToolContentResolver
{
    StageToolContentViewModel? Resolve(StageToolDescriptor descriptor, MainWindowViewModel owner);
}

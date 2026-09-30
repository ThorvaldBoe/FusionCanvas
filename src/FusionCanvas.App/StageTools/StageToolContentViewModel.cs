using FusionCanvas.Application.StageTools;
using FusionCanvas.App.Views;

namespace FusionCanvas.App.StageTools;

public sealed record StageToolContentViewModel(
    MainWindowViewModel Owner,
    StageToolDescriptor Descriptor,
    StageToolContentKind Kind)
{
    public string DetailViewKey => Descriptor.DetailViewKey;
}

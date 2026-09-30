using FusionCanvas.Application.StageTools;
using FusionCanvas.App.Views;

namespace FusionCanvas.App.StageTools;

public sealed class BuiltInStageToolContentResolver : IStageToolContentResolver
{
    private static readonly IReadOnlyDictionary<string, StageToolContentKind> ContentKinds =
        new Dictionary<string, StageToolContentKind>(StringComparer.Ordinal)
        {
            ["idea-stage-tool"] = StageToolContentKind.Idea,
            ["concept-stage-tool"] = StageToolContentKind.Concept,
            ["design-stage-tool"] = StageToolContentKind.Design,
            ["listing-stage-tool"] = StageToolContentKind.Listing
        };

    public StageToolContentViewModel? Resolve(StageToolDescriptor descriptor, MainWindowViewModel owner)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(owner);

        return descriptor.SourceKind == StageToolSourceKind.BuiltIn
            && ContentKinds.TryGetValue(descriptor.DetailViewKey, out var kind)
            ? new StageToolContentViewModel(owner, descriptor, kind)
            : null;
    }
}

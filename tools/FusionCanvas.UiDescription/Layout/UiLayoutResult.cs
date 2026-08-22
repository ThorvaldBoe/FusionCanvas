using FusionCanvas.UiDescription.Diagnostics;

namespace FusionCanvas.UiDescription.Layout;

public sealed record UiLayoutResult(UiLayoutNode? Root, IReadOnlyList<UiDiagnostic> Diagnostics)
{
    public bool IsValid => Root is not null && Diagnostics.Count == 0;
}

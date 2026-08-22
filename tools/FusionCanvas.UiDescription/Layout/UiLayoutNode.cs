using FusionCanvas.UiDescription.Model;

namespace FusionCanvas.UiDescription.Layout;

public sealed record UiLayoutNode(
    UiComponent Component,
    UiRect Bounds,
    IReadOnlyList<UiLayoutNode> Children);

using System.Collections.Immutable;
using FusionCanvas.Application.AI;

namespace FusionCanvas.Application.Settings;

public sealed record WindowLayoutSettings(
    int PositionX,
    int PositionY,
    double Width,
    double Height,
    double NavigationWidth);

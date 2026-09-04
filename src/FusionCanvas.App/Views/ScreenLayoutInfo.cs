using Avalonia;
using Avalonia.Controls;
using FusionCanvas.Application.Settings;

namespace FusionCanvas.App.Views;

internal sealed record ScreenLayoutInfo(PixelRect WorkingArea, double Scaling, bool IsPrimary);

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Domain.Assets;
using FusionCanvas.Application.Assets;

namespace FusionCanvas.App.Assets;

public sealed record AssetPurposeOption(AssetKind Kind, string Label)
{
    public override string ToString() => Label;
}

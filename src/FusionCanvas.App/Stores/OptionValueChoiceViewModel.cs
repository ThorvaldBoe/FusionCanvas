using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.App.Settings;
using FusionCanvas.Application.Catalog;
using FusionCanvas.Application.Mockups;
using FusionCanvas.App.Assets;
using FusionCanvas.Domain.Catalog;
using FusionCanvas.Domain.Mockups;

namespace FusionCanvas.App.Stores;

public sealed class OptionValueChoiceViewModel(OfferingOptionValue value, string label) : SelectableCatalogRecord(label)
{
    public OfferingOptionValue Value { get; } = value;
}

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Avalonia.Media.Imaging;
using FusionCanvas.Application.DesignFiles;
using FusionCanvas.Domain.Products;

namespace FusionCanvas.App.StageTools;

internal enum PendingRemovalKind
{
    SlotImage,
    SupportingImage,
    SpecificRow
}

/// <summary>Describes a removal awaiting confirmation.</summary>

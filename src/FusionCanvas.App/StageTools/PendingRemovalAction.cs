using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Avalonia.Media.Imaging;
using FusionCanvas.Application.DesignFiles;
using FusionCanvas.Domain.Products;

namespace FusionCanvas.App.StageTools;

internal sealed record PendingRemovalAction(
    PendingRemovalKind Kind,
    Guid RowId,
    Guid DesignAreaId,
    Guid? AssetId);

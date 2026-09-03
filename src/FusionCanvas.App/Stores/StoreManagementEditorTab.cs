using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Application.Stores;
using FusionCanvas.Application.Niches;
using FusionCanvas.Application.Tags;
using FusionCanvas.Application.Products;
using FusionCanvas.Domain.Products;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Application.Catalog;
using FusionCanvas.Application.Mockups;

namespace FusionCanvas.App.Stores;

public enum StoreManagementEditorTab
{
    BasicInfo,
    Niches,
    Tags,
    Products
}

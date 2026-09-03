using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Workflow;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using FusionCanvas.Application.ToolContexts;
using FusionCanvas.Application.StageTools;

namespace FusionCanvas.App.DocumentWindow;

public sealed record StageToolOptionViewModel(string Id, string DisplayName);

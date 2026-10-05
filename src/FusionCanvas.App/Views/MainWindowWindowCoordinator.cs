using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using FusionCanvas.App.Assets;
using FusionCanvas.App.Ideation;
using FusionCanvas.App.Settings;
using FusionCanvas.App.StageTools;
using FusionCanvas.App.Stores;
using FusionCanvas.App.Workspace;

namespace FusionCanvas.App.Views;

/// <summary>
/// Coordinates the auxiliary windows owned by the main presentation surface.
/// It keeps window creation, close feedback, geometry registration, and owner
/// focus behavior out of the main window's event wiring.
/// </summary>
internal sealed class MainWindowWindowCoordinator
{
    private readonly Window _owner;
    private readonly Control _workspaceTreeControl;
    private SettingsViewModel? _settings;
    private StoreEditorWindow? _storeEditorWindow;
    private WorkspaceManagementWindow? _workspaceManagementWindow;
    private SettingsWindow? _settingsWindow;
    private TelemetryDebugWindow? _telemetryDebugWindow;
    private AssetsWindow? _assetsWindow;
    private IdeationWindow? _ideationWindow;
    private Window? _designPreviewWindow;
    private Window? _globalColorRemovalWindow;
    private Window? _mockupPreviewWindow;

    public MainWindowWindowCoordinator(Window owner, Control workspaceTreeControl)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _workspaceTreeControl = workspaceTreeControl ?? throw new ArgumentNullException(nameof(workspaceTreeControl));
    }

    public void SetSettings(SettingsViewModel? settings) => _settings = settings;

    public void SyncSettingsWindow(SettingsViewModel settings)
    {
        if (settings.IsOpen && _settingsWindow is null)
        {
            _settingsWindow = new SettingsWindow { DataContext = settings };
            if (_settings is not null)
            {
                WindowGeometryRegistrar.Register(_settingsWindow, _settings, WindowLayoutKeys.Settings, _settingsWindow.MinWidth, _settingsWindow.MinHeight);
            }

            _settingsWindow.Closed += (_, _) =>
            {
                _settingsWindow = null;
                if (settings.IsOpen)
                {
                    settings.CloseCommand.Execute(null);
                }

                if (CanFocusOwner(_owner))
                {
                    _owner.Activate();
                }
            };
            _settingsWindow.Show(_owner);
            return;
        }

        if (!settings.IsOpen && _settingsWindow is not null)
        {
            _settingsWindow.Close();
        }
    }

    public void SyncTelemetryDebugWindow(WorkspaceTelemetrySettingsViewModel telemetry)
    {
        if (telemetry.IsDebugWindowOpen && _telemetryDebugWindow is null)
        {
            var window = new TelemetryDebugWindow { DataContext = telemetry };
            _telemetryDebugWindow = window;
            window.Opened += (_, _) => PositionTelemetryWindow(window);
            window.Closed += (_, _) =>
            {
                if (ReferenceEquals(_telemetryDebugWindow, window))
                {
                    _telemetryDebugWindow = null;
                }

                telemetry.CloseDebugWindow();
            };
            window.Show(_owner);
            return;
        }

        if (!telemetry.IsDebugWindowOpen && _telemetryDebugWindow is { } openWindow)
        {
            _telemetryDebugWindow = null;
            openWindow.Close();
        }
    }

    public void SyncWorkspaceManagementWindow(WorkspaceManagementViewModel workspaceManagement)
    {
        if (workspaceManagement.IsWorkspaceManagementOpen && _workspaceManagementWindow is null)
        {
            _workspaceManagementWindow = new WorkspaceManagementWindow { DataContext = workspaceManagement };
            if (_settings is not null)
            {
                WindowGeometryRegistrar.Register(_workspaceManagementWindow, _settings, WindowLayoutKeys.WorkspaceManagement, _workspaceManagementWindow.MinWidth, _workspaceManagementWindow.MinHeight);
            }

            _workspaceManagementWindow.Closed += (_, _) =>
            {
                _workspaceManagementWindow = null;
                if (workspaceManagement.IsWorkspaceManagementOpen)
                {
                    workspaceManagement.CloseWorkspaceManagementCommand.Execute(null);
                }

                if (_settingsWindow is { } settings && settings.IsVisible)
                {
                    settings.Activate();
                }
            };
            _workspaceManagementWindow.Show((Window?)_settingsWindow ?? _owner);
            return;
        }

        if (!workspaceManagement.IsWorkspaceManagementOpen && _workspaceManagementWindow is not null)
        {
            _workspaceManagementWindow.Close();
        }
    }

    public void SyncStoreEditorWindow(StoreManagementViewModel storeManagement)
    {
        if (storeManagement.IsStoreEditorOpen && _storeEditorWindow is null)
        {
            _storeEditorWindow = new StoreEditorWindow { DataContext = storeManagement };
            if (_settings is not null)
            {
                _storeEditorWindow.GeometryStore = _settings;
                WindowGeometryRegistrar.Register(_storeEditorWindow, _settings, WindowLayoutKeys.StoreEditor, _storeEditorWindow.MinWidth, _storeEditorWindow.MinHeight);
            }

            _storeEditorWindow.Closed += (_, _) =>
            {
                _storeEditorWindow = null;
                if (storeManagement.IsStoreEditorOpen)
                {
                    storeManagement.CloseStoreEditorCommand.Execute(null);
                }
            };
            _storeEditorWindow.Show(_owner);
            return;
        }

        if (!storeManagement.IsStoreEditorOpen && _storeEditorWindow is not null)
        {
            _storeEditorWindow.Close();
        }
    }

    public void SyncAssetsWindow(AssetsViewModel assets)
    {
        if (assets.IsOpen && _assetsWindow is null)
        {
            _assetsWindow = new AssetsWindow { DataContext = assets };
            assets.FilePicker = new AvaloniaAssetFilePicker(_assetsWindow.StorageProvider);
            if (_settings is not null)
            {
                WindowGeometryRegistrar.Register(_assetsWindow, _settings, WindowLayoutKeys.Assets, _assetsWindow.MinWidth, _assetsWindow.MinHeight);
            }

            _assetsWindow.Closed += (_, _) =>
            {
                _assetsWindow = null;
                if (assets.IsOpen)
                {
                    assets.CloseCommand.Execute(null);
                }

                _workspaceTreeControl.Focus();
            };
            _assetsWindow.Show(_owner);
            return;
        }

        if (!assets.IsOpen && _assetsWindow is not null)
        {
            _assetsWindow.Close();
        }
    }

    public void SyncIdeationWindow(IdeationViewModel ideation)
    {
        if (ideation.IsOpen && _ideationWindow is null)
        {
            _ideationWindow = new IdeationWindow { DataContext = ideation };
            if (_settings is not null)
            {
                _ideationWindow.GeometryStore = _settings;
                WindowGeometryRegistrar.Register(_ideationWindow, _settings, WindowLayoutKeys.Ideation, _ideationWindow.MinWidth, _ideationWindow.MinHeight);
            }

            _ideationWindow.Closed += (_, _) =>
            {
                _ideationWindow = null;
                if (CanFocusOwner(_owner))
                {
                    _owner.Activate();
                    var contextHeader = _owner.GetVisualDescendants()
                        .OfType<DocumentContextHeader>()
                        .FirstOrDefault(header => header.IsVisible);
                    if (contextHeader is null || !contextHeader.FocusIdeationButton())
                    {
                        _workspaceTreeControl.Focus();
                    }
                }
            };
            _ = _ideationWindow.ShowDialog(_owner);
            return;
        }

        if (!ideation.IsOpen && _ideationWindow is not null)
        {
            _ideationWindow.Close();
        }
    }

    public void SyncDesignPreviewWindow(DesignStageToolViewModel designTool)
    {
        if (designTool.ShowPreviewDialog && _designPreviewWindow is null)
        {
            _designPreviewWindow = new DesignPreviewWindow { DataContext = designTool };
            if (_settings is not null)
            {
                WindowGeometryRegistrar.Register(_designPreviewWindow, _settings, WindowLayoutKeys.DesignPreview, _designPreviewWindow.MinWidth, _designPreviewWindow.MinHeight);
            }

            _designPreviewWindow.Closed += (_, _) =>
            {
                _designPreviewWindow = null;
                designTool.ClosePreviewDialog();
            };
            _designPreviewWindow.Show(_owner);
            return;
        }

        if (!designTool.ShowPreviewDialog && _designPreviewWindow is not null)
        {
            _designPreviewWindow.Close();
        }
    }

    public void SyncGlobalColorRemovalWindow(DesignStageToolViewModel designTool)
    {
        if (designTool.ShowColorRemovalDialog && designTool.ColorRemoval is { } editor && _globalColorRemovalWindow is null)
        {
            var window = new GlobalColorRemovalWindow { DataContext = editor };
            _globalColorRemovalWindow = window;
            window.Closed += (_, _) =>
            {
                if (ReferenceEquals(_globalColorRemovalWindow, window))
                {
                    _globalColorRemovalWindow = null;
                }

                designTool.CloseColorRemovalDialog();
                if (CanFocusOwner(_owner))
                {
                    _owner.Activate();
                }
            };
            window.Show(_owner);
            return;
        }

        if (!designTool.ShowColorRemovalDialog && _globalColorRemovalWindow is not null)
        {
            _globalColorRemovalWindow.Close();
        }
    }

    public void SyncMockupPreviewWindow(ListingStageToolViewModel listingTool)
    {
        if (listingTool.ShowPreviewDialog && _mockupPreviewWindow is null)
        {
            _mockupPreviewWindow = new MockupPreviewWindow { DataContext = listingTool };
            if (_settings is not null)
            {
                WindowGeometryRegistrar.Register(_mockupPreviewWindow, _settings, WindowLayoutKeys.MockupPreview, _mockupPreviewWindow.MinWidth, _mockupPreviewWindow.MinHeight);
            }

            _mockupPreviewWindow.Closed += (_, _) =>
            {
                _mockupPreviewWindow = null;
                listingTool.ClosePreviewDialog();
            };
            _mockupPreviewWindow.Show(_owner);
            return;
        }

        if (!listingTool.ShowPreviewDialog && _mockupPreviewWindow is not null)
        {
            _mockupPreviewWindow.Close();
        }
    }

    private void PositionTelemetryWindow(TelemetryDebugWindow window)
    {
        var scale = _owner.Screens.All.FirstOrDefault(screen => screen.WorkingArea.Contains(_owner.Position))?.Scaling ?? 1;
        var screen = _owner.Screens.All.FirstOrDefault(value => value.WorkingArea.Contains(_owner.Position))
            ?? _owner.Screens.Primary;
        if (screen is null)
        {
            return;
        }

        var width = (int)(window.Width * scale);
        var height = (int)(window.Height * scale);
        var x = _owner.Position.X + (int)(_owner.Width * scale) + 8;
        var y = _owner.Position.Y;
        if (x + width > screen.WorkingArea.Right)
        {
            x = _owner.Position.X - width - 8;
        }

        x = Math.Clamp(x, screen.WorkingArea.X, Math.Max(screen.WorkingArea.X, screen.WorkingArea.Right - width));
        y = Math.Clamp(y, screen.WorkingArea.Y, Math.Max(screen.WorkingArea.Y, screen.WorkingArea.Bottom - height));
        window.Position = new PixelPoint(x, y);
    }

    private static bool CanFocusOwner(Window window)
    {
        try
        {
            return window.IsVisible;
        }
        catch
        {
            return false;
        }
    }
}

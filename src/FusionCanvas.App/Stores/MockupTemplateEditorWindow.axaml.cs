using System.ComponentModel;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace FusionCanvas.App.Stores;

public partial class MockupTemplateEditorWindow : Window
{
    private CatalogSetupViewModel? _viewModel;
    private bool _allowClose;
    private bool _enlargedEditorOpen;
    private Button? _enlargedEditorButton;

    public MockupTemplateEditorWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Opened += OnOpened;
        Closing += OnClosing;
        KeyDown += OnKeyDown;
        LayoutUpdated += OnLayoutUpdated;
        AddHandler(Button.ClickEvent, OnButtonClick, RoutingStrategies.Bubble);
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_viewModel is not null) _viewModel.EnlargedPlacementEditorRequested -= OnEnlargedPlacementEditorRequested;
        Subscribe(null);
        base.OnClosed(e);
    }

    private void OnDataContextChanged(object? sender, EventArgs e) => Subscribe(DataContext as CatalogSetupViewModel);

    private void OnOpened(object? sender, EventArgs e) => Dispatcher.UIThread.Post(() =>
    {
        TemplateNameTextBox.Focus();
        TemplateNameTextBox.SelectAll();
    });

    private void Subscribe(CatalogSetupViewModel? viewModel)
    {
        if (_viewModel is not null) _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        if (_viewModel is not null) _viewModel.EnlargedPlacementEditorRequested -= OnEnlargedPlacementEditorRequested;
        _viewModel = viewModel;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _viewModel.EnlargedPlacementEditorRequested += OnEnlargedPlacementEditorRequested;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CatalogSetupViewModel.IsAddingTemplate) && _viewModel?.IsAddingTemplate == false)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _allowClose = true;
                Close();
            });
        }
        else if (e.PropertyName == nameof(CatalogSetupViewModel.IsMockupTemplateDiscardConfirmationVisible)
                 && _viewModel?.IsMockupTemplateDiscardConfirmationVisible == true)
        {
            Dispatcher.UIThread.Post(() => KeepEditingButton.Focus());
        }
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_enlargedEditorOpen) return;
        if (_allowClose || _viewModel?.IsAddingTemplate != true) return;
        e.Cancel = true;
        _viewModel.RequestCancelMockupTemplateCommand.Execute(null);
    }

    private async void OnEnlargedPlacementEditorRequested(object? sender, EventArgs e)
    {
        if (_enlargedEditorOpen || _viewModel is not { CanEdit: true, HasSelectedLocalSource: true }) return;
        _enlargedEditorOpen = true;
        _enlargedEditorButton = this.GetVisualDescendants().OfType<Button>()
            .FirstOrDefault(button => AutomationProperties.GetName(button) == "Open enlarged image placement editor");
        try
        {
            var dialog = new EnlargedMockupPlacementEditorWindow { DataContext = _viewModel };
            await dialog.ShowDialog(this);
        }
        finally
        {
            _enlargedEditorOpen = false;
            Dispatcher.UIThread.Post(() => _enlargedEditorButton?.Focus());
        }
    }

    private void OnButtonClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is Button button && AutomationProperties.GetName(button) == "Open enlarged image placement editor")
            _enlargedEditorButton = button;
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        var editor = PlacementEditor;
        if (editor.Bounds.Width <= 0 || editor.Bounds.Height <= 0)
            return;

        var imageBounds = editor.ImageDisplayBounds;
        OpenEnlargedPlacementEditorButton.Margin = new Thickness(
            0,
            0,
            Math.Max(0, editor.Bounds.Right - (editor.Bounds.X + imageBounds.Right) + 8),
            Math.Max(0, editor.Bounds.Bottom - (editor.Bounds.Y + imageBounds.Bottom) + 8));
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || _viewModel?.IsAddingTemplate != true) return;
        e.Handled = true;
        _viewModel.RequestCancelMockupTemplateCommand.Execute(null);
    }
}

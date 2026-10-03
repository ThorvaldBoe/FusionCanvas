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
    private bool _sourceFilePointerSelectionHandled;
    private Button? _enlargedEditorButton;

    public MockupTemplateEditorWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Opened += OnOpened;
        Closing += OnClosing;
        KeyDown += OnKeyDown;
        LayoutUpdated += OnLayoutUpdated;
        AddHandler(Button.ClickEvent, OnButtonClick, RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(InputElement.PointerPressedEvent, OnSourceFilePointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
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
        PlacementEditor.PreviewImageStreamFactory = viewModel is null
            ? null
            : new Func<string, Stream>(viewModel.OpenPreviewRead);
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
        if (e.Source is not Button button) return;
        if (AutomationProperties.GetName(button) == "Open enlarged image placement editor")
            _enlargedEditorButton = button;
        else if (button.Classes.Contains("mockupTableFile") && button.DataContext is LocalMockupSourceDraftViewModel source)
        {
            if (_sourceFilePointerSelectionHandled)
            {
                _sourceFilePointerSelectionHandled = false;
                return;
            }

            _viewModel?.SelectLocalSourceCommand.Execute(source);
        }
    }

    private void OnSourceRowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border row || !e.GetCurrentPoint(row).Properties.IsLeftButtonPressed || IsFromChildButton(row, e.Source))
            return;

        row.Focus();
        SelectSourceRow(row, e.KeyModifiers);
        e.Handled = true;
    }

    private void OnSourceFilePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        var point = e.GetPosition(this);
        var file = this.GetVisualDescendants().OfType<Button>()
            .Where(button => button.Classes.Contains("mockupTableFile"))
            .FirstOrDefault(button => button.TranslatePoint(new Point(0, 0), this) is { } origin &&
                new Rect(origin, button.Bounds.Size).Contains(point));
        if (file is null) return;

        var row = file.GetVisualAncestors().OfType<Border>()
            .FirstOrDefault(candidate => candidate.Classes.Contains("mockupTableRow"));
        if (row is null) return;

        row.Focus();
        SelectSourceRow(row, e.KeyModifiers);
        _sourceFilePointerSelectionHandled = true;
        Dispatcher.UIThread.Post(() => _sourceFilePointerSelectionHandled = false);
        e.Handled = true;
    }

    private void OnSourceRowKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not Border row || !ReferenceEquals(e.Source, row) || e.Key is not (Key.Enter or Key.Space))
            return;

        SelectSourceRow(row, e.KeyModifiers);
        e.Handled = true;
    }

    private void SelectSourceRow(Border row, KeyModifiers modifiers)
    {
        if (row.DataContext is LocalMockupSourceDraftViewModel source &&
            _viewModel?.SelectLocalSourceCommand.CanExecute(source) == true)
        {
            var toggle = (modifiers & KeyModifiers.Control) != 0;
            var range = (modifiers & KeyModifiers.Shift) != 0;
            if (toggle || range) _viewModel.SelectLocalSourceWithModifiers(source, toggle, range);
            else _viewModel.SelectLocalSourceCommand.Execute(source);
        }
    }

    private static bool IsFromChildButton(Border row, object? source)
    {
        if (source is not Visual visual) return false;
        return visual.GetVisualAncestors().TakeWhile(ancestor => !ReferenceEquals(ancestor, row))
            .Prepend(visual).OfType<Button>().Any();
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

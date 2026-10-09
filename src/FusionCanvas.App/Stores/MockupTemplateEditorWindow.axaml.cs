using System.ComponentModel;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaVirtualDataGrid.Controls;
using AvaloniaVirtualDataGrid.Core;

namespace FusionCanvas.App.Stores;

public partial class MockupTemplateEditorWindow : Window
{
    private CatalogSetupViewModel? _viewModel;
    private bool _allowClose;
    private bool _enlargedEditorOpen;
    private Button? _enlargedEditorButton;
    private Button? _pendingSourceArchiveButton;
    private readonly InMemoryDataProvider<LocalMockupSourceDraftViewModel> _sourceRows = new([]);

    public MockupTemplateEditorWindow()
    {
        InitializeComponent();
        MockupSourceGrid.ItemsSource = _sourceRows;
        DataContextChanged += OnDataContextChanged;
        Opened += OnOpened;
        Closing += OnClosing;
        KeyDown += OnKeyDown;
        LayoutUpdated += OnLayoutUpdated;
        AddHandler(Button.ClickEvent, OnButtonClick, RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(InputElement.PointerPressedEvent, OnSourcePointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(InputElement.PointerReleasedEvent, OnSourcePointerReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(InputElement.KeyDownEvent, OnSourceRowKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
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
        if (_viewModel is not null) _viewModel.LocalSourceDrafts.CollectionChanged -= OnSourceDraftsChanged;
        _viewModel = viewModel;
        PlacementEditor.PreviewImageStreamFactory = viewModel is null
            ? null
            : new Func<string, Stream>(viewModel.OpenPreviewRead);
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _viewModel.EnlargedPlacementEditorRequested += OnEnlargedPlacementEditorRequested;
            _viewModel.LocalSourceDrafts.CollectionChanged += OnSourceDraftsChanged;
        }

        RefreshSourceRows();
    }

    private void OnSourceDraftsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => RefreshSourceRows();

    private void RefreshSourceRows() => _sourceRows.Reset(_viewModel?.LocalSourceDrafts.ToArray() ?? []);

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
            _viewModel?.SelectLocalSourceCommand.Execute(source);
        else if (button.Classes.Contains("mockupSourceArchive") && button.CommandParameter is LocalMockupSourceDraftViewModel archivedSource)
            _viewModel?.RemoveLocalSourceCommand.Execute(archivedSource);
    }

    private void OnSourcePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        var row = FindSourceRow(e.Source);
        if (row is null)
        {
            if (e.Source is not Panel) return;
            row = FindSourceRowAtPoint(e.GetPosition(MockupSourceGrid));
        }
        if (row is null) return;
        var sourceButton = FindSourceButton(row, e.Source);
        if (sourceButton is null && e.Source is Panel)
            sourceButton = FindSourceButtonAtPoint(row, e.GetPosition(MockupSourceGrid));
        if (sourceButton is not null)
        {
            if (sourceButton.Classes.Contains("mockupSourceArchive"))
            {
                _pendingSourceArchiveButton = sourceButton;
                e.Pointer.Capture(sourceButton);
                e.Handled = true;
            }
            return;
        }

        row.Focus();
        SelectSourceRow(row, e.KeyModifiers);
        e.Handled = true;
    }

    private void OnSourcePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var button = _pendingSourceArchiveButton;
        _pendingSourceArchiveButton = null;
        if (button is null) return;

        e.Pointer.Capture(null);
        var position = e.GetPosition(button);
        if (position.X >= 0 && position.Y >= 0 && position.X <= button.Bounds.Width && position.Y <= button.Bounds.Height
            && button.CommandParameter is LocalMockupSourceDraftViewModel source)
        {
            _viewModel?.RemoveLocalSourceCommand.Execute(source);
        }

        e.Handled = true;
    }

    private void OnSourceRowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Enter or Key.Space))
            return;

        var row = FindSourceRow(e.Source);
        if (row is null || IsFromChildButton(row, e.Source)) return;
        SelectSourceRow(row, e.KeyModifiers);
        e.Handled = true;
    }

    private void SelectSourceRow(VirtualDataRow row, KeyModifiers modifiers)
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

    private static VirtualDataRow? FindSourceRow(object? source) => source is Visual visual
        ? visual.GetVisualAncestors().Prepend(visual).OfType<VirtualDataRow>().FirstOrDefault()
        : null;

    private VirtualDataRow? FindSourceRowAtPoint(Point position) => MockupSourceGrid.GetVisualDescendants()
        .OfType<VirtualDataRow>()
        .FirstOrDefault(row => row.TranslatePoint(new Point(0, 0), MockupSourceGrid) is { } origin
            && new Rect(origin, row.Bounds.Size).Contains(position));

    private static Button? FindSourceButton(VirtualDataRow row, object? source) => source is Visual visual
        ? visual.GetVisualAncestors().Prepend(visual).TakeWhile(candidate => !ReferenceEquals(candidate, row)).OfType<Button>().FirstOrDefault()
        : source as Button;

    private Button? FindSourceButtonAtPoint(VirtualDataRow row, Point gridPosition) => row.GetVisualDescendants()
        .OfType<Button>()
        .FirstOrDefault(button => MockupSourceGrid.TranslatePoint(gridPosition, button) is { } position
            && new Rect(button.Bounds.Size).Contains(position));

    private static bool IsFromChildButton(VirtualDataRow row, object? source)
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

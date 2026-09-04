using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using FusionCanvas.Domain.Catalog;

namespace FusionCanvas.App.Stores;

public partial class OptionValueManagementWindow : Window
{
    private PointerPressedEventArgs? _gripPointerArgs;
    private Avalonia.Point _gripStart;

    public OptionValueManagementWindow()
    {
        InitializeComponent();
        Opened += (_, _) => Dispatcher.UIThread.Post(() => OptionValueDoneButton.Focus(), DispatcherPriority.Input);
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is CatalogSetupViewModel catalog)
        {
            catalog.OptionValueEditorFocusRequested += OnEditorFocusRequested;
            catalog.PropertyChanged += OnCatalogPropertyChanged;
        }
    }

    private void OnEditorFocusRequested(object? sender, EventArgs e) => Dispatcher.UIThread.Post(() =>
    {
        if (DataContext is CatalogSetupViewModel { IsAddingOptionValue: true })
        {
            OptionValueTextBox.Focus();
            OptionValueTextBox.SelectAll();
        }
        else if (DataContext is CatalogSetupViewModel { IsEditingOptionValue: true })
        {
            OptionValueEditTextBox.Focus();
            OptionValueEditTextBox.SelectAll();
        }
        else
        {
            OptionValueDoneButton.Focus();
        }
    });

    private void OnCatalogPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CatalogSetupViewModel.IsManagingOptionValues)
            && DataContext is CatalogSetupViewModel { IsManagingOptionValues: false }
            && IsVisible)
        {
            Close();
        }
    }

    private void OnDoneClick(object? sender, RoutedEventArgs e) => Close();

    private void OnGripPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control control && control.DataContext is OfferingOptionValue)
        {
            _gripPointerArgs = e;
            _gripStart = e.GetPosition(control);
        }
    }

    private async void OnGripPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_gripPointerArgs is null || sender is not Control control || control.DataContext is not OfferingOptionValue value) return;
        var point = e.GetCurrentPoint(control);
        if (!point.Properties.IsLeftButtonPressed || Math.Abs(point.Position.Y - _gripStart.Y) < 6) return;
        var transfer = new DataTransfer();
        transfer.Add(DataTransferItem.CreateText($"FusionCanvasOptionValue|{value.Id}"));
        var pressed = _gripPointerArgs;
        _gripPointerArgs = null;
        await DragDrop.DoDragDropAsync(pressed, transfer, DragDropEffects.Move);
    }

    private void OnValueDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Items.Any(item => item.TryGetText()?.StartsWith("FusionCanvasOptionValue|", StringComparison.Ordinal) == true)
            ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private async void OnValueDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not CatalogSetupViewModel catalog || sender is not Control { DataContext: OfferingOptionValue target }) return;
        var text = e.DataTransfer.Items.Select(item => item.TryGetText()).FirstOrDefault(value => value?.StartsWith("FusionCanvasOptionValue|", StringComparison.Ordinal) == true);
        if (text is not null && Guid.TryParse(text["FusionCanvasOptionValue|".Length..], out var sourceId))
        {
            var source = catalog.AvailableValues.FirstOrDefault(value => value.Id == sourceId);
            if (source is not null) await catalog.ReorderOptionValuesAsync(source, target);
        }
        e.Handled = true;
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close();

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is CatalogSetupViewModel catalog)
        {
            catalog.OptionValueEditorFocusRequested -= OnEditorFocusRequested;
            catalog.PropertyChanged -= OnCatalogPropertyChanged;
        }
        base.OnClosed(e);
    }
}

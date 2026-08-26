using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace FusionCanvas.App.Stores;

public partial class OptionValueManagementWindow : Window
{
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

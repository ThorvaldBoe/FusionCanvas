using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace FusionCanvas.App.Stores;

public partial class AddVariantWindow : Window
{
    public AddVariantWindow()
    {
        InitializeComponent();
        Opened += (_, _) => Dispatcher.UIThread.Post(() => VariantNameTextBox.Focus(), DispatcherPriority.Input);
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is CatalogSetupViewModel catalog)
        {
            catalog.PropertyChanged += OnCatalogPropertyChanged;
        }
    }

    private void OnCatalogPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CatalogSetupViewModel.IsAddingVariant)
            && DataContext is CatalogSetupViewModel { IsAddingVariant: false }
            && IsVisible)
        {
            Close();
        }
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
            catalog.PropertyChanged -= OnCatalogPropertyChanged;
        }
        base.OnClosed(e);
    }
}

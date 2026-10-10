using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FusionCanvas.App.Stores;

public partial class PrintifyListingImportWindow : Window
{
    public PrintifyListingImportWindow() => InitializeComponent();
    public event EventHandler? OpenStoreSetupRequested;

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (DataContext is PrintifyListingImportViewModel viewModel && viewModel.IsBusy) viewModel.CancelCommand.Execute(null);
        base.OnClosing(e);
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (DataContext is PrintifyListingImportViewModel { Products.Count: 0 } viewModel) await viewModel.LoadAsync();
    }

    private void OnClose(object? sender, RoutedEventArgs e) => Close();
    private void OnOpenStoreSetup(object? sender, RoutedEventArgs e)
    {
        OpenStoreSetupRequested?.Invoke(this, EventArgs.Empty);
        Close();
    }
}

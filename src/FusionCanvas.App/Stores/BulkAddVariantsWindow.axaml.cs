using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AvaloniaVirtualDataGrid.Core;
using FusionCanvas.Application.Catalog;

namespace FusionCanvas.App.Stores;

public partial class BulkAddVariantsWindow : Window
{
    private readonly InMemoryDataProvider<BulkVariantCandidate> _previewRows = new([]);
    private CatalogSetupViewModel? _catalog;

    public BulkAddVariantsWindow()
    {
        InitializeComponent();
        BulkPreviewGrid.ItemsSource = _previewRows;
        Opened += (_, _) => Dispatcher.UIThread.Post(() => BulkColorComboBox.Focus(), DispatcherPriority.Input);
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        Subscribe(DataContext as CatalogSetupViewModel);
    }

    private void Subscribe(CatalogSetupViewModel? catalog)
    {
        if (_catalog is not null)
        {
            _catalog.PropertyChanged -= OnCatalogPropertyChanged;
            _catalog.BulkPreviewCandidates.CollectionChanged -= OnPreviewCandidatesChanged;
        }

        _catalog = catalog;
        if (_catalog is not null)
        {
            _catalog.PropertyChanged += OnCatalogPropertyChanged;
            _catalog.BulkPreviewCandidates.CollectionChanged += OnPreviewCandidatesChanged;
        }

        RefreshPreview();
    }

    private void OnPreviewCandidatesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshPreview();
    }

    private void RefreshPreview()
    {
        _previewRows.Reset(_catalog?.BulkPreviewCandidates.ToArray() ?? []);
    }

    private void OnCatalogPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CatalogSetupViewModel.IsAddingBulkVariants)
            && DataContext is CatalogSetupViewModel { IsAddingBulkVariants: false }
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
        DataContextChanged -= OnDataContextChanged;
        Subscribe(null);
        base.OnClosed(e);
    }
}

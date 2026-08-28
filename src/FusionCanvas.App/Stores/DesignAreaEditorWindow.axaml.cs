using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

namespace FusionCanvas.App.Stores;

public partial class DesignAreaEditorWindow : Window
{
    private CatalogSetupViewModel? _catalog;
    private bool _allowClose;

    public DesignAreaEditorWindow()
    {
        InitializeComponent();
        Opened += (_, _) => Dispatcher.UIThread.Post(() => DesignAreaNameTextBox.Focus(), DispatcherPriority.Input);
        DataContextChanged += OnDataContextChanged;
        Closing += OnClosing;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_catalog is not null) _catalog.PropertyChanged -= OnCatalogPropertyChanged;
        _catalog = DataContext as CatalogSetupViewModel;
        if (_catalog is not null) _catalog.PropertyChanged += OnCatalogPropertyChanged;
    }

    private void OnCatalogPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CatalogSetupViewModel.IsAddingPlaceholder)
            && _catalog is { IsAddingPlaceholder: false })
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (!IsVisible) return;
                _allowClose = true;
                Close();
            });
        }
        else if (e.PropertyName == nameof(CatalogSetupViewModel.IsDesignAreaDiscardConfirmationVisible)
                 && _catalog is { IsDesignAreaDiscardConfirmationVisible: true })
        {
            Dispatcher.UIThread.Post(() => KeepEditingButton.Focus(), DispatcherPriority.Input);
        }
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_allowClose || _catalog is not { IsAddingPlaceholder: true }) return;
        e.Cancel = true;
        _catalog.RequestCancelDesignAreaCommand.Execute(null);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _catalog is { IsAddingPlaceholder: true })
        {
            _catalog.RequestCancelDesignAreaCommand.Execute(null);
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_catalog is not null) _catalog.PropertyChanged -= OnCatalogPropertyChanged;
        base.OnClosed(e);
    }
}


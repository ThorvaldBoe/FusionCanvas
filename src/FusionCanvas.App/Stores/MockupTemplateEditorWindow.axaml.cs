using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace FusionCanvas.App.Stores;

public partial class MockupTemplateEditorWindow : Window
{
    private CatalogSetupViewModel? _viewModel;
    private bool _allowClose;

    public MockupTemplateEditorWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Opened += OnOpened;
        Closing += OnClosing;
        KeyDown += OnKeyDown;
    }

    protected override void OnClosed(EventArgs e)
    {
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
        _viewModel = viewModel;
        if (_viewModel is not null) _viewModel.PropertyChanged += OnViewModelPropertyChanged;
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
        if (_allowClose || _viewModel?.IsAddingTemplate != true) return;
        e.Cancel = true;
        _viewModel.RequestCancelMockupTemplateCommand.Execute(null);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || _viewModel?.IsAddingTemplate != true) return;
        e.Handled = true;
        _viewModel.RequestCancelMockupTemplateCommand.Execute(null);
    }
}

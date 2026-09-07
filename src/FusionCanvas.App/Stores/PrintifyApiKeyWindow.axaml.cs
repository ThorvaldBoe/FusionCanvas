using Avalonia.Controls;
using Avalonia.Input;

namespace FusionCanvas.App.Stores;

public partial class PrintifyApiKeyWindow : Window
{
    private PrintifyApiKeyViewModel? _model;
    public PrintifyApiKeyWindow()
    {
        InitializeComponent();
        Opened += (_, _) => KeyInput.Focus();
        DataContextChanged += (_, _) =>
        {
            if (_model is not null) _model.CloseRequested -= CloseFromModel;
            _model = DataContext as PrintifyApiKeyViewModel;
            if (_model is not null) _model.CloseRequested += CloseFromModel;
        };
        Closing += (_, e) =>
        {
            if (_model is { CanClose: false })
            {
                e.Cancel = !_model.RequestClose(requestWindowClose: false);
            }
        };
        Closed += (_, _) => { if (_model is not null) _model.CloseRequested -= CloseFromModel; };
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) { _model?.RequestClose(); e.Handled = true; }
        };
    }

    private void CloseFromModel(object? sender, EventArgs e) => Close();
}

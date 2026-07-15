using Avalonia.Controls;
using FusionCanvas.App.Stores;

namespace FusionCanvas.App.Views;

public partial class MainWindow : Window
{
    private StoreEditorWindow? _storeEditorWindow;

    public MainWindow()
    {
        InitializeComponent();
        var viewModel = MainWindowViewModel.CreateForDefaultWorkspace();
        viewModel.StoreManagement.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(StoreManagementViewModel.IsStoreEditorOpen))
            {
                SyncStoreEditorWindow(viewModel.StoreManagement);
            }
        };
        DataContext = viewModel;
        SyncStoreEditorWindow(viewModel.StoreManagement);
    }

    private void SyncStoreEditorWindow(StoreManagementViewModel storeManagement)
    {
        if (storeManagement.IsStoreEditorOpen && _storeEditorWindow is null)
        {
            _storeEditorWindow = new StoreEditorWindow { DataContext = storeManagement };
            _storeEditorWindow.Closed += (_, _) =>
            {
                _storeEditorWindow = null;
                if (storeManagement.IsStoreEditorOpen)
                {
                    storeManagement.CloseStoreEditorCommand.Execute(null);
                }
            };
            _storeEditorWindow.Show(this);
            return;
        }

        if (!storeManagement.IsStoreEditorOpen && _storeEditorWindow is not null)
        {
            _storeEditorWindow.Close();
        }
    }
}

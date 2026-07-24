using Avalonia.Controls;
using Avalonia.VisualTree;
using FusionCanvas.App.Tests.TestSupport;
using FusionCanvas.App.Views;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Tests;

internal sealed class MainWindowFixture : IDisposable
{
    public MainWindow Window { get; }
    public MainWindowViewModel ViewModel { get; }

    public MainWindowFixture(double width = 1180, double height = 760)
    {
        ViewModel = MainWindowViewModelFactory.CreateSample();
        Window = new MainWindow { DataContext = ViewModel };
        Window.Show();
        Window.Width = width;
        Window.Height = height;
        PumpLayout();
    }

    public void PumpLayout()
    {
        Window.UpdateLayout();
        Window.UpdateLayout();
    }

    public T FindControl<T>(Func<T, bool> predicate) where T : Control =>
        Window.GetVisualDescendants().OfType<T>().First(predicate);

    public T? FindControlOrDefault<T>(Func<T, bool> predicate) where T : Control =>
        Window.GetVisualDescendants().OfType<T>().FirstOrDefault(predicate);

    public NavigationDocumentContext FirstItemContext() =>
        ViewModel.NavigationContexts.First(c => c.Context.EntityKind == WorkspaceEntityKind.Item);

    public NavigationDocumentContext FirstGroupContext() =>
        ViewModel.NavigationContexts.First(c => c.Context.EntityKind == WorkspaceEntityKind.Group);

    public NavigationDocumentContext FirstNicheContext() =>
        ViewModel.NavigationContexts.First(c => c.Context.EntityKind == WorkspaceEntityKind.Niche);

    public void Dispose()
    {
        try { Window.Close(); } catch { }
        Window.DataContext = null;
    }
}

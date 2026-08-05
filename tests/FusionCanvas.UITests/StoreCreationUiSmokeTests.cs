using FusionCanvas.Integration.Persistence;
using FusionCanvas.UITests.Infrastructure;
using FusionCanvas.UITests.Pages;

namespace FusionCanvas.UITests;

public sealed class StoreCreationUiSmokeTests
{
    [Trait("Suite", "UiSmoke")]
    [Fact]
    public async Task StoreCreation_UsesKeyboardInputAndPersistsToTheIsolatedWorkspace()
    {
        var storeName = $"UI Smoke {Guid.NewGuid():N}";
        using var session = UiApplicationSession.Start();

        new StoreEditorPage(session.Driver).CreateStore(storeName);

        var repository = new SqliteWorkspaceRepository(session.TestRoot.DatabasePath, useConnectionPooling: false);
        var snapshot = await repository.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Contains(snapshot.Stores, store => store.Name == storeName);
        Assert.True(session.TestRoot.Contains(session.TestRoot.DatabasePath));
        Assert.True(session.TestRoot.Contains(session.TestRoot.WorkspaceRootPath));
        Assert.True(session.TestRoot.Contains(session.TestRoot.SettingsPath));
    }
}

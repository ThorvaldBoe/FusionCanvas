using FusionCanvas.App.Workspace;
using FusionCanvas.Application.AI;
using FusionCanvas.Application.Ideation;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workspace;
using FusionCanvas.Domain.Ideation;
using FusionCanvas.Application.Settings;
using FusionCanvas.App.DocumentWindow;
using FusionCanvas.App.Views;
using FusionCanvas.App.Workflow;
using FusionCanvas.Application.StageTools;
using FusionCanvas.Application.ToolContexts;
using FusionCanvas.Application.WorkflowNavigation;
using FusionCanvas.Application.Workspaces;

namespace FusionCanvas.App.Tests.Ideation;

public sealed class AppWorkspaceIdeationCompositionTests
{
    [Fact]
    public async Task FactoryUsesConfiguredAiServiceAndPersistedCatalogForProductionIdeation()
    {
        using var directory = new TemporaryDirectory();
        var ai = new StubAi();
        var runtime = AppWorkspaceFactory.Create(directory.DatabasePath, ai);
        Assert.False(runtime.IdeationAccess.GetAvailability().IsAvailable);
        await runtime.IdeationAccess.RefreshAsync(TestContext.Current.CancellationToken);
        var now = DateTimeOffset.UtcNow;
        var store = new Store(Guid.NewGuid(), "Dog Shop", "Funny shirts", false, now, now, "{}");
        var niche = new Niche(Guid.NewGuid(), store.Id, "Dogs", "Dog owners", false, now, now, "{}");
        var snapshot = new WorkspaceSnapshot([store], [niche], [], [], [], [], [], [], []);
        await runtime.Repository.SaveAsync(snapshot, TestContext.Current.CancellationToken);
        var scope = runtime.Ideation.ResolveScope(snapshot, WorkspaceEntityKind.Niche, niche.Id).Scope!;

        var result = await runtime.Ideation.GenerateAsync(
            new(scope, IdeationMode.Basic, "Grumpy", 1),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.IsType<ConfiguredIdeationAccessStatus>(runtime.IdeationAccess);
        Assert.True(runtime.IdeationAccess.GetAvailability().IsAvailable);
        Assert.True(result.Succeeded);
        Assert.Equal("A concise pug idea.", Assert.Single(result.Candidates).Text);
        Assert.Equal(1, ai.GenerationCalls);
    }

    [Fact]
    public void FactoryDoesNotSynchronouslyQueryAiAvailabilityBeforeTheWindowCanOpen()
    {
        using var directory = new TemporaryDirectory();
        var ai = new BlockingAvailabilityAi();

        var runtime = AppWorkspaceFactory.Create(directory.DatabasePath, ai);

        Assert.NotNull(runtime);
        Assert.Equal(0, ai.AvailabilityCalls);
        Assert.Contains(
            "Checking",
            runtime.IdeationAccess.GetAvailability().UnavailableReason,
            StringComparison.Ordinal);
    }

    [Fact]
    public void FactoryCompletesUnderANonPumpingUiSynchronizationContext()
    {
        using var directory = new TemporaryDirectory();
        AppWorkspaceRuntime? runtime = null;
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            SynchronizationContext.SetSynchronizationContext(new NonPumpingSynchronizationContext());
            try
            {
                runtime = AppWorkspaceFactory.Create(directory.DatabasePath, new StubAi());
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        })
        {
            IsBackground = true
        };

        thread.Start();

        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "Workspace startup deadlocked on the UI synchronization context.");
        Assert.Null(failure);
        Assert.NotNull(runtime);
    }

    [Fact]
    public void AppServicesLoadCompletesUnderANonPumpingUiSynchronizationContext()
    {
        AppServices? services = null;
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            SynchronizationContext.SetSynchronizationContext(new NonPumpingSynchronizationContext());
            try
            {
                services = AppServicesFactory.Create(new YieldingSettingsStore());
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        })
        {
            IsBackground = true
        };

        thread.Start();

        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "Application settings startup deadlocked on the UI synchronization context.");
        Assert.Null(failure);
        Assert.NotNull(services);
        services.Dispose();
    }

    [Fact]
    public void MainViewModelInitializationCompletesUnderANonPumpingUiSynchronizationContext()
    {
        MainWindowViewModel? viewModel = null;
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            SynchronizationContext.SetSynchronizationContext(new NonPumpingSynchronizationContext());
            try
            {
                var contexts = new ToolContextResolver();
                viewModel = new MainWindowViewModel(
                    new WorkflowStageNavigatorViewModel(new WorkflowStageNavigatorService()),
                    new DocumentWindowViewModel(),
                    contexts,
                    new StageToolHostService(BuiltInStageTools.CreateDefaultRegistry(), contexts),
                    new YieldingWorkspaceRepository(),
                    WorkspaceSnapshot.Empty);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        })
        {
            IsBackground = true
        };

        thread.Start();

        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "Main-window view-model startup deadlocked on the UI synchronization context.");
        Assert.Null(failure);
        Assert.NotNull(viewModel);
    }

    private sealed class StubAi : IAiTextGenerationService
    {
        public int GenerationCalls { get; private set; }

        public Task<AiAvailabilityResult> GetAvailabilityAsync(
            AiRequestPurpose purpose,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(AiAvailabilityResult.Ready);

        public Task<AiTextResult> GenerateAsync(
            AiTextRequest request,
            CancellationToken cancellationToken = default)
        {
            GenerationCalls++;
            Assert.Equal(AiRequestPurpose.Ideation, request.Purpose);
            return Task.FromResult(AiTextResult.Success("A concise pug idea.", "test/model"));
        }
    }

    private sealed class BlockingAvailabilityAi : IAiTextGenerationService
    {
        public int AvailabilityCalls { get; private set; }

        public Task<AiAvailabilityResult> GetAvailabilityAsync(
            AiRequestPurpose purpose,
            CancellationToken cancellationToken = default)
        {
            AvailabilityCalls++;
            return new TaskCompletionSource<AiAvailabilityResult>(
                TaskCreationOptions.RunContinuationsAsynchronously).Task;
        }

        public Task<AiTextResult> GenerateAsync(
            AiTextRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class NonPumpingSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback callback, object? state)
        {
        }
    }

    private sealed class YieldingSettingsStore : IApplicationSettingsStore
    {
        public async Task<ApplicationSettingsLoadResult> LoadAsync(
            CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return ApplicationSettingsLoadResult.Defaulted();
        }

        public Task<ApplicationSettingsSaveResult> SaveAsync(
            ApplicationSettings settings,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ApplicationSettingsSaveResult.Success);
    }

    private sealed class YieldingWorkspaceRepository : IWorkspaceRepository
    {
        public async Task<WorkspaceSnapshot> LoadAsync(
            CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return WorkspaceSnapshot.Empty;
        }

        public Task SaveAsync(
            WorkspaceSnapshot snapshot,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory();

        public string DatabasePath => Path.Combine(_directory.FullName, "workspace.db");

        public void Dispose()
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            _directory.Delete(recursive: true);
        }
    }
}

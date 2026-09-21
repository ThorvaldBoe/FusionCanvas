using FusionCanvas.App.Stores;
using FusionCanvas.App.Tests.TestSupport;
using FusionCanvas.Application.AI;
using FusionCanvas.Application.Niches;
using FusionCanvas.Application.Stores;
using FusionCanvas.Application.Workspaces;
using FusionCanvas.Domain.Niches;
using FusionCanvas.Domain.Stores;
using FusionCanvas.Domain.Workspace;

namespace FusionCanvas.App.Tests;

public sealed class NichePopulationViewModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Populate_IsEnabledOnlyForNamedActiveNicheWhenGeneralAiIsReady()
    {
        var active = NewNiche("Coffee", isArchived: false);
        var archived = NewNiche("Archived", isArchived: true);
        var fake = new FakeNichePopulationService();
        var (viewModel, _) = await CreateViewModel([active, archived], fake);

        viewModel.SelectNicheForEditing(viewModel.ActiveNiches.Single());
        await viewModel.RefreshNichePopulationAvailabilityAsync(TestContext.Current.CancellationToken);
        Assert.True(viewModel.CanPopulateNiche);

        viewModel.NicheName = " ";
        Assert.False(viewModel.CanPopulateNiche);
        viewModel.PopulateNicheCommand.Execute(null);
        Assert.Equal(0, fake.PopulateCalls);

        viewModel.SelectNicheForEditing(viewModel.ArchivedNiches.Single());
        viewModel.ConfirmDiscardChangesCommand.Execute(null);
        Assert.False(viewModel.CanPopulateNiche);
        viewModel.NicheName = "Archived niche";
        viewModel.PopulateNicheCommand.Execute(null);
        Assert.Equal(0, fake.PopulateCalls);
    }

    [Fact]
    public async Task Populate_AppliesBlankSuggestionsToDraftOnlyAndSavePersistsReviewedValues()
    {
        var niche = NewNiche("Coffee", isArchived: false, new NicheContext(
            Audience: "Coffee fans",
            Risks: "Keep claims grounded",
            ResearchNotes: "Manual research"));
        var fake = new FakeNichePopulationService
        {
            Result = NichePopulationResult.Success(new Dictionary<NichePopulationField, string>
            {
                [NichePopulationField.Description] = "A cozy coffee niche",
                [NichePopulationField.Audience] = "Should not replace",
                [NichePopulationField.VisualStyleGuidance] = "Bold, legible mug graphic",
                [NichePopulationField.Notes] = "Review before launch"
            })
        };
        var (viewModel, repository) = await CreateViewModel([niche], fake);
        viewModel.SelectNicheForEditing(viewModel.ActiveNiches.Single());
        await viewModel.RefreshNichePopulationAvailabilityAsync(TestContext.Current.CancellationToken);

        await viewModel.PopulateNicheAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Coffee", viewModel.NicheName);
        Assert.Equal("A cozy coffee niche", viewModel.NicheDescription);
        Assert.Equal("Coffee fans", viewModel.NicheAudience);
        Assert.Equal("Bold, legible mug graphic", viewModel.NicheVisualStyleGuidance);
        Assert.Equal("Keep claims grounded", viewModel.NicheRisks);
        Assert.Equal("Manual research", viewModel.NicheResearchNotes);
        Assert.True(viewModel.HasUnsavedNicheChanges);
        Assert.Equal("Coffee", (await repository.LoadAsync()).Niches.Single().Name);
        Assert.Null((await repository.LoadAsync()).Niches.Single().Description);

        viewModel.NicheDescription = "Reviewed coffee niche";
        await viewModel.SaveSelectedNicheAsync(TestContext.Current.CancellationToken);

        var saved = (await repository.LoadAsync()).Niches.Single();
        Assert.Equal("Reviewed coffee niche", saved.Description);
    }

    [Fact]
    public async Task Populate_DoesNotOverwriteFieldEditedWhileRequestIsRunningOrStartOverlap()
    {
        var niche = NewNiche("Coffee", isArchived: false);
        var fake = new FakeNichePopulationService
        {
            Result = NichePopulationResult.Success(new Dictionary<NichePopulationField, string>
            {
                [NichePopulationField.Description] = "Generated description",
                [NichePopulationField.Audience] = "Generated audience"
            })
        };
        var (viewModel, _) = await CreateViewModel([niche], fake);
        viewModel.SelectNicheForEditing(viewModel.ActiveNiches.Single());
        await viewModel.RefreshNichePopulationAvailabilityAsync(TestContext.Current.CancellationToken);
        fake.BlockRequests = true;

        var first = viewModel.PopulateNicheAsync(TestContext.Current.CancellationToken);
        await fake.RequestStarted;
        Assert.True(viewModel.IsNichePopulationBusy);

        viewModel.NicheDescription = "Creator edit";
        await viewModel.PopulateNicheAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, fake.PopulateCalls);

        fake.Release();
        await first;

        Assert.Equal("Creator edit", viewModel.NicheDescription);
        Assert.Equal("Generated audience", viewModel.NicheAudience);
        Assert.False(viewModel.IsNichePopulationBusy);
    }

    [Fact]
    public async Task Populate_FailurePreservesValuesAndReturnsToEditableState()
    {
        var niche = NewNiche("Coffee", isArchived: false, new NicheContext(Description: "Existing"));
        var fake = new FakeNichePopulationService
        {
            Result = NichePopulationResult.Failure(
                NichePopulationFailureKind.ProviderFailure,
                "AI could not populate the niche fields. Check AI settings or try again.")
        };
        var (viewModel, _) = await CreateViewModel([niche], fake);
        viewModel.SelectNicheForEditing(viewModel.ActiveNiches.Single());
        await viewModel.RefreshNichePopulationAvailabilityAsync(TestContext.Current.CancellationToken);

        await viewModel.PopulateNicheAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Existing", viewModel.NicheDescription);
        Assert.False(viewModel.IsNichePopulationBusy);
        Assert.Contains("try again", viewModel.NichePopulationStatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RefreshAvailability_ReevaluatesPopulationWithoutReopeningEditor()
    {
        var niche = NewNiche("Coffee", isArchived: false);
        var fake = new FakeNichePopulationService
        {
            Availability = new(AiAvailabilityKind.MissingModel, "Select a General AI model in AI settings.")
        };
        var (viewModel, _) = await CreateViewModel([niche], fake);
        viewModel.SelectNicheForEditing(viewModel.ActiveNiches.Single());

        await viewModel.RefreshNichePopulationAvailabilityAsync(TestContext.Current.CancellationToken);
        Assert.False(viewModel.CanPopulateNiche);
        Assert.Contains("General AI", viewModel.NichePopulationStatusMessage, StringComparison.Ordinal);

        fake.Availability = AiAvailabilityResult.Ready;
        await viewModel.RefreshNichePopulationAvailabilityAsync(TestContext.Current.CancellationToken);

        Assert.True(viewModel.CanPopulateNiche);
    }

    private static async Task<(StoreManagementViewModel ViewModel, InMemoryWorkspaceRepository Repository)> CreateViewModel(
        IReadOnlyList<Niche> niches,
        FakeNichePopulationService population)
    {
        var store = NewStore();
        niches = niches.Select(niche => niche with { StoreId = store.Id }).ToArray();
        var repository = new InMemoryWorkspaceRepository(new WorkspaceSnapshot(
            [store], niches, [], [], [], [], [], [], []));
        var viewModel = new StoreManagementViewModel(
            new StoreManagementService(repository),
            new NicheManagementService(repository),
            nichePopulationService: population);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        return (viewModel, repository);
    }

    private static Store NewStore() =>
        new(Guid.NewGuid(), "North Star", null, false, Now, Now, "{}");

    private static Niche NewNiche(string name, bool isArchived, NicheContext? context = null)
    {
        context ??= new NicheContext();
        var metadata = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string?>
        {
            ["audience"] = context.Audience,
            ["humorStyle"] = context.HumorStyle,
            ["visualStyleGuidance"] = context.VisualStyleGuidance,
            ["constraints"] = context.Constraints,
            ["risks"] = context.Risks,
            ["researchNotes"] = context.ResearchNotes,
            ["notes"] = context.Notes
        }.Where(pair => pair.Value is not null).ToDictionary(pair => pair.Key, pair => pair.Value!));
        return new(Guid.NewGuid(), NewStore().Id, name, context.Description, isArchived, Now, Now, metadata);
    }

    private sealed class FakeNichePopulationService : INichePopulationService
    {
        private readonly TaskCompletionSource<object?> _requestStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<object?> _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public AiAvailabilityResult Availability { get; set; } = AiAvailabilityResult.Ready;
        public NichePopulationResult Result { get; set; } = NichePopulationResult.Success(new Dictionary<NichePopulationField, string>());
        public bool BlockRequests { get; set; }
        public int PopulateCalls { get; private set; }
        public Task RequestStarted => _requestStarted.Task;

        public Task<AiAvailabilityResult> GetAvailabilityAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Availability);

        public async Task<NichePopulationResult> PopulateAsync(NichePopulationRequest request, CancellationToken cancellationToken = default)
        {
            PopulateCalls++;
            _requestStarted.TrySetResult(null);
            if (BlockRequests)
            {
                await _gate.Task.WaitAsync(cancellationToken);
            }

            return Result;
        }

        public void Release() => _gate.TrySetResult(null);
    }
}

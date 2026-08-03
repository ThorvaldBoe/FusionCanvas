using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using FusionCanvas.App.ConceptRefinement;
using FusionCanvas.App.Items;
using FusionCanvas.Application.AI;
using FusionCanvas.Application.ConceptRefinement;
using FusionCanvas.Application.Items;
using FusionCanvas.Application.WorkspaceTree;
using FusionCanvas.Domain.Concepts;
using FusionCanvas.Domain.Workflow;
using FusionCanvas.Domain.Items;

namespace FusionCanvas.App.Tests;

public sealed class ConceptRefinementSessionViewModelTests
{
    [Fact]
    public void InitialState_AllCommandsDisabledWhenNoItemLoaded()
    {
        var vm = CreateSessionViewModel();
        Assert.Equal(0, vm.Score);
        Assert.False(vm.CanInitialize);
        Assert.False(vm.CanFineTuneConceptIdea);
        Assert.False(vm.CanFineTunePhrase);
        Assert.False(vm.CanFineTuneGraphicDirection);
        Assert.False(vm.CanChangeConceptIdea);
        Assert.False(vm.CanChangePhrase);
        Assert.False(vm.CanChangeGraphicDirection);
    }

    [Fact]
    public void Score_ComputesCorrectlyFromInspectorDrafts()
    {
        var inspector = CreateInspector();
        var vm = CreateSessionViewModel(inspector);

        inspector.ConceptIdea = "A long enough concept";
        inspector.Phrase = "Short";
        inspector.GraphicDirection = "";

        Assert.Equal(50, vm.Score);
    }

    [Fact]
    public void Score_RecomputesOnInspectorDraftChange()
    {
        var inspector = CreateInspector();
        var vm = CreateSessionViewModel(inspector);
        Assert.Equal(0, vm.Score);

        inspector.ConceptIdea = "Substantive concept idea text";
        Assert.Equal(33, vm.Score);
    }

    [Fact]
    public async Task InitializeSuccess_AppendsEntryAndCommits()
    {
        var inspector = CreateInspector();
        var (svc, acc) = (new StubRefinementService(), new StubRefinementAccess(true));
        var vm = new ConceptRefinementSessionViewModel(svc, acc, inspector);
        svc.InitializeResult = ConceptRefinementResult.Success("Idea result", "Phrase result", "Graphic result");

        await SetupLoadedInspectorAsync(inspector);
        vm.ResetSession();

        inspector.Idea = "Base idea text";
        Assert.True(vm.CanInitialize);

        vm.InitializeCommand.Execute(null);
        await Task.Delay(100);

        Assert.Single(vm.History);
        Assert.Equal("Initialized from base idea", vm.History[0].Label);
        Assert.Equal("Idea result", inspector.ConceptIdea);
        Assert.Equal("Phrase result", inspector.Phrase);
        Assert.Equal("Graphic result", inspector.GraphicDirection);
        Assert.Equal(100, vm.Score);
        Assert.False(vm.IsBusy);
        Assert.Null(vm.ErrorMessage);
    }

    [Fact]
    public async Task InitializeFailure_KeepsStateAndShowsError()
    {
        var inspector = CreateInspector();
        var (svc, acc) = (new StubRefinementService(), new StubRefinementAccess(true));
        var vm = new ConceptRefinementSessionViewModel(svc, acc, inspector);
        svc.InitializeResult = ConceptRefinementResult.Failure(
            AiTextFailureKind.ProviderFailure, "The AI provider declined.");

        await SetupLoadedInspectorAsync(inspector);
        vm.ResetSession();

        inspector.Idea = "Base idea text";
        Assert.True(vm.CanInitialize);

        vm.InitializeCommand.Execute(null);
        await Task.Delay(100);

        Assert.Empty(vm.History);
        Assert.Empty(inspector.ConceptIdea);
        Assert.Equal("The AI provider declined.", vm.ErrorMessage);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task FineTuneSuccess_AppendsOneEntryAndCommits()
    {
        var inspector = CreateInspector();
        var (svc, acc) = (new StubRefinementService(), new StubRefinementAccess(true));
        var vm = new ConceptRefinementSessionViewModel(svc, acc, inspector);
        svc.RefineResult = ConceptRefinementResult.Success("Improved idea", null, null);

        await SetupLoadedInspectorAsync(inspector);
        vm.ResetSession();

        inspector.ConceptIdea = "Existing concept idea";
        inspector.Phrase = "Existing phrase";
        inspector.GraphicDirection = "Existing graphic";

        Assert.True(vm.CanFineTuneConceptIdea);
        vm.FineTuneConceptIdeaCommand.Execute(null);
        await Task.Delay(100);

        Assert.Single(vm.History);
        Assert.Equal("Fine-tuned Concept idea", vm.History[0].Label);
        Assert.Equal("Improved idea", inspector.ConceptIdea);
        Assert.Equal("Existing phrase", inspector.Phrase);
        Assert.Equal("Existing graphic", inspector.GraphicDirection);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task ChangeSuccess_ReplacesCorner()
    {
        var inspector = CreateInspector();
        var (svc, acc) = (new StubRefinementService(), new StubRefinementAccess(true));
        var vm = new ConceptRefinementSessionViewModel(svc, acc, inspector);
        svc.RefineResult = ConceptRefinementResult.Success(null, "New direction phrase", null);

        await SetupLoadedInspectorAsync(inspector);
        vm.ResetSession();
        inspector.Phrase = "Current phrase";

        Assert.True(vm.CanChangePhrase);
        vm.ChangePhraseCommand.Execute(null);
        await Task.Delay(100);

        Assert.Single(vm.History);
        Assert.Equal("Changed Phrase", vm.History[0].Label);
        Assert.Equal("New direction phrase", inspector.Phrase);
    }

    [Fact]
    public async Task WorkingInputs_InitializeAndSynchronizeOnlyMatchingInspectorDraft()
    {
        var inspector = CreateInspector();
        var vm = CreateSessionViewModel(inspector);

        await SetupLoadedInspectorAsync(
            inspector,
            conceptIdea: "Initial idea",
            phrase: "Initial phrase",
            graphicDirection: "Initial graphic");
        vm.ResetSession();

        Assert.Equal("Initial idea", vm.ConceptIdeaInput);
        Assert.Equal("Initial phrase", vm.PhraseInput);
        Assert.Equal("Initial graphic", vm.GraphicDirectionInput);

        vm.PhraseInput = "Locally edited phrase";
        inspector.ConceptIdea = "Inspector idea update";

        Assert.Equal("Inspector idea update", vm.ConceptIdeaInput);
        Assert.Equal("Locally edited phrase", vm.PhraseInput);
        Assert.Equal("Initial graphic", vm.GraphicDirectionInput);
    }

    [Fact]
    public async Task FineTune_UsesCurrentWorkingTriangleAndSynchronizesSuccessfulTarget()
    {
        var inspector = CreateInspector();
        var service = new StubRefinementService
        {
            RefineResult = ConceptRefinementResult.Success("AI result", null, null)
        };
        var vm = new ConceptRefinementSessionViewModel(service, new StubRefinementAccess(true), inspector);

        await SetupLoadedInspectorAsync(inspector, conceptIdea: "Stored idea", phrase: "Stored phrase", graphicDirection: "Stored graphic");
        vm.ResetSession();
        vm.ConceptIdeaInput = "Visible edited idea";
        vm.PhraseInput = "Visible edited phrase";
        vm.GraphicDirectionInput = "Visible edited graphic";

        vm.FineTuneConceptIdeaCommand.Execute(null);
        await Task.Delay(100);

        Assert.Equal(ConceptRefinementActionKind.FineTune, service.LastAction);
        Assert.Equal(ConceptRefinementCorner.ConceptIdea, service.LastCorner);
        Assert.Equal(new ConceptRefinementTriangle(
            "Visible edited idea", "Visible edited phrase", "Visible edited graphic"), service.LastCurrent);
        Assert.Equal("AI result", vm.ConceptIdeaInput);
        Assert.Equal("Visible edited phrase", vm.PhraseInput);
        Assert.Equal("Visible edited graphic", vm.GraphicDirectionInput);
    }

    [Fact]
    public async Task Change_AllowsEmptyTargetAndUsesCurrentWorkingTriangle()
    {
        var inspector = CreateInspector();
        var service = new StubRefinementService
        {
            RefineResult = ConceptRefinementResult.Failure(AiTextFailureKind.ProviderFailure, "No result")
        };
        var vm = new ConceptRefinementSessionViewModel(service, new StubRefinementAccess(true), inspector);

        await SetupLoadedInspectorAsync(inspector, conceptIdea: "Stored idea", phrase: "Stored phrase", graphicDirection: "Stored graphic");
        vm.ResetSession();
        vm.ConceptIdeaInput = "Visible idea";
        vm.PhraseInput = "";
        vm.GraphicDirectionInput = "Visible graphic";

        Assert.True(vm.CanChangePhrase);
        vm.ChangePhraseCommand.Execute(null);
        await Task.Delay(100);

        Assert.Equal(ConceptRefinementActionKind.Change, service.LastAction);
        Assert.Equal(ConceptRefinementCorner.Phrase, service.LastCorner);
        Assert.Equal(new ConceptRefinementTriangle("Visible idea", "", "Visible graphic"), service.LastCurrent);
    }

    [Fact]
    public async Task EditingWorkingInputs_DoesNotChangeInspectorScoreOrHistory()
    {
        var inspector = CreateInspector();
        var vm = CreateSessionViewModel(inspector);

        await SetupLoadedInspectorAsync(inspector, conceptIdea: "Stored idea", phrase: "Stored phrase", graphicDirection: "Stored graphic");
        vm.ResetSession();
        var score = vm.Score;

        vm.ConceptIdeaInput = "Local idea";
        vm.PhraseInput = "Local phrase";
        vm.GraphicDirectionInput = "Local graphic";

        Assert.Equal("Stored idea", inspector.ConceptIdea);
        Assert.Equal("Stored phrase", inspector.Phrase);
        Assert.Equal("Stored graphic", inspector.GraphicDirection);
        Assert.Equal(score, vm.Score);
        Assert.Empty(vm.History);
    }

    [Fact]
    public async Task RefinementFailure_PreservesWorkingInputsAndInspectorState()
    {
        var inspector = CreateInspector();
        var service = new StubRefinementService
        {
            RefineResult = ConceptRefinementResult.Failure(AiTextFailureKind.ProviderFailure, "Provider failed")
        };
        var vm = new ConceptRefinementSessionViewModel(service, new StubRefinementAccess(true), inspector);

        await SetupLoadedInspectorAsync(inspector, conceptIdea: "Stored idea", phrase: "Stored phrase", graphicDirection: "Stored graphic");
        vm.ResetSession();
        vm.ConceptIdeaInput = "Local idea";
        vm.PhraseInput = "Local phrase";
        vm.GraphicDirectionInput = "Local graphic";
        var score = vm.Score;

        vm.ChangeGraphicDirectionCommand.Execute(null);
        await Task.Delay(100);

        Assert.Equal("Local idea", vm.ConceptIdeaInput);
        Assert.Equal("Local phrase", vm.PhraseInput);
        Assert.Equal("Local graphic", vm.GraphicDirectionInput);
        Assert.Equal("Stored idea", inspector.ConceptIdea);
        Assert.Equal("Stored phrase", inspector.Phrase);
        Assert.Equal("Stored graphic", inspector.GraphicDirection);
        Assert.Equal(score, vm.Score);
        Assert.Empty(vm.History);
        Assert.Equal("Provider failed", vm.ErrorMessage);
    }

    [Fact]
    public async Task RefinementCancellation_PreservesWorkingInputsAndInspectorState()
    {
        var inspector = CreateInspector();
        var service = new StubRefinementService
        {
            RefineFunc = _ => Task.FromCanceled<ConceptRefinementResult>(new CancellationToken(canceled: true))
        };
        var vm = new ConceptRefinementSessionViewModel(service, new StubRefinementAccess(true), inspector);

        await SetupLoadedInspectorAsync(inspector, conceptIdea: "Stored idea", phrase: "Stored phrase", graphicDirection: "Stored graphic");
        vm.ResetSession();
        vm.ConceptIdeaInput = "Local idea";
        vm.PhraseInput = "Local phrase";
        vm.GraphicDirectionInput = "Local graphic";
        var score = vm.Score;

        vm.ChangeConceptIdeaCommand.Execute(null);
        await Task.Delay(100);

        Assert.Equal("Local idea", vm.ConceptIdeaInput);
        Assert.Equal("Local phrase", vm.PhraseInput);
        Assert.Equal("Local graphic", vm.GraphicDirectionInput);
        Assert.Equal("Stored idea", inspector.ConceptIdea);
        Assert.Equal("Stored phrase", inspector.Phrase);
        Assert.Equal("Stored graphic", inspector.GraphicDirection);
        Assert.Equal(score, vm.Score);
        Assert.Empty(vm.History);
        Assert.Null(vm.ErrorMessage);
    }

    [Fact]
    public async Task FineTuneDisabledForEmptyCorner()
    {
        var inspector = CreateInspector();
        var (svc, acc) = (new StubRefinementService(), new StubRefinementAccess(true));
        var vm = new ConceptRefinementSessionViewModel(svc, acc, inspector);

        await SetupLoadedInspectorAsync(inspector);
        vm.ResetSession();

        inspector.ConceptIdea = "Some idea";
        Assert.True(vm.CanFineTuneConceptIdea);
        Assert.False(vm.CanFineTunePhrase);
        Assert.True(vm.CanChangePhrase);
        Assert.False(vm.CanFineTuneGraphicDirection);
        Assert.True(vm.CanChangeGraphicDirection);
    }

    [Fact]
    public async Task Busy_DisablesAllCommands()
    {
        var inspector = CreateInspector();
        var (svc, acc) = (new StubRefinementService(), new StubRefinementAccess(true));
        var vm = new ConceptRefinementSessionViewModel(svc, acc, inspector);

        await SetupLoadedInspectorAsync(inspector);
        vm.ResetSession();

        var tcs = new TaskCompletionSource<ConceptRefinementResult>();
        svc.InitializeFunc = ct => tcs.Task;

        inspector.Idea = "Base";
        inspector.ConceptIdea = "";
        inspector.Phrase = "";
        inspector.GraphicDirection = "";

        vm.InitializeCommand.Execute(null);
        Assert.True(vm.IsBusy);
        Assert.False(vm.CanInitialize);
        Assert.False(vm.CanFineTuneConceptIdea);
        Assert.False(vm.CanChangeConceptIdea);

        tcs.TrySetResult(ConceptRefinementResult.Success("A", "B", "C"));
        await Task.Delay(100);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task ItemSwitch_CancelsInFlightAndDoesNotApplyLateResult()
    {
        var inspector = CreateInspector();
        var (svc, acc) = (new StubRefinementService(), new StubRefinementAccess(true));
        var vm = new ConceptRefinementSessionViewModel(svc, acc, inspector);

        await SetupLoadedInspectorAsync(inspector);
        vm.ResetSession();

        var tcs = new TaskCompletionSource<ConceptRefinementResult>();
        svc.InitializeFunc = async ct =>
        {
            await Task.Delay(500, ct);
            ct.ThrowIfCancellationRequested();
            return ConceptRefinementResult.Success("Late", "Result", "");
        };

        inspector.Idea = "Base";
        inspector.ConceptIdea = "";
        inspector.Phrase = "";
        inspector.GraphicDirection = "";

        vm.InitializeCommand.Execute(null);
        Assert.True(vm.IsBusy);

        vm.ResetSession();
        await Task.Delay(50);
        Assert.False(vm.IsBusy);
        Assert.Empty(vm.History);

        await Task.Delay(600);
        Assert.Empty(vm.History);
        Assert.Empty(inspector.ConceptIdea);
    }

    [Fact]
    public async Task Rollback_RestoresDraftsWithoutNewEntry()
    {
        var inspector = CreateInspector();
        var (svc, acc) = (new StubRefinementService(), new StubRefinementAccess(true));
        var vm = new ConceptRefinementSessionViewModel(svc, acc, inspector);

        await SetupLoadedInspectorAsync(inspector);
        vm.ResetSession();

        inspector.Idea = "Base";
        svc.InitializeResult = ConceptRefinementResult.Success("Idea", "Phrase", "Graphic");
        vm.InitializeCommand.Execute(null);
        await Task.Delay(100);
        Assert.Single(vm.History);

        svc.RefineResult = ConceptRefinementResult.Success("Improved idea", null, null);
        vm.FineTuneConceptIdeaCommand.Execute(null);
        await Task.Delay(100);
        Assert.Equal(2, vm.History.Count);
        Assert.Equal("Improved idea", inspector.ConceptIdea);

        inspector.ConceptIdea = "Idea";
        inspector.Phrase = "Phrase";
        inspector.GraphicDirection = "Graphic";
        var firstEntry = vm.History[0];
        vm.SelectHistoryEntryCommand.Execute(firstEntry);
        await Task.Delay(100);

        Assert.Equal(2, vm.History.Count);
        Assert.Equal("Idea", inspector.ConceptIdea);
        Assert.Equal("Phrase", inspector.Phrase);
        Assert.Equal("Graphic", inspector.GraphicDirection);
    }

    [Fact]
    public async Task PostRollbackAction_TruncatesLaterEntries()
    {
        var inspector = CreateInspector();
        var (svc, acc) = (new StubRefinementService(), new StubRefinementAccess(true));
        var vm = new ConceptRefinementSessionViewModel(svc, acc, inspector);

        await SetupLoadedInspectorAsync(inspector);
        vm.ResetSession();

        inspector.Idea = "Base";
        svc.InitializeResult = ConceptRefinementResult.Success("I1", "P1", "G1");
        vm.InitializeCommand.Execute(null);
        await Task.Delay(100);

        svc.RefineResult = ConceptRefinementResult.Success("I2", null, null);
        vm.FineTuneConceptIdeaCommand.Execute(null);
        await Task.Delay(100);

        svc.RefineResult = ConceptRefinementResult.Success(null, null, "G3");
        vm.ChangeGraphicDirectionCommand.Execute(null);
        await Task.Delay(100);

        Assert.Equal(3, vm.History.Count);

        vm.SelectHistoryEntryCommand.Execute(vm.History[0]);
        await Task.Delay(100);

        inspector.ConceptIdea = "I1";
        svc.RefineResult = ConceptRefinementResult.Success("NewI2", null, null);
        vm.FineTuneConceptIdeaCommand.Execute(null);
        await Task.Delay(100);

        Assert.Equal(2, vm.History.Count);
        Assert.Equal("Fine-tuned Concept idea", vm.History[1].Label);
    }

    [Fact]
    public void Availability_ReflectsAccessStatus()
    {
        var inspector = CreateInspector();
        var acc = new StubRefinementAccess(false);
        var svc = new StubRefinementService();
        var vm = new ConceptRefinementSessionViewModel(svc, acc, inspector);

        Assert.False(vm.IsAvailable);
        Assert.NotNull(vm.UnavailableReason);
        Assert.False(vm.CanInitialize);
        Assert.False(vm.CanFineTuneConceptIdea);
    }

    [AvaloniaFact]
    public async Task AvailabilityChanged_EnablesInitializeCommand()
    {
        var inspector = CreateInspector();
        var acc = new StubRefinementAccess(false);
        var svc = new StubRefinementService();
        var vm = new ConceptRefinementSessionViewModel(svc, acc, inspector);

        await SetupLoadedInspectorAsync(inspector);
        vm.ResetSession();
        inspector.Idea = "Base idea";

        var notifications = 0;
        vm.InitializeCommand.CanExecuteChanged += (_, _) => notifications++;

        Assert.False(vm.InitializeCommand.CanExecute(null));

        acc.SetAvailable(true);
        acc.RaiseChanged();
        Dispatcher.UIThread.RunJobs();

        Assert.True(vm.IsAvailable);
        Assert.True(vm.InitializeCommand.CanExecute(null));
        Assert.True(notifications > 0);
    }

    [Fact]
    public async Task ResetSession_ClearsHistory()
    {
        var inspector = CreateInspector();
        var (svc, acc) = (new StubRefinementService(), new StubRefinementAccess(true));
        var vm = new ConceptRefinementSessionViewModel(svc, acc, inspector);

        await SetupLoadedInspectorAsync(inspector);
        vm.ResetSession();

        inspector.Idea = "Base";
        svc.InitializeResult = ConceptRefinementResult.Success("I", "P", "G");
        vm.InitializeCommand.Execute(null);
        await Task.Delay(100);
        Assert.Single(vm.History);

        vm.ResetSession();
        Assert.Empty(vm.History);
        Assert.False(vm.IsBusy);
        Assert.Null(vm.ErrorMessage);
    }

    [Fact]
    public async Task InitializeDisabledReason_NoBaseIdea()
    {
        var inspector = CreateInspector();
        var svc = new StubRefinementService();
        var acc = new StubRefinementAccess(true);
        var vm = new ConceptRefinementSessionViewModel(svc, acc, inspector);

        await SetupLoadedInspectorAsync(inspector);
        vm.ResetSession();

        inspector.Idea = "";
        inspector.ConceptIdea = "";

        Assert.False(vm.CanInitialize);
        Assert.Contains("base idea", vm.InitializeDisabledReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BaseIdeaChange_RaisesInitializeState()
    {
        var inspector = CreateInspector();
        var svc = new StubRefinementService();
        var acc = new StubRefinementAccess(true);
        var vm = new ConceptRefinementSessionViewModel(svc, acc, inspector);

        await SetupLoadedInspectorAsync(inspector);
        vm.ResetSession();

        var notifications = 0;
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(vm.CanInitialize))
            {
                notifications++;
            }
        };

        inspector.Idea = "Base idea";

        Assert.True(vm.CanInitialize);
        Assert.True(notifications > 0);
    }

    [Fact]
    public async Task InitializeDisabledReason_FieldsNotEmpty()
    {
        var inspector = CreateInspector();
        var svc = new StubRefinementService();
        var acc = new StubRefinementAccess(true);
        var vm = new ConceptRefinementSessionViewModel(svc, acc, inspector);

        await SetupLoadedInspectorAsync(inspector);
        vm.ResetSession();

        inspector.Idea = "Base idea";
        inspector.ConceptIdea = "Non-empty";

        Assert.False(vm.CanInitialize);
        Assert.Contains("empty", vm.InitializeDisabledReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FineTuneDisabledReason_CornerEmpty_ReturnsReason()
    {
        var inspector = CreateInspector();
        var svc = new StubRefinementService();
        var acc = new StubRefinementAccess(true);
        var vm = new ConceptRefinementSessionViewModel(svc, acc, inspector);

        await SetupLoadedInspectorAsync(inspector);
        vm.ResetSession();

        // ConceptIdea is non-empty, Phrase and GraphicDirection are empty.
        inspector.ConceptIdea = "Some concept";
        inspector.Phrase = "";

        // Fine-tune for empty corner should show reason
        Assert.NotNull(vm.FineTunePhraseDisabledReason);
        Assert.Contains("Add text to this field", vm.FineTunePhraseDisabledReason);
        Assert.NotNull(vm.FineTuneGraphicDirectionDisabledReason);
        Assert.Contains("Add text to this field", vm.FineTuneGraphicDirectionDisabledReason);

        // Fine-tune for non-empty corner should be null (enabled)
        Assert.True(vm.CanFineTuneConceptIdea);
        Assert.Null(vm.FineTuneConceptIdeaDisabledReason);

        // Change operations never check empty corner so they should be null (enabled)
        Assert.True(vm.CanChangeConceptIdea);
        Assert.Null(vm.ChangeConceptIdeaDisabledReason);
        Assert.True(vm.CanChangePhrase);
        Assert.Null(vm.ChangePhraseDisabledReason);
    }

    [Fact]
    public async Task FineTuneDisabledReason_Unavailable_ReturnsUnavailableReason()
    {
        var inspector = CreateInspector();
        var svc = new StubRefinementService();
        var acc = new StubRefinementAccess(false); // AI unavailable
        var vm = new ConceptRefinementSessionViewModel(svc, acc, inspector);

        await SetupLoadedInspectorAsync(inspector);
        vm.ResetSession();

        inspector.ConceptIdea = "Some concept";

        Assert.False(vm.IsAvailable);
        Assert.NotNull(vm.UnavailableReason);

        // All per-corner disabled reasons should reflect unavailability
        Assert.Equal(vm.UnavailableReason, vm.FineTuneConceptIdeaDisabledReason);
        Assert.Equal(vm.UnavailableReason, vm.FineTunePhraseDisabledReason);
        Assert.Equal(vm.UnavailableReason, vm.FineTuneGraphicDirectionDisabledReason);
        Assert.Equal(vm.UnavailableReason, vm.ChangeConceptIdeaDisabledReason);
        Assert.Equal(vm.UnavailableReason, vm.ChangePhraseDisabledReason);
        Assert.Equal(vm.UnavailableReason, vm.ChangeGraphicDirectionDisabledReason);
    }

    [Fact]
    public async Task FineTuneDisabledReason_Busy_ReturnsBusyReason()
    {
        var inspector = CreateInspector();
        var svc = new StubRefinementService();
        var acc = new StubRefinementAccess(true);
        var vm = new ConceptRefinementSessionViewModel(svc, acc, inspector);

        await SetupLoadedInspectorAsync(inspector);
        vm.ResetSession();

        // Set base idea and ensure all corners are empty so CanInitialize is true
        inspector.Idea = "Base idea";
        inspector.ConceptIdea = "";
        inspector.Phrase = "";
        inspector.GraphicDirection = "";

        var tcs = new TaskCompletionSource<ConceptRefinementResult>();
        svc.InitializeFunc = ct => tcs.Task;

        vm.InitializeCommand.Execute(null);
        Assert.True(vm.IsBusy);

        var expected = "A refinement operation is in progress.";
        Assert.Equal(expected, vm.FineTuneConceptIdeaDisabledReason);
        Assert.Equal(expected, vm.FineTunePhraseDisabledReason);
        Assert.Equal(expected, vm.FineTuneGraphicDirectionDisabledReason);
        Assert.Equal(expected, vm.ChangeConceptIdeaDisabledReason);
        Assert.Equal(expected, vm.ChangePhraseDisabledReason);
        Assert.Equal(expected, vm.ChangeGraphicDirectionDisabledReason);

        tcs.TrySetResult(ConceptRefinementResult.Success("A", "B", "C"));
        await Task.Delay(100);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task FineTuneDisabledReason_ReadOnlyStage_ReturnsStageReadOnlyReason()
    {
        var inspector = CreateInspector();
        var svc = new StubRefinementService();
        var acc = new StubRefinementAccess(true);
        var vm = new ConceptRefinementSessionViewModel(svc, acc, inspector);

        // Create a state that is not effectively active → read-only with restore message
        var state = CreateValidState(conceptIdea: "Some idea", phrase: "Some phrase", graphicDirection: "Some graphic")
            with { IsEffectivelyActive = false };
        var svcField = typeof(ItemInspectorViewModel).GetField(
            "_service",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (svcField?.GetValue(inspector) is StubItemInspectorService stub)
        {
            stub.StateToReturn = state;
        }

        await inspector.LoadAsync(state.Id);
        vm.ResetSession();

        var expectedReason = inspector.StageReadOnlyReason;
        Assert.False(string.IsNullOrEmpty(expectedReason));
        Assert.Equal(expectedReason, vm.FineTuneConceptIdeaDisabledReason);
        Assert.Equal(expectedReason, vm.ChangeConceptIdeaDisabledReason);
    }

    [Fact]
    public async Task FineTuneDisabledReason_EnabledWhenFullyAvailable()
    {
        var inspector = CreateInspector();
        var svc = new StubRefinementService();
        var acc = new StubRefinementAccess(true);
        var vm = new ConceptRefinementSessionViewModel(svc, acc, inspector);

        await SetupLoadedInspectorAsync(inspector);
        vm.ResetSession();

        inspector.ConceptIdea = "Some concept";
        inspector.Phrase = "Some phrase";
        inspector.GraphicDirection = "Some graphic";

        Assert.True(vm.CanFineTuneConceptIdea);
        Assert.Null(vm.FineTuneConceptIdeaDisabledReason);
        Assert.True(vm.CanChangeConceptIdea);
        Assert.Null(vm.ChangeConceptIdeaDisabledReason);
    }

    [Fact]
    public async Task ManualCommit_AppendsCorrectlyLabeledEntry()
    {
        var inspector = CreateInspector();
        var (svc, acc) = (new StubRefinementService(), new StubRefinementAccess(true));
        var vm = new ConceptRefinementSessionViewModel(svc, acc, inspector);

        await SetupLoadedInspectorAsync(inspector);
        vm.ResetSession();

        inspector.ConceptIdea = "Manually edited concept";

        await inspector.CommitEditsAsync();
        await Task.Delay(50);

        Assert.Single(vm.History);
        Assert.Equal("Edited Concept idea", vm.History[0].Label);
        Assert.Equal("Manually edited concept", vm.History[0].ConceptIdea);
    }

    [Fact]
    public async Task NonConceptCommit_AppendsNothing_EvenWithPreExistingConceptValues()
    {
        // VR-010: uses pre-existing concept values in the loaded state so the baseline
        // tracks a non-empty triangle; a Notes-only commit must NOT create an entry.
        var inspector = CreateInspector();
        var (svc, acc) = (new StubRefinementService(), new StubRefinementAccess(true));
        var vm = new ConceptRefinementSessionViewModel(svc, acc, inspector);

        await SetupLoadedInspectorAsync(inspector, conceptIdea: "Loaded concept",
            phrase: "Loaded phrase", graphicDirection: "Loaded graphic");
        vm.ResetSession();

        // At this point the baseline tracks "Loaded concept" / "Loaded phrase" / "Loaded graphic"
        inspector.Notes = "Some notes";
        await inspector.CommitEditsAsync();
        await Task.Delay(50);

        Assert.Empty(vm.History);
    }

    [Fact]
    public async Task AiTriggeredCommit_AddsNoManualEntry()
    {
        var inspector = CreateInspector();
        var (svc, acc) = (new StubRefinementService(), new StubRefinementAccess(true));
        var vm = new ConceptRefinementSessionViewModel(svc, acc, inspector);

        await SetupLoadedInspectorAsync(inspector);
        vm.ResetSession();

        inspector.Idea = "Base";
        svc.InitializeResult = ConceptRefinementResult.Success("AI Idea", "AI Phrase", "AI Graphic");
        Assert.True(vm.CanInitialize);
        vm.InitializeCommand.Execute(null);
        await Task.Delay(100);

        Assert.Single(vm.History);
        Assert.Equal("Initialized from base idea", vm.History[0].Label);
    }

    [Fact]
    public async Task FailedCommitAfterApply_RetainsHistoryEntryAndDraft()
    {
        var inspector = CreateInspector();
        var (svc, acc) = (new StubRefinementService(), new StubRefinementAccess(true));
        var vm = new ConceptRefinementSessionViewModel(svc, acc, inspector);

        await SetupLoadedInspectorAsync(inspector, failSaves: true);
        vm.ResetSession();

        inspector.Idea = "Base";
        svc.InitializeResult = ConceptRefinementResult.Success("Failed commit idea", "Failed commit phrase", "Failed commit graphic");

        Assert.True(vm.CanInitialize);
        vm.InitializeCommand.Execute(null);
        await Task.Delay(100);

        Assert.Single(vm.History);
        Assert.Equal("Initialized from base idea", vm.History[0].Label);
        Assert.Equal("Failed commit idea", inspector.ConceptIdea);

        // Inspector's error surfaced (per D6)
        Assert.True(inspector.HasError);
        Assert.NotNull(inspector.ErrorMessage);
    }

    // --- Test helpers ---

    private static ItemInspectorViewModel CreateInspector()
    {
        var svc = new StubItemInspectorService();
        return new ItemInspectorViewModel(svc, new StubItemManagementService());
    }

    private static ConceptRefinementSessionViewModel CreateSessionViewModel(
        ItemInspectorViewModel? inspector = null)
    {
        inspector ??= CreateInspector();
        return new ConceptRefinementSessionViewModel(
            new StubRefinementService(),
            new StubRefinementAccess(true),
            inspector);
    }

    private static async Task SetupLoadedInspectorAsync(ItemInspectorViewModel inspector,
        bool failSaves = false, string? conceptIdea = null, string? phrase = null, string? graphicDirection = null)
    {
        var state = CreateValidState(conceptIdea: conceptIdea, phrase: phrase, graphicDirection: graphicDirection);
        var svcField = typeof(ItemInspectorViewModel).GetField(
            "_service",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (svcField?.GetValue(inspector) is StubItemInspectorService stub)
        {
            stub.StateToReturn = state;
            stub.FailSaves = failSaves;
        }

        await inspector.LoadAsync(state.Id);
    }

    private static ItemInspectorState CreateValidState(Guid? id = null,
        string? conceptIdea = null, string? phrase = null, string? graphicDirection = null) =>
        new(
            id ?? Guid.NewGuid(),
            "Test Item",
            "Description",
            new ItemInspectorCreativeFields(
                Idea: conceptIdea is not null ? "idea" : "test-idea",
                Audience: null,
                ConceptIdea: conceptIdea ?? "",
                Phrase: phrase ?? "",
                GraphicDirection: graphicDirection ?? ""),
            "Notes",
            ItemStatus.Draft,
            WorkflowStage.Concept,
            IsArchived: false,
            IsEffectivelyActive: true,
            "Store / Niche / Test",
            [],
            [],
            [],
            DateTimeOffset.UtcNow);

    // --- Stubs ---

    private sealed class StubRefinementService : IConceptRefinementService
    {
        public ConceptRefinementResult InitializeResult { get; set; } =
            ConceptRefinementResult.Failure(AiTextFailureKind.NotConfigured, "Not set");
        public ConceptRefinementResult RefineResult { get; set; } =
            ConceptRefinementResult.Failure(AiTextFailureKind.NotConfigured, "Not set");
        public Func<CancellationToken, Task<ConceptRefinementResult>>? InitializeFunc { get; set; }
        public Func<CancellationToken, Task<ConceptRefinementResult>>? RefineFunc { get; set; }
        public ConceptRefinementActionKind? LastAction { get; private set; }
        public ConceptRefinementCorner? LastCorner { get; private set; }
        public ConceptRefinementTriangle? LastCurrent { get; private set; }

        public Task<ConceptRefinementResult> InitializeAsync(
            Guid itemId, string originalIdea, CancellationToken cancellationToken = default) =>
            InitializeFunc?.Invoke(cancellationToken) ?? Task.FromResult(InitializeResult);

        public Task<ConceptRefinementResult> RefineAsync(
            Guid itemId, ConceptRefinementActionKind action, ConceptRefinementCorner corner,
            ConceptRefinementTriangle current, string originalIdea,
            CancellationToken cancellationToken = default)
        {
            LastAction = action;
            LastCorner = corner;
            LastCurrent = current;
            return RefineFunc?.Invoke(cancellationToken) ?? Task.FromResult(RefineResult);
        }
    }

    private sealed class StubRefinementAccess : IConceptRefinementAccessStatus
    {
        private ConceptRefinementAccessAvailability _current;

        public StubRefinementAccess(bool available) =>
            _current = available
                ? ConceptRefinementAccessAvailability.Available
                : ConceptRefinementAccessAvailability.Unavailable("API key required.");

        public event EventHandler? AvailabilityChanged;

        public ConceptRefinementAccessAvailability GetAvailability() => _current;

        public void SetAvailable(bool available) =>
            _current = available
                ? ConceptRefinementAccessAvailability.Available
                : ConceptRefinementAccessAvailability.Unavailable("API key required.");

        public void RaiseChanged() => AvailabilityChanged?.Invoke(this, EventArgs.Empty);

        public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubItemInspectorService : IItemInspectorService
    {
        public ItemInspectorState? StateToReturn { get; set; }
        public bool FailSaves { get; set; }

        public Task<ItemInspectorState?> LoadAsync(Guid itemId, CancellationToken cancellationToken = default) =>
            Task.FromResult(StateToReturn);

        public Task<ItemInspectorSaveResult> SaveAsync(ItemInspectorSaveRequest request, CancellationToken cancellationToken = default) =>
            FailSaves
                ? Task.FromResult(ItemInspectorSaveResult.Failure("Save failed"))
                : Task.FromResult(StateToReturn is { } s
                    ? ItemInspectorSaveResult.Success(s)
                    : ItemInspectorSaveResult.Failure("No state"));

        public Task<ItemInspectorSaveResult> SaveStageAsync(ItemStageAwareSaveRequest request, CancellationToken cancellationToken = default)
        {
            if (FailSaves)
            {
                return Task.FromResult(ItemInspectorSaveResult.Failure("SaveStage failed"));
            }

            var updated = StateToReturn is { } s
                ? s with
                {
                    Creative = s.Creative with
                    {
                        ConceptIdea = request.StagePayload.ConceptIdea ?? s.Creative.ConceptIdea,
                        Phrase = request.StagePayload.Phrase ?? s.Creative.Phrase,
                        GraphicDirection = request.StagePayload.GraphicDirection ?? s.Creative.GraphicDirection,
                    }
                }
                : null;
            return Task.FromResult(updated is not null
                ? ItemInspectorSaveResult.Success(updated)
                : ItemInspectorSaveResult.Failure("No state"));
        }
    }

    private sealed class StubItemManagementService : IItemManagementService
    {
        public Guid? ActiveWorkspaceId => null;
        public Guid? ActiveStoreId => null;
        public Guid? ActiveItemId => null;

        public void SetActiveWorkspace(Guid? workspaceId) { }

        public Task<ItemManagementState> LoadAsync(Guid? storeId, CancellationToken ct = default) =>
            Task.FromResult(new ItemManagementState(null, null, null, [], [], [], false));

        public Task<ItemCreationDestinationResult> ResolveCreateTopicAsync(
            Guid storeId, WorkspaceTreeSelection? selection, CancellationToken ct = default) =>
            Task.FromResult(new ItemCreationDestinationResult(null, null));

        public Task<ItemManagementResult> CreateItemAsync(ItemManagementCreateRequest request, CancellationToken ct = default) =>
            Task.FromResult(new ItemManagementResult(false, null, null, new ItemManagementState(null, null, null, [], [], [], false)));

        public Task<ItemManagementResult> UpdateItemAsync(ItemManagementUpdateRequest request, CancellationToken ct = default) =>
            Task.FromResult(new ItemManagementResult(false, null, null, new ItemManagementState(null, null, null, [], [], [], false)));

        public Task<ItemManagementResult> MoveItemAsync(ItemManagementMoveRequest request, CancellationToken ct = default) =>
            Task.FromResult(new ItemManagementResult(false, null, null, new ItemManagementState(null, null, null, [], [], [], false)));

        public Task<ItemManagementResult> DuplicateItemAsync(ItemManagementDuplicateRequest request, CancellationToken ct = default) =>
            Task.FromResult(new ItemManagementResult(false, null, null, new ItemManagementState(null, null, null, [], [], [], false)));

        public Task<ItemManagementResult> ArchiveItemAsync(Guid itemId, CancellationToken ct = default) =>
            Task.FromResult(new ItemManagementResult(false, null, null, new ItemManagementState(null, null, null, [], [], [], false)));

        public Task<ItemManagementResult> RestoreItemAsync(ItemManagementRestoreRequest request, CancellationToken ct = default) =>
            Task.FromResult(new ItemManagementResult(false, null, null, new ItemManagementState(null, null, null, [], [], [], false)));

        public Task<ItemManagementResult> DeleteItemAsync(ItemManagementDeleteRequest request, CancellationToken ct = default) =>
            Task.FromResult(new ItemManagementResult(false, null, null, new ItemManagementState(null, null, null, [], [], [], false)));

        public Task<ItemManagementResult> SetItemStatusAsync(ItemManagementSetStatusRequest request, CancellationToken ct = default) =>
            Task.FromResult(new ItemManagementResult(false, null, null, new ItemManagementState(null, null, null, [], [], [], false)));

        public Task<ItemManagementResult> MoveItemStageAsync(ItemManagementMoveStageRequest request, CancellationToken ct = default) =>
            Task.FromResult(new ItemManagementResult(false, null, null, new ItemManagementState(null, null, null, [], [], [], false)));

        public Task<ItemManagementResult> SelectItemAsync(Guid itemId, CancellationToken ct = default) =>
            Task.FromResult(new ItemManagementResult(false, null, null, new ItemManagementState(null, null, null, [], [], [], false)));
    }
}

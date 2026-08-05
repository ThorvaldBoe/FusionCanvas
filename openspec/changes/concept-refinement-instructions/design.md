## Context

The Concept refinement panel lets creators Fine tune or Change one design-triangle corner (Concept idea, Phrase, Graphic direction) at a time. Each corner row in `MainWindow.axaml` currently offers a label, a work-space input, a `Fine tune` button, and a `Change` button. `ConceptRefinementSessionViewModel` owns the three working inputs and calls `IConceptRefinementService.RefineAsync(itemId, action, corner, current, originalIdea, ct)`, which assembles the AI user message with a fixed action instruction and the creative context.

Creators have no way to steer the direction, forcing repeated or manual re-typing. The requested capability is an optional per-corner instruction the AI considers for that corner's Fine tune/Change, surfaced as a small unlabeled text field beneath each button pair and cleared after a successful apply. This is a contained, explicit user decision; no conflict with accepted behavior.

The primary workflow is brief, optional steering during in-context creative iteration in the main Concept workspace. Each field sits inline beneath its own button pair and is invisible in effect when empty, so it adds minimal workspace footprint.

## Goals / Non-Goals

**Goals:**

- Add three small unlabeled instruction fields, one per corner, each beneath its corner's Fine tune/Change pair, with placeholder `Instructions`.
- Thread the targeted corner's instruction into the AI request so it steers the Fine tune or Change result while preserving the action's semantics.
- Clear the targeted corner's instruction after a successful apply; preserve it on failure or cancellation.
- Keep an empty instruction behaviorally identical to today.
- Preserve read-only, AI availability, busy, Initialize, history, rollback, score, and result-application behavior.
- Verify prompt assembly at the application layer, state/lifecycle at the view-model layer, and control/binding/accessibility behavior with headless view tests.

**Non-Goals:**

- Persisting instruction text across operations, sessions, or reloads.
- Applying instructions to the Initialize action (it has no instruction field).
- Changing Fine tune/Change semantics, scoring, history, rollback, or other stage tools.
- A dedicated save action or confirmation for instruction text.

## Decisions

### D1: Application service accepts an optional instruction

`IConceptRefinementService.RefineAsync` gains a trailing `string? instruction` parameter. `ConceptRefinementService.RefineAsync` treats the value as meaningful only when it has non-whitespace content (after trimming); otherwise it is omitted entirely from the assembled request. This keeps the request-shaping judgement in the Application layer where prompt assembly already lives, and keeps Initialize unaffected (it simply does not pass an instruction).

Alternative considered: adding instruction to a request object. Rejected as unnecessary indirection for a single optional string; a trailing nullable parameter matches the existing flat signature style.

### D2: Instruction is supplemental guidance, not a new action

The instruction is inserted into the existing `userMessage` as a separate labeled line (e.g. `Creator instruction: ...`) alongside the action instruction and current triangle. It does not relabel Fine tune versus Change semantics: a Fine tune with an instruction still aims to improve preserving direction; a Change with an instruction still proposes a materially different direction, now steered by the instruction. This satisfies the requirement that the instruction "be considered" without overriding the action contract.

Because the instruction is user-authored untrusted input sent through an AI-facing boundary, the system message used for refinement SHALL instruct the model to treat the creator instruction as bounded supplemental guidance that cannot override the action semantics (Fine tune preserves direction, Change proposes a different direction) or the output rules (three-line/one-corner contract, single-line Phrase, non-empty). This bounds prompt-injection-style attempts that try to make the model ignore output rules or action semantics; the instruction can steer the content of the finish but not the shape of the contract.

Alternative considered: replacing the action instruction with user text. Rejected because it would corrupt Fine tune/Change semantics and history labels.

### D3: Three session-local instruction properties with targeted clear

`ConceptRefinementSessionViewModel` owns `ConceptIdeaInstructions`, `PhraseInstructions`, and `GraphicDirectionInstructions` as plain presentation state. The Fine tune/Change execution paths read the instruction matching `ConceptRefinementCorner` and pass it to the service. On a successful apply of that corner (`GuardApplyAsync`, `singleCorner` set, AI result succeeded), the VM clears the matching instruction property; failure and cancellation leave it intact. Clearing fires when the AI result succeeds, regardless of whether the subsequent automatic-save commit succeeds or fails, because the corner's refinement was applied to the draft; this matches the applied-value commit requirement where a failed commit retains the applied value and history entry. `ResetSession()` and item switches clear all three. Instruction text never touches inspector drafts, score, persistence, or history.

Alternative considered: a single shared instruction field. Rejected by the explicit requirement for three fields, one per corner.

### D4: Avalonia surface adds one field per corner below each button pair

The existing per-corner action `Grid` (3 rows x 4 columns) is expanded to 6 rows in an **interleaved** layout: each corner keeps its input/action row and is immediately followed by its own instruction row, so the row order is (input+buttons, instruction field, input+buttons, instruction field, input+buttons, instruction field). Each instruction row is a small single-line `TextBox` spanning the working columns (input + buttons), uses `PlaceholderText="Instructions"`, no visible label, a distinct accessible name (e.g. `Instructions for Concept idea`), and `IsReadOnly` bound to `!ItemInspector.CanEditStage`. The field is two-way bound to its corner's instruction property with `UpdateSourceTrigger=PropertyChanged`.

Alternative considered: placing the field only under the buttons (columns 2-3). Rejected: a full-width interleaved row keeps the three instruction fields visually aligned down the panel and simpler to reason about and test.

### D5: Enablement and scoring are unaffected

The instruction fields do not gate Fine tune/Change enablement and do not affect the completeness score, which remains based on inspector drafts. All existing gates (availability, busy, read-only, empty Fine tune target) are unchanged.

### D6: Interaction lifecycle

- Initial/session reset: all three instruction fields are empty.
- Typing: text is session-local; no inspector, persistence, score, or history change.
- Successful apply of a corner: the matching instruction field is cleared; other fields are untouched. This is true whether or not the subsequent automatic-save commit succeeds, because the corner's refinement was applied to the draft and the applied-value commit requirement retains it.
- Failed/cancelled operation: instruction fields retain text; no clear.
- Read-only stage: all instruction fields are read-only and no action can start.
- Item switch/close: existing session reset/cancellation discards instruction text.

## Risks / Trade-offs

- [Wrong corner's instruction is used] → map the corner to its property in one place and assert the mapping in view-model tests; add a scenario proving a different corner's instruction is excluded.
- [Instruction leaks into Initialize or unrelated prompts] → only RefineAsync accepts an instruction; Initialize passes none; application tests confirm absence when empty.
- [Instruction affects Fine tune/Change semantics next to its intended supplement] → treat as additive guidance and bound it in the system message; tests assert the action and corner are still passed unchanged.
- [Instruction not cleared on success or cleared on failure] → clear only in the success path for the exact applied corner, including the commit-failure-after-apply case; view-model tests cover success, commit-failure, and failure/cancellation.
- [Prompt-injection via instruction text] → bound the instruction in the refinement system message as non-overriding supplemental guidance and add an application test asserting the bound is present in the assembled system message.
- [Panel grows taller and crowds the surface] → keep fields single-line and small; they stay within the existing scrollable stage surface.

## Migration Plan

No data, schema, settings, or file migration. Deployment is a contract + view-model + surface update. Rollback removes the instruction parameter/properties/fields and restores the previous three-row grid; no persisted state requires cleanup.

## Open Questions

None. The coordinator resolved: no persistence/clearing (clear after successful action), three per-corner fields below each button pair, and instruction as supplemental guidance preserving Fine tune/Change semantics.

## Implementation Plan

1. **Application contract and prompt assembly** — Update `src/FusionCanvas.Application/ConceptRefinement/IConceptRefinementService.cs` to add `string? instruction` to `RefineAsync`. In `src/FusionCanvas.Application/ConceptRefinement/ConceptRefinementService.cs`, trim the value and, when non-whitespace, append a labeled `Creator instruction:` line to the user message for Fine tune/Change. Extend the refinement system message to instruct the model to treat the creator instruction as bounded, non-overriding supplemental guidance. `InitializeAsync` is unchanged.
2. **Session view model state and threading** — In `src/FusionCanvas.App/ConceptRefinement/ConceptRefinementSessionViewModel.cs`, add `ConceptIdeaInstructions`, `PhraseInstructions`, `GraphicDirectionInstructions` string properties (with command-state irrelevant to enablement). Read the corner-matching instruction in `ExecuteFineTuneAsync` and `ExecuteChangeAsync` and pass it to `_service.RefineAsync`. In `GuardApplyAsync`, after the AI result succeeds for a single corner, clear the matching instruction property regardless of the subsequent commit outcome. Clear all three in `ResetSession()`.
3. **Avalonia surface** — In `src/FusionCanvas.App/Views/MainWindow.axaml`, expand the per-corner action grid from 3 to 6 rows and add one unlabeled single-line `TextBox` per corner below its input/action row, styled with `PlaceholderText="Instructions"`, two-way binding to the corner instruction property, a distinct accessible name, and read-only bound to `!ItemInspector.CanEditStage`.
4. **Application prompt tests** — Extend `tests/FusionCanvas.Application.Tests/ConceptRefinement/ConceptRefinementServiceTests.cs` to assert the instruction is present in the assembled prompt when non-empty, absent when empty/whitespace, that the action/corner are unchanged when an instruction is provided, that a different corner's instruction is not included, and that the refinement system message includes the bounded-guidance instruction. Verify against the AI request captured by a stub text-generation collaborator.
5. **View-model lifecycle tests** — Extend `tests/FusionCanvas.App.Tests/ConceptRefinementSessionViewModelTests.cs` to assert the corner-matching instruction is passed for each Fine tune/Change, a different corner's instruction is not passed, cleared after a successful apply of that corner (including the commit-failure-after-apply case), preserved on failure and cancellation, and cleared on session reset.
6. **Headless view tests** — Extend `tests/FusionCanvas.App.Tests/ConceptRefinementViewTests.cs` to verify three instruction `TextBox`es appear below the button pairs, expose the placeholder and distinct accessible names, support two-way edits, and become read-only when the stage is read-only.
7. **Verification** — Run focused Application and App Concept refinement tests, `dotnet build .\FusionCanvas.sln`, `dotnet test .\FusionCanvas.sln`, and `openspec validate concept-refinement-instructions --strict`. Record scenario-level results in `verification.md` before completion.

No Domain/Integration edits, migrations, external dependencies, or compatibility shims are required. Decisions not to reopen: instructions are supplemental and bounded (Fine tune/Change semantics preserved; output rules cannot be overridden); the targeted corner's instruction is used and a different corner's instruction is excluded; instruction text clears on a successful apply of that corner regardless of commit outcome; Initialize has no instruction; instruction fields do not affect enablement, score, persistence, or history.

## Planned Acceptance Verification

| Scenario | Method |
| --- | --- |
| Instruction steers a Fine tune | Application prompt test asserts instruction text present for a Fine tune; view-model test asserts the corner-matching instruction is passed and cleared on success |
| Instruction for a different corner is not included | Application and view-model tests assert a non-acting corner's instruction is absent from the request and not passed |
| Instruction steers a Change | Application prompt test asserts instruction text present for a Change; view-model test asserts the corner-matching instruction is passed and cleared on success |
| Empty instruction leaves behavior unchanged | Application prompt test asserts no instruction content when value is empty/whitespace |
| Result applied but automatic-save commit fails | View-model test with a commit-failure stub asserts the instruction is cleared and the applied value/history are retained per the applied-value commit requirement |
| The active Item changes or the session resets | View-model reset test asserts all three instruction fields are cleared |
| Failed or cancelled operation preserves the instruction | View-model tests with failure/cancel stubs assert the field retains text |
| Instruction field content has no persistence or history effect | View-model test asserts inspector values, history, and score unchanged with instruction text typed before an action |
| Instruction fields respect read-only state | Headless view test asserts read-only binding; markup inspection confirms placement/placeholder/accessible names |
| Fine tune/Change requirement scenarios (context, apply own corner, Phrase normalization, empty corner) | Existing Application and view-model coverage, extended where the request now includes an instruction |
| Request includes framework and creative context / non-empty instruction included + bounded | Application prompt-assembly tests assert framework/context present and the bounded-guidance system-message line |

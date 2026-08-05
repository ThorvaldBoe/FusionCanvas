## Context

The Concept surface already owns persisted Item inspector drafts for Concept idea, Phrase, and Graphic direction. The Refine with AI panel currently mirrors those values through small ellipsized `TextBlock`s, and `ConceptRefinementSessionViewModel.CaptureTriangle()` reads directly from the inspector when Fine tune or Change runs.

Creators need a readable working context in the AI panel and the ability to adjust the exact input sent to AI without accidentally committing those tentative edits. The existing application service already accepts a complete `ConceptRefinementTriangle`, so this can remain an App-layer presentation change with no service, persistence, or schema changes.

The primary workflow is frequent creative iteration in the main Concept workspace. The controls remain inline in the existing scrollable panel, with compact action buttons next to vertically larger wrapped editors. Local edits are meaningful temporary input, but they are not durable Item edits until an AI result succeeds.

## Goals / Non-Goals

**Goals:**

- Show complete, legible working text for all three design-triangle corners in the refinement panel.
- Let creators edit each working value and have both Fine tune and Change use the visible values at activation time.
- Keep local input synchronized with authoritative inspector drafts when the Item session or inspector draft changes.
- Preserve local edits across AI failure or cancellation without creating persistence or history side effects.
- Preserve read-only, AI availability, busy, result application, history, rollback, score, and automatic-save behavior.
- Verify state/payload behavior below Avalonia and control/binding/accessibility behavior with headless view tests.

**Non-Goals:**

- A second save path or explicit save action for local refinement input.
- Changing the application service interface, AI prompt format, response parsing, Phrase result normalization, completeness scoring, or history semantics.
- Persisting local editor values separately or restoring them after the Item document session ends.
- Redesigning the surrounding Concept editor or other stage tools.

## Decisions

### D1: Presentation-local working triangle

`ConceptRefinementSessionViewModel` owns three string properties: Concept idea, Phrase, and Graphic direction working inputs. `ResetSession()` initializes them from the inspector. Corresponding inspector property changes update only the matching local value, preventing an unrelated inspector update from erasing another locally edited corner.

Alternative considered: bind the new fields directly to the inspector. Rejected because every keystroke would become an Item draft, interact with the existing lost-focus auto-save path, affect score/history, and fail the requirement to preserve tentative local input independently.

### D2: Capture all visible inputs at action activation

`CaptureTriangle()` reads the three local properties. Fine tune and Change continue passing one immutable triangle to `IConceptRefinementService.RefineAsync`, so the targeted working value and visible contextual corners are captured together before asynchronous provider work begins. Fine tune enablement and its empty-value guidance use the local targeted value; Change remains valid for an empty local target.

Alternative considered: substitute only the targeted local value while reading contextual corners from the inspector. Rejected because the payload would not match the complete visible working triangle and would make multiple local edits misleading.

### D3: Existing success path remains authoritative

Local typing does not update the inspector, completeness score, persistence, or history. On AI success, existing result application updates the targeted inspector draft and commits through `ItemInspectorViewModel.CommitEditsAsync`; the inspector property notification synchronizes the matching local input. Initialize and rollback likewise synchronize via inspector changes. Failure and cancellation do not touch inspector or local input.

This preserves one persistence boundary and avoids duplicate history entries. A manual edit in the main Concept fields remains authoritative and updates the matching local input immediately.

### D4: Wrapped accessible editors in the existing panel

Replace each ellipsized middle label with a two-way `TextBox` in one shared four-column action grid so all labels, editors, and Fine tune/Change controls align consistently across rows. Concept idea and Graphic direction accept multiline text and use a minimum height suitable for wrapped content; Phrase wraps for readability but does not accept line breaks. No maximum text width or trimming is applied. Each editor receives a distinct automation name and participates in the existing keyboard order before its Fine tune and Change buttons.

Editors are read-only whenever `ItemInspector.CanEditStage` is false. AI-unavailable state disables actions but does not erase local input. Busy state keeps existing action gating; the action captures its input before awaiting AI, so later typing cannot mutate the in-flight request.

### D5: Interaction lifecycle

- Initial/session reset: local values equal current inspector drafts.
- Empty: editors remain usable; Fine tune is disabled for an empty local target, Change remains enabled subject to shared gates.
- Success: target inspector draft, local target, history, score, and persistence follow the existing success path.
- Blocked: action guidance continues to identify AI, read-only, busy, or empty-target causes.
- Failure/cancellation: inspector, history, score, and local inputs remain unchanged.
- Item switch/close: existing session reset/cancellation discards session-local inputs and prevents late result application.
- Focus: ordinary TextBox focus and tab order apply; no forced focus transition is introduced.

## Risks / Trade-offs

- [Local and inspector values drift unexpectedly] → Synchronize the exact matching local property on inspector change and synchronize all three on session reset; test both paths.
- [Local typing accidentally persists or adds history] → Keep setters presentation-only and verify inspector values/history remain unchanged before an AI result.
- [Action uses stale input because the field still has focus] → Use two-way property-change bindings and capture the local properties synchronously at action activation.
- [Failure or cancellation loses creator input] → Do not reset local properties in failure/cancellation paths; verify with focused tests.
- [Long content makes the panel taller] → Keep it inside the existing scrollable stage surface and use bounded minimum, not fixed, heights.
- [Duplicate editors confuse persistence expectations] → Treat panel values as AI working context only; no save affordance is added and success continues through the existing inspector path.

## Migration Plan

No data, schema, settings, or file migration. Deployment is a UI/view-model update. Rollback removes the local properties and restores the label bindings; no persisted state requires cleanup.

## Open Questions

None. The coordinator explicitly requested local editable values, current-value action capture, and preservation of existing behavior.

## Implementation Plan

1. **App presentation state** — Update `src/FusionCanvas.App/ConceptRefinement/ConceptRefinementSessionViewModel.cs` with three local input properties, session initialization, exact-property inspector synchronization, local Fine tune preconditions/guidance, and local-triangle capture. Keep result application, persistence, history, score, and cancellation paths unchanged.
2. **Avalonia surface** — Update the three action rows in `src/FusionCanvas.App/Views/MainWindow.axaml` to use wrapped two-way TextBoxes with distinct accessible names, appropriate multiline behavior/minimum heights, read-only binding, and predictable label/editor/action order.
3. **Framework-free tests** — Extend `tests/FusionCanvas.App.Tests/ConceptRefinementSessionViewModelTests.cs`; make the stub capture action/corner/triangle and verify initialization/synchronization, Fine tune and Change payloads, local command preconditions, non-persistence/history, and failure preservation.
4. **Headless view tests** — Extend `tests/FusionCanvas.App.Tests/ConceptRefinementViewTests.cs` to verify the three visible controls are TextBoxes, contain complete untrimmed bound values, accept local edits through two-way bindings, expose accessible names, and use the intended multiline/wrapping/read-only behavior.
5. **Verification** — Run focused Concept refinement App tests, `dotnet build .\FusionCanvas.sln`, `dotnet test .\FusionCanvas.sln`, and `openspec validate edit-concept-refinement-inputs --strict`. Record scenario-level results in `verification.md` before completion.

No Domain/Application/Integration edits, migrations, external dependencies, or compatibility shims are required. Decisions not to reopen: local typing is non-persistent; all three visible values form the request triangle; successful results retain the existing inspector commit/history path; score remains based on inspector drafts.

## Planned Acceptance Verification

| Scenario | Method |
| --- | --- |
| Complete current values are legible | Avalonia headless test finds all three TextBoxes, exact long text, wrapping, heights, and no ellipsized label replacement |
| Local working values start and synchronize correctly | View-model tests for session reset and per-property inspector changes without overwriting unrelated local edits |
| Fine tune uses visible working triangle | Capturing service test asserts action, corner, and all three local values |
| Change uses visible working triangle | Capturing service test asserts action, corner, and all three local values, including empty target allowance |
| Local typing has no persistence/history/score side effect | View-model test asserts inspector values, history, and score remain unchanged before action success |
| Failure/cancellation preserves local values | View-model tests with failure/cancel stubs |
| Success synchronizes and persists through existing path | Existing apply/commit/history tests plus local target synchronization assertion |
| Read-only and keyboard/accessibility behavior | Headless test asserts read-only binding and distinct automation names; markup/control order inspection supports predictable tab sequence |

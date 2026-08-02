# Design: add-concept-refinement-tool

## Context

The Concept stage today is three free-text fields (Concept idea, Phrase, Graphics description) inside the Concept border of `MainWindow.axaml`, bound to `ItemInspectorViewModel` drafts with automatic save on field exit (`CommitEditsAsync` → commit drain → `IItemInspectorService.SaveStageAsync` with the stage-aware expected-state guard). The AI foundation is already merged: `IAiTextGenerationService` (Application/AI) resolves the `AiRequestPurpose.Concept` profile, reports availability (`GetAvailabilityAsync`), and submits provider-neutral text requests (`GenerateAsync(AiTextRequest)` with System/User messages). The Ideation module establishes the patterns this module mirrors: availability gating with visible-but-disabled actions and actionable guidance (`ConfiguredIdeationAccessStatus`), payload discipline (creative context only, no credentials or operational fields), and fake-driven deterministic tests.

Resolved product decisions (from discovery, do not reopen):

- The score is a **deterministic Domain heuristic** (completeness), never an AI call.
- History is **session-only** (Photoshop-style), never persisted; no schema change.
- Placement is a **section inside the existing Concept stage border**, not a dialog and not a separate stage tool.
- AI-applied values **write into the inspector drafts** and persist through the existing automatic-save path.
- Initialization is **manual only**, sourced from the original Idea field; entering the Concept stage never calls AI.

This change also reconciles documentation drift by removing the stale `Listing inspector edits core creative fields with explicit save` requirement (accidentally re-introduced by the archived basic-product-creation-workflow change after fix-main-window-usability renamed it to automatic save).

## Goals / Non-Goals

Goals: per the proposal — initialize/fine-tune/change against the design triangle with session history, rollback, live completeness score, availability gating, and a bundled guidance document, all verified deterministically.

Non-goals: per the proposal — persisted versions/history, branch-as-new-item, AI scoring, implicit AI calls, streaming, prompt persistence, guidance UI, `basic-product-workflow` drift cleanup.

## Decisions

### D1: Score formula (Domain, deterministic, not to reopen)

`DesignTriangleScore.FromValues(conceptIdea, phrase, graphicDirection)` in `FusionCanvas.Domain/Concepts/`: each corner contributes up to one third of 100. Corner contribution: whitespace-only → 0; non-whitespace but trimmed length < 8 characters → 0.5; trimmed length ≥ 8 → 1. Score = `Round(100 × sum / 3)`. Guarantees: all empty → 0; all substantive → 100; monotonic as corners grow. Rationale: "substantive" needs a concrete, testable threshold; 8 characters ≈ two short words and is easy to explain in UI copy. Alternative considered: per-corner length scaling (rejected — harder to explain, no meaningful gain for a completeness signal).

### D2: Refinement service shape (Application)

New `FusionCanvas.Application/ConceptRefinement/` capability namespace:

- `ConceptRefinementCorner` enum: `ConceptIdea`, `Phrase`, `GraphicDirection`.
- `ConceptRefinementActionKind` enum: `Initialize`, `FineTune`, `Change`.
- `ConceptRefinementTriangle` record: the three current string values (draft state passed in by the caller — never re-read from persistence, so uncommitted draft edits are honored).
- `ConceptRefinementResult` record: `Succeeded`, `ConceptIdea`/`Phrase`/`GraphicDirection` (all three populated for Initialize; exactly one for FineTune/Change), `Error`.
- `IConceptRefinementService`:
  - `Task<ConceptRefinementResult> InitializeAsync(Guid itemId, string originalIdea, CancellationToken)`
  - `Task<ConceptRefinementResult> RefineAsync(Guid itemId, ConceptRefinementActionKind action, ConceptRefinementCorner corner, ConceptRefinementTriangle current, string originalIdea, CancellationToken)`
- `ConceptRefinementService(IWorkspaceRepository repository, IAiTextGenerationService ai, IDesignTriangleGuidanceSource guidance)`: resolves creative context from the repository (store/niche/topic-path user-authored names, descriptions, metadata, inherited tags for the item's location — identifiers, timestamps, paths, provenance excluded), assembles the prompt, calls `GenerateAsync` with `AiRequestPurpose.Concept`, parses the response.
- `IDesignTriangleGuidanceSource`: `string Load()` (sync; embedded resource; trivially fakeable).
- `ConfiguredConceptRefinementAccessStatus(IAiTextGenerationService)` mirroring `ConfiguredIdeationAccessStatus`: caches availability for `AiRequestPurpose.Concept`, exposes `AvailabilityChanged`, `RefreshAsync`. New `IConceptRefinementAccessStatus` port; no changes to the AI module.

Rationale: passing the draft triangle in keeps the service persistence-agnostic about unsaved edits; loading only workspace creative context from the repository keeps item state authoritative at the caller. Alternative considered: service loads item state itself (rejected — risks stale persisted values overriding the user's live drafts).

### D3: Prompt and response contract (Application, internal)

- Messages: one System message (role: PoD concept-refinement assistant + full guidance-document text + output rules) and one User message (action instruction, current triangle, original idea, bounded creative context).
- Initialize output contract: labeled lines `IDEA: …`, `PHRASE: …`, `GRAPHIC: …` (case-insensitive labels, tolerant of blank lines). Parser requires all three non-empty; otherwise the operation fails recoverably and no draft changes.
- FineTune/Change output contract: the new value as plain response text; parser strips one optional leading `LABEL:` prefix and one pair of surrounding quotes, trims; Phrase results pass through `ItemMetadataCodec.NormalizeSingleLine` (same-assembly internal, reuse — do not duplicate). Empty/whitespace result → recoverable failure, no draft changes.
- FineTune instruction: improve the corner preserving its direction, given the other two corners. Change instruction: propose a materially different direction for the corner that still works with the other two. Empty target corner is allowed for Change (propose a fresh direction); FineTune is blocked upstream (disabled) for empty corners.

### D4: Guidance document packaging (Integration)

`src/FusionCanvas.Integration/AI/DesignTriangleGuidance.md` as an `EmbeddedResource`; `EmbeddedDesignTriangleGuidanceSource : IDesignTriangleGuidanceSource` reads it from the assembly manifest stream. Placeholder content (maintainer replaces later in-repo): a short definition of the design triangle — idea = the emotion or familiar setting (mandatory), phrase = optional on-product text, graphics = optional visual elements; the three must reinforce each other. Rationale: embedded resource guarantees presence at runtime and needs no file-path handling; the maintainer replaces the repo file. Alternative considered: loose content file copied to output (rejected — install-location mutation and missing-file failure modes for no current benefit).

### D5: Session state and history ownership (App)

New `FusionCanvas.App/ConceptRefinement/ConceptRefinementSessionViewModel`, composed by `MainWindowViewModel` with the shared `ItemInspectorViewModel`, the `IConceptRefinementService`, the access status, and a workspace-context accessor. Owns: availability mirror, busy flag, inline `ErrorMessage`, `ObservableCollection<ConceptRefinementHistoryEntry>` + current index, and the displayed `Score`.

- `ConceptRefinementHistoryEntry`: label (e.g. `Initialized from base idea`, `Fine-tuned Phrase`, `Changed Graphic direction`, `Edited Concept idea`), the three triangle values, session timestamp.
- Score recompute: subscribe to inspector `PropertyChanged` for `ConceptIdea`/`Phrase`/`GraphicDirection`; recompute via `DesignTriangleScore.FromValues` on every change (manual, AI-applied, rollback).
- Session reset: history clears when the inspector loads a different item (`LoadedItemId` change), when `Clear()` runs, and on a full `(re)LoadAsync` of the item document (new document session → empty history per spec). The save-refresh path (`ApplySavedStatePreservingEdits`) never clears history.
- Manual-edit entries: subscribe to the inspector `Saved` event; when a save completes and the triangle values differ from the current history entry — and no AI/rollback application is in progress — append one entry labeled `Edited <field>` (first changed field; `Edited Concept fields` when several changed).
- Rollback: applying an entry sets the three draft properties, moves the current index, and awaits `CommitEditsAsync()`; no new entry. A subsequent AI action or manual commit truncates entries after the current index before appending.
- Concurrency/cancellation: one `CancellationTokenSource` per document session; starting an operation cancels/replaces it; item switch, `Clear()`, or document close cancels it. Applied results are identity-checked (captured item id + operation sequence) so a late result can never write into another item's draft. While busy, all refinement commands are disabled; the text fields themselves stay editable (existing behavior unchanged; an applying result overwrites the target draft atomically and any committed interim state is recoverable via history).

### D6: Commit integration (App)

Applying values (Initialize/FineTune/Change/rollback) = set the inspector draft properties, append the history entry, then `await ItemInspectorViewModel.CommitEditsAsync()`. The existing drain, expected-state guard, baseline refresh, and `ErrorMessage` failure handling apply unchanged. A failed commit leaves the draft and the appended history entry (the draft state is real and recoverable) and surfaces the inspector's recoverable inline error. Rationale: one persistence path, no parallel save channel.

### D7: UI composition (App)

Inside the existing Concept border in `MainWindow.axaml` (after the Graphics description field and read-only reason), add the refinement section bound to `ConceptRefinement` (exposed on `MainWindowViewModel`, same `x:DataType`):

1. Header row: `Refine with AI` label + unavailable guidance text (visible when not ready, also as tooltips on disabled buttons).
2. Three action rows, one per corner: caption + `Fine tune` + `Change` compact buttons; `AutomationProperties.Name` = `Fine tune Concept idea`, `Change Phrase`, etc. (accessible names disambiguate the repeated labels).
3. `Initialize from base idea…` button with its own guidance when disabled (no base idea, or fields not all empty).
4. Score line: `Triangle completeness: {Score}%`.
5. History: `ItemsControl`/`ListBox` (bounded height, scrolls) with entries in chronological order, current entry highlighted; selection triggers rollback (keyboard-operable); section hidden while history is empty.
6. Busy state: actions disabled; existing progress idioms (no spinner requirement beyond disabled state + busy text).

The section's visibility follows `ShowsConceptStageTool`; all controls disabled when `!ItemInspector.CanEditStage` (read-only review). Theme: reuse existing brushes (`ElevatedSurfaceBrush`, `ControlBorderBrush`, `SecondaryTextBrush`, `DangerTextBrush`); no new theme resources.

### D8: Availability refresh

Session view model refreshes availability via `IConceptRefinementAccessStatus.RefreshAsync` when the Concept surface loads and whenever AI settings are saved (same application hook that refreshes Ideation access after settings changes); subscribe to `AvailabilityChanged` for live updates. No polling.

## Risks / Trade-offs

- [AI returns off-contract text for Initialize] → strict labeled parser; failure is recoverable, drafts unchanged; prompt includes explicit format rules.
- [Late async result corrupts another item's draft] → per-session cancellation + captured item-id/sequence identity check before applying.
- [User perceives score as quality judgment] → UI labels it `Triangle completeness`; spec fixes semantics to presence/substance.
- [Manual edits during an AI operation get overwritten] → fields intentionally stay editable (no behavior change); the apply is atomic and prior committed states are recoverable through history.
- [History noise from rapid blur-commits] → entries only on *committed* changes (Saved event), not keystrokes; label identifies the field.
- [Score threshold (8 chars) feels arbitrary] → documented as the approved heuristic in D1; tunable later via spec change if creators push back.
- [Refreshing external mutations wipe history] → conservative session reset on full reload is accepted and documented (D5).

## Migration Plan

No schema or data migration. The design-triangle guidance document is new bundled content. Rollback = code rollback; session history leaves nothing behind.

## Open Questions

None. All high-impact decisions were resolved in discovery and are captured in the proposal and D1–D8.

## Implementation Plan

Sequenced so each step builds and tests green in isolation. Exact validation commands: `dotnet build .\FusionCanvas.sln` and the focused `dotnet test` filters per step; full `dotnet test .\FusionCanvas.sln` at the end.

1. **Domain score** — Add `src/FusionCanvas.Domain/Concepts/DesignTriangleScore.cs` per D1. Tests: `tests/FusionCanvas.Domain.Tests/Concepts/DesignTriangleScoreTests.cs` (empty → 0; all substantive → 100; short corner half credit; monotonic growth; whitespace handling).
2. **Guidance source** — Add `IDesignTriangleGuidanceSource` to `src/FusionCanvas.Application/ConceptRefinement/`; add `src/FusionCanvas.Integration/AI/DesignTriangleGuidance.md` (placeholder per D4) as `EmbeddedResource` and `EmbeddedDesignTriangleGuidanceSource`. Tests: `tests/FusionCanvas.Integration.Tests/AI/EmbeddedDesignTriangleGuidanceSourceTests.cs` (loads non-empty placeholder mentioning idea/phrase/graphic).
3. **Application service** — Add the D2 types + `ConceptRefinementService` with D3 prompt/parse logic, using `ItemMetadataCodec.NormalizeSingleLine` for Phrase. Add `IConceptRefinementAccessStatus` + `ConfiguredConceptRefinementAccessStatus`. Tests: `tests/FusionCanvas.Application.Tests/ConceptRefinement/` with a capturing fake `IAiTextGenerationService` and in-memory repository: initialize parse success/malformed failure; fine-tune/change value extraction, label/quote stripping, phrase single-line normalization; empty-result failure; availability mapping; payload includes guidance + creative context and excludes identifiers/timestamps/paths (assert on captured `AiTextRequest` messages).
4. **Session view model** — Add `ConceptRefinementSessionViewModel` + `ConceptRefinementHistoryEntry` per D5/D6, wired to `ItemInspectorViewModel` (draft properties, `CommitEditsAsync`, `Saved`, `LoadedItemId`). Expose as `MainWindowViewModel.ConceptRefinement`; compose in `AppWorkspaceFactory` (new runtime members: `ConceptRefinement`, `ConceptRefinementAccess`) and the `MainWindowViewModel` constructor chain, mirroring how `IdeationAccess` flows today. Tests: `tests/FusionCanvas.App.Tests/ConceptRefinementSessionViewModelTests.cs` (framework-free, fake service + inspector test doubles): apply success appends one entry and commits; failure keeps state + error; busy disables commands; item switch cancels and late result not applied; rollback restores drafts without new entry; post-rollback truncation; manual-commit entries via Saved; score updates on draft changes; availability gating states.
5. **UI section** — Add the D7 markup to the Concept border in `src/FusionCanvas.App/Views/MainWindow.axaml`. Headless view tests in `tests/FusionCanvas.App.Tests/MainWindowLayoutTests.cs` (or a new `ConceptRefinementViewTests.cs` following the existing headless fixture): section visible only for Concept stage; actions disabled with guidance when unavailable; per-corner accessible names; initialize preconditions reflected in enabled state; history list renders and rollback is keyboard-operable; read-only review disables everything; score text updates.
6. **Settings refresh hook** — Re-evaluate concept-refinement availability alongside the existing post-settings Ideation refresh (D8). Covered by the step-4/5 tests where observable.
7. **Verification artifacts** — Maintain `verification.md` mapping every acceptance scenario in `specs/concept-refinement/spec.md` to its test(s) or headless check; run `openspec validate add-concept-refinement-tool --strict`, `openspec validate --all --strict`, `dotnet build .\FusionCanvas.sln`, `dotnet test .\FusionCanvas.sln`.

Compatibility notes: no persistence changes; `InternalsVisibleTo` already covers App/Application test access where needed; keep nullable-clean and warning-free per `docs/coding-standard.md`.

Decisions the implementer must not reopen: D1 score formula; D2 service boundary (draft values passed in); D3 prompt/parse contract; D4 embedded guidance packaging; D5 history/session lifetime and reset rules; D6 commit integration; D7 placement/composition; manual-only initialization; visible-but-disabled unavailable pattern.

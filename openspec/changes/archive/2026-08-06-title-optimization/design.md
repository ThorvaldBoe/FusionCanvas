## Context

Issue #115: auto-created titles are set to the first sentence of the full generated idea (`IdeationService.CreateAsync` → `FirstSentence`), so they become long and crowded in the navigation pane. The module adds an `Optimize` command beside the **Working title** field in the listing inspector Overview that produces a short, store-unique title from the item's own creative content.

The app already has a provider-independent AI text boundary: `IAiTextGenerationService.GenerateAsync(AiTextRequest)` and `GetAvailabilityAsync(AiRequestPurpose)`, backed by OpenRouter, with purposes `General`, `Ideation`, and `Concept`, and availability kinds (`MissingModel`, `MissingCredential`, `CredentialUnavailable`, `InvalidConfiguration`, `Ready`) over `AiAvailabilityResult`. The Concept refinement module (`ConceptRefinementService` + `ConceptRefinementSessionViewModel`) is the closest existing analog: a per-item AI action with availability gating, request assembly from creative context while excluding operational/secret data, single-operation concurrency with cancellation, and committing applied values through the inspector's automatic-save path.

Item content lives in `Item.MetadataJson` via `ItemMetadataCodec` keys: `idea`, `concept.idea`, `phrase`, `graphicDirection`. The item's working title is `Item.Name`; store scope comes from `Item.StoreId`; archive/status from `Item.IsArchived`/`ItemStatus`. The inspector (`ItemInspectorViewModel`, `MainWindow.axaml` Overview) already owns the title field, autosave-on-lost-focus, a `Run(...)` async-command helper, an error line, and a `_isBusy` flag.

## Goals / Non-Goals

**Goals:**
- One `Optimize` command next to the Working title that shortens/creates a unique title from the item's content.
- Store-wide uniqueness enforced through a bounded AI loop that prefers disambiguating words and falls back to a numeric suffix only for genuinely identical data.
- Immediate overwrite + persist through the existing automatic-save path.
- Disabled-with-tooltip when the Title AI purpose is unavailable or there is no content to draw from.
- At most one in-flight operation per item, cancelled on item/context switch, never applying late results.
- Deterministic, framework-free tests for collision/termination policy; deterministic service tests for the loop; targeted headless view tests for gating/busy/keyboard.

**Non-Goals:**
- Marketplace/SEO title generation or multi-candidate pickers.
- Changing how Ideation initially seeds titles (that behavior is untouched).
- Listing copy/description optimization beyond the working title.
- New persistence schema or NuGet dependencies.

## Decisions

### D1 — Placement: `Optimize` beside the Working title in the Overview
A compact button is placed directly to the right of the Working title `TextBox` in the Overview `Border` (MainWindow.axaml:639-648), turning the single field row into a horizontal `StackPanel`/`Grid` of Field + Button. Rationale: issue #115 asks for "a button next to the overview text field"; the Overview is always visible across stages; keeps the action on the object it affects (UX: "Place commands near the object"). Alternative (Ideation-style tool) rejected — the request is a per-item inspector action, not a trackable multi-candidate surface.

### D2 — New `AiRequestPurpose.Title` reusing the General profile
A new `Title` value is added to `AiRequestPurpose` and routed through the existing `IAiTextGenerationService`. `Title` reuses the existing General purpose profile: `AiConfigurationResolver.ProfileFor` is extended to map `Title` to `settings.General` (mirroring the non-advanced `General` branch), so availability and profile resolution inherit the existing General settings without a new configurable profile. No AI settings UI, persistence-shape, or `ai-text-generation` spec change is needed. Alternative (a dedicated `Title` profile like `Ideation`/`Concept`) rejected for this module: it would expand scope into AI settings UI/persistence and an `ai-text-generation` delta; the General profile is sufficient and the decision can be revisited later without a spec change.

### D3 — Uniqueness scope: active items in the same store
Collisions are tested case-insensitively (trimmed) against the `Name` of every item whose `StoreId` equals the active item's, **excluding** the active item itself, archived items, and items whose lifecycle status is `Rejected` — matching the `IdeationService.AssembleContext` exclusion of inactive ideas. This mirrors what the user sees in normal navigation. Alternatives considered and rejected:
- Whole repo / all items including archived — would collide against invisible context and block legitimate reuse.
- Same niche/group only — issue says "the whole store."
- Including `Rejected` items — a rejected (inactive) item should not force a numeric suffix on a live item.

### D4 — Bounded uniqueness loop, numeric suffix fallback
Algorithm per `Optimize` invocation:
1. Assemble creative content from item metadata (idea, concept.idea, phrase, graphicDirection) + creative context (as Concept refinement does, excluding operational/secret fields).
2. `AskAI` for a short title (Title purpose).
3. If candidate unique vs `D3` set → accept.
4. Else if attempts remain → `AskAI` to add one distinguishing word; repeat step 3.
5. Else (bound reached, persistence of collision = genuinely identical data) → append the smallest unused integer suffix (`2`, `3`, …) to the last candidate; the suffixed title is then unique against the store.

`MaxAttempts` (default **4** AI calls total: 1 initial + up to 3 refinement rounds) is a Domain constant; a single operation is therefore always bounded. The disambiguation-preferred-over-numbers behaviour and the numeric fallback are computed deterministically in Domain (`TitleUniquenessPolicy`): `HasCreativeContent(metadata)` (non-whitespace Idea, or at least one of Concept idea/Phrase/Graphic direction — used by both the availability gate and the orchestrator), `IsUnique`, `DistinctExistingTitles` (case-insensitive, excluding the active item, archived items, and `Rejected` items), and `WithNumericSuffix`. Alternatives: hard "always add number on collision" — rejected, contradicts issue guidance; unbounded loop — rejected (cost/latency/safety).

The numeric-suffix fallback fires **unconditionally when the bound is reached**, regardless of why the collision persists (the model may keep emitting colliding titles for non-identical items); "genuinely identical data" is the canonical motivating case, not a precondition, and no identity check is performed before applying the suffix. This module treats "short" as a prompt-level instruction only and enforces no hard maximum title length; length policy is deferred to a later module.

### D5 — Immediate overwrite + persist via automatic-save path
The accepted, normalized single-line title is written into `ItemInspectorViewModel.Title` and committed through the existing stage-aware expected-state guard + autosave path (the same path a field-exit edit or Concept refinement application uses), with no explicit save action. Failure of the commit keeps the draft and reports a recoverable inline error. Alternative (draft field pending focus-loss) rejected: user asked for immediate overwrite + persist.

### D6 — Availability gating + single-operation concurrency/cancellation
`Optimize` is enabled only when `GetAvailabilityAsync(Title)` is `Ready`, the item has non-whitespace creative content (via the Domain `HasCreativeContent` predicate), **and** the item's Working title is editable (the item is not archived and not effectively inactive through archived ancestry). While unavailable it stays visible, disabled, with tooltip guidance (to AI settings / content required / restore), mirroring Concept refinement and the listing-inspector read-only rule. Good at most one operation per document: while running the command is disabled and the Working title field is non-editable (so no autosave is triggered for edits in that window); the operation is tied to a `CancellationTokenSource` cancelled on item switch/close (same ownership as the existing in-flight commit semantics and Concept refinement). A late/deserialized result is never applied. Availability is re-evaluated after AI settings change using the same observation mechanism Concept refinement uses.

## Risks / Trade-offs

- **[Token cost + latency of repeated AI calls]** → bounded `MaxAttempts`; each refinement prompt tells the model to keep the title short, and the loop stops on the first unique candidate.
- **[AI may repeatedly emit colliding or non-short titles]** → deterministic fallback to a numeric suffix guarantees termination and uniqueness regardless of model behaviour, fired unconditionally when the bound is reached. No hard length cap is enforced in this module; "short" is a prompt instruction (length policy deferred).
- **[Archived/non-scoped collisions ignored]** → accepted trade-off for navigability; the whole-store scope still catches the realistic "variants of the same theme" case.
- **[Race with concurrent item edits]** → the persistence commit reuses the stage-aware expected-state guard; a stale commit is rejected inline and keeps the draft.
- **[Unavailable AI blocks the feature]** → the command stays visible and disabled with actionable guidance, consistent with Concept refinement; no crash path.

## Migration Plan

No schema, data, or dependency migration. A new `AiRequestPurpose.Title` enum member is additive. No rollback considerations beyond reverting the feature branch.

## Open Questions

None that block implementation. `MaxAttempts = 4` is a reasoned default; it is a Domain constant and can be revisited without spec change if AI quality demands.

## Implementation Plan

### Affected layers and likely files

**Domain** (new, deterministic, framework-free):
- `src/FusionCanvas.Domain/Items/TitleUniquenessPolicy.cs` — constants `MaximumAttempts = 4`; `HasCreativeContent(metadata)` (non-whitespace Idea, or at least one of Concept idea/Phrase/Graphic direction); `IsUnique(candidate, existing)`; `DistinctTitles(IEnumerable<Item>, storeId, activeItemId)` returning the case-insensitive collision set (excluding the active item, archived items, and `Rejected` items); `WithNumericSuffix(candidate, existing)` returning the smallest unused `candidate N` (e.g. `2`).
- Tests: `tests/FusionCanvas.Domain.Tests/Items/TitleUniquenessPolicyTests.cs` (including `HasCreativeContent` cases).

**Application** (new orchestration + contract):
- `src/FusionCanvas.Application/TitleOptimization/TitleOptimizationRequest.cs` — `ItemId`, `Guid StoreId` (resolved from item), plus resolved creative context inputs.
- `src/FusionCanvas.Application/TitleOptimization/TitleOptimizationResult.cs` — success (`Title`) or a category + message (mirror `AiTextResult`/failure categories).
- `src/FusionCanvas.Application/TitleOptimization/ITitleOptimizationService.cs` — `Task<AiAvailabilityResult> GetAvailabilityAsync(CancellationToken)`, `Task<TitleOptimizationResult> OptimizeAsync(ItemId, CancellationToken)`.
- `src/FusionCanvas.Application/TitleOptimization/TitleOptimizationService.cs` — orchestrator: resolve item from `IWorkspaceRepository`, guard with `TitleUniquenessPolicy.HasCreativeContent`, assemble creative content + context (reuse the sanitize/exclude pattern from `IdeationService.AssembleContext`), run the D4 loop via `IAiTextGenerationService` with `Title` purpose, feed results through `TitleUniquenessPolicy`.
- `src/FusionCanvas.Application/AI/AiRequestPurpose.cs` — add `Title`.
- `src/FusionCanvas.Application/AI/AiConfigurationResolver.cs` — extend `ProfileFor` to map `Title` to `settings.General` (so `GetAvailabilityAsync(Title)` resolves via the existing General profile instead of throwing).
- Tests: `tests/FusionCanvas.Application.Tests/TitleOptimization/TitleOptimizationServiceTests.cs` (deterministic fake `IAiTextGenerationService`).

**App** (view model + view):
- `src/FusionCanvas.App/Items/ItemInspectorViewModel.cs` — inject `ITitleOptimizationService`; add `Optimize` command (via the existing `RelayCommand` + `Run(...)` helper), `OptimizeAvailability`/`CanOptimize` + tooltip, busy state that disables the field+button while running (Working title non-editable during the operation, so no autosave fires for edits in that window), inline error line, and a `CancellationTokenSource` cancelled when the active item changes or the document closes; gate `CanOptimize` on Title-purpose readiness, the Domain `HasCreativeContent` predicate, and the existing `CanEditShared` editability flag; commit the accepted title through the existing autosave/expected-state path.
- `src/FusionCanvas.App/Views/MainWindow.axaml` — put an `Optimize` button next to the Working title field with `AutomationProperties.Name="Optimize title"` and `ToolTip` bound to the availability guidance; wire disabled/busy states to theme resources.
- `src/FusionCanvas.App/Views/MainWindow.axaml.cs` — no change beyond existing LostFocus handling unless needed for focus return after optimize.
- Tests: `tests/FusionCanvas.App.Tests/ItemInspectorViewModelTests.cs` (optimize command, availability/guidance, busy, cancellation, commit) and a focused headless view test (e.g. in `MainWindowLayoutTests.cs`) for button presence, disabled-with-tooltip, and keyboard/document order.

### Responsibility placement
- Domain owns collision + termination + numeric-suffix policy (pure, deterministic).
- Application owns the orchestration loop, prompt assembly, secret/operational-data exclusion, and availability translation — capped by Domain's `MaximumAttempts`.
- App owns only presentation, user intent, and reusing the existing autosave commit; it holds no title business logic.

### Data/persistence & UI behavior
- No new persistence. UI: Overview row becomes Field + `Optimize` button; button disabled when `CanOptimize` is false (busy, no content, or AI unavailable); inline error under the action; busy indicator on the button while running.

### Algorithm and edge cases
- Iterate steps in D4; cancel token between calls; if any AI call fails before acceptance → return failure, no overwrite.
- If the item's creative content is entirely absent → command stays disabled (D6, via `HasCreativeContent`) so the orchestrator is not reached; the service also guards defensively.
- If the item is archived or effectively inactive → command stays disabled (D6) via the existing `CanEditShared` editability flag.
- Normalize accepted title to one line (`ItemMetadataCodec.NormalizeSingleLine`) before committing. No hard length cap is enforced (prompt-only).

### Sequencing
1. Domain policy + tests.
2. `AiRequestPurpose.Title` + Application service + tests.
3. View-model wiring (command, gating, cancellation, commit) + tests.
4. View (button) + headless view tests.
5. `dotnet test .\FusionCanvas.sln` baseline + `openspec validate`.

### Verification mapping (acceptance → method)
- **Placement/keyboard/accessibility/theme scenarios** → Avalonia headless view tests (MainWindowLayoutTests) + VM-level is-not-shown checks.
- **Availability gating scenarios** (ready/not-configured/no-content/read-only-archived/refresh) → VM tests with faked availability; one headless view assertion for disabled-with-tooltip.
- **Generation + operational/secret exclusion** → Application service tests asserting prompt content with faked AI.
- **Uniqueness loop / archived / rejected / own-title scenarios** → Domain `TitleUniquenessPolicyTests` + Application loop tests.
- **Numeric-suffix fallback (identical and non-identical data) / bounded attempts** → Domain tests + Application loop test using a fake AI that always collides.
- **Immediate overwrite + persist / one-line normalization** → VM/service tests through the persisted repo (persistence boundary) + inspector autosave path.
- **One operation / field-lock while running / cancellation / failure-leaves-unchanged** → VM + Application cancellation tests; headless view assertion of disabled-while-running and non-editable field.

Overall the module stays within one change: outcome is a single user-facing command, its scope (uniqueness loop), its gating, and its persistence are one cohesive verifiable surface.

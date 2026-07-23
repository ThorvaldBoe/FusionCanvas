# Design: fix-main-window-usability

## Context

Four defects on the main workspace surface (`MainWindow.axaml` + `MainWindowViewModel` + `ItemInspectorViewModel`), all shipped with `basic-product-creation-workflow` (still active, 68/76 tasks):

1. The navigation filter row is one `Grid` with `ColumnDefinitions="*,Auto,Auto,Auto,Auto"` (`MainWindow.axaml:133`). At the pane's `MinWidth` of 240 (204px content after padding) the two `MinWidth=92` selectors, the Filters button, and spacing cannot fit; the row clips beyond the pane.
2. The details column is a vertical `StackPanel` (`MainWindow.axaml:444`) that measures children with infinite height, so the per-section `ScrollViewer`s (`:560`, `:1038`) never engage. Content past the window bottom is unreachable. The archived FC-0106 design called for "sectioned layout in a single scroll container"; the in-progress change's own task 6.12 claims minimum-height reachability and task 9.8 will verify it — this is a bug, not new behavior.
3. Item text fields lost their `LostFocus` commit wiring in `8364c83` (it survives on group fields, `:1067-1084`) as part of the deliberate explicit-Save reversal. The product owner has now re-approved automatic save after real-use friction (see proposal). The prior automatic-save model (`add-inline-details-editing`, complete but unarchived) is the reference: persist on field exit, commit silently on context change, revert an invalid title while other valid edits persist, Tags independently immediate.
4. The Item document header `Grid` (`:446`, `*,Auto,Auto`) has no `ColumnSpacing`, so the stage label touches the status selector.

Constraints: hand-rolled MVVM with compiled bindings; Avalonia; headless view tests are the accepted baseline for view-level behavior; no business logic in the App layer beyond presentation state; `dotnet test .\FusionCanvas.sln` must stay green.

## Goals / Non-Goals

Goals: the four fixes above, delivered as one small module with one verification pass; the save-model reversal recorded durably so the third discussion starts from this record.

Non-goals: debounced save-as-you-type; changes to group details, Tag immediacy, lifecycle confirmations, filter contents/semantics, or the window's minimum size; spec deltas for the layout fixes; touching `basic-product-creation-workflow`'s artifacts (coordination is recorded in the proposal, not executed here).

## Decisions

### 1. Filter row: search box on its own row, remaining controls in a WrapPanel

Replace the single five-column Grid with two rows: the search `TextBox` spans the full pane width on row one (it is the persistently visible primary control per the search-filtering spec); row two is a `WrapPanel` holding, in order, the conditional "Clear tags" button, the stage selector, the status selector, and the Filters flyout button. When the pane is narrow, controls wrap onto additional lines exactly as the user requested; nothing leaves the pane bounds. Selectors keep `MinWidth=92` and their tooltips/classes; keyboard reachability and the flyout are unchanged.

Alternative considered: a fixed two-row Grid with star-sized selector columns. Rejected: it consumes the extra vertical line even when the pane is wide, and shrinking selectors below their usable width clips their text instead of reflowing.

### 2. One outer ScrollViewer around the whole details column

Wrap the `StackPanel` bound to `DocumentWindow.HasActiveDocument` (`:444`) in a `ScrollViewer` with `VerticalScrollBarVisibility="Auto"`, so the header, stage tool host, Item inspector, group details, confirmation banners, and the stage-move row scroll together as one surface. Remove the inner `ScrollViewer`s at `:560` and `:1038` (their content StackPanels stay). The `MinHeight=360` section borders remain — inside a real scroll container they no longer trap content. The empty-state and selection-summary siblings are untouched. Nested vertical scroll viewers are deliberately avoided: the inner one never engages inside a StackPanel and would split wheel/keyboard scrolling.

### 3. Automatic save: restore field-exit commit and silent leave-commit, with a serialized commit drain

**View wiring.** Add `LostFocus="OnDetailsFieldLostFocus"` to the six Item text fields (working title, Original idea, Concept idea, Phrase, Graphics description, Notes). The existing handler calls `CommitActiveDetailsEdits()`, which already covers the item inspector. The Tag input is not wired (Tags are immediate via their own commands).

**Commit semantics (`ItemInspectorViewModel`).** `CommitEditsAsync` stays the single commit entry point and keeps: no-op when clean or read-only, the stage-aware save request, baseline refresh, and `Saved` on success. Two changes:

- *Validation:* a title containing line breaks no longer rejects the whole save. The title reverts to its last saved baseline, the remaining valid edits persist, and an inline error explains the revert (restores the previously accepted rule and `docs/ui-guidelines.md` Details-Pane-Editing language). Blank titles remain allowed (optional working title with fallback labels, per the active workflow spec).
- *Serialization:* commits funnel through a drain so overlapping triggers are safe. If a commit is requested while another is in flight, one follow-up pass is queued; the returned `Task` completes only when the drain empties (no pending edits or a failed commit). This closes the race where blur-commit is still running when a transition is requested — the transition awaits the same drain instead of evaluating the draft mid-save. The stage-aware expected-state guard in the save path remains the protection against stale overwrites.

**Leave guard (`MainWindowViewModel.GuardActiveItemInspectorLeave`).** All transitions already funnel through this one method (navigation open, tree selection in tab/current tab, tab select/close, active-view-stage select, stage move forward/back, status change, archive/restore/delete requests). It stops prompting and instead: runs the item commit drain, then proceeds when no unsaved changes remain; when changes remain (validation or persistence failure), the transition is aborted, the draft and inline error stay, and — when the aborted transition was a status selection — the status selector is reverted to the persisted status by raising the `SelectedItemStatus*` properties (the reset currently done by `CancelPendingTransition`). The group-details commit branch is unchanged. `CommitActiveDetailsEdits` (blur path) stays fire-and-forget; only transitions await the drain.

**Removed surface.** Save/Discard buttons and their `SaveCommand`/`DiscardCommand`/`DiscardChanges`; the unsaved-changes prompt banner in AXAML; `IsUnsavedChangesPromptVisible`, `UnsavedChangesPromptMessage`, `_pendingTransition`, `SaveAndContinue*`/`DiscardAndContinue`/`CancelPendingTransition*` in the view model; the prompt-focus handler in `MainWindow.axaml.cs` (the status-confirmation focus branch stays). `OnImportDesignFiles` currently focuses `SaveItemButton` after import — it will focus the invoking Import button instead, keeping focus in the Design tool.

Alternative considered: keep explicit Save alongside blur-autosave. Rejected: two owners for one action (violates the one-clear-owner UX guideline) and the prompt would still flash whenever a transition lands mid-commit. Alternative considered: debounced save-as-you-type. Rejected: partial writes, harder failure semantics, and more machinery than the friction warrants.

### 4. Header spacing

Add `ColumnSpacing="8"` to the Item document header Grid (`:446`), matching the dense-spacing visual guidance. No selector or binding changes.

### 5. Docs stay in sync

Remove the "Item workflow explicit Save exception" section from `docs/ui-guidelines.md` and fold the Tags-immediacy sentence into the Details Pane Editing section, which once again describes Item fields. This is the durable record that supersedes `basic-product-creation-workflow/design.md` §9's rejected alternative.

## Risks / Trade-offs

- [Blur-commit in flight when a transition fires → prompt-era race returns as a lost or double write] → serialized commit drain awaited by the leave guard; covered by view-model tests that start a slow commit, request a transition, and assert exactly one final persisted state.
- [LostFocus does not fire when focus never moves (e.g., app deactivation, OS-level close) → edits unsaved] → accepted residual, identical to the previously accepted autosave model; every in-app transition path is covered by the leave guard. No window-close prompt is added (out of scope).
- [`basic-product-creation-workflow` syncs or archives after this change and re-applies the explicit-save delta] → coordination recorded in the proposal: recommended archive order is basic first, then this change, with `openspec validate` re-run at sync; its remaining verification rows for Item text saving are re-mapped here.
- [WrapPanel changes the filter row's height at narrow widths → pane sections shift] → the row lives in a `DockPanel` with the tree filling remaining space, so growth only shrinks the tree viewport, which already scrolls.
- [Removing the prompt changes keyboard flows verified by basic's task 9.8] → headless view tests assert the new commit flows; status-confirmation and lifecycle confirmations keep their explicit keyboard-reachable buttons.

## Migration Plan

No data, schema, or persisted-format changes — only when writes happen. Rollback is a plain revert of the change; no user data depends on it.

## Open Questions

None. Save model, packaging, and the four fixes were resolved during discovery with the product owner.

## Implementation Plan

Affected layers: App only (view, code-behind, view models). No Domain, Application, or Integration changes; the stage-aware save service (`IItemInspectorService.SaveStageAsync`) is reused as-is.

Sequencing (each step keeps the build green; tests move with their step):

1. **`ItemInspectorViewModel` commit semantics** — serialized drain, title-revert validation, remove `SaveCommand`/`DiscardCommand`/`DiscardChanges`. Rewrite `tests/FusionCanvas.App.Tests` inspector tests for automatic save (commit, no-op clean, title revert with others persisted, failure keeps draft, drain overlap, Saved event).
2. **`MainWindowViewModel` leave guard** — commit-then-proceed with abort-on-failure and status-selector revert; delete the prompt state machine and its commands. Rewrite the guard/tab/stage/status view-model tests.
3. **View wiring and removals** — `LostFocus` on the six Item fields; remove the prompt banner, Save/Discard row, and prompt focus handler; repoint the post-import focus to the Import button.
4. **Scroll container** — outer `ScrollViewer`, inner two removed. Headless view test: at minimum window height with an Item open, the details `ScrollViewer` reports extent beyond viewport and the Lifecycle section is reachable by scrolling to the end.
5. **Filter wrap + header spacing** — two-row filter layout with `WrapPanel`; `ColumnSpacing="8"` on the header Grid. Headless view tests: filter controls' bounds stay within the pane at minimum pane width and wrap onto more than one line; header label/selector have a measurable horizontal gap; each Item text field carries the focus-exit commit wiring (focus, edit, refocus → commit observed on a test double).
6. **Docs** — `docs/ui-guidelines.md` exception removal.
7. **Verification** — `dotnet test .\FusionCanvas.sln` green, strict `openspec validate`, and `verification.md` mapping every delta scenario plus the three no-delta fixes to evidence. Optional live desktop pass is ad hoc on a disposable workspace, not a gate.

Decisions not to reopen in this module: automatic-save model (second reversal, owner-approved, recorded above); Tags staying immediate; lifecycle/status confirmations staying explicit; no window-close prompt.

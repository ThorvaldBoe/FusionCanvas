# Tasks: fix-main-window-usability

## 0. Headless view-test harness

- [x] 0.1 Add `Avalonia.Headless.XUnit` 12.0.4 (matching the app's Avalonia 12.0.4) to `tests/FusionCanvas.App.Tests`, add a test-app fixture reusing `Program.BuildAvaloniaApp()` with `UseHeadless()` and `WithInterFont()`, add a reusable per-test `MainWindowFixture` (build/show/layout/teardown with in-memory workspace), and representative production-view tests covering construction, compiled bindings, layout/control state across item/group/niche/stage states, and keyboard/pointer focus/input.
- [x] 0.2 Verify `dotnet test` runs headless view tests without an interactive desktop and the representative tests pass.

## 1. Item inspector commit semantics

- [x] 1.1 Rewrite `ItemInspectorViewModelTests` for automatic save: commit persists valid title/current-stage/Notes edits, no-op when clean, multi-line title reverts to baseline with inline error while other valid edits persist, Phrase normalizes to one line, persistence failure keeps the draft and reports inline, and metadata/provenance preservation is unchanged.
- [x] 1.2 Change `ItemInspectorViewModel.CommitEditsAsync` validation so a multi-line title reverts to its baseline and the remaining valid edits still save, instead of rejecting the whole commit.
- [x] 1.3 Add the serialized commit drain to `ItemInspectorViewModel`: a commit requested while another is in flight queues one follow-up pass, and the returned Task completes only when the drain empties; cover overlap with a slow fake service test asserting the final persisted state and no stale overwrite.
- [x] 1.4 Remove `SaveCommand`, `DiscardCommand`, and `DiscardChanges` from `ItemInspectorViewModel` and update all callers and tests.

## 2. Leave guard commits instead of prompting

- [x] 2.1 Rewrite `MainWindowViewModel` guard tests: Item selection, tab open/select/close, active-view-stage select, stage move, status change, and archive/restore/delete requests each commit pending edits before the transition without a prompt; a failed commit aborts the transition, preserves the draft with an inline error, and reverts the status selector when the aborted transition was a status selection; a clean draft transitions immediately with no write.
- [x] 2.2 Rewrite `GuardActiveItemInspectorLeave` to await the item commit drain and proceed only when no unsaved changes remain; keep the group-details commit branch unchanged.
- [x] 2.3 Delete the prompt state machine: `IsUnsavedChangesPromptVisible`, `UnsavedChangesPromptMessage`, `_pendingTransition`, `SaveAndContinueCommand`, `DiscardAndContinueCommand`, `CancelPendingTransitionCommand`, and the `SaveAndContinueAsync`/`DiscardAndContinue`/`CancelPendingTransition` methods; move the status-selector property reset into the abort path.

## 3. View wiring and removals

- [x] 3.1 Add `LostFocus="OnDetailsFieldLostFocus"` to the six Item text fields in `MainWindow.axaml` (working title, Original idea, Concept idea, Phrase, Graphics description, Notes); leave the Tag input unwired.
- [x] 3.2 Remove the Save/Discard button row and the unsaved-changes prompt banner from `MainWindow.axaml`; remove the prompt-focus branch in `MainWindow.axaml.cs` (keep the status-confirmation branch); repoint the post-import focus from `SaveItemButton` to the invoking Import button.
- [x] 3.3 Build and run the app test suite; resolve fallout from the removed members.

## 4. Scrollable details column

- [x] 4.1 Wrap the details `StackPanel` (`DocumentWindow.HasActiveDocument`) in an outer `ScrollViewer` with `VerticalScrollBarVisibility="Auto"` and remove the inner `ScrollViewer`s around the Item inspector and group details content.
- [x] 4.2 Add a headless view test: at the minimum window height with an Item open, the details ScrollViewer's extent exceeds its viewport and scrolling to the end brings the Lifecycle section within the visible bounds.

## 5. Filter row wrap and header spacing

- [x] 5.1 Restructure the navigation filter row: search TextBox on its own full-width row; "Clear tags", stage selector, status selector, and Filters button in a `WrapPanel` below, keeping existing classes, tooltips, minimum widths, and the flyout.
- [x] 5.2 Add a headless view test: at the minimum navigation pane width, all filter controls remain within the pane bounds and occupy more than one line, and every control stays keyboard reachable.
- [x] 5.3 Add `ColumnSpacing="8"` to the Item document header Grid and cover it with a headless view test asserting a horizontal gap between the stage label and the status selector.
- [x] 5.4 Add a headless view test: focusing an Item text field, editing, and moving focus away triggers a commit on the inspector test double (wiring regression guard for all six fields).

## 6. Docs and verification

- [x] 6.1 Remove the "Item workflow explicit Save exception" section from `docs/ui-guidelines.md` and fold the Tags-immediacy note into the Details Pane Editing section.
- [x] 6.2 Run `dotnet test .\FusionCanvas.sln` and fix all failures.
- [x] 6.3 Run strict `openspec validate`; write `verification.md` mapping every `listing-inspector` delta scenario and each of the three no-delta fixes (filter wrap, scroll reachability, header spacing) to its test evidence, and record the `basic-product-creation-workflow` coordination note.

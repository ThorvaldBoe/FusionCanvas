## 1. Dialog state and lifecycle

- [x] 1.1 Add Design Area dialog mode/title/request state and owner notification to `CatalogSetupViewModel`.
- [x] 1.2 Add meaningful Design Area draft snapshot tracking and discard/keep-editing commands covering all fields and compatibility choices.
- [x] 1.3 Ensure successful save, confirmed cancel, Offering change, and workspace change end the draft while failed save preserves it.

## 2. Focused editor presentation

- [x] 2.1 Add the resizable, scrollable, accessible `DesignAreaEditorWindow` containing the complete existing form and action row.
- [x] 2.2 Route Cancel, Escape, and window close through the meaningful-draft safeguard and close only after the draft ends.
- [x] 2.3 Wire `StoreEditorWindow` as modal owner with single-dialog enforcement and Add/Edit focus restoration.
- [x] 2.4 Remove the inline editor column and let the Design Area collection use the full management surface.
- [x] 2.5 Reconcile `manage-design-areas.ui.yaml` with the list-only default and focused Add/Edit dialog.

## 3. Focused tests and evidence

- [x] 3.1 Add ViewModel tests for add/edit mode, populated/default baselines, meaningful changes, prompt decisions, failure preservation, and context reset.
- [x] 3.2 Add Avalonia headless tests for list-only layout, modal ownership, bindings, focus, Save/Cancel/Escape/close, stale context, and supported sizes.
- [x] 3.3 Correct artifacts if implementation evidence exposes a mismatch and create criterion-level `verification.md`.

## 4. Completion gates

- [x] 4.1 Run focused App tests and correct failures.
- [x] 4.2 Run `dotnet test .\FusionCanvas.sln` and record the result.
- [x] 4.3 Run strict OpenSpec validation for specs and changes and record the result.
- [x] 4.4 Complete the learning review and retrospective for archive.

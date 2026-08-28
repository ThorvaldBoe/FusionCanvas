## 1. Dialog state and lifecycle

- [x] 1.1 Add Mockup Template dialog mode/title/request state and owner notification to `CatalogSetupViewModel`.
- [x] 1.2 Add a complete meaningful-draft baseline and discard/keep-editing commands for identity, provider, Design Area, Colors, and placement.
- [x] 1.3 Ensure save success, confirmed cancel, Offering/workspace changes, and archived state end the draft while failed save preserves it.

## 2. Focused editor presentation

- [x] 2.1 Add the resizable, scrollable, accessible preview-first `MockupTemplateEditorWindow` with complete configuration and action regions.
- [x] 2.2 Route Cancel, Escape, and window close through the meaningful-draft safeguard and close only after the draft ends.
- [x] 2.3 Wire `StoreEditorWindow` as single modal owner with stable Add/Edit focus restoration.
- [x] 2.4 Remove the inline editor region and let the Offering-scoped template collection use the full management surface.
- [x] 2.5 Preserve archived-store read-only gating in both the parent action and dialog controls.
- [x] 2.6 Reconcile `manage-mockup-templates.ui.yaml`, semantic/layout tests, and generated fixtures with collection-only and focused-dialog states.

## 3. Focused tests and evidence

- [x] 3.1 Add ViewModel tests for add/edit baselines, meaningful changes, prompt outcomes, failed save, context reset, and read-only entry.
- [x] 3.2 Add Avalonia headless tests for modal ownership, bindings, preview mapping, focus, Save/Cancel/Escape/close, supported sizes, and archived state.
- [x] 3.3 Correct artifacts if implementation evidence exposes a mismatch and create criterion-level `verification.md`.

## 4. Completion gates

- [x] 4.1 Run focused App and UI-description tests and correct failures.
- [x] 4.2 Run `dotnet test .\FusionCanvas.sln` and record the result.
- [x] 4.3 Run strict OpenSpec validation and `git diff --check`, then record the result.
- [x] 4.4 Complete the learning review and retrospective for archive confirmation.

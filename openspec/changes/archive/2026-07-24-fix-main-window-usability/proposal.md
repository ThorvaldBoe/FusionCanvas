# Proposal: fix-main-window-usability

## Why

Four usability defects on the main workspace surface shipped with the basic product creation workflow: the navigation filter row clips beyond the pane width, the Item details surface cannot be scrolled when the window is short, Item text fields lost their automatic save so routine idea capture is interrupted by a save prompt, and the header stage label crowds the lifecycle-status selector. The auto-save piece reverses the recorded "explicit Save" decision: real use shows that field-by-field automatic save is the lower-friction behavior for high-frequency creative text entry, matching the behavior group details already have.

## What Changes

- **Navigation filter row wraps**: the search box, stage selector, status selector, tag-clear button, and Filters flyout command reflow onto additional lines when the navigation pane is too narrow instead of clipping beyond the pane edge. Same controls, same flyout, same filter semantics.
- **Item document surface scrolls as one container**: the details column (header, stage tool host, Item inspector sections, group details, stage-move row) lives in a single outer `ScrollViewer` so every section — including Related assets and Lifecycle at the bottom — is reachable at the minimum supported window height. The non-functional per-section inner scroll viewers are removed.
- **Item text fields auto-save again (BREAKING behavior change)**: working title, current-stage text (Idea, Concept idea/Phrase/Graphics description), and Notes persist automatically when the edited field loses focus, and pending edits commit silently before Item, tab, active-view-stage, or lifecycle transitions. The explicit Save/Discard buttons and the Save/Discard/Cancel unsaved-changes prompt are removed. An invalid title reverts to its last saved value with an inline explanation while other valid edits still persist; persistence failures keep the draft and report inline. Tags remain independently immediate; archive, restore, delete, moves, and status changes stay explicit and confirmed.
- **Header spacing**: a small gap separates the stage label from the lifecycle-status selector in the Item document header.

This reverses the decision recorded in `basic-product-creation-workflow/design.md` §9 ("blur-autosave all fields. Rejected … the user approved explicit Save"). The reversal is deliberate and approved by the same product owner after real use: being prompted while capturing a new Item's idea is friction the explicit-save model was meant to prevent, not cause. The `docs/ui-guidelines.md` "Item workflow explicit Save exception" section is removed; the general Details Pane Editing rule (blur-autosave, revert-invalid-field, no prompt) again covers Item fields.

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `listing-inspector`: the save model for Item text fields changes from explicit save with a Save/Discard/Cancel leave prompt to automatic save on field exit with silent commit before context transitions. "Listing inspector edits core creative fields with explicit save" becomes "…with automatic save"; "Listing inspector guards unsaved changes" becomes "Listing inspector commits pending edits when the context changes"; the shared desktop-control guidance scenario no longer references save/discard-guard actions.
- `testing-baseline` (promoted during the learning review): a new requirement that headless view tests use isolated in-memory or disposable workspace data and never open the contributor's real on-disk workspace, generalizing the `CreateForDefaultWorkspace` defect caught during implementation.

The three layout fixes carry **no spec deltas**: they alter presentation robustness, not accepted requirements. The search-filtering filter-surface requirement (persistent search box, flyout disclosure, active indication, keyboard reachability), the basic-product-workflow accessibility requirement, and the archived FC-0106 "sectioned layout in a single scroll container" design intent all remain true — the fixes bring the implementation back in line with them.

## Impact

- **Code**: `src/FusionCanvas.App/Views/MainWindow.axaml` (filter row layout, outer scroll container, header spacing, `LostFocus` wiring on Item text fields, removal of the prompt banner and Save/Discard row), `MainWindow.axaml.cs` (focus/commit handlers), `MainWindowViewModel.cs` (leave-guard commits instead of prompting; prompt state machine removed), `Items/ItemInspectorViewModel.cs` (Save/Discard commands removed; commit/validation semantics retained).
- **Tests**: `tests/FusionCanvas.App.Tests` — inspector and main-window view-model tests rewritten for automatic save; new headless Avalonia view tests for filter wrapping, scroll reachability at minimum height, header spacing, and field-exit commit wiring per the testing baseline.
- **Docs**: `docs/ui-guidelines.md` — remove the "Item workflow explicit Save exception" section and keep the Tags-immediacy note in the general editing guidance.
- **Dependencies/coordination**: `basic-product-creation-workflow` is still active with open verification tasks (9.8 references "Save/Discard/Cancel focus"; its delta spec and `design.md` §9 record explicit save). Its remaining verification evidence for Item text saving becomes obsolete when this change ships; whichever change syncs/archives last must re-validate the `listing-inspector` requirements. Recommended order: let `basic-product-creation-workflow` finish and archive first where practical, and re-map its affected verification rows to this change.
- **Risks**: (1) field-exit commit is asynchronous, so the leave path must coordinate with an in-flight commit rather than evaluating the draft mid-save (handled in design); (2) this is the second reversal of the save model — the rationale above is the durable record; (3) no migration or data impact: persisted Item data is unchanged, only when writes happen.

## Verification approach

- Focused framework-free view-model tests: commit on field exit, silent commit before context transitions (Item/tab/stage/lifecycle), invalid-title revert with other edits persisted, persistence-failure draft retention, no-op when clean, busy re-entry protection.
- Headless Avalonia view tests: filter controls reflow within the pane at narrow widths, all details sections scroll into reach at minimum window height, header label/selector spacing exists, and every Item text field commits on focus loss.
- Baseline: `dotnet test .\FusionCanvas.sln` green; strict `openspec validate` clean. Optional live desktop check is ad hoc only, on a disposable workspace, and not a completion gate.

## Non-goals

- No debounced save-as-you-type; commits happen on field exit and context transitions only.
- No change to group details editing (already blur-autosave), Tag immediacy, lifecycle confirmations, or filter semantics/contents.
- No redesign of the navigation pane, stage tool host, or inspector section composition beyond the four fixes above.

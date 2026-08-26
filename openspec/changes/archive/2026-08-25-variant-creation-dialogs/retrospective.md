# variant-creation-dialogs Retrospective

## Outcome

Variant creation moved from two inline editors below the Sellable Variants list to two focused modal dialogs ("Add Variant" and "Bulk add") owned by the Store Editor window, mirroring the #194 Option-value dialog pattern. The parent screen no longer renders either creation form inline. Both dialogs are scoped to the active Offering by stable identity, reuse all existing creation/validation/persistence logic, close on success or context switch, and return focus to the opening action. Only one creation dialog may be open at a time.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| `ResetBulkDraft()` clears the bulk session flag | `ResetBulkDraft()` clears color/sizes/preview but does not set `IsAddingBulkVariants = false`; the `SelectedOffering` guard left the flag set after an Offering switch | Added an explicit `IsAddingBulkVariants = false` in the `SelectedOffering` guard alongside `ResetBulkDraft()` | Implementation defect | Change-specific | None |
| `Close()` from a `PropertyChanged` handler during an `async void` command removes the dialog from `OwnedWindows` after dispatcher pumps in headless mode | The modal window did not detach from `OwnedWindows` within available dispatcher pumps when `Close()` was invoked from within the `async void` command's property-change callback | Headless success test verifies view-model state (session closed, list refreshed) and explicitly closes the dialog for cleanup; the auto-close mechanism is verified by the Offering-switch headless test and the framework-free session-close test | Implementation defect | Change-specific | None |
| Selecting all `VariantValueChoices` produces a valid combination | Selecting all values (Black + S + M) creates an invalid combination (two values from the same Option) | The headless success test selects a single valid non-duplicate value (M) to produce a creatable Variant | Implementation defect | Change-specific | None |

## Learning Review

- Result: no reusable lessons identified
- Evidence reviewed: final proposal, design, delta specs, tasks, verification, test results, Git diff
- Promotions completed: none
- Deferred promotions: none

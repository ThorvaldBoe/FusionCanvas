## Verification

### Environment

- Repository: `C:\Users\boe74\.codex\worktrees\5bc3\FusionCanvas`
- Runtime: .NET 10 (`net10.0`)
- Date: 2026-09-18

### Automated baseline

Command: `dotnet test .\\FusionCanvas.sln`

| Project | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| FusionCanvas.Application.Tests | 468 | 0 | 0 |
| FusionCanvas.Domain.Tests | 254 | 0 | 0 |
| FusionCanvas.UiDescription.Tests | 27 | 0 | 0 |
| FusionCanvas.Integration.Tests | 239 | 0 | 0 |
| FusionCanvas.App.Tests | 658 | 0 | 0 |
| **Total** | **1,646** | **0** | **0** |

Focused checks also passed:

- `CatalogSetupServiceTests`: 18 passed, including active-delete rejection, archived graph/projection cleanup, protected-reference blocking, reload non-recreation, and unrelated-record preservation.
- `ArchivedBlueprintCanBePermanentlyDeletedAfterOptingIntoArchivedProducts` and `BlueprintArchiveRequiresWarningConfirmationAndRemovesBlueprintFromActiveProducts`: 2 passed.
- Full `FusionCanvas.App.Tests`: 658 passed.

### Acceptance scenario evidence

| Acceptance scenario | Evidence | Result |
| --- | --- | --- |
| Active Blueprints are shown by default; archived Blueprints require an explicit filter | `StoreManagementViewModel.ShowArchivedProducts` defaults false; `StoreEditorHeadlessTests.ArchivedBlueprintCanBePermanentlyDeletedAfterOptingIntoArchivedProducts` verifies the archived row is absent before enabling the checkbox and present after enabling it | Pass |
| Active detail exposes archive, not permanent delete | Same headless test verifies no `Delete permanently` action is present for the active Blueprint; `CanArchiveSelectedProduct` is limited to active selections | Pass |
| Archived detail exposes permanent delete only after opt-in | Headless test enables `ShowArchivedProducts`, selects the archived Blueprint, and verifies the permanent-delete action is enabled | Pass |
| Archive remains confirmation-based and removes the Blueprint from the active list | `StoreEditorHeadlessTests.BlueprintArchiveRequiresWarningConfirmationAndRemovesBlueprintFromActiveProducts` | Pass |
| Active Blueprint cannot be permanently deleted | `CatalogSetupServiceTests.RejectsPermanentDeletionOfAnActiveBlueprint` | Pass |
| Archived deletion removes the owned normalized graph and compatibility projection | `CatalogSetupServiceTests.PermanentlyDeletesArchivedBlueprintGraphAndCompatibilityProjection` checks the post-mutation snapshot and reload | Pass |
| Protected listing/design references block deletion without mutation | `CatalogSetupServiceTests.BlocksPermanentDeletionWhenAnArchivedBlueprintStillHasAnItemListing` | Pass |
| Confirmation text describes irreversible archived deletion | Headless flow asserts the permanent-delete warning is shown before confirmation | Pass |
| Unrelated catalog records and shared assets/providers are preserved | Archived deletion test verifies unrelated records remain; service filters only Blueprint-owned records | Pass |

### Validation and limitations

- `openspec validate --changes --strict`: 11 change packages passed, including `change/archive-first-blueprint-lifecycle`.
- No schema migration was required; archive state remains persisted in existing Blueprint records.
- No interactive desktop smoke test was required because deterministic Avalonia headless tests cover the changed controls and commands. A live desktop pass remains optional for visual polish and platform-specific accessibility behavior.

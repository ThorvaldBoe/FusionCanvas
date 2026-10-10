# Verification: Blueprint Offerings Virtual Grid

## Acceptance criteria

| Scenario | Method and evidence | Result | Limitations |
| --- | --- | --- | --- |
| Populated Blueprint shows scoped aligned Offering summaries without catalog relationship editing | `StoreEditorHeadlessTests.BlueprintOfferingGridKeepsAlignedSummariesFullGuidanceAndOneClickOpen` renders the Store Editor grid, checks its six fixed columns, values, fixed row height, disabled sorting/resizing, and equal column positions and widths across active and archived rows. The view binds only summary cells and an Open action. | PASS | Headless layout verifies structure and bindings; no native desktop visual assessment was performed. |
| Full readiness summary and guidance remain available in a compact row | The same headless test supplies multiple guidance messages, checks the concise visible readiness summary, and checks full summary/guidance through `ToolTip.Tip` and `AutomationProperties.HelpText`. | PASS | Platform-specific screen-reader announcement was not tested. |
| Archived Offerings appear only when opted in and are labeled Archived | The same headless test verifies the checkbox starts unchecked, toggling it yields two scoped rows, the archived row status cell reads `Archived`, and clearing it returns to one active row. | PASS | Tested with one active and one archived Offering. |
| Blueprint without Offerings keeps its empty state and explicit Add route | `StoreEditorHeadlessTests.BlueprintOfferingGridEmptyStateKeepsTheScopedAddRoute` checks that the grid is hidden, the existing empty-state text is visible, Add is enabled, and Add starts an Offering draft for the selected Blueprint. | PASS | Persistence after completing the draft is covered by existing Offering management tests. |
| Archived Store remains read-only | Existing `ProductCatalogViewModelTests.ArchivedStore_BlocksCatalogCreation` verifies catalog creation is blocked. The changed XAML preserves the existing Add command and its `CanCreateCatalogItem` binding; no mutation command was added to the grid. | PASS | Changed-scope binding review supplements the existing view-model test. |
| One-click Open works by pointer and keyboard without changing Blueprint/Store context | The grid headless test sends pointer input to the visible Open action, verifies Offering detail becomes active and the same Blueprint remains selected, then returns and activates Open with Enter. Existing `BlueprintOfferingCard_ClickOpensOfferingAfterWorkspaceSwitch` verifies Offering selection remains valid across workspace switching. | PASS | Keyboard verification uses Avalonia headless input, not an operating-system keyboard. |
| New Offering draft and discard protections remain intact | The empty-state headless workflow verifies draft creation remains Blueprint-scoped. Existing `ProductCatalogViewModelTests.NewOfferingDraft_RequestsFocusAndCanBeCancelled`, `SwitchingProductWhileOfferingDraftIsActiveRequestsDiscard`, and related draft tests cover focus and discard behavior. | PASS | No draft or persistence logic was changed. |

## Verification runs

- Focused grid headless tests: `dotnet test tests/FusionCanvas.App.Tests/FusionCanvas.App.Tests.csproj -m:1 --no-restore --filter "FullyQualifiedName~BlueprintOfferingGrid"` — **2 passed, 0 failed**.
- Full solution baseline: `dotnet test .\FusionCanvas.sln -m:1 --no-restore --nologo -v quiet` — **2,354 passed, 0 failed** (Domain 294, Application 671, Integration 352, App 1,008, UI-description 29).
- Strict accepted-spec validation: `openspec validate --specs --strict --no-interactive` — **65 passed, 0 failed**.
- Change validation: `openspec validate blueprint-offerings-virtual-grid --strict --no-interactive` — **valid**.
- `git diff --check` — **PASS** after implementation and accepted-spec sync.

## Appium decision

No new real-desktop Appium journey is warranted. This is a focused Store Editor collection; the meaningful risks are compiled bindings, virtualized row content, pointer routing, keyboard activation, archived filtering, and scope preservation, all covered deterministically by Avalonia headless view tests. Existing end-to-end coverage continues to cover the Store Editor workflow.

## Changed-scope review

Changed files are limited to the Store Editor App presentation model, XAML, window-owned grid provider/pointer routing, headless App tests, and this OpenSpec change. No domain/application behavior, database schema, persistence, file storage, external API, secret handling, or dependencies changed. Add, archive visibility, empty state, Store read-only binding, and existing draft guards remain in place. Sorting and resizing are disabled; no search, editing, or new row-selection behavior was introduced.

Existing NuGet vulnerability-data connectivity warnings, ImageSharp advisory warnings, and repository analyzer warnings remain unrelated to this change.

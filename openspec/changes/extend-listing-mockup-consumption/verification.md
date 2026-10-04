## Verification matrix

| Acceptance scenario | Verification method | Evidence |
| --- | --- | --- |
| Listing shows generated mockups as thumbnails | Application state/output mapping plus Avalonia build | `MockupGenerationService` preserves output attribution; Listing now creates `MockupOutputViewModel` rows with thumbnail and Color/template labels. |
| Creator opens an enlarged mockup view | Application preview stream contract plus window wiring | `OpenPreviewAsync`, `ListingStageToolViewModel.PreviewAsync`, and `MockupPreviewWindow`; solution build passed. |
| Creator saves a mockup copy | Managed file export contract | `ExportCopyAsync` validates the Item-linked asset and delegates to the managed output store; solution build passed. |
| Creator removes a generated mockup | Application mutation contract and focused tests | `RemoveAsync` atomically removes Asset/AssetLink before best-effort cleanup; Listing test suite passed. |
| Protected Listing keeps generated mockups read-only | Existing Item workflow policy path | Removal remains guarded by `RelatedAssetLink`; generation remains guarded by existing Listing state; solution build passed. |
| Design source replacement invalidates dependent mockups | Focused application test | `MockupOutputInvalidationServiceTests.Precise_invalidation_removes_only_outputs_using_changed_design_asset` passed. |
| Unmappable Design mutation invalidates Item derivatives | Focused application test | `MockupOutputInvalidationServiceTests.Item_wide_invalidation_removes_all_outputs_for_ambiguous_design_change` passed. |
| Invalidation cleanup failure preserves valid persisted state | Cleanup ordering inspection | Repository save occurs before best-effort managed-file cleanup; diagnostics are returned without reintroducing output assets. |
| Creator deletes one output from a populated gallery | Listing view-model test | `ListingTool_ReapplyingTemplateRetainsPriorOutputsForReview` and the Listing stage-tool suite passed. |
| Download or preview fails | Explicit row state and error handling inspection | Missing/unreadable outputs remain as rows with unavailable messaging; copy/preview failures retain managed records and surface errors. |

## Commands

- `openspec validate extend-listing-mockup-consumption` — passed.
- `dotnet build .\FusionCanvas.sln --no-restore -m:1` — passed with pre-existing analyzer warnings.
- `dotnet test .\tests\FusionCanvas.Application.Tests\FusionCanvas.Application.Tests.csproj --no-restore --filter FullyQualifiedName~MockupOutputInvalidationServiceTests` — passed (2).
- `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-restore --filter FullyQualifiedName~StageToolViewModelsTests` — passed (12).
- `dotnet test .\FusionCanvas.sln --no-restore -m:1 --logger "console;verbosity=minimal"` — passed (2,076 tests; Domain 265, Application 572, Integration 304, App 906, UI description 29).

## Desktop coverage decision

No new Appium journey is added. The feature is covered by deterministic application tests, view-model tests, and existing Avalonia headless/build coverage. A real-desktop journey would add useful visual confidence for thumbnail sizing, but it is not required to establish the lifecycle and persistence guarantees; the enlarged preview follows the existing tested auxiliary-window pattern.

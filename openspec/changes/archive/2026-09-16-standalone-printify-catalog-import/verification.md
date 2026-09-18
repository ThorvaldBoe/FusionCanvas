# Verification

## Acceptance scenarios

| Scenario | Result | Evidence |
| --- | --- | --- |
| Standalone Printify Store opens catalog import | Pass | `PrintifyCatalogImportServiceTests.LoadsCatalogOnlyAfterAllStoreGuardsPass(Printify)`; `StoreEditorHeadlessTests.PrintifyImportPanelSupportsKeyboardCancelWithoutProviderImageControls` |
| Standalone Printify setup remains required | Pass | Existing service guard test plus policy-based guard; no catalog request occurs without valid context/credential/shop |
| Saved Printify Store opens catalog import without preview mutation | Pass | Existing headless import workflow and service guard coverage; retrieval path only reads until confirmation |
| Unsupported Store cannot retrieve catalog | Pass | `PrintifyCatalogImportServiceTests.RefusesManualStoreWithoutReadingCredential` and existing guard coverage |
| Confirmed subset import | Pass | Existing `PrintifyCatalogImportViewModelTests` and catalog import service coverage |
| Cancel before confirmation | Pass | Existing `PrintifyCatalogImportViewModelTests` and headless panel cancellation coverage |
| Idempotent repeated import | Pass | `PrintifyCatalogImportServiceTests.ImportsSelectedCatalogAndUpdatesExistingPrintifyRecords` now runs with standalone Printify |
| Safe mapping and malformed payload rejection | Pass | Existing Printify catalog import service tests |
| Recoverable provider/import failures | Pass | Existing Printify catalog import view-model and client tests |

## Commands

- `dotnet test .\\tests\\FusionCanvas.Application.Tests\\FusionCanvas.Application.Tests.csproj --no-restore --filter "FullyQualifiedName~PrintifyCatalogImportServiceTests"` — passed, 6 tests.
- `dotnet test .\\tests\\FusionCanvas.App.Tests\\FusionCanvas.App.Tests.csproj --filter "FullyQualifiedName~StoreEditorHeadlessTests"` — passed, 53 tests.
- `dotnet test .\\FusionCanvas.sln --no-restore` — passed, 1,053 tests (428 Application, 625 App).
- `openspec validate standalone-printify-catalog-import --type change --strict` — valid.
- `git diff --check` — passed; line-ending normalization warnings only.

## Limitations

The solution baseline currently reports Application and App test projects; no live external Printify request was made. Existing compiler/analyzer warnings remain outside this change.

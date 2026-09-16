# Verification

## Acceptance scenarios

| Scenario | Result | Evidence |
| --- | --- | --- |
| Saved Store shows Blueprint name first, product title second, without preview mutation | Pass | `PrintifyCatalogImportItemViewModelTests.ImportItemUsesBlueprintNameBeforeProductTitle`; `StoreEditorHeadlessTests.PrintifyImportPanelSupportsKeyboardCancelWithoutProviderImageControls`; full solution baseline. |
| Missing Blueprint metadata uses deterministic fallback | Pass | View-model assertion for `Blueprint 77`; Integration client enrichment path leaves missing detail as fallback. |
| Unsupported Store remains blocked | Pass | Existing Printify application guard tests in full Application suite. |
| Explicit subset selection submits only selected products | Pass | Existing import-session selection/confirmation tests and full App suite. |
| Cancellation leaves catalog and drafts unchanged | Pass | Existing cancellation and keyboard-cancel headless coverage; full App suite. |
| Repeated import remains idempotent | Pass | Existing Printify persistence/import tests; full Application suite. |
| Provider changes update matching records and preserve local-only records | Pass | Existing Printify persistence/import tests; full Application suite. |
| Valid product configuration maps safely | Pass | Existing Printify catalog import mapping tests; full Application suite. |
| Malformed payload does not commit partial data | Pass | Existing malformed Printify payload tests; full Application and Integration suites. |
| Provider failures and in-flight cancellation remain recoverable and safe | Pass | Existing classified failure/cancellation tests plus full App/Integration suites. |

## Commands

- `openspec validate printify-blueprint-label` — passed.
- `dotnet test .\\tests\\FusionCanvas.Integration.Tests\\FusionCanvas.Integration.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --filter FullyQualifiedName~PrintifyCatalogClientTests` — 12 passed.
- `dotnet test .\\tests\\FusionCanvas.App.Tests\\FusionCanvas.App.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --filter FullyQualifiedName~PrintifyCatalogImportViewModelTests` — 5 passed.
- `dotnet test .\\tests\\FusionCanvas.App.Tests\\FusionCanvas.App.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --filter FullyQualifiedName~PrintifyImportPanel` — 1 passed.
- `dotnet test .\\FusionCanvas.sln --no-restore -m:1 -p:UseSharedCompilation=false -v minimal` — 1,573 passed, 0 failed, 0 skipped.

## Limitations

- No live Printify credential or shop was used; HTTP fixtures cover the detail lookup and fallback.
- Restore emitted NU1900 because vulnerability-feed access was unavailable; unrelated pre-existing analyzer warnings remain.

# Verification

## Acceptance evidence

| Changed requirement / scenarios | Evidence | Result |
| --- | --- | --- |
| Store-scoped retrieval, unique Blueprint grouping, used Provider summaries, shop context, empty shop, and unsupported Store guards | `PrintifyCatalogClientTests.GroupsShopProductsByBlueprintAndLoadsFullCatalogVariantsForEveryUsedProvider`, `PrintifyCatalogClientTests.LoadsBlueprintSummariesWithoutMutatingRequests`, existing shop paging/empty/failure tests, and `PrintifyCatalogImportServiceTests.LoadsCatalogOnlyAfterAllStoreGuardsPass` | Passed |
| Explicit Blueprint selection, stable IDs, cancellation, and stale Store response handling | `PrintifyCatalogImportViewModelTests` selection/cancel/stale-result cases; `StoreEditorHeadlessTests.PrintifyCatalogGridVirtualizesRowsAndPreservesAccessibleSelectionAcrossScrolling`; `StoreEditorHeadlessTests.PrintifyImportPanelSupportsKeyboardCancelWithoutProviderImageControls` | Passed |
| Canonical Store Blueprint and one Offering per used Provider; complete sellable Variant set, exact Option Value memberships, idempotency, and preservation of legacy product-level records | `PrintifyCatalogImportServiceTests.BlueprintImportUsesCanonicalIdentityAndAllUsedProviderVariantsWhilePreservingLegacyBlueprints`; `PrintifyCatalogClientTests.GroupsShopProductsByBlueprintAndLoadsFullCatalogVariantsForEveryUsedProvider` | Passed |
| Disabled/unavailable Variants excluded; malformed or unresolved catalog relationships fail safely without partial persistence | New grouped-catalog Integration test plus existing Integration parser/validation failure cases; Application import validation, repository-load/save failure, and `RejectsVariantReferencesToUndeclaredOptionValuesAtomically` cases | Passed |
| Product-scoped Niche listing import and linked-listing Variant Setup compatibility | Existing `PrintifyListingImportServiceTests` and related Application/Integration suite; string product IDs retain `LoadSelectedProductsAsync` and `ApplySelectedCatalog`, while Store Editor uses the new integer Blueprint path | Passed |
| Virtualized grid labels, tooltips, accessible names, and selection through row recycling | `StoreEditorHeadlessTests.PrintifyCatalogGridVirtualizesRowsAndPreservesAccessibleSelectionAcrossScrolling` and `PrintifyCatalogImportViewModelTests.ImportItemSummarizesBlueprintProvidersAndShopProducts` | Passed |
| Mockup-template readiness rules remain separate | No template validation or readiness behavior was changed; the imported catalog includes all supported sellable Variants and existing template validation remains unchanged | Reviewed |

## Commands

- `dotnet test .\tests\FusionCanvas.Application.Tests\FusionCanvas.Application.Tests.csproj --no-restore -p:UseSharedCompilation=false -m:1 -v:q` — passed, 724 tests.
- `dotnet test .\tests\FusionCanvas.Integration.Tests\FusionCanvas.Integration.Tests.csproj --no-restore -p:UseSharedCompilation=false -m:1 -v:q` — passed, 462 tests.
- `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-restore -p:UseSharedCompilation=false -m:1 -v:q` — passed, 1,042 tests, including Avalonia headless coverage.
- `dotnet test .\FusionCanvas.sln --no-restore -p:UseSharedCompilation=false -m:1 -v:q` — passed; Domain 294, Application 724, Integration 465, App 1,042, and UI Description 29 tests (2,554 total) after rebasing onto current `main`.
- `openspec validate group-printify-import-by-blueprint --strict` — passed.

## Limitations and follow-up

- Full sellable Variant import can expose many Colors and increase the number of mockup templates that need attention. Template validation and coverage policy were deliberately not changed; handle that concern in a separate change.
- Earlier product-level catalog records remain intact alongside the canonical Blueprint records. This change does not merge or delete them.
- A failed full-solution run during development exposed an outdated UI test expectation for the imported Blueprint display name. The expectation was updated to the current shop/catalog context and the final complete solution run passed.

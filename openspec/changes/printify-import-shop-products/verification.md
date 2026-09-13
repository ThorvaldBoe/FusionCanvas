# Verification

## Acceptance scenarios

| Scenario group | Result | Evidence |
| --- | --- | --- |
| Saved Printify Store retrieves products from the selected shop; unsupported contexts remain blocked | Pass | Application guard coverage; shop ID is passed from persisted Store context to the client. |
| Product list uses shop products and does not fall back to the global catalog | Pass | `PrintifyCatalogClientTests.LoadsProductsFromSelectedShopAcrossPages`; endpoint assertions cover `/v1/shops/42/products.json`. |
| Explicit subset selection, cancellation, focus, and draft safety | Pass | Existing `PrintifyCatalogImportViewModelTests` and `StoreEditorHeadlessTests` pass after product-summary compatibility update. |
| Pagination, string IDs, empty shop, malformed payload, and safe provider errors | Pass | New paginated integration fixture; existing bounded response/status tests; parser rejects invalid required product relationships. |
| Product-specific mapping and duplicate underlying Blueprint identity | Pass | Product ID is retained in import metadata and offering identity; existing persistence/import tests pass. |
| Repeated import is idempotent and local-only records are preserved | Pass | Existing `PrintifyCatalogImportPersistenceTests` and Application suite pass. |
| UI terminology and strategy gating | Pass | Store Editor copy updated to describe products in the selected shop; App suite passes. |

## Commands

- `openspec validate printify-import-shop-products` — passed.
- `dotnet test .\\tests\\FusionCanvas.Application.Tests\\FusionCanvas.Application.Tests.csproj --no-restore -p:UseSharedCompilation=false` — 430 passed.
- `dotnet test .\\tests\\FusionCanvas.Integration.Tests\\FusionCanvas.Integration.Tests.csproj --no-restore -p:UseSharedCompilation=false` — 218 passed; focused Printify regression — 10 passed.
- `dotnet test .\\tests\\FusionCanvas.App.Tests\\FusionCanvas.App.Tests.csproj --no-restore -p:UseSharedCompilation=false` — 635 passed.
- `dotnet test .\\FusionCanvas.sln --no-restore -p:UseSharedCompilation=false` — 1,558 passed.

## Limitations

- The new integration test uses deterministic HTTP fixtures; no live Printify credential or shop was used.
- Existing compiler/analyzer warnings unrelated to this change remain in the repository.

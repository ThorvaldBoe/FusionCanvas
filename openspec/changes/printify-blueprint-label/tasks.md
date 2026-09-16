## 1. Application summary contract

- [x] 1.1 Add optional Blueprint brand/model/title metadata to `PrintifyShopProductSummary` without changing opaque product IDs or existing import payload contracts.
- [x] 1.2 Implement and unit-test Blueprint-first label formatting with `brand model`, title fallback, and `Blueprint {id}` fallback, plus product-title secondary context.

## 2. Printify integration

- [x] 2.1 Enrich shop-product retrieval with deduplicated Blueprint detail lookups and populate the summary metadata while preserving pagination, cancellation, and safe error classification.
- [x] 2.2 Add deterministic Integration coverage for successful Blueprint metadata lookup and incomplete/unavailable metadata fallback.

## 3. Store Editor presentation

- [x] 3.1 Update `PrintifyCatalogImportItemViewModel` and the import picker markup so the Blueprint name is primary and the shop product title/IDs are secondary and accessible.
- [x] 3.2 Add or update Avalonia headless coverage for the rendered primary label, secondary product context, selection submission, and unchanged cancellation/draft-safety behavior.

## 4. Verification and delivery

- [x] 4.1 Run criterion-level tests for every scenario in the delta spec and record methods, results, evidence, and limitations in `verification.md`.
- [x] 4.2 Run `openspec validate printify-blueprint-label` and resolve strict validation errors.
- [x] 4.3 Run `dotnet test .\\FusionCanvas.sln` and record the baseline result in `verification.md`.

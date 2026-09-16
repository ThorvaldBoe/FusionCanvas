## Context

The shop-product import picker currently binds its checkbox to `PrintifyShopProductSummary.Title`, which is the creator's Printify product title. The shop-product payload includes the numeric Blueprint ID but not necessarily the human-readable brand/model. The existing integration already knows how to parse Blueprint summaries with brand and model fields, so the import summary can be enriched at the integration boundary without changing persisted catalog entities or product identity.

This is an occasional action in the focused Store Editor Catalog & mockups surface. The picker remains compact and selection-first: the Blueprint name is the primary recognition cue, while the product title and stable IDs remain secondary context for distinguishing products.

## Goals / Non-Goals

**Goals:**

- Show a human-readable Blueprint name such as `Gildan 64000` as the primary label for each imported shop product.
- Preserve the shop product title as secondary context and keep product IDs opaque and unchanged.
- Support incomplete metadata with a deterministic fallback that still identifies the Blueprint.
- Keep retrieval read-only, Store-scoped, cancellable, and compatible with existing import persistence.
- Verify presentation and parsing with deterministic Application, Integration, and headless App tests.

**Non-Goals:**

- No database migration or change to local Blueprint names after import.
- No change to shop-product selection semantics, import mapping, or idempotency keys.
- No global Blueprint list displayed to users and no product synchronization behavior.
- No live Printify request in the test baseline.

## Decisions

1. **Enrich the application summary rather than deriving labels in XAML.** Add optional Blueprint brand/model/name data to `PrintifyShopProductSummary`; the App view model formats display text. This keeps external payload interpretation in Integration and keeps presentation formatting testable without Avalonia.

2. **Resolve missing brand/model through the existing Blueprint detail endpoint.** Shop-product retrieval will collect unique Blueprint IDs and request the corresponding catalog Blueprint detail (`/v1/catalog/blueprints/{id}.json`) for metadata. The shop endpoint remains authoritative for the product list and product configuration. If detail metadata is absent or unusable, retrieval succeeds with the deterministic `Blueprint {id}` fallback rather than hiding the product.

3. **Use brand + model as the Blueprint name.** Trim both values, join nonblank parts with one space, and use the existing Blueprint title only when brand/model are not sufficient. If no human-readable value exists, use `Blueprint {BlueprintId}`. The product title is always shown separately when nonblank.

4. **Keep labels distinct from import identity.** A duplicate Blueprint name or multiple products based on one Blueprint does not merge or alter selection identity; the opaque product ID remains the submitted key and existing persistence matching remains unchanged.

5. **Preserve focused-surface interaction states.** Loading, empty, errors, cancellation, focus return, stale-result protection, and unsaved local drafts remain as currently specified. Only copy, label composition, and metadata retrieval are changed.

## Risks / Trade-offs

- [Risk] One additional detail request per unique Blueprint increases import latency. → Deduplicate IDs, request details only once per retrieval, and retain the existing cancellation/timeout/error classification.
- [Risk] A detail endpoint may omit or reject metadata for an otherwise valid shop product. → Treat missing brand/model as non-fatal and use `Blueprint {id}` while retaining the product title.
- [Risk] Tests or custom clients construct summaries positionally. → Add optional fields at the end of the record and update only affected fixtures.

## Migration Plan

No persistence migration is needed. Deploying the change affects only retrieval-time display metadata. Rollback is a code revert; already imported local records and their identities remain valid.

## Open Questions

None. The primary label is the Blueprint name, the product title is secondary context, and the numeric-ID fallback is the approved behavior for incomplete remote metadata.

## Implementation Plan

1. Update `PrintifyShopProductSummary` in Application with optional Blueprint display metadata and add a small formatting boundary or view-model properties for primary/secondary labels.
2. Update `PrintifyCatalogClient` to parse the shop product list, deduplicate Blueprint IDs, fetch Blueprint details, and populate brand/model/title metadata while preserving safe failures and cancellation. Keep product configuration parsing unchanged.
3. Update `PrintifyCatalogImportItemViewModel` and the import picker XAML to bind the checkbox to the Blueprint label and expose the product title/identity as secondary text with consistent accessibility text.
4. Add or update Integration tests for detail lookup and missing metadata fallback, Application tests for label composition, and App/headless tests proving the rendered selection surface uses Blueprint name first and product title second.
5. Run focused tests, `dotnet test .\\FusionCanvas.sln`, and strict `openspec validate printify-blueprint-label`; record criterion-level evidence in `verification.md`.

## Acceptance-to-Verification Plan

- Blueprint-first label with product-title context: framework-free view-model test plus Avalonia headless picker assertion.
- Missing brand/model fallback: Integration parser/client fixture and view-model assertion.
- Product selection, opaque IDs, cancellation, draft safety, and Store guards: existing Application/App regression tests with unchanged selection contract.
- No persistence or import identity changes: existing Printify import persistence suite and full solution baseline.

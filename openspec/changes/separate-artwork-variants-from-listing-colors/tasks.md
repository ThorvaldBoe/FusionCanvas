## 1. Sequence and model the new authority

- [x] 1.1 Review the active `printify-listing-lifecycle` implementation and verification; record its sole remaining external smoke check as supplemental and reconcile its Design-owned Color assumption to this module's Listing authority.
- [x] 1.2 Add Domain models and invariants for color-independent artwork variants, optional contrast intent, Offering-scoped favorite Color Values, Item Color selection, unresolved legacy Color labels, and per-Color artwork assignment.
- [x] 1.3 Add focused Domain tests for favorite scope, Color assignment ownership, default-first-variant behavior, invalid variant removal, and Color-independent artwork rows.

## 2. Migrate and persist workspace data

- [x] 2.1 Extend `WorkspaceSnapshot` and SQLite persistence for favorites, Listing Color selections/assignments, swatch metadata, and artwork variant intent without changing remote Printify mappings.
- [x] 2.2 Implement the transactional idempotent migration from Design-selected Colors and row-color memberships to Listing Color assignments; preserve artwork row identities, ordering, and Design Area asset assignments.
- [x] 2.3 Preserve unmatched or ambiguous legacy Color labels as unresolved Listing data and implement creator resolution without losing the linked artwork variant.
- [x] 2.4 Add isolated Integration tests for fresh schema, legacy migration, repeated migration, transaction rollback, resolved/unresolved mappings, referential integrity, and unchanged remote product mappings.

## 3. Refactor Design around artwork variants

- [x] 3.1 Update Design application services and policies so adding, importing, generating, assigning, and removing artwork variants do not require or modify product Colors.
- [x] 3.2 Preserve selected Offering/provider context and Design Area compatibility; require explicit review when the Offering changes and retain source artwork for adaptation.
- [x] 3.3 Update Design UI wording and controls for one or more artwork variants and optional light-on-dark/dark-on-light generation intent; remove shirt Color controls.
- [x] 3.4 Add Application and Avalonia headless tests for variant creation/import, separate Design Area assignments, optional contrast intent, Offering change guidance, and deletion safeguards.

## 4. Build favorite-first Listing Color selection

- [x] 4.1 Add Offering Color favorite editing in Store setup and prove favorites persist by stable Option Value identity without changing sellable Variants or current Item selections.
- [x] 4.2 Add the compact Listing favorite collection and focused More Options surface with separate preview and selection controls, searchable Color rows, flat swatches, multi-select confirmation, cancellation, focus return, loading/unavailable states, and availability labels.
- [x] 4.3 Persist Listing Color choices by stable Option Value identity, default new Colors to the first artwork variant, allow explicit per-Color reassignment, and derive all active provider-enabled Size Variants for selected Colors.
- [x] 4.4 Add Application and headless view tests for favorite ordering/scope, preview without selection, multi-select/cancel/confirm, all-size inclusion, out-of-stock visibility, artwork assignment, and recoverable catalog errors.

## 5. Retain complete Printify catalog availability and update projection

- [x] 5.1 Request provider catalog Variants with `show-out-of-stock=1`, parse approximate Color swatch values, and persist provider availability independently from local Variant archival.
- [x] 5.2 Update catalog import to retain provider-enabled out-of-stock Variants and last-known availability when refresh fails or omits a Variant; keep stable Option Value and Variant identities.
- [x] 5.3 Update Listing projection and Printify create/update request construction to consume Item Listing Colors and their derived compatible Variant set, including selected out-of-stock Variants only.
- [x] 5.4 Update downloaded Printify product variant setup to populate Listing Color selections and color-independent artwork variants, retaining out-of-stock enabled Variants and preserving existing local setup.
- [x] 5.5 Add Integration/Application tests for query construction, swatch parsing, availability transitions, out-of-stock selection, imported Listing setup, explicit remote projection, and no remote mutation during migration.

## 6. Adapt mockup generation to Listing assignments

- [x] 6.1 Resolve each selected Color's assigned artwork variant and generate independent per-Color outputs with stable template/source attribution.
- [x] 6.2 Add missing-template guidance and preserve partial success so ready Colors generate while unresolved Colors produce recoverable diagnostics and remain selected.
- [x] 6.3 Add Application tests for alternate artwork selection, missing template, invalid source/mapping, partial success, and preservation of existing outputs.

## 7. Add Printify publish reminder and end-to-end UI coverage

- [x] 7.1 Add a manual acknowledgement step before each Printify Publish/republish; cancellation sends no request and acknowledgement is never persisted as remote verification.
- [x] 7.2 Add focused lifecycle tests for acknowledgement, cancellation, republish, and no claim of Printify availability verification or Order Routing.
- [x] 7.3 Add the `listing-color-flexibility` Appium scenario pack using a fresh disposable workspace and mocked Printify adapter; verify the imported favorite/extra-color path, preview/select distinction, multi-select confirmation, and durable reload. Verify partial mockup readiness and the publish reminder in deterministic lower-layer tests so the desktop journey does not need remote lifecycle operations.

## 8. Verify and complete the delivery package

- [x] 8.1 Complete `verification.md` with results and evidence for every acceptance scenario, including migration and the isolated UI scenario pack.
- [x] 8.2 Review changed-scope OpenSpec drift, migration safety, provider request semantics, keyboard/focus behavior, and preservation of remote mappings; correct artifacts or implementation for every failed criterion.
- [x] 8.3 Run strict OpenSpec validation and `dotnet test .\FusionCanvas.sln`; record the exact commands and results in `verification.md`.
- [x] 8.4 Correct More Options to include favorite Colors in its full collection, reconcile the requirement wording, and add a headless regression test for viewing, canceling, and confirming favorite and nonfavorite selections together.

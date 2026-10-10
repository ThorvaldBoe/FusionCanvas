## 1. Printify product discovery and safe artwork retrieval

- [x] 1.1 Add focused import DTOs and read ports for paginated shop products, full product detail, visibility, variants, print areas, and original artwork references.
- [x] 1.2 Extend the Printify listing client to retrieve all shop-product pages and selected product details using the selected Store credential and shop identity.
- [x] 1.3 Implement safe original-artwork retrieval through the managed-file boundary, including HTTPS/host, media type, byte-size, malformed-response, cancellation, and temporary-file cleanup rules.
- [x] 1.4 Add mocked Integration tests for pagination, empty shops, malformed or duplicate identities, visibility parsing, artwork URL/content validation, and read-only HTTP methods.

## 2. Duplicate detection and per-product import use case

- [x] 2.1 Implement deterministic title/description normalization and text-similarity matching against active Items across the selected Store; return candidate context and mapping eligibility without mutation.
- [x] 2.2 Implement preview, linked-product detection, explicit duplicate-check requests, and per-product new-versus-connect decisions in Application state/results.
- [x] 2.3 Implement per-product import persistence using `ExternalListingMapping`, Item-linked assets, Listing stage, visibility-derived status, remote product data, and stable product identity; preserve all local fields when connecting an existing Item.
- [x] 2.4 Implement dated unique group creation for new Items only, including collision suffixes and no empty groups after failure or cancellation.
- [x] 2.5 Implement atomic per-product persistence and retry handling: stage files, commit Item/group/assets/mapping together, clean up on failure, retain other successful products, and exclude successes on a later scan.
- [x] 2.6 Keep canonical lifecycle snapshot fields compatible while preserving imported Printify product data in the existing JSON integration/import metadata field; verify initial listing import does not change catalog or Design records.
- [x] 2.7 Implement idempotent on-demand variant-setup download from the Listing stage, reusing the selected-product catalog import and initializing Item configuration, selected colors, rows, and representable artwork assignments.
- [x] 2.8 Use the existing FusionCanvas placement calculation for imported artwork and ensure provider-specific Printify position/scale/rotation values are not persisted or replayed.
- [x] 2.9 Handle catalog conflicts and artwork outside the v1 single-finished-image assumption without overwriting local user choices; report unsupported Design-slot mappings and keep original assets linked.
- [x] 2.10 Add focused Application tests for matching thresholds, cross-Niche candidates, linked/non-linkable candidates, new and connect flows, field preservation, status, groups, read-only behavior, partial failure, retry, cancellation, variant setup idempotency, conflict handling, single-image row grouping, default placement output, and unsupported artwork.

## 3. Niche import experience

- [x] 3.1 Add `Import from Printify…` to the active Niche context menu and route the command with the invoking Store and Niche context.
- [x] 3.2 Implement the focused import dialog and view model for loading/empty/error states, all-product preview, selection checkboxes, linked-product visibility, explicit duplicate check, candidate selection, import confirmation, progress, cancellation, and per-product outcomes.
- [x] 3.3 Add Listing-stage action and status presentation for downloading variant setup, including missing-setup readiness, progress, success, partial reconstruction, conflict, and retry states.
- [x] 3.4 Add Avalonia headless tests for menu/action availability, connection readiness, dialog construction, product selection, linked-product disabling, duplicate decisions, keyboard focus, loading/error/progress, failure outcomes, and Listing-stage variant-setup states.
- [x] 3.5 Add one Appium scenario pack with an isolated real-desktop journey: Niche context menu → preview → import a selected product → invoke Listing-stage variant-setup download → verify saved mapping, imported artwork, and reconstructed configuration. Use one fresh disposable workspace/database for the pack and a mocked Printify service.

## 4. Criterion-level verification and delivery gates

- [x] 4.1 Map every scenario in `specs/printify-listing-import/spec.md` to a focused test or the Appium journey in `verification.md`; resolve any uncovered criterion.
- [x] 4.2 Run `openspec validate import-printify-listings --type change --strict` and correct all change artifact/spec errors.
- [x] 4.3 Run `dotnet test .\FusionCanvas.sln` and resolve failures or document an explicitly approved blocker.
- [x] 4.4 Complete `verification.md` with criterion-level results, commands, evidence, limitations, and any deferred checks before completion review.

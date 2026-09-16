## 1. Store preference and metadata foundations

- [ ] 1.1 Add nullable Store primary Design Area preference with domain ownership/editability validation and migration-safe defaults.
- [ ] 1.2 Persist, load, transfer, and validate the primary Design Area preference in SQLite and workspace snapshots; add round-trip and cross-Store tests.
- [ ] 1.3 Add versioned generated-artwork provenance metadata parsing/serialization with safe handling of absent or malformed metadata.

## 2. Image-generation contracts and AI configuration

- [ ] 2.1 Add Artwork to AI request purposes and settings inheritance/serialization, including readiness presentation data.
- [ ] 2.2 Extend model descriptors/catalog contracts with image output, transparency, format, and supported-size/aspect-ratio capabilities.
- [ ] 2.3 Define application image request/result/failure contracts and provider boundary without exposing provider SDK types to Domain or Application.
- [ ] 2.4 Add deterministic tests for Artwork profile inheritance, capability parsing, unavailable models, and transparency gating.

## 3. Generation planning and raster pipeline

- [ ] 3.1 Implement framework-free creative-context prompt assembly with explicit untrusted-data boundaries and operational/secret exclusion.
- [ ] 3.2 Implement exact-ratio target-size negotiation for parameterized and discrete provider capabilities, including no-supported-size failures.
- [ ] 3.3 Implement bounded image validation and deterministic raster normalization to exact target dimensions and PNG with alpha preservation.
- [ ] 3.4 Add Application and Integration tests covering prompt contents, ratio calculations, malformed/oversized outputs, alpha behavior, and PNG dimensions.

## 4. Provider and persistence orchestration

- [ ] 4.1 Implement the selected provider adapter's image request/response mapping, capability checks, bounded binary handling, and typed recoverable failures.
- [ ] 4.2 Extend the Design-stage application service to resolve the selected target, assemble the request, invoke generation, normalize the result, and atomically persist asset, provenance, supporting-image visibility, and default-row slot assignment.
- [ ] 4.3 Add cancellation, one-operation-per-Item, stale-result identity checks, and best-effort managed-file cleanup on persistence failure.
- [ ] 4.4 Add Application and Integration tests for successful placement, replacement/history retention, failure atomicity, cancellation, stale results, and workspace reload.

## 5. Design-stage user experience

- [ ] 5.1 Add the Generate Artwork section below Supporting Images with target selector, Generate action, Transparent Background checkbox, and accessible helper/error text.
- [ ] 5.2 Implement primary-target defaulting, explicit target preservation, readiness/disabled/busy/cancellation/error states, and generated provenance display in the Design view model.
- [ ] 5.3 Preserve existing manual PNG import, preview, download, remove, and read-only behavior while integrating generated assets.
- [ ] 5.4 Add focused Avalonia headless tests for construction, bindings, target selection, routed Generate action, disabled/read-only state, busy state, and recoverable error preservation.

## 6. Verification and delivery gates

- [ ] 6.1 Review implementation against every acceptance scenario in the three delta specs and correct any artifact or behavior drift.
- [ ] 6.2 Run focused Domain, Application, Integration, and App tests and record criterion-level evidence.
- [ ] 6.3 Run `openspec validate` and resolve all validation findings.
- [ ] 6.4 Run `dotnet test .\FusionCanvas.sln` and resolve all failures.
- [ ] 6.5 Complete scoped completion QA for architecture direction, persistence/migration, security/secret exclusion, provider boundaries, raster handling, and headless UI coverage.


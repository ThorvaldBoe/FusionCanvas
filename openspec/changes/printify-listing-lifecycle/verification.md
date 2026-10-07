# Printify Listing Lifecycle Verification

This record is maintained criterion-by-criterion during implementation. Aggregate build and test results are supporting evidence only.

## Pre-implementation review gates

| Criterion | Method | Result | Evidence / limitation |
| --- | --- | --- | --- |
| Prototype covers unavailable, ready, saved draft, conflict, publishing/published, unpublished/deletion-blocked, and strategy-gated states | Generated static prototype screens and reviewed the output set | Pass | `prototype/README.md` and the seven PNGs in `prototype/`. Production markup is still pending. |
| Official Printify behavior reviewed for product creation, pricing, variants, publication, and hide/show | Reviewed official Printify API documentation and Help Center pages | Pass with limitations | Product lifecycle, `visible`, `is_locked`, publish payload, publication callbacks, variant visibility, pricing, and hide/show behavior were reviewed. Exact unpublish effect remains a supervised live verification item. |
| Architecture, UX, API-safety, and accessibility stress review completed | Reviewed proposal, delta specs, implementation plan, prototype states, existing stage-tool host, UI guidelines, and mocked-service policy | Pass with accepted risks | Single-column composition, Design ownership, fail-closed readiness, explicit conflict actions, destructive guards, text status labels, and no direct Shopify API were retained. |
| Blocking review findings resolved before production implementation | Updated design and retrospective; bounded remaining API uncertainties as explicit verification items | Pass for implementation start | Unpublish semantics and shipping-template applicability are not silently assumed by the UI. They remain endpoint-adapter/live-verification concerns and must not be claimed as verified until evidence exists. |

## Acceptance scenario matrix

The rows below are the implementation checklist. Each row will be linked to a focused test or supplemental evidence as the corresponding task is completed.

| Spec area | Verification method | Status |
| --- | --- | --- |
| Connection readiness and strategy gating | Application tests with deterministic readiness/strategy fakes; headless state tests | Pass — readiness, publication gate, fail-closed unavailable state, strategy switching, and Store repair guidance are covered |
| Item/Design/catalog projection | Pure application projection tests | Pass for initial projection slice |
| Artwork placement | Pure calculation tests for matching, square, and aspect-ratio-mismatch inputs | Pass |
| Pricing policies | Pure calculation tests for fixed retail, fixed profit, cost differences, and invalid input | Pass |
| Create/update and ambiguous results | Application tests with mocked lifecycle port and persisted operation intent | Pass — definitive create, ambiguous retry blocking, update, remote-read uncertainty, and image/mutation failure paths are covered |
| Remote drift and explicit resolution | Pure comparison tests plus application reconciliation tests | Pass — remote conflict, accept-remote integration-value persistence, and keep-local update paths are covered |
| Publication/unpublication | Application tests with Printify-only publication port; supervised showcase verification for final external visibility | Pass for deterministic application behavior — strategy gate, intent, read-back verification, mismatch/uncertain states, and Printify-only adapter are covered; live visibility remains supplemental |
| Archive/delete guards | Application tests covering published, locked, uncertain, unpublished, and confirmed deletion | Pass — local archive, published/locked/uncertain guards, verified deletion, and exception-to-uncertain behavior are covered |
| Store mapping persistence/migration | Isolated SQLite repository tests | Pass |
| Unified Printify transport and secret redaction | Integration tests with mocked HTTP handlers and diagnostic assertions | Pass — shared transport, client, credential, representative failure fixtures, bounded responses, safe messages, and telemetry request metadata are covered; the composition root supplies the recorder to both credential and catalog paths |
| Listing-stage UI | Deterministic Avalonia headless view tests for state visibility and action enablement | Pass — Printify registration, fail-closed loading, readiness guidance, command state, delete confirmation/cancellation, and existing MainWindow compiled-binding/layout lanes pass |
| Fulfillment-strategy capability resolution | Pure Application tests covering Manual, Shopify Manual, Printify, Shopify + Printify, and unavailable readiness | Pass |

## Current implementation evidence

- The implementation slice is complete across Domain, Application, persistence, Printify Integration, and Listing-stage UI. Reconciliation keeps integration-owned overrides separate from Design-owned state and all remote mutation paths fail closed when outcomes are ambiguous.
- `ListingLifecycleTests`: 9 focused pure Application tests passed.
- `ListingLifecycleServiceTests`: 10 focused orchestration tests pass, including unavailable-connection fail-closed behavior, definitive identity persistence, ambiguous-create retry blocking, remote conflict resolution, retained integration values, publication gating/read-back verification, archive-only behavior, and uncertain deletion.
- `PrintifyListingClientTests`: 11 focused Integration tests passed, including shared authenticated transport/payload construction, visibility/external identity/shipping-template parsing, representative API failures, malformed responses, and ambiguous network mutation results.
- `PrintifyCredentialVerifierTests`: 12 focused Integration tests passed after migration to the shared transport.
- `StageToolViewModelsTests`: 17 focused Avalonia-headless tests pass, including unavailable Store fail-closed behavior, create/refresh/conflict/keep-local, Shopify + Printify publish/unpublish/delete gating, and remote-delete confirmation/cancellation.
- `MainWindowConstructionTests` and `MainWindowLayoutTests`: 34 Avalonia-headless tests pass after adding the compact Printify surface and compiled bindings.
- The supervised showcase-store unpublish/hide behavior check is intentionally not claimed here; it remains a supplemental manual verification item because it mutates an external Printify account.
- `openspec validate printify-listing-lifecycle --strict` passed.
- `dotnet test .\FusionCanvas.sln --no-restore -m:1` passed across the full solution after merging current `main`: Domain 290, Application 667, Integration 349, App 965, and UI Description 29; 2,300 passed, 0 failed, 0 skipped. The run emitted the repository's existing NU1900 offline vulnerability-feed warnings only.

## Application contracts and pure behavior

| Task | Evidence | Result |
| --- | --- | --- |
| 2.1 | The files under src/FusionCanvas.Application/Listings/ contain provider-neutral lifecycle, readiness, identity, and pending-operation models; Domain has no new integration references. | Pass |
| 2.2 | The files under src/FusionCanvas.Application/Listings/ define focused readiness, image, product, and publication ports. | Pass |
| 2.3 | ListingProjectionBuilder projects Item, fixed Provider offering, selected Design colors, catalog variants/options, and Design artwork assignments; focused tests cover valid projection and missing assignment. | Pass |
| 2.4 | ListingPlacementCalculator uses full-width scale, centered normalized X, top-edge-derived Y, and zero rotation; focused test covers a square asset against a non-square target. | Pass |
| 2.5 | ListingPricingCalculator supports fixed retail and fixed profit; focused tests cover cost differences and negative input rejection. | Pass |
| 2.6 | ListingDriftComparer compares last synchronized, remote, and local snapshots and reports remote-only, local-only, both-changed, missing, and unchanged fields; focused tests cover each classification. | Pass |

Focused command:

dotnet test .\tests\FusionCanvas.Application.Tests\FusionCanvas.Application.Tests.csproj --no-restore --filter FullyQualifiedName~ListingLifecycle

Passed: 9, Failed: 0

Restore emitted the repository's existing offline NuGet vulnerability-feed warning (NU1900); it did not affect compilation or the focused test result.

## Persistence and migration

| Task | Evidence | Result |
| --- | --- | --- |
| 3.1 | Schema version 22 adds the additive external_listing_mappings table with Store/Item references, publication/synchronization/operation state, snapshots, integration-owned values, and external identities. | Pass |
| 3.2 | WorkspaceSnapshot and SqliteWorkspaceRepository save/load the mapping; repository validation enforces Store/Item ownership and unique remote identity. | Pass |
| 3.3 | `ListingLifecycleService` saves an intent mapping before image/product/publication mutation and persists definitive, failed, or uncertain outcomes; focused orchestration tests cover ambiguous create blocking. | Pass |
| 3.4 | ProductCatalogPersistenceTests now round-trip a mapped Item and run the existing catalog/workspace persistence regression suite; existing catalog and Item data remain readable. | Pass |

## Strategy and publication evidence

| Task | Evidence | Result |
| --- | --- | --- |
| 5.4 | `ListingLifecycleService` persists publishing/unpublishing intent, performs a post-mutation remote read, and records `Published`, `Unpublished`, or `Unverified` state according to the verified result. | Pass — focused publication orchestration tests and the Printify adapter tests cover the deterministic path; live external visibility remains supplemental. |
| 6.3 | `ListingCapabilityResolverTests` cover all four fulfillment strategies, unavailable readiness, and standalone Printify publication exclusion. | Pass |

Persistence command:

dotnet test .\tests\FusionCanvas.Integration.Tests\FusionCanvas.Integration.Tests.csproj --no-restore --filter FullyQualifiedName~ProductCatalogPersistenceTests

Passed: 25, Failed: 0

## Final acceptance record

| Capability / scenario | Method | Result | Evidence / limitation |
| --- | --- | --- | --- |
| Connection readiness and Printify-enabled strategy gating | Application tests, integration fakes, and headless view tests | Pass | Focused lifecycle, strategy, Store repair-guidance, and fail-closed UI tests; full solution baseline also passes. |
| Projection from Item, fixed provider Offering, Design-selected colors, variants, and artwork assignments | Pure Application tests | Pass | `ListingProjectionBuilder` and related projection tests. |
| Tolerant full-width, top-aligned artwork placement | Pure Application tests | Pass | Matching, square, and aspect-ratio-mismatch cases covered. |
| Fixed retail and fixed profit pricing | Pure Application tests | Pass | Cost differences and invalid input covered. |
| Create, update, refresh, intent persistence, and ambiguous mutation recovery | Application tests with deterministic ports | Pass | Definitive identity, retry blocking, read uncertainty, and mutation failure paths covered. |
| Remote drift review with accept-remote and keep-local choices | Application tests plus headless journey | Pass | Integration-owned overrides persist separately from Design-owned values. |
| Printify-mediated publish/unpublish, gated to Shopify + Printify | Application and integration tests | Pass | No direct Shopify API; post-operation read-back and uncertain states covered. External hide/show semantics remain supplemental live evidence. |
| Local archive and guarded remote deletion | Application tests plus headless confirmation journey | Pass | Published, locked, uncertain, missing, and cancellation cases covered. |
| Store mapping migration and round-trip persistence | Isolated SQLite integration tests | Pass | Schema v22 migration and existing-data preservation covered. |
| Unified transport, safe diagnostics, and secret redaction | Mocked integration tests and inspection | Pass | No live endpoint in deterministic tests; telemetry records request metadata only. |
| Compact Listing-stage surface and accessible lifecycle controls | Avalonia headless construction, layout, view-model, and journey tests | Pass | Single-column UI, text states, conflict actions, focus-safe delete confirmation, and compiled bindings covered. |
| Existing Store Editor headless regression after current-main merge | Isolated regression test plus full App and solution baselines | Pass | Row whitespace selection now resolves through the window-level pointer tunnel while preserving child-button actions. |
| Live showcase-store unpublish/hide semantics | User-supervised external smoke check | Not run | Deliberately left as supplemental manual verification because it mutates the external showcase account. |

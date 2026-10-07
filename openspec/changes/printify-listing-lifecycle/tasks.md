## 1. Prototype and review gates

- [x] 1.1 Create reviewable prototype screens for unavailable connection, ready unmapped Item, saved draft, remote conflict, publishing/published, unpublished/deletion-blocked, and standalone-versus-Shopify strategy states using the existing Fusion Canvas shell.
- [x] 1.2 Capture and annotate official Printify reference screenshots or documentation views for product creation, variant visibility, pricing, publication, and hide/show behavior; record any terminology or state differences that affect the prototype.
- [x] 1.3 Run the targeted architecture, UX, API-safety, and accessibility stress review against the prototype, proposal, design, and delta specs; record findings and severity in the change artifacts.
- [x] 1.4 Resolve all blocking review findings in the proposal, design, specs, prototype, or task plan before starting production implementation; document any accepted non-blocking risks.

## 2. Application contracts and pure behavior

- [x] 2.1 Define provider-neutral external listing identity, connection readiness, synchronization, publication, conflict, and pending-operation models in the Application layer without adding Printify or Shopify references to Domain.
- [x] 2.2 Define focused application ports for Printify shop/readiness, image upload, product lifecycle, publication, and secure credential resolution, reusing compatible existing credential and catalog contracts.
- [x] 2.3 Implement and test the desired-product projection from Item, fixed-provider Offering, Design-selected colors, variants, and design-area assignments.
- [x] 2.4 Implement and test tolerant artwork placement using full target width, top alignment, zero rotation, and the documented normalized-coordinate formula, including square and aspect-ratio-mismatch cases.
- [x] 2.5 Implement and test fixed-retail-price and fixed-profit pricing policies, including per-variant production-cost differences and invalid input handling.
- [x] 2.6 Implement and test normalized remote snapshots and three-way drift comparison, including remote-only, local-only, both-changed, missing, and unchanged fields.

## 3. Persistence and migration

- [x] 3.1 Add an additive SQLite migration for Store-scoped Item-to-Printify mappings, remote identities, publication state, synchronization snapshots, upload references, and pending/uncertain operations.
- [x] 3.2 Add repository/application persistence paths with uniqueness and integrity rules for `(StoreId, ItemId)` and stable remote identity fields.
- [x] 3.3 Persist operation intent before remote mutation and definitive or reconciled outcome afterward, ensuring interrupted operations remain recoverable without duplicate creation.
- [x] 3.4 Add isolated persistence tests proving existing credentials, catalog data, Items, and Store records survive migration and that archived/local mappings remain readable.

## 4. Unified Printify integration

- [x] 4.1 Refactor or extend the Printify integration around one shared HTTP transport for authentication, serialization, cancellation, response parsing, diagnostics, rate-limit handling, and consistent error mapping.
- [x] 4.2 Implement mocked shop/readiness, image upload/reuse, product read/create/update/delete, publish, and unpublish operations behind the focused ports.
- [x] 4.3 Add representative mocked response fixtures for successful operations, invalid credentials, permission denial, missing shop/product, validation errors, locked products, rate limits, timeouts, and malformed responses.
- [x] 4.4 Verify that no Printify credential, authorization header, or raw secret is written to logs, snapshots, exports, or user-facing diagnostics.

## 5. Listing lifecycle orchestration

- [x] 5.1 Implement the application service/commands for readiness validation, preview, create, update, refresh, reconcile, publish, unpublish, local archive, and remote delete.
- [x] 5.2 Implement create/update identity handling and ambiguous-result reconciliation so uncertain requests are re-read before retry and never blindly duplicated.
- [x] 5.3 Implement remote-drift review and explicit accept-remote versus keep-local resolution, preserving Design-owned colors and source artwork ownership.
- [x] 5.4 Implement strategy-gated Printify-mediated publication and external identity persistence, including pending/unverified publication states and post-operation verification.
- [x] 5.5 Implement unpublished/unlocked/confirmed deletion guards and separate local archive behavior; ensure published, locked, and uncertain products cannot be remotely deleted.
- [x] 5.6 Add focused Application tests for every lifecycle, conflict, readiness, failure, and destructive-action scenario in `printify-listing-lifecycle`.

## 6. Store and strategy integration

- [x] 6.1 Add Store Editor validation that blocks selected Printify shop changes when mapped Items exist and reports the mapping count and safe recovery path.
- [x] 6.2 Add explicit Store Printify readiness states and repair guidance while preserving credentials, catalog data, mappings, and last-known state on failure.
- [x] 6.3 Update fulfillment-strategy capability resolution so Printify product operations are available only for Printify-enabled strategies and publish/unpublish is available only for `Shopify + Printify`.
- [x] 6.4 Test switching away from and back to Printify strategies, including preservation of mappings, disabled remote actions, readiness verification, and reconciliation-before-mutation.

## 7. Listing-stage UI

- [x] 7.1 Register the Printify tool through the existing Stage Tool Host with explicit Store, Item, Offering, Design, and strategy context requirements.
- [x] 7.2 Implement the compact readiness header, desired-product summary, read-only Design color/variant summary, artwork-area summary, and pricing policy controls.
- [x] 7.3 Implement lifecycle action availability, progress, success, failure, unavailable, pending, remote-changed, missing, and uncertain states with text labels and accessible names in addition to visual indicators.
- [x] 7.4 Implement focused conflict review, connection repair guidance, unpublish flow, and destructive remote-delete confirmation with predictable keyboard focus and cancellation behavior.
- [x] 7.5 Add deterministic Avalonia headless view tests for construction, bindings, action enablement, state visibility, conflict selection, focus return, and destructive confirmation behavior.

## 8. Criterion-level verification and delivery gates

- [x] 8.1 Map every acceptance scenario in all three delta specs to a focused test or explicit evidence method, and add missing tests before declaring the module ready.
- [x] 8.2 Add an isolated headless journey covering ready connection → preview → create/save → refresh → remote price conflict → accept-local or accept-remote resolution.
- [x] 8.3 Add an isolated headless journey for `Shopify + Printify` publication state, unpublish, and deletion gating if the final rendered UI crosses the required persistence and orchestration seams.
- [ ] 8.4 Run a limited manually supervised showcase-store smoke check only for API behavior that cannot be established through mocks, especially unpublish/hide verification; record it as supplemental evidence and never as the deterministic test baseline.
- [x] 8.5 Run strict OpenSpec validation and correct all artifact/spec errors.
- [x] 8.6 Run `dotnet test .\FusionCanvas.sln` and resolve all failures or document an explicit approved blocker.
- [x] 8.7 Complete the final criterion-by-criterion verification record before implementation is considered complete.

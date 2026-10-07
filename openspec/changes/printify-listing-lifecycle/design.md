## Context

Fusion Canvas already has Store-scoped Printify credentials, selected shop identity, catalog import, Blueprint/Offering/Variant/Placeholder data, Design-stage color selection, and Design-area assignments. It does not yet have a remote product lifecycle or a durable mapping between an Item and a Printify product.

The module crosses the Application, Integration, persistence, and Avalonia presentation layers. It also introduces user-visible remote mutations, so the design must make connection readiness, idempotency, conflicts, destructive actions, and external publication state explicit. The current Listing tool remains responsible for local listing preparation and mockups; the new Printify tool is a separate Listing-stage tool hosted by the existing Stage Tool Host.

The external boundary is Printify only. Shopify publication is requested through the selected Printify shop's connected sales channel; Fusion Canvas does not call Shopify APIs or provide Shopify-specific product editing.

## Goals / Non-Goals

**Goals:**

- Provide one predictable Printify listing lifecycle for create, update, refresh, reconcile, publish, unpublish, local archive, and safe remote deletion.
- Make Printify connection readiness a hard gate for remote actions while preserving local work and mappings when the connection fails.
- Project the current Item, fixed-provider Offering, Design-selected colors, variant rows, and design-area assignments into a complete Printify product.
- Use tolerant top-aligned, full-width artwork placement without requiring exact image dimensions or aspect ratios.
- Detect remote drift before mutation and provide explicit accept-remote or keep-local resolution.
- Derive Shopify publication availability only from `Shopify + Printify`.
- Keep the Printify integration unified at the transport and application-service level while retaining focused contracts for catalog, uploads, and products.
- Validate the user-facing flow with prototype screens, official Printify UI/reference screenshots, targeted architecture and UX audits, deterministic tests, and a limited supervised live smoke test when necessary.

**Non-Goals:**

- Direct Shopify API access or Shopify-specific variant image, price, taxonomy, SEO, or inventory operations.
- Provider-Network or Printify Choice resolution; this module requires a concrete fixed Print Provider.
- Editing Design-stage colors, variant policy, or source artwork from the Printify tool.
- Arbitrary per-variant pricing; the initial policies are fixed retail price and fixed profit amount.
- Automatic migration of mappings when a Store's selected Printify shop changes.
- Remote deletion of a published product, automatic deletion when a local record is archived, or automatic recreation of missing remote products.
- Full Printify Product Creator parity, mockup selection parity, shipping-profile administration, or a general marketplace abstraction.

## Decisions

### 1. Use one Printify integration pipeline with focused application ports

The Integration layer will provide a shared Printify HTTP transport responsible for authentication, request serialization, response parsing, cancellation, HTTP error mapping, rate-limit handling, and safe diagnostics. Focused ports will remain separate for shops/connection readiness, catalog lookup, uploads, and product lifecycle operations. A single giant client interface is rejected because it would make unrelated capabilities depend on each other and become difficult to test.

The Application layer will own the `PrintifyListingService` or equivalent orchestration service. It will build the desired product projection, validate readiness, reconcile remote state, request conflict resolution, and persist outcomes. The UI will call application commands and will not construct Printify payloads or access persistence directly.

### 2. Keep domain models provider-neutral

Printify-specific identifiers, payloads, API states, and JSON remain in Application contracts and Integration models. Domain code will retain Item, Design, Offering, Variant, and workflow invariants without referencing Printify or Shopify. This preserves the existing dependency direction and leaves room for later marketplace adapters without pretending that all marketplaces have identical lifecycle semantics.

### 3. Store one explicit external mapping per Item and Store

Persistence will add a Store-scoped Item-to-Printify mapping with stable Printify shop/product identity, optional external sales-channel identity and handle, product/publication/synchronization state, timestamps, pending-operation information, and a normalized last-synchronized snapshot. Artwork upload references will be scoped to the Printify account/shop and tied to a local asset fingerprint plus design-area/placeholder identity so unchanged files can be reused safely.

The mapping is not inferred from title, description, or SKU. Existing imported shop products remain separate from locally created products unless an explicit future adoption workflow is added.

### 4. Project Design state without adding a second variant editor

The desired product builder will read selected Design colors, Design variant rows, fixed Offering Provider variants, and Design-slot assignments. It will create enabled Printify variants only from that projection. Missing provider support or missing design-area assignments are readiness diagnostics, not opportunities for the Printify tool to mutate Design.

### 5. Use deterministic tolerant artwork placement

For each assigned image and target placeholder, use the target print-area dimensions and the artwork dimensions:

```text
scale = targetWidth / artworkWidth
x = 0.5
renderedHeight = artworkHeight * scale
y = renderedHeight / (2 * targetHeight)
angle = 0
```

This makes the rendered image span the full target width and places its top edge at the top of the target area. It accepts square and slightly inconsistent artwork dimensions. It does not silently distort the image. If authoritative target dimensions are absent, the product is not safely projectable and the tool reports that specific readiness issue.

### 6. Treat Printify as the source of truth for the external product, but not for Design ownership

Every mapped product has a normalized snapshot. Before an update, the service compares the snapshot, current local desired projection, and current remote product. Remote-only changes are presented as conflicts. Accepting a remote price, title, description, or other integration-owned value updates the local integration state and snapshot; it never changes Design colors or replaces local source assets automatically. Choosing the local value sends the current local projection to Printify.

The service will never blindly retry an ambiguous create, update, publish, unpublish, or delete. It will re-read by known identity where possible, mark unknown outcomes explicitly, and require reconciliation before another mutation.

### 7. Derive publication from fulfillment strategy

Product operations are available for `Printify` and `Shopify + Printify` when readiness passes. Publish and unpublish actions are available only for `Shopify + Printify` and are sent only to Printify. Publication state is separate from Item lifecycle status and includes pending/unverified states.

The product must be verified as unpublished and unlocked before remote deletion is offered. Local archive never sends a remote delete. A failed connection disables remote actions but does not discard mappings or local work.

### 8. Use explicit, focused UI surfaces

The Printify tool will live below the Listing stage navigator and share the existing Stage Tool Host context. Routine operations stay in the tool: readiness summary, product preview, refresh, create/update, publish/unpublish, and conflict review. Destructive deletion uses explicit confirmation. Detailed conflict diffs and connection repair guidance use expandable or focused sections so the normal Listing workspace remains compact.

The prototype review will cover at least: unavailable connection, ready unmapped Item, saved draft, remote conflict, publishing/published, unpublished/deletion-blocked, and standalone-vs-Shopify strategy differences. The screens will preserve clear text labels, keyboard focus, accessible names, and visible reasons for disabled actions.

### 9. Prototype and stress-audit gates precede implementation

Before implementation approval, the change will receive:

- a UI prototype review against the existing shell and Listing-stage tool host;
- a Printify screenshot/reference comparison covering product creation, publication, variant visibility, pricing, and hide/show behavior;
- an architecture audit covering ports, persistence ownership, retry/idempotency, credential boundaries, and failure states;
- a UX audit covering progressive disclosure, focus, conflict comprehension, disabled states, destructive confirmation, and re-entry after failure;
- an API safety audit confirming that all baseline tests are mocked and that live mutations are limited to an explicitly supervised showcase-store check.

These are review gates for this module, not a new permanent QA process for unrelated features.

## Risks / Trade-offs

- [Printify connection or credentials become unavailable] → Fail closed for remote mutations, preserve local mappings and last-known state, expose repair guidance, and require readiness plus reconciliation before resuming.
- [Create or publish request succeeds but the response is lost] → Persist an uncertain operation state, re-read by known shop/product identity where possible, and prohibit blind retries.
- [Remote product changed directly in Printify] → Compare against the last synchronized snapshot and require an explicit resolution before overwriting either side.
- [Printify catalog/provider data changes after mapping] → Revalidate blueprint, fixed provider, variants, placeholders, and design-area compatibility before mutation; surface a readiness problem rather than silently remapping.
- [Artwork dimensions differ from the placeholder] → Use deterministic top-aligned full-width placement; accept aspect-ratio differences while retaining a clear preview/readiness message for unusual results.
- [Top-aligned full-width artwork extends beyond the target height] → Preserve the requested placement rule and surface the resulting calculated placement in the preview; do not invent a second fit policy in this module.
- [Shop switching invalidates many mappings] → Block selected-shop changes once mappings exist and preserve the old shop identity in every mapping; consider bulk disconnect/migration only as a later module.
- [Remote deletion removes the connected sales-channel listing] → Require unpublished and unlocked verification plus explicit confirmation; keep local archive separate.
- [Printify product locking or rate limits delay operations] → Represent pending/locked states, use bounded retry/backoff for transient reads, and require refresh rather than repeated mutation.
- [A single universal Printify service becomes a god object] → Share transport and error policy, but keep focused capability ports and an application orchestration service.
- [Prototype screens hide important lifecycle states] → Review the full state matrix, including empty, loading, blocked, conflict, uncertain, success, and destructive paths, before implementation.

## Migration Plan

1. Add persistence for Store-scoped Printify mappings, snapshots, operation state, external publication identity, and upload references using an additive migration. Existing Store metadata, credentials, catalog data, and Items remain unchanged.
2. Ship the unified Printify transport and focused client ports behind mocked contracts. Keep existing catalog and credential flows working through compatible adapters or a shared transport migration.
3. Add application projections and lifecycle commands without enabling remote mutations until readiness, conflict, and persistence tests pass.
4. Add the Listing-stage tool and headless UI coverage. Existing basic Listing and mockup behavior remains available through the Stage Tool Host.
5. Complete prototype, screenshot, architecture, UX, and API safety audits; resolve findings before implementation approval or return them to this change's artifacts.
6. Run the deterministic solution test baseline and strict OpenSpec validation. If a supervised live check is needed, use only the showcase Store and record it as supplemental evidence.

Rollback is additive: disable the Printify tool and remote commands while preserving mapping records and local data. Do not automatically delete remote products during rollback. Any future migration failure must leave the pre-existing catalog, credential, Store, and Item data readable.

## Implementation Plan

### Application and domain-facing contracts

- Add provider-neutral listing lifecycle concepts for desired product data, external identity, synchronization state, publication state, conflict fields, readiness diagnostics, and pending/uncertain operations.
- Add focused ports for Printify connection/shop readiness, image upload, product retrieval/create/update/delete, and publish/unpublish operations. Keep credential retrieval behind the existing secure credential store contract.
- Add an application orchestration service/commands for preview, create, update, refresh, reconcile, publish, unpublish, local archive, and remote delete.
- Add pure projection and comparison components for Design-to-Printify payload construction, price calculation, snapshot normalization, and three-way drift detection.

### Integration

- Refactor or extend the existing Printify integration around one shared HTTP transport and consistent result/error mapping.
- Implement Printify shop validation, image upload/reuse, product endpoints, publication endpoints, response parsing, bounded retry for safe reads, and rate-limit/locked-product diagnostics.
- Add deterministic mocked clients and representative API fixtures for success, validation errors, unauthorized access, not-found, locked, rate-limited, timeout, and malformed responses.

### Persistence and compatibility

- Add Store-scoped mapping and snapshot records with stable uniqueness for `(StoreId, ItemId)` and remote identity fields.
- Persist operation state before remote mutation and finalize it only after a definitive response or reconciliation result.
- Store normalized snapshots and upload fingerprints without storing credentials or raw authorization headers.
- Preserve existing catalog/import records and support existing workspaces through an additive migration.

### UI and prototype

- Register a second Listing-stage tool through the existing Stage Tool Host.
- Follow the existing single-column stage-tool composition rather than introducing a wide split-pane layout. The tool begins with a compact connection-status pill, then uses stacked label/control rows.
- Keep Item, Design, Blueprint, Provider, shop, selected colors, variant policy, and artwork assignments read-only because they are owned by other workflow stages or Store configuration. Keep Printify-specific listing values such as title, description, shipping/default settings, and the selected initial pricing policy editable with defaults.
- Place state-dependent lifecycle actions at the bottom of the same column. Do not use color as the only status signal; pair any indicator with a text label and actionable explanation.
- Implement a compact readiness header, desired-product summary, variant/color read-only summary, artwork-area summary, pricing policy controls, lifecycle actions, status text, conflict review, and focused delete confirmation.
- Keep Design color editing in the Design tool and link back to it when readiness fails.
- Create reviewable prototype screens before production UI implementation, using the existing shell, spacing, theme, accessibility, and focus conventions.

### Verification

- Map every scenario in the delta specs to focused domain/application/integration/persistence tests or deterministic Avalonia headless view tests.
- Add one critical headless user journey covering ready connection → preview → create/save → refresh → conflict decision → update, using isolated persistence and mocked Printify services.
- Add a second headless journey for `Shopify + Printify` publication state and unpublish/delete gating if the final UI crosses enough rendered and persistence seams to justify it.
- Perform the planned prototype, screenshot-reference, architecture, UX, and API-safety audits before implementation approval.
- Run `openspec validate` and `dotnet test .\FusionCanvas.sln` before considering the module verified.

## Open Questions

No product decision is blocking proposal approval. The exact external effect of Printify's unpublish request will be verified against the connected showcase Store and the final remote GET state before implementation acceptance; the application will model the result as pending/unverified when the API does not provide a definitive response.

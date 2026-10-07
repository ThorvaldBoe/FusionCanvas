## Origin

- Primary issue: [#135](https://github.com/ThorvaldBoe/FusionCanvas/issues/135)

## Why

Fusion Canvas currently has Printify credentials and catalog data, but it does not yet provide a safe, coherent way to turn an Item's current Design and listing information into a maintained Printify product. The missing capability is critical because it is the bridge between the local creative workflow and a sellable product, and because remote mutations can create expensive or confusing drift if they are not synchronized deliberately.

This module establishes a small, predictable Printify listing lifecycle: create or update a product through the Printify API, optionally publish or unpublish it through Printify for Shopify-connected Stores, detect remote changes, and make the connection and publication state visible to the user.

## What Changes

- Add a Listing-stage Printify tool hosted through the existing Stage Tool Host.
- Consume the Item's title, optional description, selected Blueprint, fixed Print Provider, Design-stage selected colors, variant rows, and design-area artwork assignments.
- Treat Design as the authority for selected colors and variant policy; the Printify tool SHALL NOT edit Design color selections.
- Exclude Provider-Network offerings, including unresolved Printify Choice offerings, until concrete-provider support exists.
- Upload required artwork and create or update a complete Printify product, including enabled variants, per-variant prices, and one or more Printify print areas.
- Use tolerant artwork placement: align artwork to the top of the target design area and scale it to the full available width; do not reject minor dimension differences or require exact aspect-ratio matches.
- Support two initial pricing policies: one fixed retail price for all enabled variants, or one fixed profit amount calculated per variant.
- Expose publishing and unpublishing only when the Store fulfillment strategy is `Shopify + Printify`; use Printify API operations only and make no direct Shopify API calls.
- Provide lifecycle management for create, update, refresh/reconcile, publish, unpublish, local archive, and explicit remote deletion of an unpublished product.
- Prevent remote deletion while a product is published, locked, or in an uncertain remote state.
- Persist stable Store/shop/product/external publication identities and a normalized synchronization snapshot.
- Detect remote changes before mutation and let the user either accept the remote value into Fusion Canvas or update Printify from Fusion Canvas.
- Treat an unavailable or unverified Printify connection as a fail-closed remote-operation state while preserving local work and mappings.
- Prevent changing a Store's selected Printify shop once mapped Items exist.
- Add clear, accessible connection, synchronization, product, and Shopify publication states; status SHALL not rely on color alone.
- Add mocked API contract, application, persistence, and headless UI coverage, plus a prototype-screen review and targeted architecture/UX stress audit before implementation approval.

## Capabilities

### New Capabilities

- `printify-listing-lifecycle`: Store- and Item-scoped creation, synchronization, publication, conflict resolution, and safe management of Printify products.

### Modified Capabilities

- `store-management`: selected Printify shop changes are blocked once the Store has mapped Printify products, and Printify connection readiness is surfaced for integration actions.
- `store-fulfillment-strategy`: Printify product actions are available for `Printify` and `Shopify + Printify`, while Shopify publication actions are derived exclusively from `Shopify + Printify`.

## Impact

- **Application:** new Printify listing use cases, connection/readiness policy, desired-product projection, remote-drift comparison, conflict-resolution workflow, pricing policy, and application-facing Printify product ports.
- **Integration:** a unified Printify product/upload client behind application contracts, mocked service implementations, request/response mapping, retry and ambiguous-result handling, and secure credential/shop resolution.
- **Persistence:** Store-scoped external product mappings, publication identity, synchronization snapshots, operation state, and artwork-upload references; migration must preserve existing catalog and credential data.
- **App:** a second Listing-stage tool, readiness checklist, lifecycle actions, conflict review, progress/error states, accessible publication indicators, and focused destructive confirmations.
- **Existing behavior:** current catalog import and credentials remain read-only/setup capabilities and must continue to work. No direct Shopify integration is introduced. Live Printify calls remain outside the deterministic test baseline and are limited to supervised showcase-store checks when necessary.
- **Review artifacts:** the design phase will include prototype UI screens for the primary lifecycle states, a Printify-screenshot comparison pass, and an architecture/UX stress review before implementation tasks are considered ready.

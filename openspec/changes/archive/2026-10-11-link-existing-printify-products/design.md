## Context

The Listing-stage Printify tool already has a Store-scoped lifecycle service and persistent `(StoreId, ItemId)` mapping. It can read a product only when a mapping already exists. The Store credential verifier correctly uses `GET /v1/shops.json`, while the lifecycle connection adapter currently calls an unsupported `GET /v1/shops/{shop_id}.json` route. Printify's product detail response includes its `id` and `shop_id`, and the detail endpoint is scoped by shop.

The new adoption flow is separate from the existing create/update path. It verifies the candidate, presents its identity to the user, and only changes local mapping state after confirmation. It never creates, edits, publishes, or deletes a remote product.

## Goals / Non-Goals

**Goals:**

- Correct the listing readiness probe and require the configured shop to appear in the authenticated account's shop list.
- Let the user inspect and link an existing product ID for the active Item and Store.
- Verify the candidate through the selected shop's product endpoint and compare the returned product and shop identities exactly.
- Require confirmation before linking or replacing an existing mapping; retain the old mapping on failure or cancellation.
- Preserve existing synchronization data when relinking the same product and initialize an adopted mapping from the verified remote snapshot.

**Non-Goals:**

- Search products or guess identity from titles, descriptions, SKUs, or catalog data.
- Change remote product data or infer local Design state from the product.
- Add shop migration, a general marketplace abstraction, or schema migration.
- Allow replacing a mapping while its operation is pending or uncertain.

## Decisions

### Verify connection through the shops collection

`PrintifyListingClient.CheckAsync` will call the already-used account endpoint `GET /v1/shops.json`, validate a bounded JSON array of shop records, and require an exact match for the selected numeric shop ID. A missing selected shop or malformed response keeps the connection unavailable. This reuses Printify's supported discovery endpoint and avoids treating an arbitrary successful response as proof that the Store's configured shop is accessible.

### Reuse the selected-shop product read

The application lifecycle service will use `IListingProductPort.GetAsync(shopId, productId)` for candidate verification. Integration parsing will retain the returned `shop_id` and whether the response explicitly contained the requested product ID. Linking requires both exact values. The endpoint's shop scope and response identities jointly establish that the product belongs to the selected shop; 404, malformed identity, mismatches, or transport failures fail closed.

### Separate verification from confirmation

The tool provides an editable product ID and a Verify action. On success it displays the verified ID and remote title, then shows a focused confirmation naming the Item, shop, product ID, and title. Confirm re-reads the product and rechecks duplicate mapping and Store scope before saving. Cancel clears only the pending confirmation. A failed second read or a changed candidate leaves the current mapping untouched.

### Persist through the existing lifecycle mapping

Add focused methods to `ListingLifecycleService` for candidate verification and confirmed linking. The mapping remains the current `ExternalListingMapping` record. A newly adopted product records its verified identity, remote snapshot, integration-owned values, publication state, and a synchronized timestamp. Replacing a mapping preserves its original `CreatedAt` and updates only after all checks succeed. Existing `(StoreId, ItemId)` uniqueness and remote-identity uniqueness remain enforced by the repository. A product already linked to another Item in the same Store is rejected.

Products with pending or uncertain operations cannot be replaced because the remote outcome must first be reconciled. A read-only Item can view and copy its current product ID but cannot verify or change a link.

### Keep the workflow compact in the Listing tool

Show the current mapped product ID in a read-only selectable text field so standard keyboard copy works. Keep the candidate ID input and Verify action next to the current identity, then reveal the confirmation only after verification succeeds. Display existing loading, unavailable, validation, error, success, and cancellation states inline. No separate modal is needed because the tool already provides inline confirmation for destructive lifecycle actions.

## Risks / Trade-offs

- [The account shop list is malformed or unexpectedly large] → Keep the existing bounded response limit, validate every shop identity, and fail closed without exposing response content.
- [A product is deleted or moved after the preview] → Re-read it when the user confirms and preserve the existing mapping if verification no longer succeeds.
- [A product ID is already attached to another Item] → Check Store-scoped mappings before confirmation and again before persistence; rely on repository identity constraints as the final guard.
- [Adopting a product creates immediate local/remote field differences] → Seed the synchronization snapshot and integration-owned values from the verified remote product; do not overwrite the remote listing during link.
- [The Listing tool expands vertically] → Keep ID controls in one compact row and show verification details only after a candidate is accepted by the API.

## Migration Plan

No schema migration is expected. The existing mapping row can hold the verified shop/product identity, snapshot, integration-owned values, and publication state. Older clients continue to read the same schema. Rollback disables the link controls and leaves persisted mappings intact.

## Open Questions

None. Product identity, selected-shop scope, confirmation, duplicate protection, and failure behavior are bounded by issue #915 and existing Store/Item mapping invariants.

## Implementation Plan

1. **Integration** — Update `src/FusionCanvas.Integration/Stores/Printify/PrintifyListingClient.cs`: replace the shop-detail request with `shops.json`; parse and match the selected shop ID; keep failure responses bounded and safe. Extend remote-product parsing to retain the actual returned product ID and `shop_id` for strict adoption verification.
2. **Application** — Extend `ListingRemoteProduct.cs` with optional verified identity metadata. Add focused verify and confirm-link operations to `ListingLifecycleService.cs`, using the existing connection and product ports. Validate Store ownership of the Item, request shop identity, remote identities, duplicate product mappings, and pending/uncertain operations. Persist through the current repository snapshot path, initialize the snapshot and integration-owned values from the remote state, and retain `CreatedAt` for replacements.
3. **App** — Update `PrintifyListingStageToolViewModel.cs` and the Printify section in `src/FusionCanvas.App/Views/MainWindow.axaml`: expose current ID, candidate ID input, verify/confirm/cancel commands, remote title and confirmation text, disabled/read-only states, and inline outcomes. Reload the saved mapping after success. Keep the current ID keyboard-selectable for copying.
4. **Tests** — Update `tests/FusionCanvas.Integration.Tests/Stores/Printify/PrintifyListingClientTests.cs` for `/shops.json`, selected-shop present/missing/malformed responses, and product/shop identity extraction. Extend `tests/FusionCanvas.Application.Tests/Listings/ListingLifecycleServiceTests.cs` for verified linking, duplicate prevention, cancel/no-save, product/shop mismatch, stale confirmation, and uncertain mapping guards. Extend `tests/FusionCanvas.App.Tests/StageToolViewModelsTests.cs` with headless binding, confirmation, cancel, read-only, and current-ID visibility checks.
5. **Verification** — Run focused Application, Integration, and App tests; strict OpenSpec validation; then `dotnet test .\FusionCanvas.sln -m:1`. Record each acceptance scenario and its evidence in `verification.md`. No live Printify mutation or desktop account smoke test is required because all linking is read-only remotely.

## Acceptance-to-Verification Mapping

| Acceptance scenario | Planned evidence |
| --- | --- |
| Selected shop is present and valid | Mocked Integration response plus lifecycle readiness test |
| Selected shop is absent or response is invalid | Mocked Integration failure tests; lifecycle remains unavailable |
| Product ID resolves in the selected shop with matching returned identity | Mocked API and Application verification tests |
| Product missing, wrong ID/shop, malformed response, or unavailable | Mocked API/Application failure tests; prior mapping remains unchanged |
| User confirms a valid product link | Application persistence test and Avalonia headless command/binding test |
| User cancels or duplicate mapping exists | Application no-write test and Avalonia headless cancellation test |
| Existing ID is visible and copyable; read-only Item cannot change it | Avalonia headless constructed view and command-state test |

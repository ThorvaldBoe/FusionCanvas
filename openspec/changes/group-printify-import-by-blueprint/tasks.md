## 1. Blueprint and Provider retrieval contracts

- [x] 1.1 Add Printify Application contracts and result models to identify distinct Blueprint choices, the Providers used by each Blueprint in the selected shop, and selection by stable Blueprint ID; retain product-level listing import APIs.
- [x] 1.2 Update Integration shop retrieval to group all product pages by Blueprint ID, collect distinct used Provider IDs, and retain shop-product context for the selection summary.
- [x] 1.3 Retrieve full catalog data for selected Blueprint/used-Provider pairs, resolve Option and Option Value labels for all provider Variant IDs, retain exact supported combinations and Placeholder compatibility, and include only enabled and available Variants.
- [x] 1.4 Add Integration tests for paging, duplicate products per Blueprint, multiple Providers per Blueprint, disabled/unavailable Variants, complete combinations, option-value resolution, malformed responses, and provider failures.

## 2. Canonical catalog persistence

- [x] 2.1 Add a catalog import mapping path that persists one Store-scoped Blueprint per Printify Blueprint ID and one Offering per Blueprint/Provider pair, while retaining product-level mapping for listing import and Variant Setup refresh.
- [x] 2.2 Preserve stable Option, Option Value, Variant, and Placeholder identities and relationships across repeated imports; keep the selected multi-Blueprint import atomic.
- [x] 2.3 Preserve local-only records and older product-level import records without merging, archiving, or deleting them; leave catalog rows absent from later shop responses untouched.
- [x] 2.4 Add Application and persistence tests for canonical grouping, multiple used Providers, full sellable Variant sets, exact Option memberships, idempotency, local relationships, legacy-record preservation, and atomic failure.

## 3. Blueprint-level Store Editor selection

- [x] 3.1 Update the import ViewModel to present one selectable row per distinct Blueprint and submit stable Blueprint IDs while preserving loading, empty, error, cancellation, and stale-context behavior.
- [x] 3.2 Update the Store Editor virtualized grid to show Blueprint identity, used Providers, and shop-product context/count with accessible names and stable selection through row recycling.
- [x] 3.3 Update ViewModel and Avalonia headless tests for grouped rows, accessible labels, selection, focus, cancellation, and preservation of unsaved catalog drafts.

## 4. Acceptance verification and completion

- [x] 4.1 Map every changed OpenSpec scenario to focused test evidence and record criterion-level results and limitations in `verification.md`.
- [x] 4.2 Run the focused Integration, Application, persistence, ViewModel, and headless Store Editor checks; correct failures and rerun affected checks.
- [x] 4.3 Run `dotnet test .\FusionCanvas.sln` and strict `openspec validate`; record results in `verification.md`.
- [x] 4.4 Review changed-scope behavior, UI safety, Store scoping, persistence identity, and compatibility with the separate Niche-level Printify listing import and linked-listing Variant Setup; update artifacts for any accepted correction.

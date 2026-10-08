## 1. Domain policies and workspace contract

- [x] 1.1 Add validated domain records for Item Listing Details, per-Variant selling/fulfillment terms, and immutable migration-history snapshots.
- [x] 1.2 Implement and test complete normalized Variant signatures and exact one-to-one price/cost matching, including ambiguous or incomplete catalog data.
- [x] 1.3 Add Listing Details and migration-history collections to `WorkspaceSnapshot` and validate Item, Store, active Offering, and Variant ownership.

## 2. Local Listing Details use cases

- [x] 2.1 Implement load/save use cases for independent listing title and description, shared currency, per-Variant selling price/cost, shipping terms, derived gross profit, and missing-value diagnostics.
- [x] 2.2 Add strategy availability policy for Manual and enabled ShopifyManual strategies, excluding Printify strategies without external calls.
- [x] 2.3 Implement Offering migration preview and confirm operations with exact-match transfer, shipping-term reset, Design reset diagnostics, immutable source history, and atomic failure behavior.
- [x] 2.4 Route Design Offering replacement through migration only when saved manual Listing Details exist; preserve the existing direct replacement behavior otherwise.

## 3. Persistence and workspace transfer

- [x] 3.1 Add a versioned SQLite schema migration for active Listing Details, Variant terms, and read-only migration history without fabricating data for existing Items.
- [x] 3.2 Update SQLite snapshot save/load order and invariant validation for the new records and atomic Offering migration.
- [x] 3.3 Update workspace transfer filtering and merge behavior to retain Item Listing Details and history with their owning Item.
- [x] 3.4 Add isolated SQLite tests for schema upgrade, active/history round-trip, Variant references, migration atomicity, and rollback on invalid data.

## 4. Listing-stage interface

- [x] 4.1 Register an Item-bound Listing Details tool in the built-in stage-tool registry with Store strategy, Item, and Listing-stage requirements.
- [x] 4.2 Add the Listing Details view model and connect it to existing tool-content resolution and workspace services.
- [x] 4.3 Add the local editor for copy, currency, Variant amounts, derived profit, selected fulfillment context, one shipping profile, and collapsed read-only history.
- [x] 4.4 Add a focused migration review with exact-match counts, reset summary, keyboard-accessible Confirm/Cancel, and recoverable errors.
- [x] 4.5 Add deterministic view-model tests and Avalonia headless coverage for strategy visibility, field bindings, loading/empty/read-only states, and migration review actions.

## 5. Acceptance verification and completion gates

- [x] 5.1 Map every scenario in all three delta specs to test results and evidence in `verification.md`; address any failed criterion and rerun its focused checks.
- [x] 5.2 Review changed-scope behavior, migration safety, Store isolation, historical data retention, and absence of external calls against the accepted specs.
- [x] 5.3 Run strict OpenSpec validation for the change and correct all reported issues.
- [x] 5.4 Run `dotnet test .\FusionCanvas.sln -m:1` and record the final result in `verification.md`.

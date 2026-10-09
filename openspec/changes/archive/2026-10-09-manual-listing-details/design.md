## Context

The Listing stage already hosts local mockup preparation and a separate Printify lifecycle. `ItemListingConfiguration` anchors each Item to one selected Blueprint Offering in Design. The Listing Details capability adds local copy, per-Variant amounts, and manual shipping terms for strategies that do not use Printify.

Design currently replaces an Item's selected Offering by clearing selected Colors, Design Variant rows, row Color values, artwork slot assignments, and artwork-target preferences. Design assets and Item identity remain. When saved manual Listing Details exist, that replacement must become a confirmed, atomic migration that also preserves the previous setup's data.

The Store strategy is the visibility boundary. `Manual` is currently enabled; `ShopifyManual` may expose the same local tool if enabled later. `Printify` and `ShopifyPrintify` continue using the Printify workflow. The new tool itself never communicates externally.

## Goals / Non-Goals

**Goals:**

- Keep customer-facing listing copy separate from Item working copy while using current Item values as first-edit defaults.
- Record one currency, a selling price and expected non-shipping fulfillment cost per active Variant, and one manual shipping profile for the selected Offering.
- Show a calculated gross profit only when its inputs are complete.
- Keep at most one active fulfillment setup for an Item and make Offering changes deliberate, reviewable migrations.
- Retain prior setup values as readable, immutable history and avoid applying old shipping terms to a new service.
- Preserve workspace data across reload, transfer, and SQLite schema upgrades.

**Non-Goals:**

- Marketplace-specific tags, marketplace publication, Shopify/Printify synchronization, live carrier rates, shipping label purchase, order management, inventory, taxes, or per-destination shipping tables.
- Multiple simultaneous fulfillment alternatives for one Item.
- Automatically assigning prices to Variants that do not exactly match the source Variant semantics.
- Migrating Design files themselves; migration retains Item-linked Design assets while clearing Offering-bound Design selections that the current replacement already clears.

## Decisions

### Store strategy controls tool availability

Register Listing Details as an Item-bound Listing stage tool. The host presents it for `Manual` and for `ShopifyManual` only when that strategy is enabled. Printify strategies do not expose it; their Printify lifecycle stays separate. The existing mockup tool remains independently available. Preserve the last selected tool only while it remains available in the current context; otherwise select the first available Listing tool and persist the selection through the existing stage-tool host behavior.

### Keep local Listing Details separate from Item metadata

Use a normalized active record keyed by Item identity, with title, description, currency, and one setup-level shipping profile. Seed a new title and description draft from the Item's working values. Persist these as independent local values on the first confirmed edit. Continue using Item tags as-is.

Each active Offering Variant has an optional selling price and expected non-shipping fulfillment cost. The shared currency is an ISO currency code selected for the active setup. Missing values are null and remain visibly incomplete. Gross profit is derived as:

```text
selling price + customer shipping charge
- expected non-shipping fulfillment cost - expected seller shipping cost
```

Do not persist the derived result. Do not show it until all four monetary inputs for that Variant are present.

The shipping profile contains one user-entered option name, customer-facing shipping charge, expected shipping cost to the creator, and delivery estimate. These are user-maintained notes and values, not provider-validated terms. A new setup after migration starts without shipping values.

### Migrate only exact Variant matches

When an Item with saved Listing Details selects a different active same-Store Offering in Design, prepare a preview before any mutation. Show source and target, exact-match transfer counts, destination Variants needing new prices, shipping data reset, and Offering-bound Design state that will be cleared. The user must confirm.

Build a Variant signature from the complete set of its Option Kind, normalized Option name, and normalized Option Value. Trim and Unicode-normalize each string, compare ordinally without case, and sort the tuples before comparison. Transfer a source price/cost pair only when the complete signature matches exactly one target Variant. Do not match by Variant display name alone, partial options, color-only equivalence, or guessed provider IDs. Unmatched target prices remain null.

On confirmation, snapshot the source Listing Details and Variant prices into a read-only migration-history entry, copy title, description, and currency to the active Listing Details, build target-Variant terms from exact matches, clear the new setup's shipping values, and apply the existing Design replacement reset in one workspace snapshot mutation. Persist the full result atomically. Cancelling or failing persistence leaves the old Offering and all source data active and unchanged. A user may migrate again later; each migration adds a history entry while maintaining one active setup.

The history snapshot records source Offering identity and display name, copy, currency, shipping terms, Variant signatures and amounts, and migration time. It has no live foreign-key dependency on catalog rows, so later catalog archival or removal cannot silently erase the record. History is visible read-only in Listing Details.

If an Item has no saved manual Listing Details, retain the current Design Offering replacement behavior. A same-Offering selection is not a migration.

### Save confirmed edits using current Item safety rules

Follow the existing Listing-stage editability policy. Save title and description on field commit following the Item Inspector pattern; save each validated amount/profile edit atomically and report failures inline. On load, an Item without details shows an empty editor seeded from its working copy but does not create a persisted record merely by opening the tool. Keep pending text safe when context changes. Protected Items remain read-only.

### Use normalized workspace persistence

Add domain snapshot collections for active manual Listing Details, active Variant terms, and migration-history snapshots. Persist them in SQLite tables with Item-scoped ownership and transaction-backed snapshot replacement. Version the schema; an upgrade creates empty structures and does not infer title, description, currency, prices, or shipping values for existing Items. Update snapshot validation and workspace transfer filtering so details, Variant terms, and history travel with their Item and selected Offering context.

## Risks / Trade-offs

- **[Shipping terms can become stale after switching providers]** → Clear them for the destination setup, retain the source values in history, and disclose this in migration review.
- **[Variant option labels may differ even when products seem similar]** → Copy amounts only for full normalized semantic matches and leave everything else blank.
- **[Migration touches Design and Listing data together]** → Build one application-level migration result and persist one complete workspace snapshot; verify failure and cancellation leave source state intact.
- **[History can grow with repeated changes]** → Keep one concise record per confirmed migration with no files or duplicated assets.
- **[No Store currency setting exists]** → Select one supported ISO currency code per active manual Listing Details profile and apply it to all associated amounts.

## Migration Plan

1. Add the next versioned SQLite schema migration for Listing Details, Variant terms, and migration history.
2. Load previous schema versions with empty new collections; do not generate manual details for existing Items.
3. Save and reload new records through the existing transaction-backed repository.
4. During normal Offering changes with saved manual details, use the migration review and snapshot operation; no external system is contacted.
5. If the workspace database upgrade fails, preserve the prior database version and report the existing recoverable schema-upgrade error.

Rollback is an application release rollback against the versioned database. The schema migration is additive, and the previous application can continue to ignore the new tables according to the repository's supported downgrade policy.

## Open Questions

None for the scoped first version. Currency is selected per active setup because Store currency is not currently a persisted setting. The migration deliberately leaves unmatched values blank and shipping terms unset for user review.

## Implementation Plan

1. **Domain model and policy**
   - Add focused types under `src/FusionCanvas.Domain/Products/` for Item Listing Details, Variant selling/fulfillment amounts, and immutable migration-history snapshots.
   - Validate stable Item/Variant IDs, active one-setup invariants, optional non-negative decimal amounts, currency code, text normalization, and timestamps.
   - Add a pure migration planner that validates same-Store active Offerings, creates complete normalized Variant signatures, builds an exact one-to-one match map, and returns transfer/reset diagnostics without mutating state.
   - Add the three new collections to `src/FusionCanvas.Domain/Workspace/WorkspaceSnapshot.cs` and enforce Item/Offering/Variant ownership in workspace snapshot validation.

2. **Application use cases and Design integration**
   - Add `src/FusionCanvas.Application/Listings/ManualListingDetailsService.cs` and focused request/result contracts for load, save, preview migration, and confirm migration.
   - Keep all policy in Application/Domain; no Avalonia types or SQLite references.
   - Route `DesignStageService` Offering replacement through the migration service only when the Item has persisted manual Listing Details. Retain the existing direct path for Items without a details record.
   - Make confirmation one application operation that archives the source details, applies target Offering configuration, resets the existing Offering-bound Design projections, copies only exact-match Variant terms, clears shipping values, updates Item timestamps, and returns the authoritative migrated projection.
   - Reject stale source/target IDs, archived or cross-Store Offerings, repeated confirmations against an already changed source, and ambiguous Variant signatures before persistence.

3. **SQLite and workspace compatibility**
   - Update `SqliteDatabaseSchema` and the versioned migration logic in `SqliteWorkspaceRepository` for active Listing Details, per-Variant terms, and history snapshots.
   - Update full-snapshot delete/insert/load ordering, foreign-key validation, and snapshot invariant checks. Keep migration-history catalog values as immutable snapshot data rather than foreign-keyed live catalog rows.
   - Update `WorkspaceSnapshotFilter` and `WorkspaceTransferService` so active details and history stay with transferred Items and their catalog context.
   - Add isolated SQLite tests for new-schema creation, prior-version upgrade without fabricated details, round-trip values/history, and transaction rollback on invalid references.

4. **Listing-stage UI and strategy routing**
   - Register an Item-bound `Listing details` tool in `src/FusionCanvas.Application/StageTools/BuiltInStageTools.cs` with Store, Item, Listing stage, and Manual/ShopifyManual strategy requirements.
   - Extend `ListingCapabilityResolver` or a focused listing availability policy without allowing the view model to infer strategy from text.
   - Add a focused App view model under `src/FusionCanvas.App/StageTools/` and connect it through `BuiltInStageToolContentResolver`, `MainWindowViewModel`, and the stage-tool view in `MainWindow.axaml` (or a dedicated AXAML view if the existing host allows a clean extraction).
   - Present listing copy first, followed by currency and per-Variant amounts, then the one selected Offering and its shipping profile. Show derived gross profit only when all inputs are present.
   - Provide an explicit migration review with source/target names, exact-match/missing-Variant counts, fields carried/reset, and Offering-bound Design data reset. Confirm and Cancel are keyboard-accessible; the confirmation does not discard input on cancellation.
   - Keep history collapsed by default and readable-only when expanded. Show empty, loading, read-only, saved, validation-error, persistence-error, and no-Offering states.

5. **Focused verification**
   - Domain tests: amount/currency validation, margin formula, signature normalization, exact one-to-one mapping, and unmatched values.
   - Application tests: manual Details load/save, strategy gating, preview diagnostics, confirm/cancel behavior, reset semantics, atomic failure, and stale request rejection.
   - Integration tests: schema upgrade and round-trip of active and historical data.
   - App tests: framework-free view-model editing states plus an Avalonia headless view test for strategy-based tool visibility, accessible field bindings, and migration review confirmation/cancellation because selection, visibility, and routed control state carry framework risk.
   - No Appium journey is warranted: the module uses local controls with no native-window, OS, or external-service boundary. Deterministic application, persistence, and headless view tests cover the user-visible risk.

## Acceptance-to-Verification Mapping

| Acceptance scenarios | Planned verification |
|---|---|
| Manual/ShopifyManual visibility and Printify exclusion; Store context change | Listing availability policy tests and Avalonia headless stage-tool selector test |
| Copy defaults, independent edits, tag reuse, empty copy | Application service tests and headless field binding test |
| Per-Variant amounts, missing values, invalid amounts/currency, no Variants | Domain policy tests and Application validation tests |
| Shipping values, incomplete shipping, no Offering | Application service tests and headless empty/blocked state test |
| Migration preview, exact match, unmatched Variant, Design reset, confirm, history | Migration planner and Application service tests; SQLite history round-trip test |
| Migration cancellation, invalid/stale source, persistence failure | Application tests against deterministic repository fakes and isolated SQLite rollback test |
| Read-only Item, pending text, local save failure | Application/view-model tests and Avalonia headless editing-state test |
| Existing workspace upgrade without fabricated data | Isolated SQLite schema migration test |
| Strict requirement syntax and traceability | `openspec validate --strict` for this change and criterion-level review of every scenario |

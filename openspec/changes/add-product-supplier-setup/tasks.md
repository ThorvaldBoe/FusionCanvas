## 1. Domain model and contracts

- [ ] 1.1 Add product-blueprint, fulfillment-kind/offering, variant, design-area, and Item-target Domain records with focused invariant tests.
- [ ] 1.2 Extend `WorkspaceSnapshot` with default-compatible catalog and target collections and update shared test fixtures.
- [ ] 1.3 Add Application requests, summaries, result types, and service contracts for Store catalog management and Item target selection.

## 2. Application behavior

- [ ] 2.1 Implement Store-scoped catalog load/create/update flows with fixed-provider and Choice-network validation.
- [ ] 2.2 Implement variant/area validation, applicable-variant ownership checks, and recoverable failure outcomes.
- [ ] 2.3 Implement reference-aware catalog removal that blocks records selected by Items or containing dependent catalog data.
- [ ] 2.4 Implement Design-stage target load and atomic replace selection, including Item/area Store ownership and editability checks.
- [ ] 2.5 Add deterministic Application tests for every product-supplier and target-selection acceptance scenario.

## 3. Local persistence

- [ ] 3.1 Add SQLite tables, indexes/foreign keys, schema-version migration, and safe migration coverage for catalog and target records.
- [ ] 3.2 Extend snapshot save/load ordering and repository validation for the new relations.
- [ ] 3.3 Add isolated SQLite round-trip, migration, invalid-reference, and deletion-safety tests.

## 4. Store Management experience

- [ ] 4.1 Wire catalog services through the workspace composition root and Store Management ViewModel.
- [ ] 4.2 Add the Products & fulfillment Store Editor tab with Store-filtered product and offering selection, useful empty state, and read-only archived-Store behavior.
- [ ] 4.3 Implement explicit catalog drafts, Save/Cancel, unsaved-change routing, deletion confirmation, error presentation, and keyboard focus behavior.
- [ ] 4.4 Add focused ViewModel and Avalonia headless tests for tab state, empty/read-only state, draft discard behavior, and meaningful bound controls.

## 5. Design-stage target selection

- [ ] 5.1 Extend the Design-stage ViewModel and view with compact target guidance, no-target state, multi-selection, inline failures, and existing read-only behavior.
- [ ] 5.2 Surface Choice-network warning text without inventing a fixed provider identity.
- [ ] 5.3 Add focused ViewModel and Avalonia headless tests for target selection, persisted target display, cross-Store rejection, and read-only controls.

## 6. Completion verification

- [ ] 6.1 Map each scenario in `product-supplier-setup`, `design-area-target-selection`, `store-management`, and `basic-product-workflow` deltas to executed tests in `verification.md`.
- [ ] 6.2 Run `openspec validate add-product-supplier-setup --strict` and correct all validation failures.
- [ ] 6.3 Run `dotnet test .\\FusionCanvas.sln` and correct all relevant regressions.

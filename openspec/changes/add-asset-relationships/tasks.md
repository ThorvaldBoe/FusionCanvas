## 1. Multi-Context Relationship Model

- [ ] 1.1 Extend the asset context-link model to support multiple links per asset, each with its own purpose, validated against the active workspace, the asset's store, and the target context's active ancestry.
- [ ] 1.2 Add Concept and Design as valid asset context kinds, with concept/design active-ancestry validation and archive-aware exclusion.
- [ ] 1.3 Reject duplicate (context, purpose) links and cross-store links with actionable errors.
- [ ] 1.4 Add domain tests for multi-context linking, duplicate rejection, cross-store rejection, and concept/design context validation.

## 2. Asset Relationship Application Layer

- [ ] 2.1 Define `IAssetRelationshipService` requests and results for add-link, relink, per-context purpose relabel, and remove-link, with recoverable error reporting.
- [ ] 2.2 Implement add-link with per-context purpose pre-selection by target context kind and user override.
- [ ] 2.3 Implement per-context purpose relabel that preserves other links' purposes and the asset identity.
- [ ] 2.4 Implement remove-link that removes only the targeted context link and preserves the asset record and managed file.
- [ ] 2.5 Add application tests for add-link, relink, purpose relabel, remove-link, duplicate and cross-store rejection, and repository-failure atomicity.

## 3. Persistence Verification

- [ ] 3.1 Confirm the existing schema and snapshot validation support multiple context links per asset and the extended context-kind enum without a data migration.
- [ ] 3.2 Add SQLite integration tests for add-link, per-context purpose, remove-link, multi-context preservation across listing moves and concept/design lifecycle, and complete reload.
- [ ] 3.3 Verify failed or cancelled relationship operations persist no partial links, purpose changes, or asset removals.

## 4. Asset Surface and Cross-Capability Integration

- [ ] 4.1 Add concept and design asset context views to the focused asset surface with per-context purposes, file state, and empty states.
- [ ] 4.2 Preserve asset records and unrelated context links when concepts or designs are edited, superseded, rejected, or removed.
- [ ] 4.3 Keep store-level asset view listing every store-owned asset with linked contexts or an unlinked indicator.
- [ ] 4.4 Apply compact action sizing, icon tooltips, keyboard-reachable link and relabel actions, busy states, and shared desktop control guidance.
- [ ] 4.5 Add view-model and UI tests for concept/design asset views, per-context purpose editing, multi-context preservation, removal, rollback, and shared control guidance.

## 5. Verification

- [ ] 5.1 Run domain, application, integration, app, and UI automation test suites and resolve asset-management, concept-versions, and design-records regressions.
- [ ] 5.2 Manually verify add-link, per-context purpose, remove-link, concept/design asset views, preservation across lifecycle and moves, missing-file detection, and application reload.
- [ ] 5.3 Run strict OpenSpec validation and confirm every asset-relationships and modified asset-management scenario is covered by implementation or automated tests.

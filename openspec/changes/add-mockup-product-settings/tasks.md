## 1. Three-Level Configuration Model

- [ ] 1.1 Add MockupProduct, MockupTemplate, and MockupColorVariant domain entities owned by store, product, and template respectively, with stable identity, timestamps, active flag, and archive flag.
- [ ] 1.2 Add MockupProduct fields: vendor name, product name, provider product name, product type, design area width/height, notes, metadata.
- [ ] 1.3 Add MockupTemplate fields: name, template asset reference, view name, default color name, placement X/Y/width/height/scale/rotation, metadata.
- [ ] 1.4 Add MockupColorVariant fields: provider color name, display color name, color-specific template asset reference, swatch hex, sort order, metadata.
- [ ] 1.5 Add domain tests for the three-level hierarchy, store-scoping, field validation (positive integer design area, rotation default zero), and unknown-metadata preservation.

## 2. Mockup Product Settings Application Layer

- [ ] 2.1 Define `IMockupProductSettingsService` requests and results for create, edit, archive, restore, active-flag toggle, reorder, and active-set query, with recoverable error reporting.
- [ ] 2.2 Implement width-preferred placement mapping resolution (placement width over scale; rotation default zero).
- [ ] 2.3 Implement exact provider color name preservation (verbatim, no normalization).
- [ ] 2.4 Implement active-set query that returns only active, non-archived products, templates, and color variants for a store.
- [ ] 2.5 Add application tests for create/edit/archive/restore/active-toggle, width-preferred mapping, exact color names, active-set filtering, and repository-failure atomicity.

## 3. Persistence and Migration

- [ ] 3.1 Add a forward-only schema migration that creates the MockupProduct, MockupTemplate, and MockupColorVariant tables and the MockupTemplate asset context kind, and increments the snapshot version.
- [ ] 3.2 Migrate existing stores by assigning zero configuration without data backfill.
- [ ] 3.3 Add SQLite integration tests for create, edit, archive, restore, active-toggle, reorder, active-set query, complete reload, and migration of pre-existing snapshots.
- [ ] 3.4 Verify failed or cancelled settings operations persist no partial products, templates, color variants, active-flag changes, or archive changes.

## 4. Asset Integration and Store Settings Surface

- [ ] 4.1 Store template images and color-specific template images as assets through asset-management, with MockupTemplate as a valid asset context kind; preserve referenced assets on archive or removal.
- [ ] 4.2 Add a focused store settings surface opened from the store with progressive disclosure across products, templates, and color variants.
- [ ] 4.3 Implement add, edit, archive, restore, active-flag toggle, and reorder with explicit save, dirty tracking, unsaved-change prompts, and replacement selection.
- [ ] 4.4 Add a visual display of the template image with the placement rectangle rendered from the current numeric placement values, updated live as the user edits the numeric fields; defer drag-and-drop editing to a later change.
- [ ] 4.5 Apply compact action sizing, icon tooltips, keyboard-reachable actions, busy states, duplicate-submission prevention, and shared desktop control guidance.
- [ ] 4.6 Add view-model and UI tests for progressive disclosure, archive/restore, active-set exposure, visual placement display, dirty tracking, rollback, and shared control guidance.

## 5. Verification

- [ ] 5.1 Run domain, application, integration, app, and UI automation test suites and resolve store-management, asset-management, asset-relationships, mockup-records, and basic-listing-tool regressions.
- [ ] 5.2 Manually verify product/template/color-variant configuration, placement mapping, exact provider color names, archive/restore, active-set exposure in the Basic Listing Tool, persistence, migration, and application reload.
- [ ] 5.3 Run strict OpenSpec validation and confirm every mockup-product-settings scenario is covered by implementation or automated tests.

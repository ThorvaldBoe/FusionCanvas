# Design Stage Implementation — Tasks

## 1. Domain Model and Invariants

- [ ] 1.1 Rename/replace `ItemDesignAreaTarget` usage with new `ItemListingConfiguration(itemId, offeringId)` record and add validation (offering belongs to item's Store, is active; singular per item).
- [ ] 1.2 Add `DesignSelectedColor(itemId, colorValue)` record with store/presence validation.
- [ ] 1.3 Add `DesignVariantRow(id, itemId, isDefault, sortOrder)` and `DesignVariantRowColor(rowId, colorValue)` records; enforce exactly one default row; partition invariant (selected colors union = rows' colors; each selected color in exactly one row).
- [ ] 1.4 Add `DesignSlotAssignment(rowId, designAreaId, assetId?)` record with PK (rowId, designAreaId); enforce area belongs to the item's configuration's offering and optional-but-not-empty asset binding.
- [ ] 1.5 Add `DesignStagePolicy`/invariant helper in Domain for the operations: select configuration, add/remove selected color, make-specific-for-color (atomic move + empty-row removal + revert-to-default on specific-row removal), assign/replace/remove slot image. Keep editability/read-only gating out of Domain; it belongs in the Application service via `ItemWorkflowPolicy` (extend `ItemOperationKind` with a DesignStage kind).
- [ ] 1.6 Add Domain unit tests (deterministic, no frameworks) covering configuration validation, color deduplication, partition invariant on add/remove/make-specific, empty-row removal, slot uniqueness, and area-belonging.

## 2. Application Service

- [ ] 2.1 Define `IDesignStageService` with result types and a `DesignStageState` DTO (configuration offering + areas, available colors, selected colors, rows with slots and per-slot asset capability/missing state, supporting images).
- [ ] 2.2 Implement `LoadDesignStageStateAsync(itemId)` deriving available colors by deduplicating `Color` option values across the configuration's offering variants, and areas from the offering.
- [ ] 2.3 Implement `SelectConfigurationAsync(itemId, offeringId)` (replaces old target selection), clearing/reassigning stale slot assignments on config switch.
- [ ] 2.4 Implement `AddSelectedColorAsync` / `RemoveSelectedColorAsync` with default-row membership and partition maintenance; removing a color also cleans it from any row.
- [ ] 2.5 Implement `MakeSpecificForColorAsync(itemId, colorValue)` as an atomic move creating a new row and removing an emptied source row.
- [ ] 2.6 Implement `RemoveSpecificRowAsync(itemId, rowId)` as an atomic revert of the row's colors to the default row and removal of the row and its slot assignments.
- [ ] 2.7 Implement `AssignSlotImageAsync` / `ReplaceSlotImageAsync` / `RemoveSlotImageAsync` using the existing design-file/asset import and managed-file boundaries, returning recoverable errors.
- [ ] 2.8 Implement `OpenSlotPreviewAsync` / `ExportSlotImageAsync` for slot view-and-download mirroring `IDesignFileService`, and `ListSupportingImagesAsync` / `ImportSupportingImageAsync` / preview / export / remove for supporting images using the existing asset link model.
- [ ] 2.9 Add Application use-case tests with deterministic collaborators (in-memory snapshot, clock, deterministic file store) covering all service operations, error/recoverable states, and read-only gating.
- [ ] 2.10 Update the `ProductSupplierSetupService` deletion guards (`DeleteProductAsync`, `DeleteOfferingAsync`, `DeleteDesignAreaAsync`) to block removal when an offering is referenced by an `ItemListingConfiguration` or a `DesignArea` is referenced by a `DesignSlotAssignment`, replacing the old `ItemDesignAreaTarget` references; remove the obsolete `ReplaceDesignTargetsAsync`/`LoadDesignTargetsAsync` path. Add Application use-case tests for the product-supplier-setup scenarios: unreferenced removal succeeds; referenced removal is blocked.

## 3. Persistence and Migration

- [ ] 3.1 Extend `WorkspaceSnapshot` with new collections and remove `ItemDesignAreaTargets`.
- [ ] 3.2 Add SQLite tables `item_listing_configuration`, `design_selected_colors`, `design_variant_rows`, `design_variant_row_colors`, `design_slot_assignments` and drop `item_design_area_targets` in the schema and the drop-all-order list.
- [ ] 3.3 Implement repository load/insert for the new collections and remove `ItemDesignAreaTarget` load/insert; add save-time validation (FK existence, offering/area belonging, one default row, partition).
- [ ] 3.4 Add Integration tests (temporary SQLite DB) covering round-trip persistence of configuration, colors, rows, and slot assignments, the migration path, and reload consistency.

## 4. App UI — Configuration, Working Set, Slot Grid

- [ ] 4.1 Rework `DesignStageToolViewModel`: expose configuration selector state (available offerings, selected offering), hide the slot grid **and the color working-set selector** when no configuration, show configuration prompt + supporting images area.
- [ ] 4.2 Add `DesignColorViewModel`, `DesignRowViewModel`, and `DesignSlotViewModel` (thumbnail / add / view / download / remove states, missing state, busy).
- [ ] 4.3 Build the color working-set selector (chips, add/remove, make-specific-for-color affordance, remove-specific-row affordance) bound to the service.
- [ ] 4.4 Build the row × design-area slot grid (rows with color chips, columns per area, per-cell commands, remove-specific-row action), replacing the old flat design-file list and target-selection UI in MainWindow.axaml.
- [ ] 4.5 Add Avalonia headless view tests for: no-configuration state, configuration selection, slot grid rendering, color add/remove/make-specific, remove-specific-row, per-cell commands, and read-only states.

## 5. App UI — Drag-and-Drop, Large Preview, Supporting Images

- [ ] 5.1 Add drag-and-drop handlers to slot cells (accept supported image drops; reject others with a recoverable message before copy/persistence).
- [ ] 5.2 Add the in-app large-preview dialog for slot and supporting images using the authoritative managed copy.
- [ ] 5.3 Build the Supporting images panel (independent of configuration, import / thumbnail list / view-large / download / remove) and wire to the service.
- [ ] 5.4 Add Avalonia headless view tests for: drop affordance validation, large-preview open, supporting-images import/remove, and supporting images visible without a configuration.

## 6. Verification and Baseline

- [ ] 6.1 Run `openspec validate design-stage-implementation --strict` and `openspec validate --all --strict`; resolve any validation findings.
- [ ] 6.2 Run `dotnet build .\FusionCanvas.sln`.
- [ ] 6.3 Run `dotnet test .\FusionCanvas.sln` and fix any failures.
- [ ] 6.4 Cross-check every acceptance scenario in the delta specs against the tasks and planned verification; record anything not covered in the handoff as an open item.

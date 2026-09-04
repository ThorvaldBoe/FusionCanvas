# Design Stage Implementation — Design

## Context

The Design stage today manages a flat list of managed PNG "design files" plus an optional set of selected printable-area targets (`ItemDesignAreaTarget`). The product catalog already models `StoreProduct → FulfillmentOffering → ProductVariant (color/size options)` and `DesignArea` (position, decoration, dimensions, applicable variants), stored in dedicated tables. There is no connection between a final image and a specific printable area, no variant-aware working model, and no separation of final artwork from supporting reference images.

Issue 129 asks the Design stage to produce the concrete output the pipeline needs: one final design image per pre-allocated slot derived from a selected product configuration, with an intuitive Printify-style workflow.

The agreed product model (from discovery):

- A **listing configuration** (catalog offering) is **mandatory**. Its design areas define the final-design slot grid columns.
- The creator narrows the catalog's full color/size universe to a **color working set** per item. **Size never participates** in design.
- A **default row** serves all selected colors with one set of images. The creator can **make a specific design for a color** to move that color into its own row.
- Rows **partition** the selected colors: every selected color belongs to exactly one row; rows may serve multiple colors; one color appears in exactly one row.
- The **slot grid** is row × design-area; each cell holds one final image (thumbnail / add / view-large / download / remove).
- **Supporting images** are independent of configuration and areas and always available.

## Goals / Non-Goals

**Goals:**
- Provide a mandatory, singular listing configuration that anchors the Design stage and gates the final-design slot grid.
- Implement the color-row working model that partitions selected colors across rows.
- Render a row × design-area slot grid with one final image per cell and per-cell commands.
- Add drag-and-drop filling and a large-preview surface.
- Add an independent Supporting images area.
- Build and verify the data model (Domain, persistence, migration, invariants) before layering UI.

**Non-Goals:**
- AI image generation for designs.
- A full `Design` entity with versioning, status workflow, or `IsFinalSelected` semantics (deferred; slot-assignment model is its foundation).
- Changing the catalog/setup model (the full universe of variants remains defined by Product Supplier Setup).
- Mockup generation or listing-stage consumption of final designs.

## Decisions

### D1 — Mandatory singular listing configuration via a dedicated record
Introduce `ItemListingConfiguration(itemId PK, offeringId FK)` in Domain, replacing `ItemDesignAreaTarget` for anchoring. The offering must belong to the item's Store and be active. Switching configuration clears or reassigns slot assignments whose areas are no longer present.

*Why:* the slot grid derives entirely from the configuration's design areas, so the anchor must be singular and enforced. A dedicated record (matching the project's existing join-table precedent) gives referential integrity and clean coverage checks, rather than JSON metadata. `ItemDesignAreaTarget` is removed (see Migration).

### D2 — Color-row working model with dedicated records
Introduce:
- `DesignSelectedColor(itemId FK, ColorValue)` — the working-set colors chosen for the item, deduplicated by color value.
- `DesignVariantRow(id, itemId, IsDefault, SortOrder)` — one row; exactly one `IsDefault` row per item at any time.
- `DesignVariantRowColor(rowId FK, ColorValue)` — which colors a row serves; PK (rowId, ColorValue).
- `DesignSlotAssignment(rowId FK, designAreaId FK, assetId FK?)` — the final image for one cell; PK (rowId, designAreaId); assetId nullable until filled.

*Why:* matches the Printify model and enforces the partition. The "color value" is derived from `ProductVariant` options where option name is `Color`, deduplicated across the offering's variants. Size is never keyed because `ProductVariant` full combos (color×size) would explode the grid; only color participates.

### D3 — Partition invariant lives in Domain
The Domain enforces: every `DesignSelectedColor` appears in exactly one `DesignVariantRowColor`; the union of all rows' color sets equals the selected-color set; exactly one default row; slot cells are unique per (row, area); a slot's area belongs to the item's configuration's offering.

*Why:* "one color lives in one row" is the load-bearing rule; putting it in Domain keeps it testable without frameworks and prevents the UI from drifting into inconsistent states. Making specific for a color is a single atomic operation that moves the color and, when its old row becomes empty, removes that row.

### D4 — Build model first, then UI
Sequencing within the module: Domain records + invariants → Application service + tests → persistence + migration + tests → slot-grid/working-set UI → drag-and-drop + large preview → supporting images. Persistence and invariant tests gate before UI is layered.

*Why:* the data model is the long-term-critical, expensive-to-change part. Verifying it before UI avoids building throwaway UI on a wrong model. This is achieved by task ordering within the single module, not by a separate module.

### D5 — Supporting images reuse the existing asset model
Supporting images are `Asset` records (kinds `ReferenceImage`, `SourceDesign`, `Other`) linked to the item via `AssetLink`, surfaced in a dedicated Supporting images panel. No new data model; reuse `IDesignFileService`-style import/preview/export/remove or the asset-management surface.

*Why:* orthogonal to configuration and areas; the existing asset/link machinery already supports managed files, missing-state detection, and confirmed removal.

### D6 — Configuration service placement
A new `IDesignStageService` in Application owns design-stage operations: load state, select configuration, add/remove selected colors, make specific for color, assign/replace/remove slot image, list supporting images. Design-area-availability and column derivation come from the existing product model via `IProductSupplierSetupService` where sensible, or the new service directly against the snapshot.

*Why:* keeps design-stage orchestration in one coherent application service while reusing existing product/asset building blocks. The old `ReplaceDesignTargetsAsync` is removed and replaced by `SelectConfigurationAsync` and the design-stage operations (including `RemoveSpecificRowAsync`, slot preview/export, and supporting-image operations). The `ProductSupplierSetupService` deletion guards are updated to block removal of offerings/areas referenced by `ItemListingConfiguration` or `DesignSlotAssignment` (see Data / Persistence Changes).

## Data / Persistence Changes

New tables (SQLite), deletion of `item_design_area_targets`:

```
item_listing_configuration (item_id PK → items, offering_id → fulfillment_offerings)
design_selected_colors     (item_id → items, color_value, PK(item_id, color_value))
design_variant_rows        (id PK, item_id → items, is_default, sort_order)
design_variant_row_colors  (row_id → design_variant_rows, color_value, PK(row_id, color_value))
design_slot_assignments    (row_id → design_variant_rows, design_area_id → design_areas,
                            asset_id → assets NULL, PK(row_id, design_area_id))
```

`WorkspaceSnapshot` gains `ItemListingConfigurations`, `DesignSelectedColors`, `DesignVariantRows`, `DesignVariantRowColors`, `DesignSlotAssignments` and drops `ItemDesignAreaTargets`. Repository gains load/insert for the new collections and the table-clear list entry. Save-time validation mirrors existing patterns (FK existence, offering-belonging, area-belonging, one-default-row, partition).

### Migration
- The `item_design_area_targets` table is dropped; any existing selected targets are not silently preserved because the new model requires a singular configuration that has no direct equivalent to the old multi-area selection. Items previously at Design with targets simply have no configuration and thus show the configuration-selection prompt (acceptable degraded state; no data loss of design files/supporting images).
- New tables are created via the existing `CREATE TABLE IF NOT EXISTS` path; the snapshot save forwards them.

The `ProductSupplierSetupService` deletion guards that reference `ItemDesignAreaTargets` must be updated to reference `ItemListingConfiguration` and `DesignSlotAssignment`, and the obsolete `ReplaceDesignTargetsAsync`/`LoadDesignTargetsAsync` path removed, so the product-supplier-setup scenarios (unreferenced removal succeeds; referenced removal blocked) hold and compilation is not broken by the snapshot change.

## Application / Service Changes

`IDesignStageService` (new) with result types:
- `LoadDesignStageStateAsync(itemId)`
- `SelectConfigurationAsync(itemId, offeringId)`
- `AddSelectedColorAsync(itemId, colorValue)` / `RemoveSelectedColorAsync(itemId, colorValue)`
- `MakeSpecificForColorAsync(itemId, colorValue)`
- `RemoveSpecificRowAsync(itemId, rowId)` — atomically returns a specific row's colors to the default row and removes the row and its slot assignments
- `AssignSlotImageAsync(itemId, rowId, designAreaId, sourcePath)` / `ReplaceSlotImageAsync` / `RemoveSlotImageAsync(rowId, designAreaId)`
- `OpenSlotPreviewAsync(rowId, designAreaId)` and `ExportSlotImageAsync(rowId, designAreaId, destinationPath)` for slot view/download (mirroring `IDesignFileService.OpenPreviewAsync`/`ExportCopyAsync`)
- `ListSupportingImagesAsync(itemId)` / `ImportSupportingImageAsync(itemId, sourcePath)` / remove / preview / export

State model returned to UI includes: the configuration offering with its design areas, available colors (deduped), selected colors, rows with their color sets and slots (each slot with its asset thumbnail/preview capability).

Editability is derived from `ItemWorkflowPolicy.CanPerformOperation(item, DesignStage)` reusing the same policy check as the current DesignFile operation (extend `ItemOperationKind` with a DesignStage kind); read-only context blocks every mutation. This gating lives in the Application service, not in a Domain helper.

## App / UI Changes

The Design Stage Tool (`DesignStageToolViewModel` + MainWindow surface) is reworked:
- **Configuration selector** (mandatory): a control listing active catalog offerings for the item's Store; selecting persists the configuration. When none selected, show the supporting images area + a prompt; hide the slot grid.
- **Color working set**: multi-select chips of available colors; adding/removing updates rows (default row serves unclaimed colors).
- **Row × area slot grid**: rows listed with their color chips; columns = design areas; each cell shows a thumbnail or an "Add image" affordance, with view-large / download / remove commands on filled cells and make-specific-color per color.
- **Drag-and-drop**: each slot cell accepts supported image file drops → fill/replace, reusing the same validation as import.
- **Large preview**: an in-app dialog showing the managed copy.
- **Supporting images panel**: independent of configuration, with import / thumbnail list / view-large / download / remove.

New/updated view models: `DesignRowViewModel`, `DesignSlotViewModel`, `DesignStageToolViewModel` (extends/replaces current), plus a supporting-images set. MainWindow.axaml gains the grid templates and drop handlers; the old flat design-file list and `DesignAreaTargetViewModel`/target selection are removed.

## Edge Cases

- **No configuration**: slot grid hidden, prompt shown, supporting images still available; the color working-set selector is also hidden until a configuration is selected.
- **Configuration switch**: stale slot assignments/areas removed; rows re-derived from new offering's areas.
- **Make specific for a color**: atomic; old row removed when it becomes empty.
- **Remove specific row with colors**: colors atomically revert to the default row; the specific row and its slot assignments are removed.
- **Read-only review**: all controls disabled; no mutation.
- **Unsupported dropped file**: rejected before copy/persistence.
- **Duplicate color across variants**: collapsed by value.
- **Missing managed file**: slot/supporting-image shows missing state; remove still available.
- **Column set**: every configuration design area is a column for every row, regardless of that area's variant applicability.

## Risks / Trade-offs

- [Data-model wrongness is expensive] → build + verify Domain/persistence/invariants before UI (D4).
- [Partition invariant drift] → enforced in Domain + deterministic tests for Add/Remove/Make-specific/row-removal.
- [Color extraction rule] → color value is the `VariantOption.Value` where `Name == "Color"` (case-insensitive), deduplicated; this is resolved (see Open Questions). Residual risk is only that a future provider names its color option differently, mitigated by a future-resilience note.
- [Wide grid if many areas/colors] → rely on the working set to keep color count small; areas per offering are inherently bounded.
- [UI regression from reworking MainWindow surface] → headless view tests for slot grid, color row ops, and drop affordance; keep change scoped to the Design section.

## Migration Plan

Drop `item_design_area_targets`; add new tables; extend snapshot/repository for new collections; treat pre-existing multi-target selections as "no configuration" (degraded but non-destructive). Rollback is not applicable for an additive/forward migration within an unreleased early-stage app; existing design files and supporting images are always preserved.

## Open Questions

1. ~~Color extraction~~ **Resolved**: design-variant axis is color **value** from `VariantOption` where `Name == "Color"` (case-insensitive), deduplicated across the offering's variants (see spec scenario "Color value is derived from the Color option"). A future provider that names its color option differently would require a provider-agnostic extraction rule; recorded as a future-resilience note, not a blocker.

## Acceptance-to-Verification Mapping

Each acceptance scenario in the delta specs maps to:
- Domain partition/invariant scenarios → deterministic Domain tests (no frameworks).
- Application operations (select config, add/remove color, make specific, assign/replace/remove slot) → Application use-case tests with deterministic collaborators (in-memory snapshot/clock).
- Persistence round-trip → Integration tests over a temporary SQLite DB.
- Slot grid, color-row UI, drag-and-drop affordance, supporting images panel, read-only states → Avalonia headless view tests.
- The complete flow is gated by strict OpenSpec validation and `dotnet test .\FusionCanvas.sln`.

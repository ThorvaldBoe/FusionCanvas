# Proposal: Design Stage Implementation

## Origin

- GitHub issue: [#129 Design stage implementation](https://github.com/ThorvaldBoe/FusionCanvas/issues/129)

## Why

The Design stage currently only manages a flat list of PNG design files plus an optional set of selected printable-area targets. It does not connect final design images to the product configuration, so creators cannot see which image fills which printable area, cannot prepare multiple design variants (e.g. a separate artwork for light vs. dark shirts), and cannot distinguish final artwork from supporting reference material. Issue 129 asks to make the Design stage produce the concrete output the rest of the pipeline needs: one final design image per pre-allocated design slot, derived from a selected product configuration.

## What Changes

- Replace the optional "select zero-or-more printable areas" behavior with a **mandatory single listing configuration selection** (a Store catalog offering). The final-design slot grid is derived from that configuration's design areas and is only shown when a complete configuration is selected.
- Introduce a Printify-style per-item **working model**: the creator narrows the catalog's full color/size universe to a subset of colors for a given design; a single **default row** serves all selected colors with one set of final images; the creator can **make a specific design for a color** to split that color into its own row with its own image slots.
- Render the final design as a **row × design-area slot grid** where each cell shows a thumbnail (or "Add image") plus view-large / download / remove commands directly on that cell.
- Add a separate **Supporting images** area for sketches, references, and existing artwork. Supporting images are independent of configuration and design areas and remain available even without a selected configuration.
- Keep image import/drag targeted at filling the right slot; add drag-and-drop onto slot cells.
- **Out of scope:** AI image generation for designs.

## Capabilities

### New Capabilities
- `design-stage-implementation`: per-item listing-configuration selection, the color-row working model, the row × design-area slot grid with thumbnails and per-cell commands, drag-and-drop filling, and the Supporting images panel.

### Modified Capabilities
- `design-area-target-selection`: the optional zero-or-more printable-area selection is removed; the surviving editability requirement is preserved. The mandatory singular configuration anchor is owned by `design-stage-implementation`.
- `basic-product-workflow`: "Design tool presents selected printable-area guidance" becomes "Design tool presents listing configuration guidance", reflecting that the Design tool shows the configuration, the row × area slot grid, and the color working set instead of printed area-target guidance.
- `asset-management`: the two Design-file requirements become slot-images/supporting-images requirements — final design slots are managed PNG (one per cell) and supporting images accept the supported creative set, with preview/export/missing-state/confirmed-removal preserved.
- `product-supplier-setup`: "Catalog edits preserve selected Item targets" becomes "Catalog edits preserve selected Item configurations", protecting the offering an Item selects as its configuration and the areas referenced by slot assignments.

### Dependency (not otherwise changed)
- `product-supplier-setup` catalog structure, `asset-management` import/file-storage machinery, and `basic-product-workflow` stage/editability rules are relied upon but their general behavior is unchanged beyond the deltas above.

## Impact

- **Domain**: new records `ItemListingConfiguration`, `DesignVariantRow`, `DesignVariantRowColor`, `DesignSlotAssignment`; removal or deprecation of `ItemDesignAreaTarget`; invariants enforcing the color partition and one-image-per-cell.
- **Application**: new design-stage service (load selection, select configuration, add/remove selected colors, make-specific-color, assign/replace/remove slot image, list supporting images); modifications where design-area-target selection is replaced by configuration selection.
- **Integration**: new SQLite tables + migration; new repository reads/writes; design-file import reused; supporting-image import reused.
- **App**: a new Design stage tool surface (configuration selector, color working set, slot grid with thumbnails, supporting images panel, drag-and-drop handlers, large-preview dialog); design-file view model extended or replaced.
- **Spec changes**: deltas to `basic-product-workflow`, `asset-management`, and `product-supplier-setup` to keep those accepted requirements aligned with the new slot/supporting model (see Capabilities).
- **Tests**: Domain invariant tests, Application use-case tests with deterministic collaborators, integration persistence tests, and Avalonia headless view tests for the slot grid, color-row operations, drag-and-drop affordance, and supporting-images panel.
- **BREAKING**: the accepted optional multi-target selection is replaced by mandatory single configuration selection; Design-file assets are reorganized into slot-bound final images (PNG) and Supporting images.

## UX Preflight Summary

The Design stage tool is the primary workspace for design work, so the slot grid, color working set, configuration selector, and supporting images panel live inline in the Design Stage Tool (not in a separate window). Per-action frequency is moderate (per item), so progressive disclosure keeps the full 360-variant universe hidden; the working set and rows are the primary surface. Empty, blocked (config missing), success, recoverable-error, and destructive-removal states are designed for each area. Full details are in `design.md`.

## Risks & Verification Approach

- **Data-model risk** (the expensive part to change): mitigated by building and verifying Domain records, invariants, persistence, and migration before layering UI (see `design.md` sequencing).
- **Partition correctness**: the "one color lives in one row" invariant is enforced in Domain and covered by deterministic tests.
- **Verification**: strict OpenSpec validation; `dotnet test .\FusionCanvas.sln` baseline; persistence/invariant/headless-view tests; per-scenario evidence recorded in `verification.md`.

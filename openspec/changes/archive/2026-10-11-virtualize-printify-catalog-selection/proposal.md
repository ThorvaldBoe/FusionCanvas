## Why

The Store Editor currently creates a full checkbox visual tree for every Printify shop product, so rendering and scrolling can become sluggish for large shops. Virtualizing rendered rows can keep the selector responsive while preserving the current full-result retrieval and explicit import workflow.

## What Changes

- Replace the materialized product `ItemsControl` with the existing `VirtualDataGrid`.
- Show aligned selection, Blueprint, shop product, and identity details in fixed-height rows; keep selection exclusively in keyboard-accessible checkboxes.
- Keep selection bound to stable product identity across row recycling, retain full identifying text through accessible names and tooltips, and preserve confirmation/cancel and all existing loading/error/store-draft behavior.
- Keep full-shop retrieval before display; do not add search, sorting, or paging.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `printify-catalog-import`: Add the virtualized selection-grid presentation and accessibility, stable multi-selection, and scope constraints.

## Impact

This user-facing change remains in the occasional Printify product-selection panel in the Store Editor. It affects the panel XAML, the existing import-item view model, and Avalonia headless tests. It uses the existing virtual-grid dependency and requires no API, persistence, migration, or dependency changes. The accepted issue decision resolves the row model, fixed-height layout, checkbox behavior, and excluded search/sorting/paging scope; no high-impact decisions remain open.

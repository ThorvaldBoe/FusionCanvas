## Why

The Sellable Variants list currently repeats an unaligned name, semantic-value summary, and archive action for every record. A virtual grid will align the information already shown, make long explicit Variant lists more scannable, and retain the existing variant-creation and lifecycle workflows.

## What Changes

- Present active sellable Variants in a fixed-row virtual grid with aligned Name, Color, Size, Other, and Action columns.
- Keep the current Variant name and resolved stable Option-kind values visible; retain unavailable provider-value disclosure.
- Keep the per-row Archive action bound to the existing direct archive command, including stale-target checks, dependency blocking, and recoverable-error behavior; do not add a confirmation step.
- Preserve the existing Add Variant and Bulk add dialogs, active Variant count, default order, and hidden archived Variants.
- Exclude search, sorting, paging, inline editing, and archive-policy changes; no evidence establishes a need for those new behaviors in this module.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `variant-management`: define the virtual grid columns and require preservation of the existing Variant summary and row-action behavior.

## Impact

- Affects the Store Editor's Sellable Variants presentation and its headless view tests. The current `SellableVariantRowViewModel`, archive command, creation dialogs, domain/application behavior, and persistence remain authoritative.
- Primary workflow: creators scan confirmed Offering Variants and occasionally archive one. This remains in the existing focused Variant Management surface; Add Variant and Bulk add remain available in the section header.
- UX decisions: keep active Variants in their existing order; archived Variants remain hidden as today; show long cell values without row-height changes and provide their full text through tooltips/help text; keep Archive a direct, explicit, target-specific action and test pointer and keyboard activation. The issue's reference to an existing confirmation does not match the current code or accepted behavior, so this change does not add one.
- Verification: focused view-model and Avalonia headless tests cover column content, row/action targeting, scrolling/provider refresh, and preserved blocked-archive feedback; run strict OpenSpec validation and the solution test baseline. No Appium journey is warranted for this presentation-focused change because the deterministic headless tests exercise the material interaction and view risks.

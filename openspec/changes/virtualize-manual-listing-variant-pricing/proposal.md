## Why

Manual Listing Details renders one independent row per active Variant, which becomes difficult to scan and edit when an Offering has many Variants. An aligned virtual grid keeps prices and fulfillment costs associated with their Variant while reducing the amount of realized UI.

## What Changes

- Replace the per-Variant pricing list with an aligned, virtualized grid for Variant, selling price, and fulfillment cost.
- Preserve direct inline editing, existing validation and error feedback, editability gating, Variant identity, and Save details behavior.
- Keep pricing guidance, shipping terms, gross profit summary, status, and save controls in their current workflow.
- Do not add search, sorting, paging, persistence, or new pricing behavior.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `manual-listing-details`: define the aligned virtual-grid presentation and preserve per-Variant editing behavior.

## Impact

- App presentation: `MainWindow.axaml`, `MainWindow.axaml.cs`, and App headless tests.
- No domain, application, persistence, database, or dependency changes.
- Verification covers aligned columns, inline editing and keyboard focus, stable Variant association, read-only gating, and the existing listing save behavior.

The module is one focused presentation change on the existing Listing Details workflow. The pricing grid is a frequent editing surface that belongs in the main workspace. The change is reviewable because it does not alter pricing rules or persistence. If the existing grid cannot support reliable editor focus and keyboard traversal, retain the list instead of shipping an unreliable editor. Real-desktop Appium coverage is not warranted: this small layout replacement can be verified with headless Avalonia tests and existing service tests.

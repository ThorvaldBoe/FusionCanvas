## Why

An Item whose persisted Design listing configuration points to an archived or removed Blueprint Offering becomes completely read-only, leaving the creator unable to recover the Item even when another valid Offering exists. This module adds one explicit, safe recovery path so the creator can replace the stale configuration and continue Design work without silently remapping incompatible catalog relationships or discarding creative history.

## What Changes

- Detect when an otherwise editable Design-stage Item is blocked only because its persisted listing configuration is no longer active.
- Keep the stale configuration visible for context while enabling one recovery control that lists active Blueprint Offerings from the same Store; all other Design mutations remain unavailable until recovery succeeds.
- Require an explicit confirmation before replacing the stale configuration and summarize which configuration-specific state will be reset and which creative and downstream data will remain.
- Replace the stale configuration atomically, using the existing configuration-switch boundary: clear selected Design colors, variant rows, row-color relationships, slot assignments, and saved artwork target/transparency preferences.
- Preserve the Item, Idea/Concept/SLL content, managed asset files, Supporting Images, generic Item relationships, workflow/lifecycle state, and downstream Listing data. Cleared slot assignments are not automatically reattached, and preserved downstream outputs are not asserted to be compatible with the replacement Offering.
- Rebuild the Design surface from the replacement Offering so the creator explicitly selects colors, rows, targets, and file assignments appropriate to its current Variants and Placeholders.
- Do not match Placeholders, Variants, colors, rows, or assets automatically by name, dimensions, position, or provider identity.
- When no active same-Store Offering is available, keep the Item reviewable and guide the creator to create or restore an Offering in Store Editor.
- Reject cancellation, stale candidate, cross-Store, protected Item, archived Store, validation, and persistence failures without partially changing the existing Item configuration or Design relationships.

This is one cohesive recovery module: it owns the single transition from a stale listing configuration to a valid active Offering and the directly required confirmation, reset, error, and verification behavior. It does not broaden into catalog administration or intelligent mapping.

## Capabilities

### New Capabilities

- `design-listing-configuration-recovery`: defines stale-configuration detection, the constrained Design-stage recovery workflow, its preservation/reset boundary, confirmation and failure behavior, and post-recovery editability.

### Modified Capabilities

- `basic-product-workflow`: clarifies that intentional stale-configuration recovery preserves creative history and downstream data while resetting only Offering-specific Design relationships.

## Impact

- Application: extend the Design-stage use case so stale configuration replacement is an explicit atomic operation with a preview/confirmation boundary instead of relying on the normal fully editable selector path.
- Presentation: update `DesignStageToolViewModel` and the Design section in `MainWindow.axaml` so only the recovery selector/action is enabled for an otherwise editable Item with a stale configuration; add concise confirmation, empty, error, and success guidance with keyboard focus recovery.
- Data: no schema migration is expected. Existing `ItemListingConfiguration`, Design color/row/slot records, Item metadata preferences, Assets, Supporting Images, and Listing records retain their current storage models.
- Verification: focused application tests for atomic reset/preservation and rejection paths, view-model tests for recovery state and confirmation, Avalonia headless tests for enabled/disabled controls, confirmation/cancellation, focus, and post-success editability, followed by strict OpenSpec validation and the full solution test baseline.
- Dependencies: the normalized same-Store Blueprint Offering catalog and existing Design configuration-switch logic.
- Non-goals: restoring or creating Offerings inside the Design tool; smart or user-authored Placeholder/Variant mapping; automatic slot/file reassignment; deleting preserved managed assets or downstream Listing outputs; changing catalog archive/delete policy; or adding provider synchronization.
- Risks: the reset is intentionally lossy for Offering-specific relationships, so confirmation must be precise; concurrent catalog changes must fail without partial mutation; preserved downstream data may require later review, and the recovery flow must not imply compatibility that has not been revalidated.

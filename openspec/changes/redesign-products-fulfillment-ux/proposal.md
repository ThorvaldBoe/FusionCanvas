## Why

The Store Management “Products & fulfillment” screen currently exposes product setup, fulfillment offerings, variants, and printable areas in one dense three-column editor. The functionality and relationships are useful, but the simultaneous “New product”, “New offering”, “Add”, and “Add area” actions make the hierarchy and next step unclear. This module makes the existing workflow understandable through progressive disclosure without changing the underlying catalog behavior.

## What Changes

- Replace the simultaneous three-column editor with a focused flow: Products overview → Product detail → Fulfillment offering detail.
- Make the hierarchy explicit: a Product contains fulfillment offerings; an offering contains variants and printable areas; a printable area may apply to selected variants.
- Give each level one clear primary action: “New product”, “Add fulfillment offering”, and section-specific “Add variant” or “Add printable area”.
- Present product and offering details in focused surfaces with breadcrumbs/back navigation, compact summaries, and disclosed sections rather than showing every field at once.
- Group offering controls into Basics, Variants, Printable areas, and Advanced sections; keep provider-specific fields and Choice-network guidance conditional.
- Replace ambiguous labels such as generic “Add” and “Remove” with explicit terminology while preserving current commands and relationships.
- Preserve existing draft creation, explicit saves, selection changes, discard safeguards, validation, destructive confirmations, store isolation, and persistence behavior.
- Add UX states and headless view coverage for empty, populated, draft, navigation, conditional, blocked, validation, and destructive-action flows.

## Capabilities

### New Capabilities

None. This is a presentation and interaction redesign of the accepted store catalog capability.

### Modified Capabilities

- `product-supplier-setup`: clarify the observable management flow, terminology, progressive disclosure, navigation, and preservation of existing catalog editing behavior.

## Impact

- Affects `StoreEditorWindow.axaml` and `StoreManagementViewModel` presentation/navigation state; existing product-supplier application services, domain records, persistence, and relationships remain the source of truth.
- Adds derived overview summaries and focused navigation state, with no new persistence schema or external dependency.
- Adds or updates Avalonia headless view tests and view-model tests for disclosure, hierarchy navigation, conditional Choice behavior, focus, and unsaved-change safeguards.
- The change is limited to the dedicated Store Editor focused surface and does not alter the primary workspace or marketplace/fulfillment integrations.

## Why

The Listing mockup tool now creates managed `MockupImage` assets, but the visible result is only a list of file names. That satisfies the technical generation step while failing the creator's broader outcome: inspect the product presentation, judge whether the design and color treatment work, iterate when they do not, and then use the approved image outside FusionCanvas.

Generated mockups are derived outputs, not independent source work. If a Design asset changes, an earlier mockup can become misleading. The Listing workflow therefore needs an explicit result-consumption and invalidation policy.

## Outcome

Creators can see generated mockups in context, inspect any one at thumbnail and enlarged size, save a copy for downstream listing work, remove an unwanted output, and trust that outputs are removed or clearly marked when their source Design changes.

## Included features

- Replace the filename-only output list with an inspectable Listing mockup gallery.
- Show generated outputs as thumbnails with enough attribution to distinguish color, template, and source Design where available.
- Open an output in an enlarged preview and close it without losing Listing context.
- Save a copy of an output to a user-selected destination.
- Permanently remove a generated mockup after consequence-confirming user action.
- Keep protected Listing items read-only and explain why output mutation is unavailable.
- Invalidate derived mockups when a source Design asset or the Design assignment that produced them changes, with an explicit user-visible explanation.
- Preserve existing local generation, partial-success, attribution, and managed-file safety behavior.

## Dependencies

- Existing `listing-mockup-generation` output metadata and `Asset`/`AssetLink` storage.
- Existing managed workspace file reader/output/deleter boundaries.
- Existing asset preview, export-copy, and removal patterns where they fit the Listing workflow.
- Existing Design-stage mutation and Item stage/status policies.

## Non-goals

- Editing a mockup inside FusionCanvas.
- Replacing the source Design or changing template placement from the Listing gallery.
- Marketplace publishing or external synchronization.
- A new generated-mockup database table when existing attributable assets can represent the result.
- Automatically regenerating outputs after a Design change.
- Building a general-purpose asset browser as part of this module.

## Risks and verification approach

- A stale or missing managed file must be visible as unavailable, never as a blank thumbnail or a successful result.
- Delete and invalidation must remove the persisted asset/link and managed file without orphaning files or deleting source Design assets.
- Download must copy the managed output without exposing or mutating the workspace-managed original.
- Preview and gallery state must remain tied to the active Item when the user changes tabs or selection.
- Verification should combine application tests for output lifecycle and invalidation, integration tests for file copy/delete boundaries, Avalonia headless tests for gallery/control state, and one focused real-desktop journey only if thumbnail/enlarged-view rendering remains framework-sensitive after headless coverage.

## UX preflight

- Primary workflow: review and prepare generated mockups for an Item at Listing stage.
- Expected frequency: repeated during design iteration and before downstream listing/publishing work.
- Surface: remain in the Listing stage tool; use progressive disclosure for enlarged preview and destructive confirmation.
- Workspace footprint: a compact gallery replaces the filename list; the enlarged view is transient and must preserve the Listing context.
- Result states: empty, loading, populated, missing/unreadable, partial generation, stale-after-design-change, deleting, downloading, and protected/read-only.
- Completion destination: after generation, the gallery itself is the inspection destination; after download or deletion, remain in Listing with the resulting state visible.
- User control: no implicit regeneration; download and delete are explicit actions; invalidation is automatic only because the source result is no longer trustworthy and must be explained.

## Open decisions to resolve before implementation

1. Whether changing one Design assignment invalidates only mockups whose metadata names that source asset, or all mockups for the Item when the affected Design mutation cannot be mapped precisely.
2. Whether the enlarged preview should be a modal window using the existing preview pattern or an inline expanded state. The default recommendation is the existing focused preview pattern to avoid permanent workspace expansion.
3. Whether deleting the last output should leave an empty-state explanation that offers “Generate mockups” directly, or only return the user to the existing template controls.

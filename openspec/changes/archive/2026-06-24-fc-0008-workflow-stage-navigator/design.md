## Context

FusionCanvas is establishing Phase 0 foundations for the product workflow before the full listing, asset, and marketplace features exist. The workflow stage navigator sits in the document window and reflects the currently active item, so it intersects UI presentation, active-tab context, and the domain language for the creative stages.

The current accepted specs cover the desktop foundation and architecture guidelines, but they do not define workflow stage navigation behavior. FC-0008 therefore introduces a new capability that future stage tools, listing lifecycle status, and tabbed document behavior can build on.

## Goals / Non-Goals

**Goals:**
- Represent `Idea`, `Concept`, `Design`, and `Listing` as the primary ordered workflow stages for the active item.
- Provide a view model or application-facing state shape that marks each stage as current, available, or unavailable.
- Allow enabled stages to trigger stage navigation for the same active item.
- Keep archive available as a related state or destination without making it part of the primary four-step row.
- Update navigator state when the active item or active tab changes.
- Cover stage-state calculation and navigation decisions with appropriate tests.

**Non-Goals:**
- Enforcing strict mandatory workflow transitions.
- Automatically advancing an item when content changes.
- Implementing marketplace publishing, publishing sync, or complete listing metadata editing.
- Designing the final visual style beyond required state distinctions.
- Implementing full undo/redo for stage navigation.

## Decisions

### Stage vocabulary belongs outside Avalonia controls

The workflow stages should be represented by domain or application-level types, with the Avalonia UI consuming a prepared navigator model.

Rationale: stage identity and ordering are product concepts, while visual emphasis and click handling are UI concerns. Keeping the vocabulary outside Avalonia allows domain or application tests to verify stage ordering and availability without UI framework coupling.

Alternative considered: define stages only in the view model as UI strings. This is simpler for the first screen, but it risks duplicating stage definitions once listing status, stage tools, or persistence need the same vocabulary.

### Navigator state is derived from active item context

The navigator should be rebuilt from the active document item or active tab context whenever that context changes. Each displayed stage should expose label, stage key, availability, current-state flag, and navigation command availability.

Rationale: the navigator must stay synchronized with tab changes and selected items. A derived model avoids stale UI state and keeps the click rules testable.

Alternative considered: allow the navigator control to inspect tabs or selected items directly. That would couple the control to document-window internals and make later reuse harder.

### Availability is permissive, not transition enforcement

The initial availability rule should support navigation to stages that already exist, are complete, or are otherwise available for the item. Unreached future stages are disabled. The navigator should not enforce whether the user may create or advance to a future stage.

Rationale: the PRD explicitly excludes strict mandatory stage transitions and automated advancement. Future stage tools can introduce creation or advancement actions without overloading the navigator.

Alternative considered: block or validate all transitions in the navigator. This would create workflow policy before the product has enough stage-specific behavior to justify it.

### Archive is related state, not a fifth primary stage

Archive should be shown or exposed separately from the `Idea`, `Concept`, `Design`, `Listing` row when the active item is archived, rejected, retracted, or otherwise inactive.

Rationale: the core workflow must remain visible and stable. Archive needs review access without teaching users that archived work is a normal next step after Listing.

Alternative considered: always render Archive as a fifth stage. This would make inactive work visible, but it dilutes the four-step creative workflow and may clutter the foundation UI.

## Risks / Trade-offs

- Stage availability may be underspecified before full concept, design, and listing records exist -> Define the navigator contract in terms of available stage descriptors so the first implementation can use minimal data and later features can enrich the source.
- Archive representation may need visual adjustment once lifecycle status exists -> Keep the spec focused on behavior, not final styling, and expose archive as related state rather than fixed placement.
- Tab synchronization can become brittle if multiple owners mutate active context -> Route navigator updates through a single active-document context or view model rather than letting the control query global state.
- Minimal Phase 0 data may limit realistic navigation targets -> Use placeholders or lightweight stage view targets only where needed, and keep the contract ready for FC-0009 and later stage tool work.

## Migration Plan

No data migration is required. Implement the navigator behind new domain/application/UI types and wire it into the document window only where an active item context exists. Existing empty-shell behavior should remain unchanged when no item is active.

Rollback is straightforward: remove the UI wiring and related stage navigation state without affecting persisted data.

## Open Questions

- Should archive be rendered as a separate command, a status badge, or a secondary destination in the first UI implementation?
- What minimum item data should make a future stage available once concept, design, and listing records exist?
- Should direct stage skipping be represented as availability on the navigator, or handled by separate create/advance actions in stage tools?

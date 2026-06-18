## Why

FusionCanvas needs to make the core creative progression visible while a user works on an item, so creators can understand maturity and navigate available prior work without losing context. This is a Phase 0 foundation feature because later stage tools, tabbed documents, listing status, and creative history features depend on a shared workflow-stage model.

## What Changes

- Introduce a workflow stage navigator for the active document item.
- Display the core stages `Idea`, `Concept`, `Design`, and `Listing` as the primary workflow path.
- Visually distinguish the current stage from available and unavailable stages.
- Enable navigation to stages that exist or are otherwise available for the current item.
- Disable future stages that are not available for the current item and prevent navigation when selected.
- Update the displayed stage state when the active item or active tab changes.
- Represent archived or retracted work as a related state or destination without replacing the four core stages.

## Capabilities

### New Capabilities
- `workflow-stage-navigator`: Defines how FusionCanvas exposes, updates, and uses the Idea -> Concept -> Design -> Listing stage navigator for the active item.

### Modified Capabilities
- None.

## Impact

- Affects the UI layer for document-window workflow presentation and stage navigation commands.
- Affects application-facing contracts or presentation models that describe an item's current workflow stage, available stages, and archive state.
- May use existing domain workflow-stage concepts if present, or add minimal domain/application types needed to keep UI decisions explicit and testable.
- Does not add strict transition enforcement, automated advancement, marketplace publishing sync, or full undo/redo behavior.

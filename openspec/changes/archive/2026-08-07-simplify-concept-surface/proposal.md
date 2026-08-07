## Why

The Concept surface currently presents the same three creative values twice: once in the Concept section and again in the refinement controls. This makes the workflow feel split and hides the original/base idea that the creator is refining. The Concept stage should have one coherent editing surface that works fully without AI while gaining refinement actions when AI is available.

## What Changes

- Replace the three fields directly under the Concept heading with one read-only Base idea field showing the original Idea-stage value.
- Keep `Initialize from base idea` in the Concept surface.
- Remove the `Refine with AI` heading and its extra section framing; make refinement controls part of the basic Concept surface.
- Keep editable Concept idea, Phrase, and Graphic direction fields with their existing Fine tune, Change, and Instructions controls.
- Keep triangle completeness, refinement history, read-only stage behavior, AI availability guidance, and history rollback behavior.
- Ensure manual edits made in the three Concept refinement fields persist through the normal automatic-save path, including before context transitions.
- Preserve history selection as a rollback-and-save operation for the selected Concept idea, Phrase, and Graphic direction configuration.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `concept-refinement`: Make the refinement surface the single Concept editing surface, show the original Base idea as read-only context, remove the redundant heading/framing, and require manual edits in the working triangle fields to commit through the accepted automatic-save behavior.

## Impact

- Avalonia Concept surface layout and headless view tests in `FusionCanvas.App`.
- Concept refinement session/view-model behavior for committing manual triangle edits and preserving transition safety.
- OpenSpec acceptance scenarios for Concept surface composition, editing, history rollback, read-only review, and AI-unavailable use.
- No domain model, persistence schema, public API, or AI provider changes are expected.

This is a cohesive workspace module because it has one outcome: make the Concept stage a single, understandable surface for manual concept development with optional AI enhancement. Verification is bounded to Concept refinement view-model behavior, Avalonia headless view behavior, and the standard solution test/validation baseline.

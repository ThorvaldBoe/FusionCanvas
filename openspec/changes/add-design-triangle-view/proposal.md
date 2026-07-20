## Why

Concept Versions already preserve idea, phrase, graphic direction, and design-triangle score, but creators have no focused view that presents the three together, makes weak alignment visible, and treats each node as an explicit refinement target. FC-0208 adds the Design Triangle View as the central interaction model of the Basic Concept Tool: idea at the top, phrase at the lower left, graphic at the lower right, each editable and selectable for refinement, with optional advisory scoring in the middle and notes for concerns or alternatives.

## What Changes

- Add a Design Triangle View that presents idea, phrase, and graphic direction together, sourced from the selected concept version.
- Make each triangle node editable inline and clickable as the target for refinement.
- Treat the idea node as required; allow phrase or graphic to be marked as intentionally not used.
- Surface advisory scoring in the middle of the triangle (overall quality, weak element, readiness state, critique hint) when AI scoring is available, without making scoring a gatekeeper.
- Add a notes area for concerns, alternatives, or improvement ideas attached to the concept.
- Keep the view usable with human judgment only — no AI required.
- Persist edits through the concept-versions service so the triangle view never writes directly to persistence.

## Capabilities

### New Capabilities

- `design-triangle-view`: Defines the Concept-stage triangle presentation that shows idea, phrase, and graphic together, supports per-node editing and refinement-target selection, marks phrase or graphic as intentionally absent, displays advisory scoring and readiness, captures concept notes, and delegates all persistence to the concept-versions service.

### Modified Capabilities

None. FC-0208 reuses `concept-versions` for idea, phrase, graphic, score, and notes storage, `stage-tool-host` for tool hosting and context, `context-aware-tools` for inherited context, and `workflow-stage-navigator` for stage ownership without changing their accepted requirements.

## Impact

- Adds a `DesignTriangleViewModel` and Avalonia triangle control hosted inside the Basic Concept Tool (FC-0210) through the Stage Tool Host.
- Reuses the concept-versions service for all mutations; the view never writes directly to persistence or workspace storage.
- Adds app and UI tests for node editing, refinement-target selection, not-used markers, advisory scoring display, notes, read-only states, and shared desktop control guidance.
- No schema migration is expected; the view reads and writes concept-versions fields already accepted by the in-flight concept-versions change.

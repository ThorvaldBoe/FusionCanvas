## Why

Concept Versions, the Design Triangle View, and the Creative History Timeline together preserve concept data and present it, but creators have no default Concept-stage tool that hosts the triangle, supports manual refinement, optionally invokes AI to improve a selected triangle node in context, records history, saves promising alternates as new items, and advances the item toward Design. FC-0210 adds the Basic Concept Tool as the default built-in Concept-stage tool hosted through the Stage Tool Host, usable manually and optionally with AI assistance when a provider is configured.

## What Changes

- Add the Basic Concept Tool as the default built-in Concept-stage tool, available from the Concept stage for an existing item and unavailable without one.
- Host the Design Triangle View (FC-0208) as the primary interaction model, with context summary, stage tool selector, triangle canvas, action area, and history pane.
- Support manual concept refinement without requiring AI: edit idea, phrase, and graphic nodes; mark phrase or graphic as not used; save to the current concept version.
- Optionally invoke AI to improve the selected triangle node when an AI concept-refinement provider is configured, using the full item, niche, topic path, triangle values, tags, metadata, prior concept versions, accepted/rejected suggestions, and nearby sibling items as context.
- Constrain AI to affect only the selected node unless the user accepts a broader rewrite; never overwrite saved concept data without approval.
- Let the user accept, edit, reject, regenerate, or save an AI suggestion as a new item under the current topic.
- Preserve rejected suggestions as negative guidance where practical.
- Show an advisory quality indicator in the middle of the triangle when AI scoring is available.
- Record history-pane entries for manual edits, AI suggestion requested/accepted/rejected, score updates, concept version changes, restored states, and suggestions saved as new items; feed important entries into the Creative History Timeline.
- Allow the user to restore a previous history entry.
- Allow the tool to advance the item toward Design once the creator is satisfied with the concept.

## Capabilities

### New Capabilities

- `basic-concept-tool`: Defines the default built-in Concept-stage tool that hosts the Design Triangle View, supports manual and optional AI-assisted concept refinement, records history, saves promising alternates as new items, and advances the item toward Design through the workflow-stage-navigator.

### Modified Capabilities

None. FC-0210 reuses `concept-versions` for concept storage and selection, `design-triangle-view` for the triangle presentation, `creative-history-timeline` for cross-stage history, `stage-tool-host` for hosting and context, `context-aware-tools` for inherited context, `workflow-stage-navigator` for stage advancement, and `idea-inbox`-style listing creation for save-as-new-item without changing their accepted requirements. AI assistance is accessed through an abstract AI concept-refinement provider port that a future AI-capability change will implement.

## Impact

- Adds a `BasicConceptTool` view model and Avalonia tool registered as the default Concept-stage tool through the Stage Tool Host registry.
- Defines an `IConceptRefinementProvider` application port for AI-assisted node improvement and scoring; a future AI-capability change will provide the implementation.
- Reuses the concept-versions service for all persistence and the design-triangle-view for triangle interaction.
- Adds app and UI tests for manual refinement, AI-assisted refinement against a deterministic fake provider, accept/reject/regenerate/save-as-new-item, history-pane entries and restore, advisory scoring display, save-as-new-item inheritance, and stage advancement.
- No schema migration is expected; the tool reads and writes through existing services.

# FC-0209 - Creative History Timeline

## Summary

The Creative History Timeline shows important events in a listing's development.

The Basic Concept Tool also uses a focused history pane for Concept-stage changes. Important concept history entries can appear in the broader Creative History Timeline.

The Basic Design Tool can also contribute important Design-stage events, especially imports, generations, cleanup operations, variant status changes, and final design selection.

## User Need

Creators need to understand how a listing evolved without searching through notes, files, and prompt outputs.

## Requirements

- The timeline can show important listing events.
- Events may include concept changes, prompt records, status changes, asset imports, design updates, mockup records, and metadata changes.
- Concept-stage events may include manual edits to idea, phrase, or graphic, AI suggestions, accepted suggestions, rejected suggestions, scoring updates, restored prior states, and suggestions saved as new items.
- Design-stage events may include manual asset imports, external AI imports, in-app AI generations, prompt records, variant creation, variant rejection, cleanup actions, final variant promotion, and final selection changes.
- Timeline entries should preserve useful context.
- Users can review history without changing current listing state.
- The timeline should help explain creative decisions.
- Restoring a prior concept state belongs to the Concept tool or undo/redo behavior, not the read-only timeline itself unless explicitly supported.

## Acceptance Criteria

- A user can see a listing's major creative events.
- A user can understand when important changes happened.
- Status, asset, prompt, and concept history can appear together.
- Important Basic Concept Tool history entries can be preserved as listing history.
- Important Basic Design Tool history entries can be preserved as listing history.
- The timeline supports context preservation.

## Out of Scope

- Full audit logging
- Undo/redo
- Collaboration comments
- Automated recommendations

## Related Notes

- [[Roadmap]]
- [[Product]]
- [[Principles]]
- [[FC-0210 - Basic Concept Tool]]
- [[FC-0211 - Basic Design Tool]]

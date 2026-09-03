# Add Explicit Design Slot Artwork Controls Retrospective

## Outcome

Issue #276 is implemented. Design-stage final-artwork slots now expose clear drag/drop guidance, PNG Browse/Replace actions, managed thumbnails, and discoverable Enlarge, Download, and Remove actions while preserving independent persisted assignments and category separation from Supporting Images and Mockup Template sources.

## Feedback-Driven Adjustments

| Initial assumption | Evidence | Correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| Existing `+ Add image` copy was sufficient discoverability. | Issue #276 requires explicit drag/drop and Browse/Upload affordances. | Added final-artwork slot labels, empty-state PNG guidance, Browse/Replace copy, tooltips, and automation names. | Missing requirement / UX adjustment | Design-stage image controls | Kept in the capability delta spec; no broader rule needed. |
| Existing slot service behavior was enough without a multi-slot regression. | The service already handled row/area scoping, but independent durability was a core acceptance criterion. | Added a two-slot assignment and reload test. | Testing lesson | Any future multi-slot workflow | Kept as focused application coverage; no global test rule needed. |

## Learning Review

- Result: reusable lessons identified, but no durable documentation promotion is warranted.
- Evidence reviewed: issue #276 acceptance criteria, final proposal/design/spec/tasks, focused Avalonia and application tests, constrained solution baseline, strict OpenSpec validation, and recent history for Design-stage persistence.
- Promotions completed: none.
- Deferred promotions: none; the two lessons are capability-specific and are retained in the delta spec and tests rather than broadening shared guidance.

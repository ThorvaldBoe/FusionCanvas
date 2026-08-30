# Fix Secondary Editor Window Layout Persistence Retrospective

## Outcome

Reusable Store Management editor windows now participate in the existing local, screen-safe normal-geometry persistence path. Their positions and sizes are keyed independently; transient confirmation dialogs remain unpersisted.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| The prior secondary-window change covered the full “every window” expectation. | Exploration of issue #230 found five reusable Store Management editor windows added afterward without geometry keys or helper attachment. | Expand the accepted non-transient boundary to include those five editors while keeping confirmation dialogs transient. | Missing requirement | Change-specific | Promoted to the delta specification. |

## Learning Review

- Result: reusable lesson identified.
- Evidence reviewed: issue #230, accepted `window-layout-persistence` and `application-settings` specs, prior secondary-window change artifacts, current Store Editor construction paths, focused Avalonia tests, and strict OpenSpec validation.
- Promotion completed: the capability delta now names reusable focused editors explicitly and preserves the confirmation-dialog exclusion.

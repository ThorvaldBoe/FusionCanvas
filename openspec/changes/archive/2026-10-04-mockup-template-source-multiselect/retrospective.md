# Mockup Template Source Image Multi-Selection Retrospective

## Outcome

The Mockup Template source-image table now supports transient replace, toggle, and range selection while preserving one active row for the detail editor. Creators can archive the selected rows through the existing save workflow, with keyboard parity, selected-count feedback, and reconciliation when rows are removed. The implementation was merged in PR #724 and its verification baseline is recorded in `verification.md`.

## Feedback-Driven Adjustments

| Initial assumption | Evidence | Correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| A single active row was enough for source-image management. | Archiving several source images is a meaningful repeated management task, but the detail editor still needs one authoritative active row. | Separate transient selection from the active editor row and make bulk archive explicit. | Missing requirement | Focused collection editors with a single-row detail surface | Captured in the delta specification and design. |
| Pointer multi-selection alone would complete the workflow. | The source-image table is a keyboard-reachable management surface and selection changes affect a destructive action. | Provide equivalent keyboard gestures, accessible selected state, selected-count feedback, and post-removal focus/active-row reconciliation. | Reusable UX principle | Collection management with selection and bulk actions | Captured in the delta specification, design, and verification evidence. |

## Learning Review

- Result: reusable lessons identified; no additional factory-wide promotion was needed.
- Evidence reviewed: final proposal, design, delta specification, completed task list, verification evidence, implementation history (`207ebc0`, `4c7eaef`), and merged PR #724.
- Promotions completed: the capability-specific selection and active-row rules remain in the accepted delta specification; existing repository UX guidance already covers selection, focus, keyboard reachability, and destructive-action aftermath.
- Deferred promotions: none.


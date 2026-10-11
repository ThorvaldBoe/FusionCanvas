# Clarify Linked Product Visibility Toggle Retrospective

## Outcome

The Printify import dialog now gives its linked-products toggle a state-bound action label and matching accessible name. Linked products remain hidden by default and cannot be selected for import.

## Feedback-Driven Adjustments

| Initial assumption | Evidence | Correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| The action's fixed “Show/hide” wording was sufficient because the list visibly changes. | Issue #914 reports that the label does not communicate current state or next action; the accessibility tree also had no explicit name. | Bind visible and accessible names to the same state-derived action label. | Missing acceptance clarity | Toggle controls throughout the UI should communicate state/action; the repository UX guideline already expresses this. | None; existing guidance already covers the principle. |

## Learning Review

- Result: no reusable lessons
- Evidence reviewed: issue #914, the accepted Printify import spec, UX/UI guidelines, proposal, delta spec, design, implementation, headless UI test, and solution baseline.
- Promotions completed: none; applicable state clarity guidance is already present in `docs/ux-guidelines.md`.
- Deferred promotions: none.

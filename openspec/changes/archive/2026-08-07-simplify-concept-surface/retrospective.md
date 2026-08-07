# Simplify Concept Surface Retrospective

## Outcome

Implemented the approved Concept-page simplification from GitHub issue 146. The Concept stage now has one continuous surface with a read-only Base idea, editable Concept/Phrase/Graphic working fields, optional AI actions, completeness, and history. Manual edits use the existing inspector save path, including transition flushing, and history rollback remains save-backed.

## Feedback-Driven Adjustments

| Initial assumption | Evidence and correction | Classification | Promotion |
| --- | --- | --- | --- |
| The three upper Concept fields could simply be removed. | User clarified that the original Idea must remain visible as Base idea while the refinement fields become the manual editing path. | Missing UX requirement | Captured in the delta spec; no further promotion needed. |
| History selection only needed to update the visible fields. | User clarified that selecting history should auto-save the full Concept configuration. | Missing persistence requirement | Captured in the delta spec and rollback tests. |
| AI refinement could remain a separate feature section. | User requested AI actions be integrated into the basic Concept surface and remain optional. | UX/UI principle | Captured in the delta spec and design; no broader guideline promotion needed. |

## Learning Review

- Result: reusable lessons identified in the change artifacts; no additional repository-wide promotion required.
- Evidence reviewed: approved proposal, design, delta spec, implementation, focused Avalonia/session tests, verification record, and user feedback.
- Promotions completed: Concept-specific behavior and rationale are recorded in the OpenSpec delta artifacts.
- Deferred promotions: none.
- Known verification limitation: the full solution baseline has one unrelated existing headless layout failure in `StoreEditorHeadlessTests.NicheDetailsFields_KeepTrailingMargin`; Concept refinement tests pass.

# Concept Refinement Instructions Retrospective

## Outcome

Creators can give the AI a short per-corner instruction that steers a Fine tune or Change action for one design-triangle corner. Three narrow, unlabeled instruction fields (placeholder `Instructions`) are provided, one per corner, tucked under each corner's Fine tune/Change buttons beside the corner's value box. A non-empty instruction is added to the AI request as bounded supplemental guidance; empty leaves behavior unchanged; the field clears after a successful apply of that corner.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| Instruction fields should span a full-width row beneath each corner (6-row interleaved grid) | Reviewer/user: layout was wrong; intended a narrow textbox fitting under the buttons, beside the corner's value box, not a full-width row that grows the panel | Reverted to one row per corner (3-row grid); each instruction field occupies columns 2-3 with `VerticalAlignment="Bottom"`, narrow and beside the value box | UI / implementation defect | Change-specific | None (documented in design D4) |

## Deferred or Change-Specific Notes

- The layout decision (narrow field under the buttons beside the value box) is recorded in design D4 and the corresponding headless view test.

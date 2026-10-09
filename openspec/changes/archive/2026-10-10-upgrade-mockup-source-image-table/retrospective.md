# Upgrade Mockup Source Image Table Retrospective

## Outcome

The focused Mockup Template source-image table now uses the existing virtual grid with fixed-height rows. Existing sort, multi-selection, selected-image editing, archive, and incomplete-draft behavior remains owned by the current view model. Preview-read warnings remain visible and expose their full explanation through a tooltip and automation help text.

## Feedback-Driven Adjustments

| Initial assumption | Evidence | Correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| A row action could rely on the Button receiving the grid's pointer event and command. | The headless hit test showed the grid panel as the routed source for the Archive cell, and the normal command path did not activate. | Resolve the realized row and child action from the pointer position, capture the action for release, and keep keyboard activation on the Button click path. | Ordinary implementation defect | Specific to interactive child controls in this virtual-grid template | No broad promotion; the virtual-grid experiment guidance already requires focused child-input tests. |
| The existing multi-line preview warning could fit the grid unchanged. | The grid requires fixed row heights. | Use a visible warning glyph and keep the full text in its tooltip and automation help text. | Missing requirement | Source-image table warning presentation | Promoted in the delta spec and to be synced to the accepted source-image capability. |

## Learning Review

- Result: no additional reusable lessons
- Evidence reviewed: proposal, design, source-image delta spec, tasks, criterion-level verification, implementation commit `872a9e1`, focused headless coverage, full solution test run, and `docs/experiments/virtual-data-grid.md`.
- Promotions completed: the requirement for sortable source-image columns and accessible preview warnings is captured in the delta spec; the existing experiment already documents grid-level pointer handling and the need to test interactive child controls.
- Deferred promotions: none. The observed coordinate routing is specific to this grid template and does not justify a broader engineering rule beyond the existing experiment guidance.

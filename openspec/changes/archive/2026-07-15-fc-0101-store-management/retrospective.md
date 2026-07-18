# Store Management Retrospective

## Outcome

Store management was approved after the main window was reduced to lightweight store selection and management access, with create, edit, archive, restore, and delete workflows moved into a dedicated editor window.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| A compact store selector and management surface could share the main shell. | Inline administration consumed too much of the primary workspace and competed with normal navigation. | Keep only selection and editor access in the shell; move occasional store administration into a dedicated editor window and use compact/expanded disclosure for the store list. | Reusable UX principle | User-facing management features with occasional CRUD or setup workflows | Promoted to `docs/ux-guidelines.md` under primary-workspace protection and progressive disclosure. |
| Generic create/edit controls were sufficient once the management surface existed. | Validation exposed unclear persistence timing, unsafe selection changes, irrelevant actions, and weak next-step focus. | Create new stores as unsaved drafts, guard discard on switch/close, preselect the active store, enable only relevant actions, focus the name field, and choose a remaining store after deletion. | Capability behavior with reusable UX aspects | Store management specifically; editing workflows generally | Store behavior was captured in the accepted store-management spec; reusable editing, state, and focus principles were promoted to `docs/ux-guidelines.md`. |
| Default button layout and multiple visible affordances could be left to implementation. | Full-width action rows and duplicate collapse controls made the interface heavier and less clear. | Use compact action groups, small tooltip-labeled icon controls, and one clear owner for each action. | Reusable UI/UX principle | Dialog actions and compact workspace controls | Previously promoted to `docs/ui-guidelines.md`; action ownership is also reflected in `docs/ux-guidelines.md`. |

## Learning Review

- Result: reusable lessons identified.
- Evidence reviewed: initial and final store-management design/specification, feedback-driven task additions, and implementation history.
- Promotions completed: workspace protection, progressive disclosure, editing safety, coherent action state, focus, and action ownership are documented in shared UX/UI guidance.
- Deferred promotions: none.

## Deferred or Change-Specific Notes

- Remembering the active store across restarts remains an explicit open product decision rather than a generalized lesson.
- Store deletion rules remain capability-specific accepted behavior and should not become a universal deletion policy.

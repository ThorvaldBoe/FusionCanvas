# FC-0104 Listing Management Retrospective

## Outcome

FC-0104 delivered topic-resolved listing creation and management, atomic persistence, editable-tree operations, a focused properties/lifecycle surface, and coordinated tree/tab navigation. The final approved desktop behavior uses one reusable working tab for normal selection, preserves existing tabs for Ctrl-click/Open in Tab, selects the matching tree row when a tab is activated, provides a bounded resizable navigation pane, and presents compact Obsidian-inspired tabs. All automated suites and the complete manual verification checklist passed.

## Feedback-Driven Adjustments

| Initial assumption | Evidence | Correction | Classification | Applicability | Promotion |
|---|---|---|---|---|---|
| Canonical tree selection should remain independent of document tabs. | User testing found Ctrl-click did not preserve a useful existing selection and requested at least one working tab. | Normal selection now creates or reuses one working tab; Ctrl-click/Open in Tab is additive and duplicate-safe; the final tab remains visible. | Missing requirement and reusable UX principle | All tree-and-document workflows | Listing-management spec and `docs/ux-guidelines.md` |
| Tab activation only needed to update document and workflow context. | User reported that clicking a tab did not select its item or group. | Tab activation now selects and reveals the corresponding tree row through a non-recursive synchronization path. | Ordinary implementation defect revealing a reusable interaction principle | Any navigation-backed tab | `docs/ux-guidelines.md` and existing UI tab guidance |
| A fixed-width navigation pane was sufficient. | User screenshot showed New Group and New Listing labels clipped at the implemented width. | Added a bounded splitter and protected minimum document width. | Reusable UI/layout principle | Primary desktop shells with navigation command groups | `docs/ui-guidelines.md` |
| Generic button styling was adequate for document tabs. | User comparison with Obsidian identified excessive height, awkward close-button alignment, weak active styling, and untrimmed long titles. | Tabs became slender, single-line and ellipsized, with a centered borderless close affordance, rounded active outline, and top inset. | Reusable visual principle | All document tab strips | `docs/ui-guidelines.md` |
| FC-0104 could be finalized while FC-0103 was evolving in parallel. | The user paused work, completed FC-0103 verification, and rebased its implementation before FC-0104 artifacts were revised. | FC-0104 was realigned to the finalized `GroupHierarchy`, niche-rooted tree, canonical selection, and default-niche contracts before implementation. | Architecture/change-sequencing lesson | Changes extending unfinished foundational work | Retained in design and this retrospective; broader promotion deferred because the repository workflow already requires dependency inspection and this case adds no narrower rule. |

## Learning Review

- Result: reusable lessons identified
- Evidence reviewed: final proposal, design, delta specification, completed task checklist, user dependency/rebase updates, user screenshots and interaction feedback, final implementation tests, manual verification result, and recent Git history through `Coordinate listing selection with reusable working tabs`.
- Promotions completed: capability-specific tab behavior in the listing-management delta specification; navigation/tab coordination in `docs/ux-guidelines.md`; bounded resizable navigation and compact tab presentation in `docs/ui-guidelines.md`.
- Deferred promotions: the FC-0103/FC-0104 sequencing lesson remains change-specific because existing workflow guidance already requires dependency analysis; no additional architecture rule was necessary.

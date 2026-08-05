# Default Snowclones Retrospective

## Outcome

Ships a curated UTF-8 starter snowclone library alongside the app, added only when the creator explicitly uses `Import bundled library`; the library stays empty by default and the bundled content is never auto-imported or resurrected.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| Bundled starter snowclones auto-import once on first load | Late product/discovery decision reversed this; auto-import conflicts with an empty-by-default library and could surprise creators | Remove automatic one-time initialization; keep the library empty by default and import bundled content only on explicit `Import bundled library` | Missing requirement / UX | Reusable scope | Promoted into `snowclone-library-empty-by-default` delta + this change's delta (clear-spec reconciliation) |
| Verification evidence cited auto-init tests that were later removed | QA-5 drift review found `verification.md` cited `InitializeAsync_ImportsOnceAndPersistsMarker` etc. that no longer exist | Re-baselined `verification.md` to the surviving explicit-import behavior and real test names; recorded the supersession | Implementation defect | Change-specific | Recorded in this change's verification/retrospective |

## Learning Review

- Result: reusable lessons identified
- Evidence reviewed: QA-5 report, `default-snowclones` delta spec + verification, `SnowcloneLibraryService`/`SnowcloneLibraryServiceTests`, and the conflicting sibling delta in `snowclone-library-empty-by-default`.
- Promotions completed: empty-by-default behavior captured in `snowclone-library-empty-by-default` delta and reconciled here.
- Deferred promotions: none.

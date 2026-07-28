# manage-rejected-phrases Retrospective

## Outcome
Added a focused Rejected Phrases management dialog launched from the Ideation dialog that lets creators view, filter (whole-workspace / niche / group), edit (phrase + reason, preserving identity/scope/mode/`CreatedAt` and advancing a new optional `UpdatedAt`), manually create (`Basic` mode, active-scope default), and permanently delete durable `IdeationRejection` records. Backed by a new `RejectedPhraseManagementService` over the existing `IWorkspaceRepository`, a within-scope normalized-uniqueness rule, and a transactional SQLite schema-v8 migration adding the nullable `ideation_rejections.updated_at` column. The module introduced no Ideation generation/context-assembly changes, no CSV/archive/sync behavior, no workspace-transfer semantic changes, and no main-window/settings launcher.

## Feedback-Driven Adjustments
No user feedback invalidated an assumption after implementation began. The four discovery decisions (whole-workspace filterable scope; manual creation defaults to active scope reusing `Basic` mode; editing limited to phrase+reason with a new `UpdatedAt`; launcher from the Ideation dialog only) were captured in the proposal and held through implementation.

## Learning Review
- Result: no reusable lessons identified for promotion.
- Evidence reviewed: proposal, design, delta specs, tasks, verification.md, and the implemented Domain/Application/Integration/App changes plus the 770-test baseline.
- Promotions completed: none.
- Deferred promotions: none.
  - Rationale: the module reused established patterns (Snowclone Library dialog shape, `IWorkspaceRepository` atomic save path, versioned SQLite `ALTER TABLE ADD COLUMN` migration, `WorkspaceChanged` event for navigation refresh). No novel reusable rule, UI principle, or architecture guideline emerged that is not already captured in the accepted specs or existing guidance. Within-scope uniqueness for rejected phrases is capability-specific behavior already specified in the `rejected-phrase-management` delta spec; the `UpdatedAt` audit pattern mirrors the existing `Snowclone.UpdatedAt` shape and the `items.updated_at` column.

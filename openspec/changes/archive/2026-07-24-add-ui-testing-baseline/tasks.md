## 1. Canonical Testing Guidance

- [x] 1.1 Update `AGENTS.md` and `openspec/project.md` to require targeted, risk-based real desktop UI verification for every user-facing delivery module while preserving the separate headless xUnit baseline
- [x] 1.2 Update the architecture testing strategy and FC-0007 planning baseline to describe desktop UI coverage, isolation, interaction-risk selection, and evidence expectations
- [x] 1.3 Update canonical planning guidance so module planning explicitly includes targeted real desktop UI verification

## 2. QA Review Coverage

- [x] 2.1 Add a dedicated full-QA desktop UI regression task to `docs/qa-review.md` with an accepted-feature inventory and pass/fail/blocked/not-applicable matrix
- [x] 2.2 Define the complete all-features UI regression checklist, including primary workflows and applicable keyboard, pointer, focus, validation, filtering, destructive, persistence/restart, recovery, accessibility, and tab/window paths
- [x] 2.3 Update QA execution modes, reporting format, cadence, and finding routing to include desktop UI results and isolation evidence

## 3. Active Change Alignment

- [x] 3.1 Align FC-0103's design, tasks, and verification record with the project-wide real desktop UI verification terminology and evidence requirements
- [x] 3.2 Confirm no other active user-facing OpenSpec change lacks an explicit desktop UI verification plan

## 4. Validation

- [x] 4.1 Search canonical non-archived testing documents for obsolete statements that exclude or subordinate real desktop UI verification and reconcile them
- [x] 4.2 Run strict OpenSpec validation for `add-ui-testing-baseline` and all active changes, then run `git diff --check`

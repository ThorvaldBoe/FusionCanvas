# Focused Mockup Template Editor Retrospective

## Outcome

Mockup Template management now gives the collection the full parent surface and moves the existing preview-first mapping workflow into one guarded Add/Edit modal without changing catalog or revision behavior.

## Feedback-Driven Adjustments

Existing placement, mapping-label, provider-data, unavailable-image, and UI-description tests were migrated to the modal rather than replaced, retaining evidence for the behavior that the layout change was required to preserve.

## Learning Review

- Candidate reusable lesson: independently confirms #199's finding that modalizing a described inline fixture requires semantic IDs, layout/state expectations, and any generated renderings to be reconciled as one completion check.
- Promotion completed through #199's earlier merge-order branch: the shared `docs/qa-review.md` changed-scope UI-description checklist was confirmed once, with no duplicate rule added here.
- Evidence reviewed: issue #201, proposal/spec/design/tasks, implementation diff, focused and legacy headless suites, UI-description suite, and full baseline.
- Deferred lesson: a shared guarded-draft controller remains premature because the two editors have materially different snapshot fields and lifecycle details.

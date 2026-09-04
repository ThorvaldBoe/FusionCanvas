# Reorder Catalog Color and Size Values Retrospective

## Outcome

Catalog Color and Size values now have explicit persisted order, deterministic migration/backfill, accessible move commands, and visible drag handles while preserving stable identities and relationships.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| Existing `SortOrder` was sufficient | New values all defaulted to zero and there was no user operation | Append new values and normalize active values in the catalog service | Implementation defect | Change-specific | Captured in design and tests; no global promotion |
| A single pointer interaction was enough | Keyboard and assistive technology need equivalent movement | Add Move up/Move down commands and target-specific labels | UX/UI | Reusable | Existing UX guidelines already cover keyboard/focus; no additional promotion |

## Learning Review

- Result: reusable lessons identified and already covered by existing project guidance.
- Evidence reviewed: proposal, delta spec, design, tasks, implementation diff, focused tests, build output, and migration behavior.
- Promotions completed: none; existing UI/UX and testing guidance already states the applicable rules.
- Deferred promotions: none.

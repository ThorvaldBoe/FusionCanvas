# Searchable Mockup Template Table Retrospective

## Outcome

Issue 878 replaces the Mockup Template card list with a bounded, searchable virtual table. Search remains Offering-scoped and transient. Selecting a row exposes full details and Edit, Duplicate, and confirmed soft-archive actions. The full solution baseline passed with 2,349 tests.

## Feedback-Driven Adjustments

The initial embedded-action approach was replaced with a selected-row detail and action strip after headless input showed that the virtual grid's pointer handling suppresses interactive cell buttons. Keyboard Up/Down navigation was added after headless coverage showed the grid did not select rows by keyboard in its current configuration.

## Learning Review

- Result: no additional reusable lessons.
- Evidence reviewed: issue 878, proposal, delta spec, design, implementation, headless interaction tests, full solution baseline, existing virtual-grid experiment, and repository UI/UX guidance.
- Promotions completed: none. The virtual grid's provider snapshot, cell input, and event-routing constraints are already documented in `docs/experiments/virtual-data-grid.md`.
- Deferred promotions: none.

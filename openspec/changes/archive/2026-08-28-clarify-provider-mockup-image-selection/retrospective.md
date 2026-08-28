# Provider Mockup Image Selection Guidance Retrospective

## Outcome

Provider image selection now has explicit provenance, accessibility, negative capability, typed load states, and actionable recovery while preserving the existing candidate and placement workflow.

## Feedback-Driven Adjustments

Loading required a controlled pending collaborator; collection counts and free-text messages alone could not prove the transient state. The final tests therefore verify the state before completing the provider task.

## Learning Review

- Candidate reusable lesson: asynchronous external-data selectors should model loading, available, empty, unavailable, and error as explicit presentation states and keep source/recovery guidance visible instead of inferring state from a message or item count.
- Promotion completed: added this principle to `docs/ux-guidelines.md` under complete interaction states after user confirmation.
- Evidence reviewed: issue #203, proposal/spec/design/tasks, provider candidate contract, implementation diff, controlled ViewModel tests, production-XAML headless tests, and full baseline.
- Deferred promotions: none.

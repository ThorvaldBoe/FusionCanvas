# Search Mockup Template Colors Retrospective

## Outcome

The Mockup Template editor now supports transient, case-insensitive Color search with no-match guidance while preserving canonical Color selection, readiness, dirty-state, source applicability, and persistence behavior. The feature was delivered in PR #815. A layout regression exposed by the new control was corrected in PR #820, and the full deterministic suite is green.

## Feedback-Driven Adjustments

| Initial assumption | Evidence | Correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| Adding the search controls would be a local presentation change with no effect on existing editor interactions. | The existing `MockupSourceRow_SelectsFromCellsWhitespaceAndKeyboard_WithoutArchiving` headless test failed when the search controls were present; removing them made it pass. | Increased the Mockup Template editor default height from 820 to 900 so the source table retains a reliable hit-testable region. | Ordinary implementation defect / UI layout | Mockup Template editor layouts that combine a source table with a detailed selected-item editor. | No durable guideline promotion; the focused regression test and verification evidence are sufficient. |
| The initial verification record was final once the feature tests passed. | The follow-up regression fix changed the quality-gate result from one failing test to a green 2,142-test baseline. | Updated `verification.md` with the final local and CI evidence before archive. | OpenSpec process correction | Future archive reviews of changes that receive post-merge fixes. | No durable guideline promotion; archive verification already requires final evidence. |

## Learning Review

- Result: reusable lessons identified, but no new durable guideline promotion was needed.
- Evidence reviewed: final proposal, design, delta spec, tasks, verification, implementation diff, Git history, PR #815, PR #820, focused tests, full solution baseline, and CI deterministic-test result.
- Promotions completed: none.
- Deferred promotions: none; the layout lesson is specific to this editor and is covered by the existing headless regression test.

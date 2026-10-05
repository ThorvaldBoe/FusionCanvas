# Extend Listing Mockup Consumption Retrospective

## Outcome

The Listing mockup workflow now presents generated outputs as inspectable, attributable assets rather than filename-only results. Creators can review thumbnails, open an enlarged preview, save a copy, remove an output with confirmation, and receive explicit stale-result behavior when the source Design changes. The implementation was merged in PR #817 and its verification baseline is recorded in `verification.md`.

## Feedback-Driven Adjustments

| Initial assumption | Evidence | Correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| Generating and persisting mockup files was sufficient to complete the user-facing action. | The generated results were displayed only as filenames, so the creator could not judge the design or color treatment in the workflow. | Treat inspection, decision, downstream handoff, iteration/recovery, and freshness as applicable outcomes alongside file creation. | Reusable UX principle | Any action that creates or transforms a user-relevant artifact | Promoted to the Software Factory UX and Functionality & Logic standards; capability details are captured in the delta specification. |
| A generated derivative could remain available after its source Design changed. | A stale mockup can misrepresent the current Design while still looking like a valid result. | Record source attribution and invalidate affected derivatives, falling back to Item-wide invalidation when precise mapping is not trustworthy. | Reusable functionality and logic principle | Any derived result with mutable source data | Promoted to the Functionality & Logic standard and captured in the delta specification. |
| A filename list was an adequate result surface. | The workflow also needs visual review, export, removal, and recovery states. | Make the result surface a gallery with thumbnail, enlarged view, save-copy, removal, unavailable, and stale states. | Missing requirement | Generated visual or media outputs | Captured in the delta specification, design, and verification matrix. |

## Learning Review

- Result: reusable lessons identified and promoted.
- Evidence reviewed: final proposal, design, delta specification, completed task list, verification evidence, user feedback, implementation history (`a00b14d`, `92f07ab`), and merged PR #817.
- Promotions completed: outcome completeness was promoted to the Software Factory UX standard; result-consumer and source-invalidation checks were promoted to the Functionality & Logic standard; capability-specific behavior remains in the Listing mockup delta specification.
- Deferred promotions: none.


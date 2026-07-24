# Add UI Testing Baseline Retrospective

## Outcome

Every user-facing delivery module plans targeted, risk-based real desktop UI verification covering its critical workflow and distinct high-risk acceptance paths, while allowing equivalent low-risk variants to remain in deterministic tests. The full QA review includes a comprehensive UI regression task covering all implemented user-facing features. Desktop UI verification uses isolated/disposable data and covers relevant keyboard, pointer, focus, persistence, restart, destructive-action, validation, and recovery behavior.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| Code-level tests were sufficient for module completion. | The real Avalonia interaction path was not exercised, leaving framework-level defects uncaught. | Require targeted, risk-based real desktop UI verification for user-facing modules. | Missing requirement / process | Reusable for all user-facing delivery modules | Promoted to `testing-baseline` and `qa-review-baseline` specs. |
| Desktop verification could use the contributor's workspace. | Mutating scenarios risk the contributor's normal workspace data. | Require isolated/disposable workspace and database for any mutating desktop check. | Architecture / security | Reusable for all desktop UI verification | Promoted to `testing-baseline` spec. |

## Deferred or Change-Specific Notes

- This change was later reconciled with `adopt-headless-view-testing` and `adopt-module-delivery-workflow` to make desktop UI verification risk-based and budget-conscious rather than unconditional.
- The comprehensive all-features UI regression in the full QA review was later refined to QA-6 headless view coverage as the mandatory lane, with live desktop as supplemental evidence.

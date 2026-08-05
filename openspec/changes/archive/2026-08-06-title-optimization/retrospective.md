# Title Optimization Retrospective

## Outcome

Adds an `Optimize` command next to the Working title in the listing inspector Overview that asks AI (Title purpose) to produce a short title from the item's creative content, makes it unique against the store via a bounded retry loop with a numeric-suffix fallback, and excludes operational/secret fields.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| A hard maximum title length should be enforced | `short` is a prompt-level intent, not an invariant; enforcing a length is out of scope for this module | Treat `short` as a prompt instruction only; no hard length bound | Missing requirement / scope | Reusable scope | Captured in the delta spec (deferred length policy) |
| Uniqueness could loop indefinitely | Repeated collisions need a bounded escape | Bound the loop to `MaximumAttempts`, fall back to the smallest numeric suffix | Implementation decision | Change-specific | Kept in design/spec bound |
| AI-candidate collisions against archived/rejected items | Only active items should constrain uniqueness | Exclude archived and `Rejected` items from the collision set | Missing requirement | Reusable scope | Captured in the delta spec |

## Learning Review

- Result: reusable lessons identified
- Evidence reviewed: QA review of the 6 features, `title-optimization` delta spec + verification, `TitleOptimizationService`/`TitleUniquenessPolicy` and their tests.
- Promotions completed: uniqueness/collision and secret-exclusion requirements captured in the accepted spec.
- Deferred promotions: none.

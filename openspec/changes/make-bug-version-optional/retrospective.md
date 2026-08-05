# Make Bug Version Optional Retrospective

## Outcome

The Bug Report form retains version-or-commit information as useful context, but accepts reports when it is omitted. All other diagnostic fields remain required.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| Version or commit should be mandatory for bug reports. | FusionCanvas has not published versioned releases, so many reporters cannot supply it. | Retain the visible field but make it optional. | Missing requirement | Capability-specific | Delta spec for `github-issue-workflow`; sync pending archive review. |

## Learning Review

- Result: no reusable lessons beyond the updated GitHub Issue workflow requirement.
- Evidence reviewed: final proposal, design, delta spec, task record, Issue Form, strict validation, and deterministic test evidence from 2026-08-06.
- Promotions completed: none outside the capability specification; the requirement change is ready to sync.
- Deferred promotions: none.

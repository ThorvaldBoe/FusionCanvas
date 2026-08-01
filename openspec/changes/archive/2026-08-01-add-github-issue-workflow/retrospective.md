# add-github-issue-workflow Retrospective

## Outcome

FusionCanvas now has a documented GitHub Issue intake and backlog workflow, version-controlled Bug Report and Feature Request forms, a small live label taxonomy, and explicit links to the existing OpenSpec delivery lifecycle. OpenSpec remains the authoritative source for significant behavior and acceptance evidence.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| GitHub settings could not be verified during planning. | Read-only GitHub access confirmed Issues were enabled and default labels were unused; form files remain unpushed. | Create the documented labels now, preserve a post-push rendered-form validation step. | Implementation / operational verification | Change-specific | Recorded in `verification.md`; no durable rule beyond the new workflow. |

## Learning Review

- Result: reusable lessons identified.
- Evidence reviewed: final proposal, design, delta specifications, completed tasks, verification evidence, current repository configuration, and recent Git history.
- Promotions completed: created accepted `github-issue-workflow` capability; updated the accepted `openspec-project-workflow` requirement; added contributor and agent guidance in `CONTRIBUTING.md` and `AGENTS.md`.
- Deferred promotion: post-push rendered-form submission remains a recorded environment-dependent verification check in `verification.md`.

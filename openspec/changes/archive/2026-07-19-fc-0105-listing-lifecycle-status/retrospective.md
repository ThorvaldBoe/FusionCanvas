# FC-0105 Listing Lifecycle Status Retrospective

## Outcome

Workflow stage and lifecycle status are now two independent, user-owned, persisted facts per listing. `ListingStatus` is redefined to the four operational states (Draft, Published, Paused, Rejected); the stage-like `Active`/`Ready` and the duplicate `Archived` enum values are gone, with archive remaining solely the `IsArchived` flag. The workflow stage navigator is driven by the persisted stage: reached stages are navigable, future stages are disabled, and the navigator distinguishes the prominent current stage from a secondary active-view marker. Stage moves happen through explicit footer controls (advance right / regress left, adjacent-only, boundary- and inactive-disabled); clicking a stage in the navigator remains view-only. A compact status selector in the document card header is the single quick-change surface. Rejected listings stay visible in the tree with inactive treatment for reference and can be reactivated via status change. Stage and status filter selectors ride the existing navigation-pane filter pipeline. A schema v4 migration translates old persisted values deterministically. All 222 deterministic tests pass; the desktop UI pass is deferred for a desktop-capable agent or human per the updated AGENTS.md.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| AGENTS.md required an unconditional real desktop UI verification pass for every user-facing change, with QA-6 reported as blocked when an interactive desktop is unavailable | OpenCode has no interactive desktop runtime; marking the pass as blocked stopped task 6.2 and gave the agent no honest completion path | Updated AGENTS.md: desktop UI verification is required for desktop-capable agents (e.g., Codex) and deferred for agents without an interactive desktop (e.g., OpenCode), where the deterministic baseline is the completion gate; QA-6 reports as not-applicable rather than blocked | OpenSpec process rule | Reusable across all changes and agents | Promoted to `AGENTS.md` (Testing and QA Reviews sections) during this change |

## Learning Review

- Result: one reusable process rule identified and promoted; no other reusable lessons.
- Evidence reviewed: proposal, design, three delta specs, tasks, the two implementation commits, the full test suite (222 passing), strict OpenSpec validation, and the user's explicit instruction to relax the desktop-verification gate for OpenCode.
- Promotions completed: `AGENTS.md` Testing and QA Reviews sections updated to make desktop UI verification conditional on the agent runtime (required for Codex, deferred for OpenCode with the deterministic baseline as completion gate).
- Deferred promotions: none. The remaining design decisions (single-pointer stage model with M3 revisit trigger, adjacent-only moves, duplicate resets to Idea, migration mapping table) are change-specific and already captured in `design.md`; none rose to durable project-wide guidance.

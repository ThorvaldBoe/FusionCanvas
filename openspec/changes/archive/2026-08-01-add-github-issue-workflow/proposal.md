## Why

FusionCanvas has no structured public entry point for bug reports or feature requests, and internal candidate work has no lightweight backlog separate from OpenSpec. Adding GitHub Issues now lets contributors report and discuss work while preserving OpenSpec as the authoritative specification and verification workflow for accepted behavior.

## What Changes

- Establish GitHub Issues as the intake, triage, prioritization, ownership, and delivery-tracking system for external requests, separately tracked internal bugs, and high-level planned features.
- Define the promotion and linking rules between issues, OpenSpec changes, branches, and pull requests so they never become competing behavior or task sources of truth.
- Add structured public Bug Report and Feature Request Issue Forms, with sensitive-information guidance and no blank-issue path.
- Add contributor and agent guidance for triage, direct bug fixes, promotion to OpenSpec, and issue closure through merged pull requests.
- Define a small label taxonomy and repository configuration steps without adding GitHub Discussions, Projects, sprints, complex automation, or task-level issue mirroring.

## Capabilities

### New Capabilities

- `github-issue-workflow`: Defines GitHub Issue intake, triage, promotion, linking, and closure rules that complement the OpenSpec delivery lifecycle.

### Modified Capabilities

- `openspec-project-workflow`: Clarifies that GitHub Issues may track candidate work and delivery while OpenSpec remains mandatory and authoritative for significant behavior changes.

## Impact

- New version-controlled GitHub Issue Form files under `.github/ISSUE_TEMPLATE/`.
- New contributor documentation and a concise agent-workflow addition in `AGENTS.md`.
- GitHub repository administration: enable Issues if needed; create the documented labels; disable blank issues. These settings are configured outside Git-tracked files.
- No application code, runtime behavior, persistence model, external API, or desktop UI changes. UX preflight and Avalonia headless view tests are not applicable.

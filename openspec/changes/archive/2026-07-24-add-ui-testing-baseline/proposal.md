## Why

FusionCanvas can currently treat a user-facing module as complete after code-level tests even when its real Avalonia interaction path has not been exercised. Real desktop verification has proven practical and should become a targeted, risk-based module-completion requirement when an interactive environment is available, with a comprehensive all-features UI regression pass in every full QA review.

## What Changes

- Require every user-facing delivery module to plan a targeted real desktop UI pass covering its critical workflow and distinct high-risk acceptance paths, while allowing equivalent low-risk variants to remain in deterministic tests.
- Keep the fast, deterministic solution-level test suite separate from desktop UI verification so ordinary baseline runs remain reliable and headless-friendly.
- Require UI verification to use isolated/disposable data and to cover relevant keyboard, pointer, focus, persistence, restart, destructive-action, validation, and recovery behavior.
- Require verification evidence to identify the tested build, scenarios, environment, result, and any justified omissions.
- Add a complete UI regression specification to the full QA review, covering every implemented user-facing feature rather than only the feature most recently changed.
- Update canonical contributor, architecture, planning, and QA guidance so the same expectation appears wherever project testing policy is defined.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `testing-baseline`: Extend module-completion testing from code-level automation to targeted, risk-based real desktop UI verification for user-facing behavior.
- `qa-review-baseline`: Extend the full QA review with a comprehensive real desktop UI regression task covering all implemented user-facing features.

## Impact

- Affects OpenSpec change design/task expectations, the QA review playbook, contributor guidance, architecture/testing documentation, and the FC-0007 planning baseline.
- Adds no runtime API, application behavior, package, or external-service dependency.
- Desktop UI verification may be agent-driven or automated; it remains a separate test lane from `dotnet test` and must not mutate a contributor's normal workspace data.
- UX review is not applicable because this change governs development and QA process rather than product interaction design.

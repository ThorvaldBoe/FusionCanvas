# FC-0007 - Testing Baseline

## Summary

The Testing Baseline establishes the expectation that FusionCanvas behavior should be protected by focused automated tests and proportional real desktop UI verification from the start.

Phase 0 should define what kinds of foundational behavior need tests and make future contributors confident that core behavior can evolve safely.

## User Need

As a contributor, I need a reliable layered testing baseline so changes to the domain model, application behavior, persistence, workflow logic, and actual desktop interaction can be made without breaking behavior silently.

## Goals

- Establish automated tests as part of normal development.
- Require real desktop UI verification for every new or changed user-facing feature.
- Protect foundational data and workflow behavior.
- Make tests easy to run.
- Keep test coverage focused and useful.
- Keep the fast headless suite separate from desktop UI verification.

## Requirements

- The project has a clear test baseline.
- Foundational domain behavior should be testable.
- Application-level use cases should be testable where practical.
- Persistence boundaries should be testable enough to catch data loss or relationship errors.
- Tests should be easy for contributors to run.
- New foundational behavior should generally include focused tests.
- Every new or changed user-facing feature should include a proportional test through the built Avalonia application.
- Desktop UI tests should use isolated disposable data and cover every applicable interaction risk from the feature's accepted scenarios.
- Verification evidence should identify the build, environment, scenarios, results, isolation method, limitations, and material screenshots or automation logs.
- Tests should support confidence without requiring excessive ceremony.

## Priority Test Areas

- domain rules
- core entity relationships
- application services
- navigation behavior
- persistence boundaries
- asset reference behavior
- specification-driven acceptance behavior
- real keyboard and pointer interaction
- focus, selection, validation, filtering, and destructive confirmation
- persistence/restart, recovery, accessibility, and tab/window behavior where applicable

## User Workflows Supported

### Validate Foundational Behavior

A contributor runs tests before or after a change to confirm core behavior still works.

### Add a Feature

A contributor adds focused code-level tests and, for user-facing behavior, verifies the accepted workflow through the built desktop application using disposable data.

### Refactor Safely

A contributor changes internals while relying on tests to catch broken behavior.

## Acceptance Criteria

- The project has an initial automated testing approach.
- Foundational behavior can be covered by focused tests.
- Contributors know which areas should receive tests first.
- Tests can be run consistently.
- Phase 1 features have a clear expectation for adding relevant tests.
- Each user-facing feature has a real desktop verification plan and evidence before completion.
- Full QA reviews exercise all accepted and implemented user-facing features in one regression matrix.

## Out of Scope

- Requiring one particular UI automation framework
- Adding desktop UI automation to the default `dotnet test` command
- Exhaustive permutation testing beyond accepted workflows and applicable interaction risks
- Performance testing
- Marketplace integration testing
- Pixel-perfect visual regression infrastructure unless a feature specifically requires it
- External-service access during the local baseline

## Open Questions

- What minimum test set must exist before Phase 1 implementation starts?
- Which behaviors should be covered by unit tests versus integration tests?
- Should PRD acceptance criteria map directly to named tests?
- When should repeatable agent-driven desktop scenarios be promoted into a dedicated UI automation harness?

## Related Notes

- [[Phase 0 - Foundation]]
- [[Roadmap]]
- [[Architecture]]
- [[Principles]]

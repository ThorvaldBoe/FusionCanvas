## Context

FusionCanvas already has separate production projects for `Domain`, `Application`, `Integration`, and `App`, with matching test projects under `tests/`. The existing tests are enough to prove the projects can compile and basic Phase 0 behavior can be exercised, but FC-0007 needs to make the test baseline explicit, useful, and repeatable before Phase 1 features add more product workflow behavior.

The architecture guidelines already state that behavior-changing features need appropriate tests. This change makes that expectation concrete by defining where tests live, what foundational areas come first, and how contributors should run the suite.

## Goals / Non-Goals

**Goals:**

- Keep the current mirrored test project structure as the baseline.
- Establish focused automated tests for foundational domain, application, integration boundary, navigation, asset reference, and specification-driven acceptance behavior.
- Make `dotnet test` from the solution the normal contributor command for the baseline suite.
- Prefer fast, deterministic tests that do not require external services, network access, marketplace accounts, AI providers, or a running desktop UI.
- Require new foundational behavior to include relevant automated tests or an explicit reason when automated testing is not applicable.

**Non-Goals:**

- No complete end-to-end test suite.
- No full UI automation or visual regression testing.
- No performance testing.
- No marketplace, AI provider, or cloud integration testing.
- No separate manual QA process.

## Decisions

### Use mirrored test projects as the baseline

The baseline should keep test projects named after the production project they verify: `FusionCanvas.Domain.Tests`, `FusionCanvas.Application.Tests`, `FusionCanvas.Integration.Tests`, and `FusionCanvas.App.Tests`.

Alternative considered: create one broad `FusionCanvas.Tests` project. That would be simpler initially, but it would blur layer ownership and make it easier for tests to depend on the wrong production layer.

### Treat `dotnet test` as the canonical runner

The first testing baseline should be runnable with the standard .NET CLI from the solution root. This keeps the contributor workflow easy and avoids adding a custom test runner before the project needs one.

Alternative considered: add a wrapper script immediately. A script may become useful later, but the baseline should first work through the toolchain every .NET contributor already has.

### Prioritize behavior over framework wiring

Tests should focus on domain rules, entity relationships, application use cases, persistence/file boundary behavior, navigation decisions, asset reference behavior, and specification acceptance scenarios. Static Avalonia markup, constructor-only wiring, and framework-owned behavior do not need superficial tests.

Alternative considered: pursue high line coverage across all projects. That would create brittle tests before the product behavior is deep enough to justify it.

### Keep integration tests local and deterministic

Integration-layer baseline tests should cover local adapters and persistence boundaries using temporary local resources or isolated SQLite storage. They should not require external services, installed marketplace credentials, AI provider keys, or network access.

Alternative considered: defer all integration tests until later. FC-0007 specifically calls out persistence boundaries and asset reference behavior, so local deterministic boundary tests belong in the baseline.

### Map acceptance behavior to tests where practical

OpenSpec scenarios and PRD acceptance criteria should inform test names and coverage for foundational behavior, especially for domain rules and application use cases. The mapping does not need to be one test per line of spec text, but contributors should be able to see which tests protect accepted behavior.

Alternative considered: require a strict one-to-one test for every scenario. That would add ceremony and duplicate tests for scenarios that are better covered together.

## Risks / Trade-offs

- [Risk] The baseline becomes too ceremonial for early contributors -> Mitigation: keep the required command simple and focus tests on behavior with real regression value.
- [Risk] UI tests become brittle if they reach into Avalonia rendering too early -> Mitigation: limit app tests to UI-owned decisions, view models, shell state, and navigation behavior where practical.
- [Risk] Integration tests become slow or environment-dependent -> Mitigation: keep them local, isolated, and free of network or external account requirements.
- [Risk] Spec-to-test mapping is inconsistent -> Mitigation: encourage descriptive test names and task-level checks without requiring a rigid traceability matrix.

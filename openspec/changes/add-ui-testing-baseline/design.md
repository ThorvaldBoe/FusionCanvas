## Context

FusionCanvas currently has a fast xUnit baseline that deliberately avoids launching Avalonia. That remains valuable, but it cannot verify framework wiring, focus, keyboard and pointer interaction, native input routing, visual state, or persistence across an actual desktop restart. FC-0103 demonstrated that the built Windows application can be exercised safely through UI Automation while using a disposable workspace database.

This is a cross-cutting process change affecting every future user-facing delivery module and the recurring full QA review. It does not change the application runtime.

## Goals / Non-Goals

**Goals:**

- Make targeted real desktop UI verification a completion criterion for every user-facing delivery module when an interactive environment is available.
- Define a risk- and information-value-based scenario-selection model so desktop tests focus on critical workflows and distinct risks without repeating equivalent low-risk variants.
- Keep UI verification safe, reproducible, and evidence-backed.
- Make a full QA review inventory and exercise all implemented user-facing features.
- Preserve the speed and determinism of the existing solution-level xUnit lane.

**Non-Goals:**

- Mandating a particular UI automation framework or immediately building a permanent end-to-end test harness.
- Adding UI automation to the default `dotnet test` command.
- Requiring pixel-perfect visual regression for every feature.
- Testing external services, marketplace accounts, or AI providers as part of desktop UI verification.
- Changing any end-user interaction or visual design; the UX preflight is therefore not applicable.

## Decisions

### Keep code-level and real-desktop verification as separate lanes

`dotnet test .\FusionCanvas.sln` remains the fast, deterministic, non-UI baseline. A separate desktop lane launches the built application and exercises it through the operating system's input/accessibility surface. This avoids making routine unit and integration runs slow or dependent on an interactive session while still preventing UI-only failures from escaping.

Alternative considered: fold desktop automation into the solution test suite. Rejected because it would make the baseline dependent on an interactive Windows desktop and increase flakiness and runtime.

### Require risk-based coverage derived from acceptance behavior

Each user-facing module identifies its critical end-to-end path and applicable distinct interaction risks: keyboard, pointer, focus/selection, validation, filtering, destructive confirmation, persistence/restart, recovery, accessibility exposure, state synchronization, and multi-window/tab behavior. Desktop scenarios are selected by user impact, integration risk, novelty, prior failures, and information value. Equivalent low-risk variants may be sampled when focused deterministic tests cover the remaining rule combinations; the plan records why the selected set is sufficient.

Alternative considered: use one universal fixed checklist as a pass gate. Rejected because many dimensions do not apply to every feature and would encourage ceremonial rather than risk-based testing.

### Require disposable state and observable evidence

Desktop runs use a temporary database/workspace or another isolated fixture and must not mutate the contributor's normal workspace. Verification records the build/configuration, environment, scenarios and results, data isolation, and any limitations. Screenshots or automation logs are included when they materially establish visual or interaction outcomes.

Alternative considered: permit testing against the normal local workspace for convenience. Rejected because destructive workflows, persistence tests, and failure injection make accidental data loss unacceptable.

### Define full QA UI coverage from the accepted feature inventory

The full QA review constructs a UI regression matrix from all accepted, implemented user-facing capability specs. It then exercises every feature's primary workflow plus applicable cross-cutting interaction risks. The report marks every inventory row pass, fail, blocked, or not applicable, so “full” cannot silently mean only the latest feature or a smoke test.

Alternative considered: run only the per-feature UI evidence accumulated during implementation. Rejected because integrated behavior and regressions emerge after features are combined, and old evidence does not validate the current build.

### Allow tooling to evolve independently of the requirement

Windows UI Automation is the current practical mechanism, but the requirement is expressed in terms of launching and interacting with the real desktop application. A future dedicated harness may replace agent-driven automation without another policy change.

## Risks / Trade-offs

- [Desktop UI tests can be slower or flaky] → Keep them outside the default xUnit lane, use deterministic fixtures, wait on observable UI state rather than fixed delays, and record blocked infrastructure separately from product failures.
- [Coverage may become inconsistent across changes] → Require a UI verification section and explicit task in every user-facing delivery module, with scenario selection and omissions justified against its acceptance scenarios and risk profile.
- [“All features” can become ambiguous] → Build the full-QA matrix from accepted implemented capability specs and include every row in the report.
- [UI Automation may not expose custom controls well] → Prefer stable automation identifiers and accessible names, supplement with keyboard/pointer input and screenshots, and log accessibility gaps as findings.
- [Test data could damage a real workspace] → Require a disposable workspace/database and verify the normal workspace is not selected before destructive actions.

## Migration Plan

1. Add delta requirements to the testing and QA review baselines.
2. Update contributor, architecture, planning, and QA playbook documentation.
3. Update active user-facing changes so their remaining verification artifacts follow the new policy.
4. On archive, sync the delta requirements into the accepted baseline specs.
5. Apply the requirement to all new user-facing delivery modules; existing features are comprehensively covered by the next full QA UI regression review.

Rollback is documentation-only: revert the policy change and related guidance. No runtime or data migration is involved.

## Open Questions

None. Selection of a permanent UI automation framework remains an implementation choice and is not required to adopt the testing policy.

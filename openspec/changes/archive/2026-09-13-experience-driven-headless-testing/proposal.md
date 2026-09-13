## Why

FusionCanvas has a large deterministic test suite, but recent defects have escaped because individual bindings, commands, and persistence operations were tested without proving the complete experience a user performs. The testing baseline should preserve fast focused tests while adding a small, explicit layer of rendered headless user journeys and a feedback loop that turns escaped defects into reusable prevention.

## What Changes

- Define user-job coverage as the organizing model for user-facing verification: meaningful user outcomes are inventoried across surfaces, rather than requiring ceremonial tests for every window or static control.
- Require a small number of deterministic headless experience journeys for critical workflows whose outcome crosses Avalonia bindings, routed input, view-model orchestration, persistence, or close-and-reopen state.
- Distinguish focused headless component tests from experience journeys, including a user-boundary rule that prevents journey tests from bypassing the UI seam they claim to verify.
- Require bug fixes to reproduce the escaped expectation with an automated regression test first, or record a specific reason deterministic automation is not suitable.
- Add a lightweight escape-analysis taxonomy and promotion rule so recurring gaps improve project-wide guidance without turning every isolated bug into a global rule.
- Add reusable deterministic helpers for dispatcher settling, rendered interaction, and scenario-scoped persistent fixtures.
- Pilot the strategy on three recent defect families: Printify shop selection and re-entry, Blueprint editing and save/re-entry, and navigation-tree group nesting through routed pointer geometry.
- Enforce the deterministic solution baseline for pull requests while keeping real-desktop Appium automation separately selectable.
- Reconcile the documented baseline command with the Avalonia telemetry constraint so contributors and CI use one dependable invocation.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `testing-baseline`: Add user-job inventories, critical headless experience journeys, journey interaction boundaries, persisted re-entry checks, and regression-first escape learning.
- `qa-review-baseline`: Make completion and QA reviews verify experience coverage, bug escape analysis, deterministic baseline enforcement, and proportionate promotion of reusable lessons.

## Impact

- Accepted requirements: `openspec/specs/testing-baseline/spec.md` and `openspec/specs/qa-review-baseline/spec.md` through delta specs in this change.
- Process guidance: `docs/qa-review.md`, `docs/coding-standard.md`, and testing guidance in `docs/architecture.md`.
- Automation: a repository pull-request workflow under `.github/workflows/` that runs the canonical deterministic baseline; the separately selectable `FusionCanvas.UITests` lane remains unchanged.
- Test infrastructure: focused support types under `tests/FusionCanvas.App.Tests`, including deterministic dispatcher/lifecycle coordination, thin user-language surface drivers, and isolated persisted-workspace fixtures.
- Pilot coverage: `StorePrintifyTests`, `StoreEditorHeadlessTests`, `MainWindowLayoutTests`, and lower-layer tests only where the pilot exposes a missing decision or persistence contract.
- No production behavior, database schema, public API, package dependency, or user-facing layout is intended to change.

## Module Boundary

This is one cohesive quality-infrastructure module because the requirements, helpers, CI gate, and pilot journeys all serve one independently verifiable outcome: critical user experiences fail deterministically before defective UI behavior can be accepted. The pilots validate the strategy against known escape patterns without attempting a repository-wide test rewrite.

Dependencies are the existing Avalonia headless harness, xUnit baseline, disposable workspace patterns, accepted capability scenarios, and OpenSpec verification records. UX review is not applicable because this module does not change application interaction or presentation.

Non-goals are broad screenshot or pixel regression, numeric coverage gates, making live-desktop automation mandatory, rewriting all existing headless tests, or requiring tests for static markup merely because a view exists.

Principal risks are slow or brittle journeys, helpers that conceal the UI seam, duplicate lower-layer coverage, CI failures caused by environment setup, and over-promotion of one-off bugs. The design will limit journeys to critical cross-seam outcomes, keep drivers thin and user-named, require observable assertions, isolate mutable state per scenario, and preserve focused tests for behavioral variants.

Verification will map every new scenario to spec inspection, focused helper tests, the three rendered pilot journeys, disposable persistence/re-entry evidence where required, pull-request workflow inspection, the canonical solution baseline, and strict OpenSpec validation. No unresolved product, UX, data, or architecture decision remains for implementation.

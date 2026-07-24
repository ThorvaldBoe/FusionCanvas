## Why

Real-desktop UI verification is slow, depends on an interactive environment, and cannot be performed consistently by both Codex and OpenCode. FusionCanvas needs a routine UI verification lane that runs in the normal automated test suite on either agent, while retaining live desktop testing as an optional diagnostic and exploratory tool.

## What Changes

- Make Avalonia headless view testing the routine framework-level UI verification approach for user-facing changes where view construction, bindings, control state, routed input, focus, or visual-tree behavior carries meaningful risk.
- Keep view-model and other UI-owned decision-logic tests as focused framework-free tests; headless view tests complement rather than replace them.
- Make live real-desktop UI testing optional and ad hoc. It may be selected for investigation, release confidence, accessibility/platform integration, native window behavior, or other risks that headless testing cannot represent, but it is not a module-completion or full-QA gate.
- Update contributor, architecture, README, OpenSpec project, testing-baseline, and QA-review guidance to use the same headless-first policy.
- Record the current state accurately: FusionCanvas has app-layer view-model tests but no Avalonia headless package, harness, or view tests yet.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `testing-baseline`: Replace required or handed-off real-desktop verification with routine, deterministic Avalonia headless view testing where applicable, and make live desktop testing optional.
- `qa-review-baseline`: Make automated headless view coverage part of testing QA and remove real-desktop regression from the mandatory full-review task set.

## Impact

- Affects development and QA process documentation in `AGENTS.md`, `README.md`, `openspec/project.md`, `docs/architecture.md`, `docs/qa-review.md`, and the repository OpenSpec workflow skills under `.codex/skills/`.
- Updates accepted testing and QA requirements through delta specs.
- Adds no runtime behavior, data migration, or end-user interaction change.
- This change establishes policy and documents the coverage gap; creating the Avalonia headless test harness and initial representative tests is a separate implementation change because it requires package/API selection and test-fixture design.
- UX review is not applicable because this is a contributor verification-process change with no product surface.

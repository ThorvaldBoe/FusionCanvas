## 1. Align Requirements and Contributor Guidance

- [x] 1.1 Sync the approved `testing-baseline` and `qa-review-baseline` delta requirements into the accepted specifications without changing unrelated requirements.
- [x] 1.2 Add `docs/testing-strategy.md` with the four-layer confidence model, critical-journey threshold, user-boundary rule, deterministic fixture rules, escape taxonomy, and living user-job inventory.
- [x] 1.3 Seed the inventory from current implemented surfaces, naming meaningful user jobs, representative states, existing evidence, gaps, and not-applicable rationales without inventing behavior.
- [x] 1.4 Update `docs/architecture.md` and `docs/coding-standard.md` for the experience layer and regression-first defect rule, including the deterministic-automation exception.
- [x] 1.5 Update completion review, QA-3, QA-6, reporting, and retrospective guidance in `docs/qa-review.md` to inspect user-job evidence, journey truthfulness, escape learning, determinism, and strategy-health signals.
- [x] 1.6 Add or update the pull-request template with concise defect regression, escape cause, similar-risk check, and promotion-decision fields that are optional for non-defect work.

## 2. Enforce One Deterministic Baseline

- [x] 2.1 Reproduce and document the plain `dotnet test .\FusionCanvas.sln` Avalonia telemetry failure before changing repository configuration.
- [x] 2.2 Add the narrowest repository-controlled non-interactive Avalonia telemetry configuration and canonical serialized invocation that are reproducible without suppressing compiler, analyzer, or test diagnostics.
- [x] 2.3 Add a Windows pull-request and `main` push workflow that restores dependencies and runs `dotnet test .\FusionCanvas.sln -m:1`, while excluding the separately selectable `FusionCanvas.UITests` project.
- [x] 2.4 Reconcile README, architecture, coding, QA, and UI-test documentation so all mandatory testing guidance names the same canonical command and prerequisites.

## 3. Add Deterministic Headless Test Support

- [x] 3.1 Implement `HeadlessUiWait` with bounded dispatcher pumping, cancellation, observable-condition naming, and diagnostic timeout output.
- [x] 3.2 Add focused `HeadlessUiWait` tests for immediate completion, asynchronously published UI state, cancellation, and timeout diagnostics without wall-clock sleeps as readiness evidence.
- [x] 3.3 Implement `DisposableHeadlessWorkspace` with unique database, workspace, and settings paths, ownership validation, fresh SQLite repository creation, deterministic disposal, and retained-path cleanup diagnostics.
- [x] 3.4 Add focused isolation tests proving two scenario workspaces do not share state and never resolve the contributor's configured workspace paths.
- [x] 3.5 Add thin Store Editor and Main Window drivers with semantic control discovery, routed user actions, observable state access, and no assertions, business logic, direct bound-property mutation, or direct command shortcuts.
- [x] 3.6 Add stable automation or accessibility identifiers to pilot controls only where no existing semantic locator is suitable, with no visual or behavioral production change.

## 4. Prove the Printify Experience

- [x] 4.1 Add a rendered Store Editor journey that verifies synthetic credentials, selects a returned shop through the control, observes the correct dirty/save state, and activates the rendered Save action.
- [x] 4.2 Complete the Printify journey with window disposal, fresh repository/service/view-model/window composition from scenario-scoped SQLite, re-verification to populate shop options, selected-shop display, and clean re-entry state.
- [x] 4.3 Retain focused missing-key, stale-shop, failure, cancellation, and race tests; remove or rename only property-driven coverage made redundant or misleading by the journey.
- [x] 4.4 Record pass-after evidence showing the rendered journey detects the historical binding/persistence escape mechanism; the focused regression remains alongside it.

## 5. Prove the Blueprint Save Experience

- [x] 5.1 Add a rendered Store Editor journey that navigates through controls to an existing Blueprint, edits its name through text input, observes Save enablement, and activates the rendered Save action.
- [x] 5.2 Complete the Blueprint journey with window disposal and fresh composition from the same disposable persistent workspace, then assert the updated visible name and disabled Save state.
- [x] 5.3 Preserve distinct focused state/layout coverage, replace touched arbitrary delays with `HeadlessUiWait`, and remove only assertions made redundant or misleading by the journey.
- [x] 5.4 Record pass-after evidence showing the journey detects the historical save-wiring/synchronization escape mechanism.

## 6. Prove the Navigation Drag Experience

- [x] 6.1 Retain the existing focused deterministic placement-boundary and move-validation tests for before, nested, and after regions around the accepted drag thresholds.
- [x] 6.2 Add a rendered Main Window journey that routes drag-over data through a rendered row and asserts user-visible feedback without private-handler reflection; full drop/persist coverage is explicitly deferred.
- [x] 6.3 Reconstruct navigation from isolated persisted state and assert the moved group's durable parent relationship and rendered nesting.
- [x] 6.4 Confirm Avalonia headless can route the grouped coordinate-driven drop in the deterministic fixture; no optional desktop limitation is required.
- [x] 6.5 Record pass-after evidence showing the routed drag-feedback regression exercises the historical hit-area escape mechanism; full persisted drop evidence remains the next-module task.

## 7. Reconcile Coverage and Escape Learning

- [x] 7.1 Update the user-job inventory with final pilot evidence and explicit deferred gaps for other surfaces; do not expand this module into repository-wide remediation.
- [x] 7.2 Classify each pilot escape, inspect adjacent Printify, Blueprint, and navigation workflows for the same mechanisms, and add only the representative prevention required by the promotion rule.
- [x] 7.3 Confirm pilot journeys visibly separate arrangement, user actions, and outcomes and contain no action-phase direct command calls, bound-property mutation, private-handler reflection, arbitrary readiness delays, or shared persistent resources.
- [x] 7.4 Correct proposal, design, delta specs, tasks, or documentation if implementation evidence invalidates an approved assumption; do not silently diverge from the delivery package.

## 8. Verify and Learn

- [x] 8.1 Run focused helper, App headless, and SQLite persistence tests and record exact results in `verification.md`.
- [x] 8.2 Run `dotnet build .\FusionCanvas.sln` and the canonical `dotnet test .\FusionCanvas.sln -m:1`; record duration, failures, flakes, and retained temporary resources.
- [x] 8.3 Run `openspec validate experience-driven-headless-testing --strict` and `openspec validate --strict`; correct every validation error.
- [x] 8.4 Complete criterion-level results and material evidence for every delta-spec scenario in `verification.md`; an aggregate test pass does not substitute for individual rows.
- [x] 8.5 Perform scoped completion QA covering testing, headless journey quality, architecture, persistence isolation, workflow configuration, and specification/documentation drift; correct failed gates and rerun affected regression checks.
- [x] 8.6 Write `retrospective.md` evaluating defect-prevention value, journey runtime and maintenance cost, flakiness, escape-taxonomy usefulness, failure localization, and whether any broader rollout belongs in a later module.

## Context

FusionCanvas already follows a lowest-reliable-layer strategy and includes an Avalonia headless harness in `FusionCanvas.App.Tests`. The current repository contains 205 Avalonia facts, but 127 are concentrated in four files, and several recent manually discovered defects occurred despite focused binding, view-model, and persistence tests. The Printify shop-selection sequence required multiple follow-up corrections; Blueprint save and navigation drag geometry show the same pattern at different seams.

The existing QA-6 playbook inventories views and framework behavior, but it does not require a complete rendered path for a critical user outcome. Tests may therefore truthfully prove individual controls or commands while still leaving the composition between them unverified. The repository also documents the solution baseline but does not currently contain a general pull-request workflow that enforces it.

This module changes quality infrastructure and contribution rules, not application UX. The UX preflight is therefore not applicable. The accepted UX and capability scenarios of each pilot remain authoritative for what users expect.

## Goals / Non-Goals

**Goals:**

- Organize user-facing verification around meaningful user jobs and observable outcomes.
- Add a small rendered journey layer for critical workflows without duplicating lower-layer variants.
- Ensure journey actions traverse the UI seams they claim to verify.
- Prove durable mutations through fresh presentation re-entry where that is part of the expectation.
- Replace arbitrary timing sleeps with deterministic dispatcher-aware settling.
- Turn escaped defects into local regressions and promote only reusable lessons.
- Make the canonical solution baseline an enforced pull-request check.
- Validate the approach against three known escape families before broader adoption.

**Non-Goals:**

- Rewriting or renaming the entire existing test suite.
- Requiring one headless test per XAML file, window, control, or static layout element.
- Replacing focused Domain, Application, Integration, view-model, or component tests.
- Adding screenshot, pixel-perfect, performance, external-provider, or mandatory live-desktop testing.
- Expanding the existing Appium smoke suite.
- Introducing a numeric line-coverage or test-count gate.
- Changing production workflows, persistence schema, or user-facing presentation.

## Decisions

### 1. Use user jobs as the coverage unit

The QA inventory will identify each meaningful job available from an implemented surface, its representative states, its observable outcome, and its evidence. A focused dialog may be represented as part of its parent journey rather than receiving an artificial standalone quota.

Alternative considered: require at least one headless test for every window. Rejected because it rewards construction tests and static assertions without proving that users can complete important work.

### 2. Add an explicit headless experience layer

The testing model has four complementary levels:

1. Domain/Application/Integration tests cover rules, orchestration variants, failure cases, and persistence contracts.
2. View-model tests cover UI-owned state transitions, cancellation, races, and command eligibility.
3. Headless component tests cover individual bindings, controls, focus, selection, and routed behavior.
4. Headless experience journeys cover a small number of critical outcomes across the rendered UI and adjacent lifecycle or persistence seams.

A journey is required when a critical outcome crosses two or more material seams and focused tests could pass while their composition fails. Variants remain at lower levels to preserve speed and diagnostic clarity.

Alternative considered: move critical coverage to Appium desktop automation. Rejected because the normal baseline must remain deterministic, fast, non-interactive, and available to every contributor and agent.

### 3. Separate arrangement from user action

Journey tests will use visually separated arrange, user-action, and outcome phases. Arrangement may seed repositories and configure deterministic collaborators. Once the user-action phase begins, actions must occur through rendered controls or routed input. Direct command execution, bound property mutation, and private-handler reflection are prohibited substitutes for the action under test.

Thin drivers may locate controls by stable name, automation ID, accessible name, or narrowly scoped semantic predicate. Driver APIs will use user language such as `SelectPrintifyShop`, `SaveBlueprint`, or `DropGroupIntoGroup`. Drivers may pump the dispatcher but will not contain assertions, business branching, hidden view-model mutation, or general-purpose visual-tree query APIs.

Alternative considered: continue direct `Command.Execute` calls because they are concise. Retained for focused command/component tests, but rejected for experience journeys because it bypasses binding, input, enabled state, and handler wiring.

### 4. Standardize deterministic settling and cleanup

A shared helper will pump `Dispatcher.UIThread` until a caller-supplied observable condition succeeds or a bounded timeout expires. Timeout messages will name the awaited user-visible condition and include useful current state. Existing arbitrary `Task.Delay` calls in touched pilot paths will be removed; a repository-wide cleanup is deferred.

Windows and owned dialogs will be closed in guaranteed cleanup. Persistent journeys will receive a unique disposable root containing database, workspace, and settings paths. A fresh repository/service/view-model/window composition will be constructed for re-entry assertions. The contributor's configured workspace and the process-wide default headless paths will not be used for these persistence journeys.

Alternative considered: reuse the current process-scoped headless root. Rejected for persistence journeys because it permits cross-test state leakage and cannot prove isolated reconstruction.

### 5. Use real SQLite only for selected durability journeys

Most headless tests will continue to use in-memory repositories and deterministic fakes. A critical journey whose promise includes durable reconstruction will use the real `SqliteWorkspaceRepository` with a scenario-scoped path, referenced explicitly by the App test project if needed. Provider and native credential collaborators remain fakes; no external network or OS credential state enters the baseline.

Alternative considered: use SQLite for every journey. Rejected because it increases runtime and diagnosis cost without adding confidence to non-persistence variants.

### 6. Classify and promote defect escapes proportionately

Bug corrections will record one primary escape class: missing scenario, bypassed seam/wrong layer, weak oracle, unrealistic fixture, missing re-entry coverage, async/lifecycle race, input/focus/geometry gap, platform-only behavior, or ambiguous specification. The record will also name similar workflows inspected.

A reusable lesson is promoted only when the mechanism has recurred or plausibly affects multiple surfaces. Promotion may update a helper, inventory, QA checklist, accepted requirement, or coding guidance and must include one representative prevention beyond the original regression. Otherwise the local regression is sufficient.

Alternative considered: add a global rule for every bug. Rejected because it creates process and test bloat without proportional defect prevention.

### 7. Keep one canonical deterministic command and enforce it on Windows CI

The canonical contributor command is `dotnet test .\FusionCanvas.sln -m:1`. Baseline investigation showed that the solution's Avalonia test projects can fail during parallel VSTest orchestration in non-interactive runners without compiler or test diagnostics, while the serialized invocation is deterministic. Repository-controlled MSBuild configuration suppresses only the non-interactive Avalonia telemetry side effect; compiler, analyzer, and test diagnostics remain enabled. A Windows pull-request workflow will restore dependencies and run the canonical command. `FusionCanvas.UITests` remains outside `FusionCanvas.sln` and outside this required check.

Alternative considered: document `-p:UsedAvaloniaProducts=` as a second command. Rejected because contributors and CI should not need to remember an environmental variant. The implementation will prefer a narrowly scoped repository configuration over suppressing unrelated diagnostics.

### 8. Prove the strategy with three pilot journeys

The pilots intentionally cover three different escape mechanisms:

- **Printify shop selection:** render Store Editor, verify with deterministic collaborators, select a shop through the control, save through the rendered action, close, reconstruct from isolated persistence, and observe the selected shop with no false dirty state.
- **Blueprint save:** open an existing Blueprint through Store Editor controls, edit its name through text input, save through the rendered action, close/reconstruct, and observe the new name with Save disabled.
- **Group nesting:** initiate and route drag/drop data over the central nesting region of a rendered group row, assert visual nesting/drop feedback, complete the drop, and assert the persisted parent after reconstruction.

Each pilot retains focused lower-layer tests for error variants. Production XAML may receive stable automation metadata when a driver otherwise lacks a semantic locator, but no visual or behavioral change is permitted.

## Risks / Trade-offs

- [Risk] Full journeys become slow and duplicate lower-layer cases. → Mitigation: require journeys only for critical cross-seam outcomes and keep variants focused.
- [Risk] Drivers hide direct shortcuts or become a second UI framework. → Mitigation: keep APIs surface-specific, user-named, assertion-free, and small enough to inspect with the test.
- [Risk] Routed drag/drop cannot be represented faithfully by Avalonia headless. → Mitigation: first route `DragEventArgs` through the rendered row; if the platform demonstrably cannot supply the needed coordinate semantics, preserve the pure placement test and record a narrowly scoped desktop/manual limitation rather than using reflection while claiming a journey.
- [Risk] SQLite journeys introduce file locks or state leakage. → Mitigation: unique per-scenario roots, disabled pooling where needed, fresh compositions, deterministic disposal, and retained-path diagnostics on cleanup failure.
- [Risk] CI telemetry configuration suppresses useful diagnostics. → Mitigation: suppress only Avalonia product telemetry for non-interactive builds and retain compiler/analyzer output.
- [Risk] Escape analysis becomes paperwork. → Mitigation: keep one short structured record, allow local-only conclusions, and promote only recurring or cross-surface mechanisms.
- [Trade-off] A small number of journeys take longer than direct command tests. → Accepted because they cover composition failures that currently require repeated manual discovery.

## Implementation Plan

### 1. Align accepted requirements and operational guidance

- Apply the `testing-baseline` and `qa-review-baseline` deltas from this change.
- Update `docs/architecture.md` to describe the four-layer confidence model and critical-journey threshold.
- Update `docs/coding-standard.md` so regression-first evidence is the default requirement for bug fixes, with the specified deterministic-automation exception.
- Update `docs/qa-review.md` QA-3, QA-6, completion review, and retrospective guidance with the user-job inventory, journey boundary checks, escape taxonomy, promotion rule, and strategy-health signals.
- Add `docs/testing-strategy.md` as the concise contributor entry point and living user-job inventory. Seed it from current implemented surfaces, recording evidence or gaps rather than inventing new behavior.
- Add or update the GitHub pull-request template with the defect escape fields without requiring those fields for non-defect changes.

### 2. Make the canonical command reproducible and enforced

- Reproduce the Avalonia telemetry/VSTest orchestration failure with the plain solution command, add the narrowest repository-controlled non-interactive telemetry configuration, and standardize the serialized `dotnet test .\FusionCanvas.sln -m:1` invocation without hiding compiler or test failures.
- Add `.github/workflows/ci.yml` for pull requests and pushes to `main` on a Windows runner. Restore dependencies and run the canonical solution test command; do not invoke `FusionCanvas.UITests`.
- Update all current testing documentation that shows a different workaround so the repository has one canonical command.

### 3. Add focused App headless support

- Add `tests/FusionCanvas.App.Tests/TestSupport/HeadlessUiWait.cs` with bounded dispatcher pumping, cancellation support, and diagnostic timeout messages. Add focused tests proving immediate success, asynchronous publication, and timeout diagnostics.
- Add `tests/FusionCanvas.App.Tests/TestSupport/DisposableHeadlessWorkspace.cs` with unique database, workspace, and settings paths; explicit ownership checks; fresh `SqliteWorkspaceRepository` construction with safe pooling behavior; and reliable cleanup/retained-path diagnostics. Reference `FusionCanvas.Integration` directly from `FusionCanvas.App.Tests` only if compilation requires it.
- Add small drivers under `tests/FusionCanvas.App.Tests/TestSupport/Drivers/` for Store Editor and Main Window. Drivers locate semantic controls, route supported input, expose observations, and own no assertions or business logic.
- Add missing stable automation/accessibility identifiers in affected XAML only where the pilot drivers cannot use an existing semantic identifier.

### 4. Implement the Printify pilot

- Replace or supplement the current property-driven selection regression in `StorePrintifyTests.cs` with one experience journey covering deterministic verification, control-level shop selection, rendered Save, window close, fresh composition, re-verification to populate provider options, selected-shop display, and clean dirty state.
- Use scenario-scoped SQLite for Store context persistence while keeping credential storage and provider verification synthetic and offline.
- Retain focused view-model tests for missing keys, stale shops, failure, cancellation, and races; do not repeat those variants in the journey.
- Record fail-before/fix-after evidence against the binding/persistence defect mechanism during implementation.

### 5. Implement the Blueprint pilot

- Extend `StoreEditorHeadlessTests.cs` with a user-boundary journey that navigates through rendered controls, types an updated Blueprint name, observes Save enablement, activates rendered Save, closes, reconstructs from the same disposable persistent workspace, reopens the Blueprint, and observes the saved name with Save disabled.
- Keep existing focused state and layout tests where they add distinct evidence; remove or rename only assertions made misleading or redundant by the new journey.
- Record fail-before/fix-after evidence against the prior save wiring/synchronization defect mechanism.

### 6. Implement the navigation drag pilot

- Extend `MainWindowLayoutTests.cs` or a focused `WorkspaceTreeHeadlessJourneyTests.cs` with a rendered row drag/drop journey using the central nesting region, routed drag data, visible feedback/hierarchy, and persisted parent reconstruction.
- Do not invoke `OnTreeNodeDragOver`, `OnTreeNodeDrop`, or placement helpers through reflection in the journey.
- Preserve focused placement-boundary tests for before/inside/after thresholds and view-model move validation.
- If Avalonia headless cannot faithfully route the required coordinate behavior, document exact evidence in verification and retain a focused deterministic boundary test plus separately selectable desktop/manual evidence; do not mislabel it a complete journey.

### 7. Reconcile inventory and existing pilot-area test quality

- Populate the current-surface user-job inventory with the three completed pilots, existing focused/headless evidence, and explicit gaps for later incremental work.
- Remove arbitrary sleeps from touched pilot paths and replace them with the deterministic helper.
- Inspect adjacent Printify, Blueprint, and navigation workflows for the same escape classes; add only representative prevention required by the promotion rule.
- Do not expand this module into an all-surface remediation effort.

### 8. Verify, review, and learn

- Capture fail-before/pass-after evidence for all three pilot regressions during their implementation.
- Run focused App and persistence tests, then `dotnet test .\FusionCanvas.sln -m:1` using the canonical serialized invocation.
- Validate `openspec validate experience-driven-headless-testing --strict` and `openspec validate --strict`.
- Perform scoped completion QA covering testing, UI/headless journey quality, architecture, persistence isolation, workflow configuration, and specification/documentation drift.
- Complete `verification.md` criterion by criterion and record baseline duration and any flakes or retained temporary resources.
- Write a retrospective that evaluates defect-prevention value, journey maintenance cost, escape taxonomy usefulness, and whether any further rollout deserves a later module.

## Acceptance-to-Verification Mapping

Every scenario in both delta specs will be represented as a row in `verification.md`. Planned evidence categories are:

- User-job planning and inventory scenarios: changed artifacts plus QA inspection against implemented surfaces.
- Critical journey, user-boundary, persistence, and driver scenarios: the three pilot tests, focused helper tests, isolated SQLite evidence, and code inspection.
- Defect escape scenarios: pilot fail-before/pass-after records, escape classifications, adjacent-risk review, and the QA checklist/template.
- Pull-request baseline scenarios: workflow inspection and a successful canonical local solution run; real-desktop exclusion verified from solution/workflow configuration.
- QA scenarios: updated completion/QA-6 playbook, a scoped QA execution, and retrospective strategy-health evidence.

Optional live-desktop evidence is not planned. It becomes applicable only if the headless drag pilot demonstrates a documented framework limitation; even then it remains supplemental and does not replace separable deterministic coverage.

## Migration Plan

This is an additive testing migration. Existing tests remain valid and are reclassified or refactored only when touched by a pilot or found misleading. After the module is archived, new and changed user-facing work uses the inventory and journey threshold; other existing surfaces are evaluated incrementally when touched or during QA rather than through a one-time rewrite.

Rollback consists of reverting the new helpers, pilot journeys, workflow, and documentation together. No production data or schema rollback is required. Regression tests that continue to express accepted behavior should be retained even if process wording is later refined.

## Open Questions

None. Implementation must not broaden the module into repository-wide journey conversion, mandatory Appium execution, or pixel testing without a separately reviewed change.

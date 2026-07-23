# fix-main-window-usability Retrospective

## Outcome

Four main-window usability fixes shipped and merged (#52 + harness follow-up #53). Item text fields (working title, current-stage Idea/Concept/Phrase/Graphics, Notes) restored to blur-autosave: commit on field exit through a serialized drain that preserves mid-flight edits, silent commit before context transitions (aborting + reverting the status selector on failure), and invalid-title revert to baseline while other valid edits persist. The Save/Discard buttons, the unsaved-changes prompt, and the prompt state machine were removed. Navigation filter controls reflow with a `WrapPanel`; the details column uses one outer `ScrollViewer`; the header grid has an 8px gap. The owed Avalonia headless test harness (`Avalonia.Headless.XUnit` 12.0.4) was added with a reusable `MainWindowFixture` and 13 representative view tests covering construction, compiled bindings, layout/control state, and keyboard/pointer input.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| Explicit Save + Save/Discard/Cancel prompt is the right Item text-save model (per `basic-product-creation-workflow` design §9) | Real-use friction: the prompt interrupts high-frequency creative text entry during idea capture, the very friction the explicit model was meant to prevent | Restore blur-autosave + silent commit-on-leave; remove Save/Discard and the prompt; revert invalid title while persisting other edits | UX behavior reversal | Change-specific (captured in `listing-inspector` delta) + durable UX guidance | Promoted to `docs/ui-guidelines.md` (Details Pane Editing; exception section removed) |
| The `adopt-headless-view-testing` baseline includes a runnable headless harness | That change was process/specs-only; no `Avalonia.Headless` package, fixture, or view tests existed, so the four view-test tasks could not run | Add the harness in this change (package + `HeadlessTestApp` + `MainWindowFixture` + representative tests) | Implementation defect / scope gap | Change-specific | None — the harness now exists; the process lesson that a "complete" process change deferring implementation should track the follow-up explicitly is marginal and deferred |
| `MainWindowViewModel.CreateForDefaultWorkspace()` is a safe test factory for headless view tests | It opens the contributor's real on-disk SQLite workspace (`%LocalAppData%\FusionCanvas\workspace.db`); an early test committed a `"headless edit"` Notes value to a real item before the mistake was caught | Use `new MainWindowViewModel()` (in-memory sample) for all headless view tests | Implementation defect | Reusable — any headless view test can hit this trap | Proposed promotion to `testing-baseline` (see Learning Review) |

## Learning Review

- Result: reusable lessons identified.
- Evidence reviewed: proposal, design, `listing-inspector` delta spec, tasks, `verification.md`, and the real-workspace mutation incident during implementation.
- Promotions completed:
  - The Item autosave model reversal is captured durably in the `listing-inspector` delta (RENAMED + MODIFIED requirements) and in `docs/ui-guidelines.md` (Details Pane Editing section; the explicit-Save exception section removed).
- Promotions proposed (pending confirmation):
  - Add a `testing-baseline` rule that headless view tests must use isolated (in-memory or disposable) workspace data, never a factory that opens the contributor's real on-disk workspace. This generalizes the `CreateForDefaultWorkspace` defect so future headless view tests cannot mutate contributor data.
- Deferred promotions:
  - The "complete process change that defers implementation should track the follow-up explicitly" lesson is deferred: it is marginal, and the harness gap is now closed. Rationale: no durable rule beyond what the OpenSpec workflow already implies; recording it here suffices.

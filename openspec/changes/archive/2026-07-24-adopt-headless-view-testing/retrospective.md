# Adopt Headless View Testing Retrospective

## Outcome

Avalonia headless view testing is the routine framework-level UI verification approach for user-facing changes where view construction, bindings, control state, routed input, focus, or visual-tree behavior carries meaningful risk. Live desktop testing is optional and ad hoc, not a module-completion or QA gate. The headless harness (`HeadlessTestApp`) and representative `MainWindow` view tests were subsequently implemented and now pass in the deterministic baseline.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| Real-desktop UI verification could be a mandatory gate. | Interactive desktop testing cannot run consistently across Codex, OpenCode, and CI, making it an unreliable gate. | Headless view tests are the routine lane; live desktop testing is optional and supplemental. | Process / architecture | Reusable across all user-facing modules | Promoted to `testing-baseline` and `qa-review-baseline` specs. |
| The harness did not exist and was future work. | The harness was needed before settings and other UI modules could satisfy the testing baseline. | Implement `HeadlessTestApp` and representative `MainWindowLayoutTests` as a prerequisite. | Missing requirement | Reusable for all Avalonia view testing | Promoted to `testing-baseline` spec and `docs/architecture.md`. |

## Deferred or Change-Specific Notes

- This change established policy only; the harness implementation was a separate change.
- Live desktop testing remains available for native-window, OS-input, accessibility, and visual-judgment risks that headless tests cannot represent.

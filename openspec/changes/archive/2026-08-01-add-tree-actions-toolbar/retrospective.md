# add-tree-actions-toolbar Retrospective

## Outcome

The left navigation pane gained a tree-actions toolbar docked between the filter controls and the workspace tree, shipping its first tool: an expand/collapse-all toggle. The toggle acts on all topic nodes (niches and groups), remembers its last action with expand-all as the initial pending action, is disabled while filters are active or nothing is expandable, protects an in-progress inline editor's ancestor chain on collapse, and retains expansion across projection refreshes for the session. Delivered in two production files (`WorkspaceTreeViewModel.cs`, `MainWindow.axaml`) with 10 new tests (8 framework-free view-model, 2 headless view); full baseline 780/780, strict OpenSpec validation 35/35. All discovery decisions (scope, filter interaction, icon approach, draft-edit behavior) held through implementation without reopening.

## Feedback-Driven Adjustments

| Initial assumption | Evidence | Correction | Classification | Applicability | Promotion |
|---|---|---|---|---|---|
| Single-chain nested fixture suffices for the draft-protection test | fc-verifier VR-001: every expandable node was a draft ancestor, so the scenario's "every topic node collapses EXCEPT the ancestors" clause was unverifiable — a collapse-nothing-when-editing defect would have passed | Fixture gained an unrelated expandable branch with expanded-before / collapsed-after assertions | Ordinary implementation defect (test); carries a reusable fixture-design lesson | Any test of "everything except X" behavior | Deferred: candidate testing-baseline rule (see Learning Review) |
| Build recorded as "only pre-existing xUnit analyzer warnings" | Actual build reports 0 warnings | verification.md wording corrected (VR-002) | Documentation drift | This change only | None needed |
| Design claimed RelayCommand "takes only an Action<object?>" | RelayCommand accepts optional canExecute; CanExecuteChanged add/remove are no-ops | fc-spec-reviewer SR-001: design Decision 4 reworded; conclusion (bind IsEnabled to a VM property) unchanged | Documentation drift | House enablement pattern | None needed — design now states the accurate rule |
| Flat Sample.Create fixture reusable for nested-expansion tests | Its group has no children | fc-spec-reviewer SR-002: design note added requiring a nested fixture | Test-planning clarification | This change | None needed |

## Learning Review

- Result: reusable lessons identified; promotions deferred with rationale
- Evidence reviewed: final proposal/design/delta spec/tasks/verification; fc-spec-reviewer findings SR-001..SR-003; fc-verifier findings VR-001/VR-002 and re-verification; build/test/validation outputs; owner decisions recorded during discovery (all four held).
- Promotions completed: none beyond within-change corrections (design, delta spec, verification evidence).
- Deferred promotions:
  1. **Testing-baseline candidate**: tests of "everything except X" behavior must include at least one non-excluded control case with before/after assertions so the except-clause cannot pass vacuously. Rationale for deferral: promoting it edits the accepted testing-baseline spec, which deserves its own small reviewed change rather than a scope expansion of this module after verification passed.
  2. **UI-guidelines candidate**: icon-only toggle buttons name the pending action and keep the accessibility name bound to the same source as the tooltip. Rationale for deferral: docs/ui-guidelines.md line 290 and docs/ux-guidelines.md line 26 nearly cover this; the incremental value is minor and can ride along with the next docs-touching change.
- No missing requirements, architecture lessons, or process defects were found. The one material defect caught (VR-001) was an ordinary test defect resolved inside the verification loop at iteration 1 of 3.
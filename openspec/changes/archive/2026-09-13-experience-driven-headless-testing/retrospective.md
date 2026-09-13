# Retrospective — experience-driven-headless-testing

## Outcome

The approved testing-strategy module is implemented and verified. Critical Printify, Blueprint, and grouped-navigation outcomes now have deterministic Avalonia headless journeys with routed user actions, observable assertions, isolated persistence where re-entry is part of the expectation, and focused lower-layer variants retained for diagnosis. The canonical serialized solution baseline and Windows pull-request workflow are documented and enforced.

## Feedback-Driven Adjustments

| Initial assumption | Evidence | Correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| The plain solution test command was deterministic everywhere. | Parallel VSTest orchestration failed without useful diagnostics in the non-interactive runner. | Standardized `dotnet test .\FusionCanvas.sln -m:1` and narrowly suppressed the Avalonia telemetry side effect through repository configuration. | Architecture/process lesson | All contributors and CI | Promoted to build guidance and workflow. |
| Focused binding or command tests represented the user outcome. | Printify selection/persistence, Blueprint save/re-entry, and navigation geometry escaped to manual testing. | Added rendered journeys that use routed controls and verify visible and durable outcomes. | Missing scenario / bypassed seam | Similar critical cross-seam workflows | Promoted to testing strategy, QA checklist, and pilots. |
| Every escaped defect should create a global rule. | Pilot mechanisms were distinct; broader duplication would add maintenance without evidence of recurrence. | Retained local regressions and promoted only the recurring user-boundary, deterministic-wait, inventory, and escape-review rules. | Proportionate learning | Future critical workflows | Global rules promoted; unrelated surface conversion deferred. |

## Learning Review

- Result: reusable lessons identified.
- Evidence reviewed: final proposal, design, delta specs, completed tasks, verification matrix, pilot journeys, focused helper/persistence tests, strict OpenSpec validation, and available Git history.
- Promotions completed: canonical deterministic baseline, four-layer confidence model, user-job inventory, rendered-action boundary, bounded dispatcher waits, isolated persistence fixtures, escape taxonomy, and QA/PR review fields.
- Deferred promotions: repository-wide journey conversion and full persisted navigation drop coverage; both remain follow-up scope because the current pilots do not justify broader expansion.

## Evidence reviewed

- `HeadlessInfrastructureTests` covers immediate settling, continuation-published state, cancellation, timeout diagnostics, and isolated SQLite roots.
- The Printify pilot selects a shop through the rendered ComboBox, saves through the rendered Store Editor action, closes the window, reconstructs a fresh service/view-model/window composition, re-verifies synthetic shops, and observes the persisted selection.
- The Blueprint pilot types through the rendered Blueprint name field, activates the rendered Save action, closes the window, and reconstructs a fresh composition from the same disposable SQLite workspace with the updated name and clean Save state.
- Navigation drag/drop is routed through rendered rows using a deterministic two-group fixture; persisted nesting and fresh reconstructed rendering now pass.

## Lessons

The original assumption that the plain solution command was the deterministic baseline was invalid in this runner: parallel VSTest orchestration failed without compiler or test diagnostics, while `-m:1` was repeatable. The repository now documents and enforces the serialized command, and the telemetry side effect is suppressed narrowly through `Directory.Build.targets`. This is an environment/orchestration escape, not an application behavior change.

The user-boundary rule exposed a useful failure-localization distinction: keyboard activation on a focused rendered button exercises the binding and enabled-state seam, while direct command execution only proves the command. The small drivers make that distinction visible without becoming a second assertion framework.

The escape taxonomy was useful for the Printify binding/persistence issue, Blueprint save synchronization, and the navigation input/geometry issue. All three pilots now prove their selected durable or visible outcome; future promotion should wait for recurrence on additional surfaces.

## Follow-up scope

Do not expand this module into repository-wide journey conversion. A later module may extend the same pattern to other critical jobs when their seam risk justifies it.

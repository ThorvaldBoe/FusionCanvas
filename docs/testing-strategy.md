# FusionCanvas Testing Strategy

This is the contributor entry point for testing user-facing FusionCanvas behavior. The objective is to catch defects through deterministic tests before manual use, while keeping the routine baseline fast and diagnosable.

## Confidence model

Use the lowest reliable layer for each decision, then add a small experience journey when composition itself carries risk:

1. **Domain, Application, and Integration** — business rules, orchestration variants, failure cases, persistence contracts, and external-boundary mapping.
2. **View model and coordinator** — UI-owned state transitions, command eligibility, cancellation, races, and lifecycle decisions.
3. **Headless component** — rendered construction, bindings, control state, focus, selection, templates, and routed input for one surface behavior.
4. **Headless experience journey** — a complete critical user job across rendered controls and adjacent orchestration, persistence, lifecycle, or re-entry seams.

The fourth layer is intentionally small. Add it when a critical accepted outcome crosses two or more material seams and focused tests could pass while the composed experience fails. Keep variants at lower layers so the suite remains quick and failures remain local.

Do not add a test only because a XAML file or control exists. Static markup, framework-owned rendering, and behavior already proven completely without a material Avalonia seam may be recorded as not applicable.

## User-job inventory

Each changed or reviewed user-facing surface gets rows for meaningful jobs, representative states, expected observable outcomes, and evidence. This inventory is maintained with module verification and expanded when a surface changes; it is not a test-count scoreboard.

| Surface family | Screens / views | Meaningful jobs | Current evidence or gap |
| --- | --- | --- | --- |
| Workspace shell | `MainWindow` | Navigate, search/filter, select, edit, multi-select, drag/nest, switch stage | Headless layout/input coverage plus grouped drag/drop/re-entry pilot |
| Store and catalog editor | `StoreEditorWindow`, catalog tabs and editors | Create/edit stores, select strategy, edit Blueprint/offering/variant/design-area/mockup data, save/cancel, return focus | Extensive component/headless coverage plus rendered Blueprint SQLite save/re-entry pilot |
| Printify credentials | `PrintifyApiKeyWindow`, Store Editor credential controls | Add/replace key, verify, select shop, save, dismiss safely, re-enter | Focused and headless coverage plus disposable-SQLite shop selection/re-entry pilot |
| Assets | `AssetsWindow`, `AssetPreviewWindow` | Open context assets, import, relabel, remove, preview | View-model coverage and preview headless test; `AssetsWindow` journey remains a gap |
| Groups | `GroupSelectionWindow`, `GroupActionConfirmationWindow`, `GroupDeleteConfirmationWindow` | Create group, validate destination/name, confirm or cancel destructive actions | Group selection headless coverage; confirmation surface coverage requires review |
| Ideation | `IdeationWindow`, `RejectIdeaWindow`, `IdeationDiscardConfirmationWindow` | Generate, accept/reject, edit rejection, discard safely | Ideation window headless coverage; dialog composition to be audited when touched |
| Rejected phrases | `RejectedPhrasesWindow` | Review, filter, restore/manage rejected phrases | Dedicated headless window coverage |
| Snowclones | `SnowcloneLibraryWindow` | Browse, create/import/manage templates and entries | Dedicated headless window coverage |
| Settings | `SettingsWindow`, `AiSettingsView`, `AiProfileEditorView` | Navigate settings, configure AI, manage workspace/theme/about | Dedicated headless coverage |
| Item import/export | `ItemImportWindow` and export surface | Import CSV, validate, confirm/cancel, export items | Dedicated import headless tests and export view coverage |
| Workspace management | `WorkspaceManagementWindow` and transfer surfaces | Create/switch/transfer workspace, confirm/cancel | Dedicated transfer headless coverage; end-to-end persistence scope reviewed per change |
| Design stage | `DesignPreviewWindow` and design-stage embedded views | Configure target, select colors/slots, assign artwork, preview/remove | Dedicated design-stage headless coverage; preview journey reviewed when changed |
| Mockup dialogs | `MockupTemplateEditorWindow`, `EnlargedMockupPlacementEditorWindow` | Create/edit template, upload/select source, map placement, keep aspect ratio, cancel | Extensive Store Editor headless coverage |
| Startup | `SplashWindow` | Display startup/version state and transition | Focused headless construction/version coverage |

Rows marked as gaps are scoped follow-up work, not blockers for unrelated modules. A module may add or refine rows for its affected jobs and must provide an explicit rationale for any omission.

## Experience journey contract

Keep each journey visibly divided into:

```text
Arrange deterministic state → Perform user actions → Assert visible outcome → Re-enter if durable
```

During the action phase, use rendered controls and routed pointer, keyboard, text, selection, or focus operations. Do not replace the action with direct view-model command execution, bound-property mutation, private-handler reflection, or an unbounded wait. Thin surface drivers may expose user-language actions and observations, but they contain no assertions or business rules.

For durable mutations, use a scenario-owned disposable database, workspace root, and settings path. Close the screen, construct fresh application/presentation instances, and assert the rehydrated visible state and clean pending-change state.

Use dispatcher-aware settling with a bounded timeout and a diagnostic condition name. Do not use arbitrary sleeps as readiness evidence.

## Bug escape review

Every defect discovered after automated tests passed gets a local regression and a short record:

```text
User expectation that failed:
Regression test and fail-before/pass-after evidence:
Why the existing suite passed:
Escape class: missing scenario | bypassed seam | weak oracle | unrealistic fixture |
              missing re-entry | async/lifecycle | input/focus/geometry |
              platform-only | ambiguous specification
Similar surfaces inspected:
Local-only regression or reusable prevention (and why):
```

Promote a lesson into shared helpers, this inventory, QA guidance, specifications, or coding standards only when the mechanism recurs or plausibly affects multiple surfaces. Otherwise keep the focused regression without adding process or duplicate tests.

## Pull-request baseline

The canonical deterministic command is:

```powershell
dotnet test .\FusionCanvas.sln -m:1
```

It includes App headless tests and excludes `FusionCanvas.UITests`. The same command must work in local contributor environments and pull-request CI using repository-controlled non-secret configuration. Real-desktop Appium runs remain a separately selectable supplemental lane.

## Strategy-health signals

Review escaped UI defects, repeated escape classes, headless flake rate, baseline duration, journey maintenance cost, and failure-localization quality. Test count, line coverage, and number of windows with tests are diagnostic signals only—not completion gates.

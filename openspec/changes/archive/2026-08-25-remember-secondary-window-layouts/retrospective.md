# remember-secondary-window-layouts Retrospective

## Outcome

Every non-transient secondary window (Settings, Workspace Management, Store Editor, Assets, Ideation, Reject Idea, Snowclone Library, Rejected Phrases, Design Preview, Item Import) now persists its normal-state position and size and restores it on reopen, with the same screen-safe normalization as the main window. Transient confirmation dialogs keep default placement. Existing settings documents (versions 1–3) load cleanly with no geometry section.

## Feedback-Driven Adjustments

| Assumption | Evidence | Correction | Classification | Applicability |
|---|---|---|---|---|
| Main-window normalization logic is reusable for secondary windows | `MainWindowLayoutNormalizer` screen-validation and clamping are geometry-agnostic | Extracted `TryCaptureGeometry`/`TryNormalizeGeometry` as shared helpers alongside existing main-window signatures | Architecture lesson | All window-geometry persistence |
| Settings document version bump needed for per-window geometry | Version 3 has no `windowGeometry` section | Introduced version 4 with `windowGeometry`; versions 1–3 default to empty geometry | Implementation detail | `JsonApplicationSettingsStore` |
| Ideation sub-windows need geometry from their parent, not MainWindow | Sub-windows are owned by `IdeationWindow`, not `MainWindow` | Added internal `IWindowGeometryStore` property on `IdeationWindow` set by `MainWindow` before `ShowDialog` | Implementation detail | Ideation window hierarchy |

## Learning Review

- Result: Reusable lessons identified.
- Evidence reviewed: proposal, design, delta specs, tasks, test output (1377 passed, 0 failed), `openspec validate --strict` pass, code inspection of attach sites.
- Promotions completed: window-layout-persistence main spec extended with secondary-window requirements; application-settings main spec extended with backward-compatibility requirement.
- Deferred promotions: none.

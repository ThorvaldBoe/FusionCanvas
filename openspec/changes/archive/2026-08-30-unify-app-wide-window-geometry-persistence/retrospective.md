# Unify App-Wide Window Geometry Persistence Retrospective

## Outcome

All non-transient FusionCanvas windows now route geometry persistence through a shared registrar and lifecycle. Stable keys and settings compatibility are preserved; transient confirmation and selection dialogs remain default-placed.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| Per-window helper attachment was sufficient | Store Management and Settings could re-enter close processing and lose geometry | Centralize registration and defer open-state synchronization | Architecture / implementation defect | Reusable across all secondary windows | Captured in the modified window-layout specification and design |
| Avalonia managed position was authoritative | Native Windows movement left persisted coordinates at `0,0` | Capture native coordinates before handle teardown with managed fallback | Implementation defect | Windows-native window persistence | Captured in the modified specification and design |

## Learning Review

- Result: reusable lessons identified and promoted into the capability delta and design.
- Evidence reviewed: user-reported Store Management and Settings regressions, persisted settings inspection, focused geometry tests, and full solution test results.
- Promotions completed: app-wide registration contract and native close-time coordinate capture are recorded in the active spec/design.
- Deferred promotions: none.

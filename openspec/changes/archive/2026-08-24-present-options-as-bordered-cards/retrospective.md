# present-options-as-bordered-cards Retrospective

## Outcome

Available choices in Manage Variants now render each Option as a distinct bordered `choice-card` built from shared semantic theme resources, so the boundary stays visible in Light and Dark appearance. Cards sit two-across at the default Store Editor width, wrap to one per row at the minimum supported width, and wrap long names and value summaries instead of clipping. The change is presentation-only; no domain, persistence, or behavioral rules changed. All acceptance scenarios were verified headlessly and the full solution baseline passes.

## Feedback-Driven Adjustments

| Initial assumption | Evidence | Correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| Option cards already had a boundary (`Classes="listItem"`). | No `Border.listItem` style exists; only `Button.listItem` is defined, so borders were transparent against the page background. | Introduced a dedicated `Border.choiceCard` style instead of widening the global `listItem` class. | Ordinary implementation defect | Repo-wide (global restyling risk when editing shared classes) | None; dedicated classes keep the change bounded. |
| Two 300px cards fit side by side in Available choices. | WrapPanel measures ~498px available in the scrollable pane at the default 860px window, so 300px cards always stacked. | Tuned card width to 235px (two-across at default, one per row at minimum width). | Ordinary implementation defect (geometry) | This screen and similar card grids | Deferred: a shared, responsive card-grid rule could be promoted to `docs/ui-guidelines.md` if a second card grid needs the same treatment. |
| Comparing card `Bounds` proves alignment/stacking. | Generated item bounds are local to per-item `ContentPresenter`s (all reported `0,0`), so local comparisons are meaningless across containers. | Compared window-relative positions computed with `TranslatePoint`. | Implementation technique | Avalonia headless geometry assertions | None; `TranslatePoint` patterns already exist in `MainWindowLayoutTests`, so the repo already carries this guidance. |

## Learning Review

- Result: no reusable lessons promoted
- Evidence reviewed: Issue #193, delta spec, design decisions (choice-card style, width tuning, text wrapping), test failure diagnostics during geometry assertions, and `verification.md` results
- Promotions completed: none
- Deferred promotions:
  - Shared/responsive card-grid guidance in `docs/ui-guidelines.md` — deferred until a second card grid actually converges (rationale: only one consumer today, and premature shared guidance risks over-generalizing).
  - The presentational decisions (theme-brush choice, card width, corner radius) remain change-scoped in `design.md` and should not be reopened as global rules yet.
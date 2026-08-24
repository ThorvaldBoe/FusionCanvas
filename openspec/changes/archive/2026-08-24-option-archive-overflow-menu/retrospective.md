# option-archive-overflow-menu Retrospective

## Outcome

Each Available-choice Option card (Color, Size, and custom kinds) now exposes **Manage values** as the routine directly-available action and keeps the infrequent destructive **Archive option** action inside a compact three-dot overflow `MenuFlyout` in the card's upper-right corner. Archive eligibility, dependency checks, confirmations, error messages, and blocked-archive behavior are unchanged; the existing `ArchiveOptionCommand`/`CatalogSetupService.ArchiveAsync` path is reused verbatim. The overflow trigger is keyboard focusable, has an accessible name (`More actions for <Option Name>`), and returns focus on dismissal.

## Feedback-Driven Adjustments

| Initial assumption | Observed problem or feedback | Approved correction | Classification | Applicability | Promotion |
| --- | --- | --- | --- | --- | --- |
| Menu item could bind directly to `$parent[ItemsControl].DataContext.CatalogSetup.ArchiveOptionCommand` | Avalonia flyout popup content is not part of the ItemsControl's visual tree, so the relative binding resolves to null and the command never executes; focused headless tests caught a null `Command` on the realized menu item | Relay `ArchiveOptionCommand` through `OfferingChoiceGroupViewModel` (the card's data context), which the placement button and its flyout inherit | UI / architecture defect | Reusable scope | Consider a flyout-menu guidance note in `docs/ui-guidelines.md` so future popup menus bind via inherited data context rather than ancestor lookups |

## Learning Review

- Result: reusable lessons identified
- Evidence reviewed: the headless test failure at `OptionOverflowMenu_ContainsDestructiveArchiveEntryForTheOption` (null `Command` when using `$parent[ItemsControl]` inside a `MenuFlyout`), the corrected relay binding, and the passing 8-test focused suite plus the full baseline.
- Promotions completed: none; all corrected behavior is represented in the accepted `variant-management` main spec and this retrospective.
- Deferred promotions: a concise "popup/flyout menus bind commands through the placement target's data context, not ancestor lookups" note in `docs/ui-guidelines.md` is deferred because it belongs with a broader menu/focus guidance pass; the correction is already encoded in code and validated by tests.
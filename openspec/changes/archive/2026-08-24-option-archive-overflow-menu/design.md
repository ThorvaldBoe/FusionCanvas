## Context

On **Manage Variants → Available choices**, each Option card for Color, Size, or a custom Option kind is rendered by a single data template in `StoreEditorWindow.axaml` (approximately lines 754-772). The card currently shows the Option name, a kind label, a values summary, a **Manage values** secondary button, and a large red **Archive Option** button (`Classes="danger"`). The archive button is visually dominant even though archiving is infrequent, and it competes with the routine **Manage values** action.

The archive flow already routes through `CatalogSetupViewModel.ArchiveOptionCommand` → `RunArchive(parameter, CatalogRecordKind.Option)` → `CatalogSetupService.ArchiveAsync`, which performs dependency checks (an Option with any active Option Value is blocked) and surfaces the reason through the existing recoverable inline error (`ErrorMessage` / `HasError` in the store editor). None of that behavior should change.

This change is presentation-only: move **Archive option** from a dominant inline `danger` button into a compact three-dot overflow `MenuFlyout` on each card while keeping **Manage values** directly available. Issue #193 (bordered Option cards) is a separate module; this change must stay self-contained and composable so the two can land independently.

### UX preflight

- **User and objective:** A creator or store administrator maintaining one Offering's selectable Option Values. The routine action is invoking **Manage values**; the rare destructive action is archiving a whole Option.
- **Frequency:** **Manage values** (or its disclosure) is periodically used during catalog setup; **Archive option** is infrequent.
- **Surface ownership:** Both actions remain inside the existing focused Store Management editor, on the Option card in the Available choices region.
- **Workspace footprint:** No new persistent workspace area. A 28x28 icon button adds negligible card footprint; the removed `danger` button reduces the card's action row width.
- **Progressive disclosure:** The destructive action moves inside a compact overflow menu; **Manage values** stays visible as the routine action.
- **States:** Menu open/anchor, menu dismissal without change, blocked archive (existing inline error), archived/read-only Store (overflow menu hidden/disabled consistent with existing mutation gating).
- **Selection and focus:** Starting value management still focuses the value editor; closing it returns focus through the existing `OptionChoiceFocusRequested` helper to the card's routine **Manage values** button. After the overflow menu is dismissed, focus returns to the overflow button that opened it.
- **Drafts and destructive actions:** Archive remains explicit and confirmed per existing behavior; no new confirmation is invented, and existing dependency checks and blocked-archive messages stay authoritative.

### Authority hierarchy

1. Domain identities and invariants (unchanged).
2. Accepted OpenSpec behavioral requirements.
3. This change's delta requirements.
4. `docs/Visuals/ui-descriptions/manage-variants.ui.yaml` as an illustrative information-hierarchy reference (the semantic template already models Options as `choice-card` panels and shows **Manage colors**/**Manage sizes** actions).
5. Detailed labels, styling, exact icon geometry, and menu placement remain non-normative implementation decisions.

## Goals / Non-Goals

**Goals:**

- Remove the visually dominant inline **Archive Option** `danger` button from every Option card.
- Add a compact three-dot overflow control in each card's upper-right corner that opens a small menu containing a clearly destructive **Archive option** entry.
- Keep **Manage values** directly available as the routine action.
- Preserve archive eligibility, dependency checks, confirmations, error messages, and blocked-archive behavior exactly.
- Expose an accessible name such as **More actions for Color**, keyboard-focusable, with standard keyboard menu interaction and focus-return on dismissal.
- Apply consistently to Color, Size, and custom Option kinds (the single data template covers all).
- Verify pointer, keyboard, focus-return, and accessible-name behavior with focused Avalonia headless tests.

**Non-Goals:**

- Changing archive eligibility, persistence, dependency checks, confirmations, or error text.
- The bordered Option-card visual treatment in #193.
- Introducing a confirmation dialog for Option archive (none exists today; that behavior is unchanged).
- Refactoring other Archive buttons (Option Value, Variant, Placeholder, Template) — those are outside this issue.
- Changes to Domain, Application, Integration, or the data model.

## Decisions

### 1. Use a `Button.Flyout` hosting a `MenuFlyout` with one destructive `MenuItem`

The overflow control is an icon-only `Button` (`Classes="iconButton"`, `Content="..."`) whose `Button.Flyout` is a `MenuFlyout` containing a single `MenuItem` **Archive option**. This reuses the exact established pattern from `MainWindow.axaml` (the store-actions `...` button at lines 89-102 and settings/expand glyphs at 66-88), keeping appearance consistent with the shell.

- Why not a `ContextMenu` on the card? A `ContextMenu` (right-click) is less discoverable than an explicit trigger, and the issue asks for a visible three-dot trigger. A `Flyout` provides an anchored, dismissible menu.
- Why not keep a smaller red `Button`? The issue explicitly rejects permanently visible destructive weight; alternatives considered in the issue (smaller archive button) were rejected because archive is rare.
- The `MenuItem` gets `Classes="danger"`-style destructive presentation via an existing or small added style so it is *identified* as destructive without dominating the card (destructive styling on a menu entry, not a large filled button).

### 2. Reuse the existing command and focus machinery with minimal changes

The `MenuItem` binds `Command="{Binding ArchiveOptionCommand}"` with `CommandParameter="{Binding Option}"` relaying the owning `CatalogSetupViewModel.ArchiveOptionCommand` through the card's `OfferingChoiceGroupViewModel`. A flyout popup is not part of the ItemsControl's visual tree, so `$parent[ItemsControl]` cannot resolve inside it; relaying through the card's own data context (which the placement button and its flyout inherit) makes the binding robust and keeps command semantics identical. The **Manage values** button and its binding stay exactly where they are. `OnOptionChoiceFocusRequested` in `StoreEditorWindow.axaml.cs` continues to find and focus the **Manage values** button (it looks up by `OfferingChoiceGroupViewModel` + content text "Manage values", which is unchanged); no focus-handler logic needs to change. Flyout dismissal already returns focus to the owning `Button` in Avalonia, satisfying focus-return with no added code-behind.

### 3. Provide accessible name and stable automation identity

The overflow `Button` sets `AutomationProperties.Name` bound to an accessible label such as **More actions for Color**. Because the card's data context is `OfferingChoiceGroupViewModel`, the name can be computed in the view model (`AccessibleOverflowName` returning `More actions for <Name>`) to keep AXAML simple and deterministic, or set inline via `AutomationProperties.Name="{Binding Name, StringFormat=More actions for {0}}"`. Decide deterministically in the view model to make headless assertions straightforward. Add `AutomationProperties.AutomationId` (e.g., `Catalog.OptionOverflowMenu`) so headless tests and assistive tech have a stable handle.

### 4. Keep the card layout compact

The card header `Grid ColumnDefinitions="*,Auto"` (name + kind label) gains a third `Auto` column for the overflow button in the upper-right corner, matching the plan in #193 (the bordered card that hosts this overflow action). The bottom action row keeps only **Manage values**. This is the minimal, composable layout change.

## Implementation Plan

### Affected layers and likely files

- **Domain / Application / Integration:** no changes.
- **App view model:** `src/FusionCanvas.App/Stores/OfferingChoiceGroupViewModel.cs` — add `ArchiveOptionCommand` relay plus `AccessibleOverflowName` (e.g., `"More actions for {Name}"`) and `OverflowAutomationId = "Catalog.OptionOverflow."`; `CatalogSetupViewModel.cs` passes `ArchiveOptionCommand` when building groups.
- **App view:** `src/FusionCanvas.App/Stores/StoreEditorWindow.axaml` — rework the choice-card data template (lines ~754-772): move overflow control into the card header grid, add `MenuFlyout` with destructive **Archive option** `MenuItem` bound to `ArchiveOptionCommand`, remove the old `danger` button.
- **App styles:** `StoreEditorWindow.axaml` `Window.Styles` — add an `iconButton`-style selector (mirror of `MainWindow.axaml:1892-1902`) if not resolvable from App-level theme, and a destructive `MenuItem` style if the theme lacks one.
- **Tests:** extend `tests/FusionCanvas.App.Tests/StoreEditorHeadlessTests.cs` with headless view tests (or a dedicated file if they grow) covering: card has no direct **Archive Option** button; overflow menu contains destructive **Archive option**; pointer/keyboard open; focus-return to overflow button on dismissal; accessible name; `Manage values` still present; blocked archive error surfaces unchanged.

### Responsibility placement

- View layer owns presentation/disclosure only; no business rules in code-behind or AXAML.
- View model provides deterministic accessor strings (accessible name) so tests assert behavior without markup parsing.
- All archive semantics live in the untouched command/service stack.

### Sequencing

1. Update `OfferingChoiceGroupViewModel` with `AccessibleOverflowName` (+ test in `CatalogPresentationModelsTests.cs` or `CatalogSetupViewModelTests.cs`).
2. Add headless view tests that fail against the current card (presence of direct **Archive Option** button).
3. Rework the AXAML data template and styles; run focused headless tests to green.
4. Run the full solution test baseline and strict OpenSpec validation; fill `verification.md` with criterion-level evidence.

### Algorithms and edge cases

- Accessible name must reflect the actual Option (Color/Size/custom), never "Option" generically.
- Archive eligibility is unchanged: an Option with any active Option Value is blocked and the existing inline error surfaces; the menu entry is not removed or special-cased (consistency over hiding).
- Menu dismissal (Escape / outside click) makes no change and returns focus to the overflow button.
- Keyboard path: Tab to overflow button, Space/Enter opens the menu (Avalonia default for `Button` + `Flyout`), arrow keys navigate, Enter activates **Archive option**, Escape dismisses.
- Archived/read-only Store: existing mutation gating disables the command; the overflow control follows the card's existing enabled state.
- The single template guarantees Color, Size, and custom Option kinds behave identically.

### Compatibility and migration

No schema or workspace migration. Rollback is a revert of the AXAML + view-model presentation change; the stored catalog model and command stack are untouched.

### Decisions implementers must not reopen

- **Archive option** lives in a three-dot overflow `MenuFlyout` on every Option card; no visible inline Archive button remains.
- `Manage values` remains the directly visible routine action.
- Archive command, dependency checks, confirmations, and error messages are reused verbatim; no new confirmation dialog.
- Accessible name pattern is `More actions for <Option Name>`.
- Exact icon glyph, styling, and geometry are non-normative implementation details.

## Risks / Trade-offs

- **[Risk] A `Flyout`-based menu can be harder to test for true OS-level focus** → Avalonia headless tests assert the flyout is opened, the menu item exists and is bound to the archive command, and focus returns to the owning button; framework-freed view-model tests cover the command path. This matches the existing `ItemsCsvExportViewTests.cs` menu-testing precedent.
- **[Risk] Removing the visible Archive button could reduce discoverability** → The issue explicitly accepts this trade-off; the overflow menu keeps the action discoverable while matching its frequency. Both actions remain on the card.
- **[Trade-off] Single menu item inside a flyout** → Slightly more clicks than a button, but the overflow menu also accommodates future card actions without further layout churn (composable with #193).

## Open Questions

None. Archive semantics, menu pattern, accessible naming, and scope boundaries are resolved by the issue and this design.
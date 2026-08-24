## Context

The catalog management screens were reorganized by earlier delivery modules so that Manage Variants separates Available choices from Sellable Variants. The Available choices region already binds `CatalogSetupViewModel.AvailableChoiceGroups` (`OfferingChoiceGroupViewModel`) into an `ItemsControl` over a horizontal `WrapPanel`, and each Option renders as a `Border` with the `listItem` class. That class is only defined for buttons (`Button.listItem`); no `Border.listItem` style exists, so the Option cards are transparent and borderless and visually "run together" against the page background.

The accepted design direction (`docs/Visuals/ui-descriptions/manage-variants.ui.yaml`) models each Option as a `choice-card` panel with a title, value summary, and a manage action inside one boundary. This change aligns the Avalonia presentation with that direction. It is purely presentational: no domain, application, persistence, or data changes are involved, and Issue #193 explicitly scopes the work to the UI.

## Goals / Non-Goals

**Goals:**

- Give every available Option a distinct, compact bordered card containing the name, kind label, value summary, Manage values action, and Archive Option action inside one visual boundary.
- Reuse shared semantic theme resources (`App.axaml` Light/Dark dictionaries) so the boundary is visible in both appearances.
- Keep the section visually calm: a subtle 1px border rather than a heavy decorative card wall.
- Align cards in the available width and wrap/stack gracefully at narrower widths without clipping names or value summaries.
- Verify behavior deterministically with Avalonia headless view tests.

**Non-Goals:**

- Changing domain, persistence, layout data, Option/Variant semantics, or existing actions.
- Adding new theme resources (existing brushes already cover the need).
- Moving the Archive Option action into an overflow menu (Issue #192, separate change).
- Introducing virtualization, adjustable card sizes, or a custom panel control.

## Decisions

### 1. Use a dedicated `Border.choiceCard` style, not a global `Border.listItem` style

`Border.listItem` is used across many unrelated screens (Store Editor rows, Workspace Management, Snowclone Library, Rejected Phrases) with no defined style. Widening the definition of `Border.listItem` would restyle all those screens beyond this module's scope. Instead, the choice cards get a dedicated `choiceCard` class style defined near the other `StoreEditorWindow.axaml` styles:

- `BorderBrush`: `ControlBorderBrush` (subtle but clearly visible in Light `#D9DEE7` and Dark `#364154`).
- `Background`: `ElevatedSurfaceBrush` (white in Light, `#222833` in Dark) so the card lifts slightly off the surrounding `PanelSurfaceBrush` panel.
- `BorderThickness`: `1`; `CornerRadius`: `6` (the codebase's standard card radius).

An `ElevatedSurfaceBrush`-on-`PanelSurfaceBrush` plus border contrast keeps the section scannable without a heavy card wall. Applied only where the `choiceCard` class is used, so the change stays bounded to the Available choices region.

### 2. Keep the `WrapPanel` and fixed card width, tuned to the real available width

The option cards already live in a horizontal `WrapPanel`. Measurements show the actual available width for the cards inside the scrollable right pane is about 498px at the default 860px window, so the previous 300px card width never sat two-across and looked left-aligned and sparse. A fixed card width of `235` plus the `10` trailing margin (`235*2 + 10 = 480`) fits two cards per row at the default width and wraps to one per row at the minimum supported width (`MinWidth=720` → ~394px available). A `UniformGrid` or dynamic `ItemWidth` were considered but add complexity and behave less predictably for a variable Option count; the existing `WrapPanel` with a tuned width satisfies "align cleanly and wrap/stack gracefully" with minimal change.

### 3. Prevent clipping with wrapping text and stable card automation identity

The Option name `TextBlock` did not wrap, so long names could overflow the card at `Width=235`. Both the name and the value summary now use `TextWrapping="Wrap"`, and the kind label is top-aligned in an Auto column with added column spacing. Each card also gains `AutomationProperties.AutomationId="Catalog.OptionCard"` so headless tests and assistive technology can address the cards by a stable identity, matching the pattern used elsewhere in the file (for example `Catalog.OpenOffering`).

### 4. Empty and custom Options need no special casing

`OfferingChoiceGroupViewModel` already produces a truthful value summary (`"No values configured"` when a group has no values) and a kind label derived from `Option.OptionKind` (`Color`, `Size`, or the custom kind). Because the card template is shared by all groups, empty Options and custom kinds automatically receive the same boundary and layout without extra branches.

### UX preflight

- **Primary workflow:** A creator opens Manage Variants for one Blueprint Offering and scans Available choices to confirm which Color, Size, and other values can be combined. Scanning is frequent; editing values and archiving Options are occasional and stay behind explicit actions. The work remains in the existing focused Store Management editor; no new surface or navigation is introduced.
- **Workspace footprint:** The card layout keeps the region compact and scannable, matching the existing two-column mockup intent. No new drafts or permanent panels were added.
- **States:** Empty Options, custom Option kinds, and unavailable provider catalogs keep the same card treatment; the existing region-level "No Options configured" empty state is unchanged.
- **Focus/accessibility:** Actions remain on the card, and the stable `Catalog.OptionCard` automation identity exposes each card to automation. Focus behavior of the existing Manage values and Archive actions is unchanged.

## Risks / Trade-offs

- **[Risk] Fixed card width may not perfectly fill available width for all window sizes** → The width is tuned for the default and minimum supported widths with comfortable slack; the `WrapPanel` naturally reflows at intermediate widths. Headless tests assert both the two-across default and stacked minimum cases.
- **[Risk] `Border.choiceCard` styling could be confused with future generic cards** → The class is scoped to this region and follows the existing per-window style convention; if a shared card style is later wanted, it can be lifted into `App.axaml` without behavior change.
- **[Trade-off] Visual "subtlety" is judged qualitatively** → Theme resources (elevated surface + `ControlBorderBrush`) match the codebase's existing card styling; qualitative appearance is captured by the optional live desktop review, while geometry, visibility, and border application are proven headlessly.

## Implementation Plan

### Affected layers and files

- **App (Avalonia view):** `src/FusionCanvas.App/Stores/StoreEditorWindow.axaml`
  - Add a `Border.choiceCard` style (CornerRadius `6`, BorderThickness `1`, `BorderBrush` = `ControlBorderBrush`, `Background` = `ElevatedSurfaceBrush`, all as `DynamicResource`).
  - Change the Available choices card template to use `Classes="choiceCard"`, `Width="235"`, `Margin="0,0,10,10"`, and `AutomationProperties.AutomationId="Catalog.OptionCard"`.
  - Wrap the Option name and keep the kind label top-aligned with column spacing so long names/values do not clip.
- **Tests:** `tests/FusionCanvas.App.Tests/StoreEditorHeadlessTests.cs`
  - Add a headless view test that opens Variant management with Color and Size Options and asserts: two `Catalog.OptionCard` borders exist; each card's `BorderThickness` is `1`, `BorderBrush` and `Background` are applied, and `CornerRadius` is `6`; cards are side by side on one row at the default width; and cards stack onto a new row when the window is narrowed to `MinWidth`.
- **Domain / Application / Integration:** no changes.

### Responsibility placement

All presentation lives in the App layer's AXAML view; view-model projections (`AvailableChoiceGroups`, `OfferingChoiceGroupViewModel`) are unchanged and remain the single source of card content. No business rules are implemented in the view.

### Algorithms and edge cases

- Card width `235` + `Margin` right `10` ⇒ `480` for two cards, which fits the ~498px measured available width at the default window size and wraps to one per row at the minimum width. The `WrapPanel` handles any Option count; there is no virtualization.
- Long names and value summaries wrap within the card; the auto column holds the kind label on the first line.
- Empty groups and custom kinds flow through the same template; `OfferingChoiceGroupViewModel.ValuesSummary` already yields the truthful `"No values configured"` fallback.

### Sequencing

1. Add the `Border.choiceCard` style.
2. Update the Available choices card template (class, width, automation id, text wrapping).
3. Add the focused headless view test.
4. Run `dotnet test .\FusionCanvas.sln` and complete `verification.md` with criterion-level evidence; optional live desktop review only as supplemental visual evidence.

### Verification mapping (acceptance scenarios → evidence)

| Delta spec scenario | Planned verification |
| --- | --- |
| User scans available choices as cards; Empty Option; Custom Option kind | Headless view test: card count == Option count for Color/Size fixtures, border/background/corner-radius assertions, per-card content inside the card boundary via template structure. |
| Cards align cleanly in the available width | Headless view test: translated positions place the two cards on the same row at the default window width. |
| Cards stack at narrower supported widths | Headless view test: resizing the window to `MinWidth` and re-running layout places the second card on a new row. |
| Long content does not clip | Structural assertion (name/value `TextWrapping="Wrap"`) + existing long-name fixtures reviewed for overflow; visual appearance optionally confirmed in supplemental live desktop review. |

### Compatibility and migration

None. The change is confined to App-layer presentation; no schema, domain invariant, or workspace data changes, so rollback is a pure source revert.

### Decisions implementers must not reopen

- The boundary comes from the dedicated `choiceCard` class; do not widen `Border.listItem`.
- Cards use shared theme resources; no new brushes.
- Available choices remain before Sellable Variants; card actions stay inline (overflow menu is Issue #192).
- Two-across at default width and one-per-row at minimum width come from the fixed `235` card width; do not change geometry without re-running the headless geometry assertions.

## Open Questions

None. This is a bounded presentation change with the direction already established by the accepted `choice-card` design reference and the catalog-management delivery modules.

## Migration Plan

No staged deployment or data migration is needed. The presentation change is implemented, regression-tested with the solution baseline, and reverted by reverting the App-layer edits.
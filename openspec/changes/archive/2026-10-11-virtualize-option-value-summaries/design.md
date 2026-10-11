## Context

The Store Editor's Variant management surface renders one `OfferingChoiceGroupViewModel` per enabled Option in `CatalogSetupViewModel.RefreshOfferingCollections`. Each wrapped card is 235 px wide for Size and custom kinds; it currently displays `ValuesSummary`, while Color already has a separate paged virtual grid. The issue explicitly changes only Size and custom summaries, keeps them read-only, preserves saved order, and excludes search, sorting, paging, and Color-grid changes.

The primary workflow is frequent scanning of configured choices while preparing an Offering's Variants. The summaries belong inside the existing Available choices cards. **Manage values** remains the direct routine action for editing and the existing overflow menu owns archive. A fixed-height grid bounds each card's vertical footprint; vertical scrolling exposes values beyond the visible rows. This surface has no selection, draft, loading, or save state. An Option with no active values keeps a short truthful empty message and does not render an empty grid.

## Goals / Non-Goals

**Goals:**
- Show active Size and custom Option Values in bounded, read-only virtual grids in their persisted `SortOrder`.
- Preserve the current card width/wrapping, action placement, and Color card behavior.
- Provide full long values through tooltips and automation name/help text when visible row text is truncated.
- Verify virtualization, order, accessibility properties, empty state, and unaffected Color/action behavior with deterministic headless UI coverage.

**Non-Goals:**
- Change Option Value storage, ordering, editing, archival, Variant creation, or persistence.
- Change the Color grid or add search, sort, page, filter, selection, or editing controls to Size/custom grids.
- Add a new user journey or alter Appium coverage. The existing workspace and workflow remain in place, and the relevant binding, realization, layout, and accessibility properties are observable in headless tests.

## Decisions

1. **Use one fixed-height grid per non-Color card.** Keep each card at its current width and give the summary grid a 120 px viewport with 32 px rows. This shows roughly four rows and lets the rest scroll inside the card. A text summary remains appropriate only for the empty state. This follows the issue's bounded compact-card decision and avoids list length controlling card height. A single aggregate grid was rejected because it would separate values from their Option names and actions and would not fit the wrapped card interaction.
2. **Reuse the existing virtual-grid provider boundary.** Replace the positional `OfferingChoiceGroupViewModel` record with a focused view model that exposes an `InMemoryDataProvider<AvailableOptionValueGridRowViewModel>`. Populate it from the already filtered active `Values` ordered by saved `SortOrder` and stable value ID when the group is built. Do not reorder or persist values. Keep the grid rows simple and read-only, with a derived accessible label and full value.
3. **Keep Color and card actions independent.** The XAML branch for `IsColor`, `AvailableColorsGrid`, Color page controls, `Manage values`, and archive overflow remain behaviorally unchanged. The Size/custom branch swaps only its summary text for an empty message or grid. Set selection mode to `None`, disable sorting/resizing, and add no browse controls.
4. **Preserve full value text without widening cards.** Use single-line ellipsis in the grid cell and bind the full value to the tooltip, AutomationProperties.Name, and HelpText. Give each grid an accessible name that identifies its Option kind/name, and retain row automation text even when the visual value is trimmed.
5. **No migration or new package is needed.** The UI derives provider rows from existing in-memory Option Values on each `RefreshOfferingCollections`; existing Offering-change and archive refresh paths rebuild the rows with the active context. Persistence remains unchanged.

## Risks / Trade-offs

- [A dense fixed viewport can make a short list feel sparse or a longer list less discoverable] → Keep the empty state separate, display approximately three readable rows beneath the grid header, and use a normal vertical scrollbar when additional rows exist.
- [Rebuilding a provider for each choice group can disturb the visible list during frequent collection refreshes] → Reuse the existing one-pass `RefreshOfferingCollections` projection and reset each per-card provider once from its ordered snapshot; do not reset it per row.
- [Truncated text can hide the distinguishing part of a long value] → Keep the full string in tooltip and automation text and assert this on realized rows in a headless view test.

## Migration Plan

No data migration or compatibility shim is required. Existing saved `SortOrder`, Option Value IDs, Color setup, and management commands remain authoritative. Rollback is the code-only restoration of the current `ValuesSummary` binding; persisted data is unaffected.

## Open Questions

None. Issue #888 explicitly decides the kinds in scope and constraints; the existing 235 px card width and wrapped card layout anchor the compact viewport choice.

## Implementation Plan

1. In `src/FusionCanvas.App/Stores/OfferingChoiceGroupViewModel.cs`, retain the option, active values, and archive command, and add a per-group `InMemoryDataProvider<AvailableOptionValueGridRowViewModel>`. Populate it once from active values in `SortOrder` with stable ID tie-breaking. Add `HasValues`, a descriptive grid accessible name, and a row model whose full value can be used for visual text, tooltip, AutomationProperties.Name, and HelpText. Preserve existing Name, KindLabel, IsColor, CardWidth, and overflow properties/IDs.
2. In `src/FusionCanvas.App/Stores/CatalogSetupViewModel.cs`, keep `RefreshOfferingCollections` as the source for active group values. Ensure each group receives the current Option's active values in saved order and its provider is rebuilt when the selected Offering or Option Values refresh. Do not change `AvailableColorsGrid` or domain/persistence behavior.
3. In `src/FusionCanvas.App/Stores/StoreEditorWindow.axaml`, retain the current Color branch. For non-Color cards, show the current empty message when `HasValues` is false; otherwise render a 120 px `VirtualDataGrid` bound to the group provider with 30 px row height, no selection, sorting, or resizing, a single full-width trimmed text cell, tooltip/accessibility text, and a descriptive grid name. Keep the card at its current width and preserve **Manage values** and overflow controls.
4. Add focused framework-free tests beside the new row/group provider if projection behavior warrants them, and extend `tests/FusionCanvas.App.Tests/StoreEditorHeadlessTests.cs` to construct Size and custom groups with ordered long lists. Assert fixed viewport and row height, fewer realized rows than values, saved display order through scrolling, full tooltip/automation values for long text, no row selection/edit controls, correct empty state, and unchanged Color grid plus Manage values/archive controls. Use the existing Avalonia headless fixture.
5. Record each acceptance scenario in `verification.md` with its criterion, command or headless assertion, result, and evidence. Run focused tests, `dotnet test .\FusionCanvas.sln -m:1`, `openspec validate --all --strict`, and `git diff --check`. Do not add a live desktop gate or Appium journey.

### Acceptance-to-verification mapping

| Acceptance scenario | Planned evidence |
| --- | --- |
| User scans available choices as cards | Headless Store Editor view asserts both non-Color grids are inside their existing card, with Color and actions present. |
| Empty Option uses the same card treatment | Headless view asserts the message is visible and that the empty group has no realized grid. |
| Custom Option kind uses the same card treatment | Headless view asserts custom kind label, provider-backed grid, and existing actions. |
| User scans Size values in configured order | Headless view asserts ordered rows, no selection/sort/edit, and existing action availability. |
| User scrolls through a long custom Option value list | Headless view asserts bounded virtualization, scroll realization, and unchanged order. |
| Cards align cleanly in the available width | Headless layout assertions check existing card width and no card overflow at the supported fixture width. |
| Cards stack at narrower supported widths | Headless layout assertions narrow the available region and verify wrapped card bounds. |
| Long content remains readable | Headless row assertions check ellipsis plus full tooltip, AutomationProperties.Name, and HelpText. |

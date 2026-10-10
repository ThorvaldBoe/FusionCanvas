## Context

The Blueprint detail surface in `StoreEditorWindow.axaml` currently renders `BlueprintOfferingCards` as stacked buttons. Each button carries the same core summary (name, fulfillment context, status, setup counts, and readiness), but wrapping long summaries makes the collection vertically expensive and difficult to scan. Opening an Offering is currently a one-click action routed through `StoreManagementViewModel.SelectOfferingCommand`; archived rows have a distinct selection path. The virtual grid component is already imported and used in this window, and requires `IDataProvider` to render rows.

The scope is a UI presentation refactor on the focused Store Editor surface. Existing Blueprint and Store scope, creation, archived visibility, Offering navigation, and read-only safeguards remain authoritative.

## Goals / Non-Goals

**Goals:**

- Align Offering identity, fulfillment, lifecycle status, setup counts, and readiness summaries in grid columns.
- Keep the Open action discoverable and one-click for active and archived Offering rows, with existing selection command semantics.
- Keep every row compact while preserving complete setup and readiness detail through accessible help text and tooltips.
- Preserve provider updates when Offering cards are added, removed, refreshed, or filtered by archived visibility.
- Cover rendered bindings, row content, tooltip/help details, and the Open route with deterministic Avalonia headless tests.

**Non-Goals:**

- No new sorting, filtering, search, editing, row selection, or Offering lifecycle behavior.
- No domain/application/persistence/API changes or data migration.
- No new Appium scenario; the focused collection's framework risks are covered by headless tests.

## Decisions

1. **Use fixed, read-only grid columns and no grid selection.** Put the primary per-row Open button in the first column so it stays visible at the Store Editor's default width. It invokes the existing `SelectOfferingCommand`, avoiding an added selection click and working for active and archived rows. Column sorting and resizing remain disabled; later changes can decide those behaviors explicitly.
2. **Use the existing in-memory provider snapshot pattern.** `StoreEditorWindow` owns `InMemoryDataProvider<BlueprintOfferingCardViewModel>`, binds it to the grid, and resets the provider from the view model's observable cards. Subscribe and detach alongside the window's other collection subscriptions, and reset to an empty snapshot when the data context changes.
3. **Use compact cell text with full help text.** Keep the readiness summary visible in a single line and expose the summary plus every `ReadinessGuidance` item in `ToolTip.Tip` and `AutomationProperties.HelpText`. Setup counts remain visible in their own column; full cell values are available through tooltips where text truncates. Do not make row height depend on message length.
4. **Keep existing list-state controls in place.** Add, Show archived, and the existing empty-state message remain outside the grid. The grid is visible only when Offering cards exist, and archived Store safeguards continue to bind to their current command state.

Alternatives considered: keeping the entire row as a Button is incompatible with a row of independent aligned cells; opening on grid row selection or double-click would weaken the current one-click route; expanding rows would make the viewport and fixed-height virtualization harder to scan; adding sorting/search is explicitly outside this issue's resolved scope.

## Risks / Trade-offs

- **Long columns can create horizontal scrolling at narrow editor widths** → Use fixed, bounded widths, ellipsis, per-cell full text tooltips, and leave horizontal scrolling available; verify geometry in headless layout.
- **The grid's tunneling input handler may affect the Open button** → Exercise the actual button in headless routed-input tests and preserve the existing command parameter; adjust routing only if the test demonstrates interception.
- **Provider snapshots may become stale when archived filtering refreshes cards** → Subscribe to `BlueprintOfferingCards.CollectionChanged`, reset from the latest collection, and test replacement/filter refresh and data-context detachment.
- **Tooltip alone can be inaccessible on some input modes** → Also populate `AutomationProperties.HelpText` and verify its value on realized cells.

## Migration Plan

No data or persisted format changes. Replace the card list markup with a named virtual grid and wire its provider to the existing view model collection. Rollback is a source change reverting the grid/provider and restoring the existing ItemsControl; persisted Store and Offering records are unaffected.

## Open Questions

None. Sorting/searching and further row selection behavior remain future decisions and are not required for this presentation change.

## Implementation Plan

### Affected layers and responsibilities

- **App UI:** `src/FusionCanvas.App/Stores/StoreEditorWindow.axaml` declares the Offering grid, its columns, Open button, and the existing Add/archive/empty controls.
- **App window lifecycle:** `src/FusionCanvas.App/Stores/StoreEditorWindow.axaml.cs` owns the `InMemoryDataProvider<BlueprintOfferingCardViewModel>`, binds it to the named grid, subscribes/unsubscribes to `BlueprintOfferingCards.CollectionChanged`, resets snapshots on refresh and DataContext teardown, and clears on close.
- **App presentation model:** `src/FusionCanvas.App/Stores/BlueprintOfferingCardViewModel.cs` exposes an aggregated full readiness detail string, while retaining existing counts, summary, guidance, and command identity.
- **Tests:** `tests/FusionCanvas.App.Tests/StoreEditorHeadlessTests.cs` verifies the integrated Store Editor workflow and action; add focused Offering-grid view coverage in `tests/FusionCanvas.App.Tests/BlueprintOfferingGridHeadlessTests.cs` or the existing headless test file according to local fixture reuse.
- **No lower layers:** Application/domain services already produce the card data and readiness guidance; they remain unchanged.

### Sequence and edge cases

1. Add a computed full readiness detail value: summary followed by all translated guidance messages, with no duplicate empty lines when guidance is absent.
2. Add a bounded-height `BlueprintOfferingGrid` with aligned Name, Fulfillment, Status, Setup, Readiness, and Open columns. Disable sorting/resizing and grid row selection. Ensure long values are ellipsized visually but remain in tooltip/help text. Open button invokes `SelectOfferingCommand` with that row's card.
3. Bind a window-owned `InMemoryDataProvider<BlueprintOfferingCardViewModel>` and handle collection-change reset and lifecycle cleanup. Preserve the empty state when there are no cards; active/archived filtering remains owned by the current view model.
4. Add a headless view test that opens a populated Store Editor, finds realized Offering rows, checks aligned content and full readiness help text, executes the row Open button, and asserts the same Offering becomes authoritative context. Cover archived row visibility/filtering with existing collaborator setup where feasible, and provider refresh without stale rows.
5. The grid's tunneling pointer handler prevents the embedded button's normal pointer command from reaching it. Route only pointer presses whose visual source or hit-test position resolves to the row's Open button through a window-level handled-events-too handler; execute the button command once and mark the event handled. Keep keyboard activation on the button's normal command path. Cover both routes in the headless view test.
6. Record scenario evidence in `verification.md`, run focused tests, the full solution baseline, strict OpenSpec validation, and changed-scope review; correct failures before checking the tasks complete.

### Acceptance-to-verification mapping

| Acceptance scenario | Planned verification |
| --- | --- |
| Populated Blueprint shows scoped, aligned Offering summaries with no relationship editing | Avalonia headless view test checks grid columns and realized row values in a Store Editor configured for one Blueprint |
| Full readiness summary and guidance remain available in a compact row | Headless test checks concise visible readiness text, tooltip, and `AutomationProperties.HelpText` including each guidance item |
| Archived Offerings appear only when opted in and are labeled Archived | View-model filtering coverage plus headless grid assertions for the archived toggle and row status |
| Empty Blueprint keeps its existing explanation and Add route | Existing Store Editor headless coverage plus a focused empty-state assertion if not already covered |
| Archived Store remains read-only | Existing archived Store command-state/headless tests; changed-scope review confirms Add and mutation bindings are unchanged |
| One-click Open works by pointer/keyboard route and preserves Blueprint/Store context | Headless realized button command execution and existing workspace-switch navigation coverage; include focus/keyboard route if the grid button is focusable in the view |
| Offering draft and discard protection remain unchanged | Existing StoreEditorHeadlessTests draft, focus, and close-guard coverage |

No database migration or compatibility adapter is needed. No user-visible state is persisted by the grid.

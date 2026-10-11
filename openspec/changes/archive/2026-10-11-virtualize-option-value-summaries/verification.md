# Verification — virtualize-option-value-summaries

## Acceptance scenarios

| Scenario | Method | Result and evidence |
| --- | --- | --- |
| User scans available choices as cards | Avalonia headless Store Editor tests | **Passed.** AvailableOptionChoiceCards_UseBorderedCardTreatmentAndStackOnNarrowWidth verifies bordered cards, same-row alignment at 1100 px, and wrapping at the minimum width. NonColorOptionValues_UseBoundedVirtualGridsWithSavedOrderAndAccessibleText verifies Color, Size, and custom cards keep their contents/actions inside their cards. |
| Empty Option uses the same card treatment | Avalonia headless Store Editor test | **Passed.** NonColorOptionValues_UseBoundedVirtualGridsWithSavedOrderAndAccessibleText verifies the empty Option card remains present, its truthful empty message is visible, and no visible summary grid is shown. |
| Custom Option kind uses the same card treatment | Avalonia headless Store Editor test | **Passed.** The same test verifies the Other kind label, a provider-backed grid, and the existing Manage values and overflow actions. |
| User scans Size values in configured order | Avalonia headless Store Editor test | **Passed.** The test supplies values in reverse input order and one archived value, then verifies the active provider follows saved SortOrder, the grid has no selection or sortable/resizable columns, and the existing action bindings remain intact. |
| User scrolls through a long custom Option value list | Avalonia headless Store Editor test | **Passed.** The test starts with 90 custom values, verifies fewer rows are realized than the full result count, scrolls to the last row, and confirms its saved-order value remains unchanged. |
| Cards align cleanly in the available width | Avalonia headless Store Editor test | **Passed.** The existing card journey verifies the Color and Size cards align in one row when space permits. |
| Cards stack at narrower supported widths | Avalonia headless Store Editor test | **Passed.** The existing card journey narrows the window to its minimum width and verifies the cards wrap to separate rows. |
| Long content remains readable | Avalonia headless Store Editor test | **Passed.** The new journey verifies both Size and custom long values are visually ellipsized and their full strings are available as tooltip, AutomationProperties.Name, and HelpText. The existing Color grid regression test verifies its full-name tooltip remains intact. |

## Required validation

| Check | Result |
| --- | --- |
| Focused headless tests: new Size/custom grid journey, card layout journey, Color grid regression theory | **Passed: 4, failed: 0.** |
| dotnet test .\FusionCanvas.sln -m:1 | **Passed: 2,391 tests.** Domain 294; Application 686; Integration 364; App 1,018; UI Description 29. |
| openspec validate --all --strict | **Passed: 90 items, 0 failed.** |
| git diff --check | **Passed.** |

## Limitations

No live desktop or Appium check was run. The changed surface remains in the existing Store Editor workflow and the relevant layout, provider realization, scroll, tooltip, and automation behavior is covered by deterministic Avalonia headless tests. Restore/build output continues to report the repository's existing SixLabors.ImageSharp NU1902/NU1903 advisories; this change adds no packages.

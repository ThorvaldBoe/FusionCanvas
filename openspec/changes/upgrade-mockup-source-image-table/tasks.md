## 1. Protect existing source-image behavior

- [x] 1.1 Review the existing source-image sort, multi-selection, keyboard, active-detail, and archive view-model tests; add focused gaps only where the current contract is not already protected.
- [x] 1.2 Add an Avalonia headless test harness for the rendered Mockup Template source-image grid with deterministic source drafts and an isolated editor window.

## 2. Replace the table presentation

- [x] 2.1 Replace the hand-built row ItemsControl with a fixed-row-height VirtualDataGrid showing File, Applicability, Status, and Action in aligned columns.
- [x] 2.2 Add an App-window-owned InMemoryDataProvider snapshot that tracks the active draft collection and detaches on DataContext replacement and window close.
- [x] 2.3 Preserve external accessible sort headings and refresh the complete provider snapshot after source collection order changes.
- [x] 2.4 Add a visible preview-read warning indicator with full tooltip and automation text; preserve selected, alternating, empty, incomplete, and long-text presentation states.

## 3. Preserve selection and row actions

- [x] 3.1 Route grid row pointer and Enter/Space input to the existing plain, Ctrl, and Shift source-selection behavior while keeping the active detail row distinct from the selected set.
- [x] 3.2 Keep per-row Archive activation isolated from row selection and preserve existing post-archive selection reconciliation.
- [x] 3.3 Add headless coverage for sort direction and selection preservation, pointer and keyboard modifier selection, Archive isolation, warning accessibility, empty/incomplete rows, virtualization scrolling, provider refresh, DataContext replacement, and close cleanup.

## 4. Verify the delivery module

- [x] 4.1 Map every delta-spec and preserved-selection acceptance scenario to focused test or inspection evidence in the change verification record.
- [x] 4.2 Run `dotnet test .\FusionCanvas.sln -m:1` and correct any failures in the changed scope.
- [x] 4.3 Run `openspec validate --specs --strict --no-interactive` and correct any artifact errors.
- [x] 4.4 Review the changed scope for preservation of sorting, selection, keyboard access, warnings, archive behavior, and the lower master-detail workflow; record any optional live visual check separately from deterministic gates.

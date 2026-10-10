# Verification — Design Area Management Grid

## Acceptance criteria

| Scenario | Result | Evidence |
| --- | --- | --- |
| User scans configured Design Areas | PASS | `StoreEditorHeadlessTests.DesignAreaGrid_AlignsSummariesAndKeepsRowActionsTogether` verifies the five headings, aligned five-cell rows, fixed 32 px height, visible primary marker, full tooltips/accessibility names, row command parameters, and action order. |
| User opens the empty Design Area collection | PASS | `StoreEditorHeadlessTests.DesignAreaGrid_EmptyOfferingKeepsAddActionVisible` verifies the empty message, zero provider rows, and visible Add Design Area action. Existing Store command gating remains bound to the same catalog commands. |
| User edits or archives a row | PASS | `StoreEditorHeadlessTests.DesignAreaGrid_RowButtonsSupportPointerAndKeyboardWithExistingFocusBehavior` verifies pointer Edit opens the correct editor, focus returns to the invoking row, pointer Archive opens the confirmation for that row, cancellation preserves the row and restores focus, and keyboard Archive still invokes the command. Existing `DesignAreaManagement_EditDialogPopulatesSavesAndReturnsFocus` and `DesignAreaArchiveCommand_OpensConfirmationWithoutMutationAndCancelRestoresFocus` pass as regression coverage. |
| User reviews Design Areas at minimum window width | PASS | `StoreEditorHeadlessTests.DesignAreaGrid_UsesNormalWidthAndScrollsAtMinimumWidthWithoutHidingAdd` measures the normal viewport and confirms all declared columns fit, sets the Store Editor to its 720 px minimum, verifies horizontal Auto scrolling reaches the Actions column, and checks the collection heading and Add action remain visible. |

## Validation

- Focused Design Area grid and existing Edit/Archive headless regression filter: **6 passed, 0 failed**.
- `dotnet build .\FusionCanvas.sln --no-restore`: **succeeded, 0 errors**.
- `dotnet test .\FusionCanvas.sln`: **2,352 passed, 0 failed** across all solution test projects.
- `openspec validate --all --strict --no-interactive`: **87 passed, 0 failed**.
- `git diff --check`: **passed**.

## Changed-scope review

Changes are limited to Store Editor presentation and its App headless tests, plus this OpenSpec change. The virtual grid consumes the existing Design Area card collection; no Domain, Application, Integration, persistence, schema, or dependency changes were made. Search and sorting remain out of scope. No new Appium journey was added because the workflow and operations remain unchanged and deterministic headless coverage exercises layout, routed input, focus, and scroll behavior.

## Limitations

Restore emitted NU1900 warnings because NuGet vulnerability metadata could not be reached. Existing ImageSharp 3.1.12 advisories were also reported. They did not prevent restore or test execution and are outside this issue's scope.

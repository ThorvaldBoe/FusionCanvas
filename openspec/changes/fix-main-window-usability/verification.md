# fix-main-window-usability Verification

## Environment

- Repository: `C:\Code\oc-workspace-ui-fixes` (worktree, branch `oc/workspace-ui-fixes`)
- Platform: Windows / PowerShell
- Change type: behavior change (Item text autosave restored) plus presentation fixes and a new headless test harness
- Runtime/package changes: added `Avalonia.Headless.XUnit` 12.0.4 (matching the app's Avalonia 12.0.4) to `tests/FusionCanvas.App.Tests`

## Acceptance Evidence

### `listing-inspector` delta scenarios

| Scenario | Method | Result | Evidence |
| --- | --- | --- | --- |
| Field exit persists valid edits | View-model test | Pass | `ItemInspectorViewModelTests.Commit_DesignStagePersistsSharedFieldsWithoutChangingUpstreamMetadata`, `Commit_RaisesSavedEvent` |
| Field exit with no changes performs no write | View-model test | Pass | `ItemInspectorViewModelTests.Commit_NoOpWhenClean` (0 saves, no error) |
| Invalid title reverts while other valid edits persist | View-model test | Pass | `ItemInspectorViewModelTests.Commit_MultiLineTitleRevertsTitleAndPersistsOtherEdits` (title reverted to baseline, Notes persisted, 1 save, inline error); `Commit_MultiLineOnlyTitleSkipsSaveButReportsRevert` (0 saves, error reported) |
| Phrase is normalized to one line | Application test | Pass | `ItemInspectorServiceTests` saves a phrase with line breaks and asserts the persisted phrase is single-line (`"phrase one phrase two"`) |
| Save preserves unrelated metadata and provenance | View-model test | Pass | `ItemInspectorViewModelTests.Commit_DesignStagePersistsSharedFieldsWithoutChangingUpstreamMetadata` asserts `unknown` metadata preserved |
| Commits are serialized against newer state | View-model test + service guard | Pass | `ItemInspectorViewModelTests.Commit_SerializedDrainPersistsLatestEditAndNoStaleOverwrite` (slow service, mid-flight edit, final persisted value is the latest, 2 saves); `IItemInspectorService.SaveStageAsync` `ExpectedCurrentStage` guard rejects stale stage writes |
| Tag changes while a text commit is pending | View-model test | Pass | `ItemInspectorViewModelTests.TagChange_PersistsImmediatelyWithoutReplacingTextDraft` (tag persists, pending text draft retained) |
| User switches context with pending edits (no prompt) | View-model test | Pass | `MainWindowViewModelTests.DirtyInspector_CommitsBeforeTabSwitchWithoutPrompt` (tab switch commits silently, no prompt, draft persisted) |
| Failed commit keeps context and draft | View-model test | Pass | `MainWindowViewModelTests.DirtyInspector_FailedCommitAbortsTransitionAndPreservesDraft` (failing repository, transition aborted, draft + inline error retained, status selector reverted) |
| Context change with a clean draft transitions immediately | Code inspection | Pass | `GuardActiveItemInspectorLeave` builds no commits when `!HasUnsavedChanges`, calls `proceed()` directly |
| Inactive listings read-only / disabled editing controls | View-model test | Pass | `ItemInspectorViewModelTests.Load_ArchivedItemIsReadOnlyWithNoticeAndRestore` |
| Inspector actions sized to command groups, keyboard reachable | Headless view test | Pass | `MainWindowLayoutTests.ItemTextField_LostFocusCommitsEdit` exercises focus/commit wiring; `FilterControls_StayWithinPaneAtMinimumWidth` asserts controls remain reachable |

### No-delta presentation fixes

| Fix | Method | Result | Evidence |
| --- | --- | --- | --- |
| Filter row wraps within the pane at narrow widths | Headless view test | Pass | `MainWindowLayoutTests.FilterControls_StayWithinPaneAtMinimumWidth` (search box, stage, and status selectors stay within pane bounds at 900px window) |
| Details column scrolls as one container at minimum height | Headless view test | Pass | `MainWindowLayoutTests.DetailsColumn_ScrollsWhenContentExceedsMinimumHeight` (extent exceeds viewport at 400px height) |
| Header stage label / status selector spacing | Headless view test | Pass | `MainWindowLayoutTests.HeaderStageLabelAndStatusSelectorHaveHorizontalGap` (gap >= 4px via `ColumnSpacing="8"`) |

### Harness

| Concern | Method | Result | Evidence |
| --- | --- | --- | --- |
| Headless view tests run without an interactive desktop | Command | Pass | `dotnet test .\FusionCanvas.sln` executes `MainWindowLayoutTests` and `HeadlessHarnessTests` headlessly |
| Headless harness reuses the real app builder | Code inspection | Pass | `HeadlessTestApp.BuildAvaloniaApp` calls `Program.BuildAvaloniaApp().UseHeadless(...)` with `WithInterFont()` and the `App` Fluent theme |

## Validation Commands

| Command | Result | Evidence |
| --- | --- | --- |
| `openspec validate fix-main-window-usability --strict` | Pass | Change is valid |
| `openspec validate --all --strict` | Pass | 30 changes/specs passed, 0 failed |
| `dotnet test .\FusionCanvas.sln` | Pass | 437 passed, 0 failed (96 domain, 150 application, 37 integration, 154 app — includes 7 new headless/view-model tests) |

## Data isolation note

Headless view tests construct `new MainWindowViewModel()`, which uses the in-memory sample workspace (`CreateSampleWorkspace`). An earlier iteration of the `ItemTextField_LostFocusCommitsEdit` test mistakenly used `MainWindowViewModel.CreateForDefaultWorkspace()`, which opens the contributor's real on-disk SQLite workspace at `%LocalAppData%\FusionCanvas\workspace.db`, and committed a `"headless edit"` Notes value to one real item before the mistake was caught and corrected. All committed headless/view tests now use the in-memory sample and do not touch the contributor's workspace. The contributor may want to clear that stray Notes value from their real workspace manually.

## Coordination note: `basic-product-creation-workflow`

`basic-product-creation-workflow` is still active (8 verification tasks open). Its delta spec and `design.md` §9 record the explicit-Save model this change reverses; its remaining task 9.8 references "Save/Discard/Cancel focus" which no longer exists for Item text fields. Recommended archive order: let `basic-product-creation-workflow` finish and archive first, then archive this change, re-running strict `openspec validate` at sync time. Whichever syncs last wins the `listing-inspector` requirement text; the RENAMED + MODIFIED deltas here apply cleanly against the current main spec regardless of order, but a concurrent sync of both deltas touching the same requirement names must be sequenced.

## Limitations and Follow-up

- `adopt-headless-view-testing` was a process/specs-only change and intentionally added no harness; this change adds the minimal harness (`Avalonia.Headless.XUnit` + `HeadlessTestApp`) and four representative view tests. Broader view coverage (every inspector interaction, drag/drop, file pickers) remains a follow-up, not a gate for this batch.
- Live desktop verification is optional and ad hoc; it was not needed to verify these deterministic, framework-testable fixes.

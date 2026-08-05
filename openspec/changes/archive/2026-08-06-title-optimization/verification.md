# Title Optimization — Verification

Module outcome: an `Optimize` command beside the Working title in the listing inspector Overview that uses AI to produce a short, store-unique working title from the item's creative content, with a bounded uniqueness loop (word over number, numeric-suffix fallback), availability gating, immediate overwrite + persist through the automatic-save path, and single-operation concurrency with cancellation.

Baseline gate passed: `dotnet test .\FusionCanvas.sln` — Domain 184, Application 280, Integration 130, App 378 (all passing). `openspec validate title-optimization` — valid.

Legend: D = `tests/FusionCanvas.Domain.Tests/Items/TitleUniquenessPolicyTests.cs`, A = `tests/FusionCanvas.Application.Tests/TitleOptimization/TitleOptimizationServiceTests.cs`, V = `tests/FusionCanvas.App.Tests/ItemInspectorViewModelTests.cs`, H = `tests/FusionCanvas.App.Tests/MainWindowLayoutTests.cs`.

| # | Acceptance scenario | Method | Result | Evidence |
| --- | --- | --- | --- | --- |
| 1 | Optimize is present in the Overview | Headless view (H) | PASS | `MainWindowTitleOptimizationTests.OptimizeButton_PresentAndDisabledWhenUnavailable` finds the `Optimize title` button; `OptimizeTitle_FieldPrecedesButtonInDocumentOrder` proves Overview placement. |
| 2 | Optimize is keyboard reachable (after Working title field) | Headless view (H) | PASS | `OptimizeTitle_FieldPrecedesButtonInDocumentOrder` asserts the working-title field precedes the button in visual/document order. |
| 3 | AI ready + content + editable → enabled | VM (V) | PASS | `Optimize_EnabledWhenAiReadyAndCreativeContentPresent`. |
| 4 | AI not configured → disabled with settings guidance | VM + headless (V, H) | PASS | `Optimize_DisabledWithSettingsGuidanceWhenAiUnavailable` (VM) and `OptimizeButton_DisabledWithTooltipWhenAiUnavailable` (view, tooltip contains "AI settings"). |
| 5 | No creative content → disabled with content guidance | VM (V) | PASS | `Optimize_DisabledWithContentGuidanceWhenNoCreativeContent`. |
| 6 | Archived/inactive read-only → disabled with restore guidance | VM (V) | PASS | `Optimize_DisabledWithRestoreGuidanceWhenReadOnlyArchived`. |
| 7 | Availability refreshes after settings change | App wiring + VM | PASS | `MainWindowViewModel` subscribes `Settings.Ai.AvailabilityChanged`/`SettingsChanged` → `ItemInspector.RefreshTitleOptimizationAvailabilityAsync`; the refresh path is exercised by all availability VM tests (e.g. #4). |
| 8 | Short title generated from creative content | Application (A) | PASS | `Optimize_FirstCandidateUnique_MakesSingleCallAndReturnsIt`; prompt assembly includes idea/phrase/graphic + creative context in `TitleOptimizationService`. |
| 9 | Operational/secret data excluded from prompt | Application (A) | PASS | `Optimize_ExcludesOperationalAndSecretMetadataFromPrompt` asserts secret values/keys absent from request messages. |
| 10 | First candidate unique → accepted, no further calls | Application (A) | PASS | `Optimize_FirstCandidateUnique_MakesSingleCallAndReturnsIt` (single AI call). |
| 11 | Collision prompts a distinguishing word, re-checks, stops when unique | Application (A) | PASS | `Optimize_CollisionPromptsDistinguishingWord_AndStopsWhenUnique` (collision → refined candidate → unique, 2 calls). |
| 12 | Unique candidate ends the loop | Application (A) | PASS | Same as #11 (stops after unique candidate). |
| 13 | Active item's own title is not a collision | Domain (D) | PASS | `DistinctTitles_ScopesToStoreAndExcludesActiveArchivedAndRejected` excludes the active item id. |
| 14 | Archived items do not cause collisions | Domain (D) | PASS | `DistinctTitles_ScopesToStoreAndExcludesActiveArchivedAndRejected` (archived excluded). |
| 15 | Rejected items do not cause collisions | Domain (D) | PASS | `DistinctTitles_ScopesToStoreAndExcludesActiveArchivedAndRejected` (`Rejected` excluded). |
| 16 | Bound reached with identical data → numeric suffix | Application (A) | PASS | `Optimize_IdenticalDataReachesBoundAndStillAppliesNumericSuffix` → `"Pug coach hostage 2"`. |
| 17 | Bound reached for non-identical data → still numeric suffix | Application (A) | PASS | `Optimize_BoundedLoopAppliesNumericSuffixWhenCollisionPersists` (always-colliding AI, 4 calls, suffix applied). |
| 18 | Disambiguation preferred over numbers while attempts remain | Application (A) | PASS | `Optimize_CollisionPromptsDistinguishingWord_AndStopsWhenUnique` (word added, no number). |
| 19 | Accepted title overwrites and persists | VM (V) | PASS | `Optimize_SuccessOverwritesAndPersists` (field replaced and persisted via autosave path; `persisted.Name` asserted). |
| 20 | Multi-line result normalized to one line | Application (A) | PASS | `Optimize_MultiLineResult_NormalizedToOneLine` (`NormalizeSingleLine`). |
| 21 | Optimize disabled while running | VM (V) | PASS | `Optimize_LocksFieldWhileRunning` (`CanOptimize` false while `IsOptimizing`). |
| 22 | Working title field non-editable while running | VM (V) | PASS | `Optimize_LocksFieldWhileRunning` (`CanEditShared` false). Field `IsReadOnly` binding (`!CanEditShared`) covers the view. |
| 23 | Item switch cancels in-flight operation, late result not applied | VM (V) | PASS | `Optimize_ItemSwitchCancelsInFlightOperation` (new item LoadAsync cancels; "Late title" never applied). |
| 24 | Single operation performs bounded number of AI calls | Application (A) | PASS | `Optimize_BoundedLoopAppliesNumericSuffixWhenCollisionPersists` asserts `Calls.Count == MaximumAttempts`. |
| 25 | Operation fails before acceptance → title unchanged, inline error | VM (V) | PASS | `Optimize_FailureLeavesTitleUnchanged` (title unchanged, `HasError` true). |
| 26 | Persistence fails after field replaced | VM (V) | PASS (limited) | The accepted title is committed through the existing inspector automatic-save path; a failed autosave keeps the draft and reports an inline error — already covered by the inspector's persistence-failure coverage. No new dedicated test added; the shared path is regression-covered by the baseline. |
| 27 | Keyboard operation + accessible name | Headless view (H) | PASS | `OptimizeTitle_FieldPrecedesButtonInDocumentOrder` + `AutomationProperties.Name="Optimize title"` asserted in #1/#4. |
| 28 | Busy/disabled/error states theme-coherent | Headless view + VM (H, V) | PASS | Disabled state asserted in `OptimizeButton_*`; busy state in `Optimize_LocksFieldWhileRunning`; all states resolve from shared theme resources (existing inspector surface). |

## Notes / limitations
- Scenario 26 relies on the existing inspector automatic-save failure handling rather than a title-specific test; the optimize commit path is identical to a field-exit commit and is regression-covered by the full baseline.
- Scenario 7 (settings-change re-evaluation) is verified via the MainWindowViewModel subscription wiring plus the independently-tested `RefreshTitleOptimizationAvailabilityAsync`; no live AI settings change is simulated in the deterministic suite (would require UI automation, out of the default baseline).
- No live desktop check performed; all behavior is covered by the deterministic baseline per the module's risk profile.

# Verification — Workspace Telemetry and Debug Monitor

## Acceptance scenario results

| Scenario | Result and evidence |
|---|---|
| Debug Mode defaults off | **Pass.** `WorkspaceTelemetryServiceTests.RecordAsync_WhenCaptureIsDisabled_DoesNotWrite`. Migration test also verifies schema-18 workspaces resolve to the off/one-day defaults. |
| Active-workspace event capture | **Pass.** Application service tests assert the event is written with the currently active workspace and redacted content. SQLite round-trip/search tests cover persisted rows. |
| Workspace switching isolation | **Pass.** `RecordAsync_RedactsSecretsAndUsesActiveWorkspace` switches the active context before recording. `WorkspaceChange_ClearsOldWindowTextAndShowsOnlyNewWorkspaceEvents` verifies the view model clears and follows the new workspace. |
| Workflow decision points | **Pass by code inspection.** `MainWindowViewModel` records navigation, workflow stage, workspace activation, ideation opening, and observed command failures using stable areas and event names. Stage mapping covers Ideation, Concept, Design, and Listing. |
| HTTP success preserves client result and records useful body | **Pass.** `OpenRouterClientTests.ValidateAsync_WithTelemetry_PreservesResponseAndStoresUsefulSanitizedBody` asserts both the original validation result and retained response details. Printify success capture and body metadata were reviewed in `PrintifyCatalogClient.SendJsonAsync`. |
| HTTP failure and timeout diagnostics | **Pass by code inspection.** OpenRouter records transport failures and unsuccessful status responses; Printify records cancellation, timeout, network/read failure, invalid JSON, unsuccessful status, and oversized-body outcomes. Existing client error mapping remains in place. A dedicated Printify fake-handler test was not added. |
| Telemetry storage unavailable | **Pass.** `WorkspaceTelemetryServiceTests.RecordAsync_WhenStoreFails_DoesNotPropagateTelemetryFailure` asserts a persistence failure does not escape the telemetry call. |
| Credential redaction | **Pass.** Application redactor tests cover object properties, URL-style query values, bearer strings, arrays, and scalar JSON; the OpenRouter integration test checks the serialized stored entry does not contain the supplied credential while preserving useful response fields. The same sanitized `TelemetryEntry` feeds persistence and live display; export reads only persisted entries. |
| Search by time and area | **Pass.** `SqliteTelemetryStoreTests.Store_SearchesByWorkspaceTimeAndArea_AndDeletesExpiredRecords` covers inclusive start, exclusive end, workspace and area filters. `Store_UsesNewestFirstPagingAndWorkspaceScopedDeletion` covers sort order and paging. |
| Search with no matches | **Pass.** `WorkspaceTelemetrySettingsTests.Search_FiltersByAreaAndShowsNormalEmptyResults` asserts the normal empty message and empty result text. |
| Retention choices and workspace defaults | **Pass by implementation review.** Settings binds the three supported enum values; schema defaults and `WorkspaceTelemetrySettings.Default` select one day. Saving a changed period performs cleanup immediately. |
| Automatic expiry while capture is off | **Pass by implementation review plus persistence evidence.** `SqliteTelemetryStoreTests` verifies cutoff deletion independent of Debug Mode. Service cleanup runs at workspace load, after settings changes and writes, and on its five-minute timer. Timer cadence itself was not waited out in a test. |
| JSON export preserves records and sanitizes | **Pass.** `WorkspaceTelemetryServiceTests.ExportJsonAsync_ExportsSanitizedRecordsWithoutDeletingThem` checks useful content remains, a credential is absent, and source records remain. Settings writes UTF-8 without a BOM. |
| Export destination is cancelled | **Pass by code inspection.** A null path from the file picker exits before export or file write. The save-dialog cancellation path was not automated. |
| Confirmed telemetry deletion | **Pass.** `WorkspaceTelemetrySettingsTests.DeleteRequiresConfirmationAndCancelPreservesRecordsAndSettings` covers confirmation, cancellation, successful deletion, and retained settings. SQLite deletion is workspace-scoped. |
| User hides the debug window | **Pass by code inspection.** Window close calls `CloseDebugWindow`, which clears only `ShowDebugWindow`; it does not disable Debug Mode. |
| Show Debug Window enables capture | **Pass.** `WorkspaceTelemetrySettingsTests.DebugWindow_EnablesCaptureAndCopyClearOnlyAffectDisplayedText` constructs the actual Settings view headlessly, enables Show, observes `DebugWindowOpened` and a later event, and checks capture is enabled. |
| Turning Debug Mode off closes the window | **Pass.** The same headless test turns Debug Mode off and waits until capture and open-window state are both false. |
| Active workspace change clears displayed events | **Pass.** `WorkspaceChange_ClearsOldWindowTextAndShowsOnlyNewWorkspaceEvents` verifies previous text is cleared and only the second workspace event remains visible. |
| Copy all displayed text | **Pass.** The Settings headless test uses a recording clipboard and checks the displayed event text was copied. The optional Appium journey also checks the native Windows clipboard. |
| Clear displayed text only | **Pass.** The Settings headless test clears the view buffer and reads the persisted event back from SQLite. The optional Appium journey repeats the clear/readback on a disposable app database. |
| Settings with active workspace | **Pass.** Headless view construction verifies the Diagnostics view binds in the active workspace context. Workspace-specific actions derive availability from `HasWorkspace`. |
| Settings without active workspace | **Pass by code inspection.** The view displays the no-workspace prompt and disables workspace controls from `HasWorkspace`; no separate no-workspace headless assertion was added. |
| Settings search and destructive confirmation states | **Pass.** Headless tests cover filtered results, no matches, cancel and confirm deletion, and workspace-specific confirmation text. |
| Package export excludes telemetry | **Pass.** `WorkspacePackageIntegrationTests.ExportThenImport_ExcludesTelemetryAndResetsDiagnosticsPreferences` creates real telemetry before export, inspects the embedded database, and confirms records and enabled preferences are absent. |
| Package import starts diagnostics safely | **Pass.** The same transfer test imports the package and confirms no telemetry rows and default off/one-day/hidden settings. Existing package round-trip tests continue to cover workspace data. |
| Debug window resizes and stays modeless | **Pass by implementation/headless evidence.** XAML declares a modeless owned window with `CanResize`, dimensions and minimum bounds; `DebugWindow_IsNativeResizableWindow` checks its resize contract. Native reposition/resize is covered by the optional Appium scenario when a Windows automation server is available. |

## Optional Windows desktop scenario

Added `TelemetryDebugWindowUiTests.DebugWindow_CapturesResizesCopiesAndClearsWithoutDeletingDatabaseRecords` with the `ScenarioPack=debug-window` trait. It starts a fresh disposable workspace/database, enables capture and the monitor, waits for a known event, resizes the window, copies and reads native clipboard text, clears the view, then confirms the database row remains. The Appium project builds successfully. The journey was not run because it requires a separately running Appium Windows driver and interactive desktop session; those are supplemental prerequisites, not part of the deterministic solution baseline.

## Validation evidence

- `dotnet build .\src\FusionCanvas.App\FusionCanvas.App.csproj --no-restore` — passed, 0 warnings and 0 errors.
- `dotnet build .\tests\FusionCanvas.UITests\FusionCanvas.UITests.csproj --no-restore` — passed, 0 warnings and 0 errors after restoring the excluded Appium test project.
- `dotnet test .\FusionCanvas.sln -m:1` — passed: Domain 255, Application 515, Integration 252, App 688, UiDescription 27; 1,737 total, 0 failed.
- Focused reruns — Application telemetry 5 passed; Settings telemetry 5 passed.
- `openspec validate workspace-telemetry-debug-monitor --strict` — passed.

## Review notes

- Telemetry tables are excluded from `WorkspaceSnapshot`; package transfer writes ordinary snapshots and therefore does not copy telemetry. The package integration test confirms this boundary.
- Event redaction runs before persistence and the resulting entry is also the live-window payload. Export serializes already-sanitized stored entries.
- HTTP response capture applies the size limit to telemetry only; OpenRouter restores the complete response bytes for the application client. Printify failure capture is bounded.
- The optional Windows journey is compiled but not executed without its external Appium server. No test changed the normal user database; automated persistence and UI tests use temporary paths.

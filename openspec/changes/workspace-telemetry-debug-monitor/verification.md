# Verification Plan — Workspace Telemetry and Debug Monitor

This plan maps every acceptance scenario to deterministic or supplemental evidence. Implementation completion must replace planned methods with actual test names/results and record command output.

| Capability / scenario | Planned evidence |
|---|---|
| Telemetry / Debug Mode has not been enabled | Application service test: no record is persisted by a disabled workspace runtime; Settings headless test asserts default toggle off. |
| Telemetry / Event is recorded for the active workspace | Application service test plus SQLite integration insert/readback asserting workspace, UTC time, area, event, and context fields. |
| Telemetry / Workspace switching keeps telemetry isolated | Workspace runtime/Application test plus SQLite query asserting records stay in their owning databases and do not cross workspaces. |
| Telemetry / Workflow decision point is reached | Instrumentation test for representative event per major first-pass area; assert stable area/event/outcome identifiers. |
| Telemetry / HTTP operation succeeds | Fake-handler tests for OpenRouter and Printify asserting method, sanitized endpoint, status, duration presence, and request/response body while preserving returned result. |
| Telemetry / HTTP operation fails | Fake-handler tests for timeout, network failure, and unsuccessful response asserting sanitized failure event and unchanged existing error classification. |
| Telemetry / Telemetry persistence is unavailable | Application test with failing telemetry store asserting the user operation result is preserved and no recursive telemetry calls occur. |
| Telemetry / HTTP credentials are present | Redactor unit tests and persisted/exported/live-event readback tests assert known secret values are absent while non-secret body text remains. |
| Telemetry / Search by time and area | Application and SQLite tests for start-inclusive/end-exclusive boundaries, workspace/area filters, newest-first stable order, and bounded paging. |
| Telemetry / Search returns no matches | Settings view-model/headless test asserts normal empty state, not error state. |
| Telemetry / User selects a retention period | Application and Settings tests for all three choices and saved workspace-specific selection; SQLite test verifies immediate expiry after shortening retention. |
| Telemetry / Expired records are cleaned up | SQLite tests cover startup, post-write, periodic/manual scheduler trigger, each retention age, non-expired preservation, and cleanup while Debug Mode is off. |
| Telemetry / Export retained telemetry | JSON codec and integration tests assert all retained workspace records, UTF-8 JSON, sanitized body content, and unchanged database. |
| Telemetry / User cancels telemetry export | Application export workflow test asserts no file is written and source records remain unchanged. |
| Telemetry / User confirms telemetry deletion | Application/SQLite test asserts all active-workspace telemetry is deleted while settings and other workspace records remain unchanged. |
| Telemetry / User cancels telemetry deletion | Settings view-model/headless test asserts records remain unchanged after cancellation. |
| Telemetry / User opens the debug window | Settings and Avalonia headless tests assert Show enables Debug Mode, opens modeless resizable state, and receives subsequent event updates. |
| Telemetry / User turns off Debug Mode | Settings/window headless tests assert Show turns off, window closes, and later telemetry calls are not persisted. |
| Telemetry / User hides the debug window | Settings/window headless test asserts closing hides the window while Debug Mode continues capture. |
| Telemetry / Active workspace changes | Workspace-switch headless test asserts old buffer clears and only new workspace events appear; capture follows new workspace settings. |
| Telemetry / User copies debug output | Clipboard fake and headless command test assert the entire displayed text in order is copied and SQLite is unchanged. |
| Telemetry / User clears debug output | Headless command plus SQLite integration test asserts buffer clears, stored records remain searchable/exportable, and later events appear. |
| Application Settings / Active workspace is available | Settings headless view test asserts Diagnostics controls bind to active workspace and availability follows state. |
| Application Settings / No workspace is active | Settings headless view test asserts explanatory text and disabled workspace-specific controls. |
| Application Settings / User searches telemetry | Settings view-model/headless test asserts filters reach query and results/empty/error states render. |
| Application Settings / User confirms destructive deletion | Settings headless test asserts workspace-specific confirmation, cancel behavior, and confirmed delete command path. |
| Workspace Transfer / Workspace package is exported | Transfer integration test inspects package contents for absence of telemetry and confirms source records remain. |
| Workspace Transfer / Workspace package is imported | Transfer integration test asserts no telemetry rows are restored and Debug Mode/Show Debug Window start off. |

## Supplemental desktop journey

One optional Windows Appium journey is assigned to a `debug-window` scenario pack, with a fresh disposable workspace/database per pack. The journey enables Debug Mode, opens and resizes the modeless window, observes a deterministic event, copies and checks clipboard text, clears the window, then verifies the record remains in SQLite. This is supplemental evidence for native window placement/resizing and clipboard integration; it is not a required baseline gate.

## Required completion commands

- `openspec validate --strict`
- `dotnet test .\FusionCanvas.sln -m:1`

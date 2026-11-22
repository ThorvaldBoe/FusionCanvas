## Context

The main shell already has a global utility rail and a Settings action. The repository already has a public GitHub Bug Report Issue Form, an application version diagnostic block, and workspace-scoped telemetry with Debug Mode, redaction, retention, search, export, and whole-workspace deletion. Current telemetry export writes all retained records; the service has no bounded report-capture deletion operation. The report path should reuse GitHub as the public intake and keep the final submission in the user's hands.

The primary workflow is occasional: a user encounters a problem, prepares a report, and may reproduce it with Debug Mode enabled. The main workspace should only carry a compact rail action; instructions, capture controls, screenshot preview, and copy feedback belong in a focused dialog.

## Goals / Non-Goals

**Goals:**

- Make a structured public bug report easy to start from the utility rail.
- Help users write useful reproduction details and provide optional screenshot and diagnostic evidence.
- Keep diagnostics and screenshots local until the user deliberately copies, pastes, or uploads them.
- Let report capture be stopped and cleared without deleting older workspace telemetry.
- Preserve GitHub as the final review and submit step.

**Non-Goals:**

- Anonymous direct issue submission, a new intake backend, or GitHub credentials in the desktop application.
- Automatic transmission of logs, screenshots, issue text, or workspace data.
- Creating feature requests, support tickets, or private security reports through this surface.
- Automatic diagnosis, issue triage, code changes, releases, or installation of fixes.
- Changing general telemetry retention or replacing the existing telemetry Debug Monitor.

## Decisions

### Use a focused preparation dialog and the existing GitHub form

The utility rail opens a small preparation dialog. It explains how to describe where the issue occurred, what the user was trying to do, what happened, and what they expected. A Report Issue action opens the existing public Bug Report form in the default browser. The dialog stays open so the user can stage or locate evidence while completing the form.

No report body is placed in URL query parameters, avoiding descriptions in browser history. Version and commit information remain available through the existing diagnostic block and issue form fields. Browser launch failures leave the user's prepared state intact and provide a copyable issue-form address.

Alternative: create issues directly with an API token — rejected because a public desktop binary cannot safely hold a repository credential and anonymous intake would require a separately operated abuse-protected service.

### Make Debug Mode an explicit workspace control with report-scoped cleanup

The report dialog shows the active workspace and an on/off Debug Mode control reflecting the workspace's real setting. Starting a report capture records a timestamp boundary. Copy diagnostics queries only entries from that capture and the latest ten minutes. The copy payload uses a bounded allowlist of sanitized diagnostic fields and excludes request/response bodies and raw provider payloads. The copy command reports success by immediately changing its label to “Copied to clipboard”; failures leave the clipboard untouched.

Turning Debug Mode off stops new capture but does not itself delete records. After successful copy, the dialog offers an explicit clear action scoped to telemetry recorded since this report capture began. Existing records before the capture boundary are retained. If the user turns capture off before copying, the report entries remain available until copied or explicitly cleared. Workspace changes terminate the capture and clear only that capture's entries to prevent cross-workspace export.

This requires an application-level time-bounded telemetry query/delete contract and an Integration implementation in SQLite. Deletion is scoped by workspace and the report's start/end timestamps; it must not call the existing whole-workspace delete operation. Search remains bounded and ordered so the latest ten minutes cannot exceed the existing maximum page size silently; if a maximum is reached, the copied output states that it was truncated.

Alternative: clear all telemetry when Debug Mode is switched off — rejected because it can delete diagnostics that predate the report. Alternative: leave report entries until general retention expires — rejected because it does not meet the user's request to return to a no-capture/no-report-data state.

### Stage clipboard screenshots as separate temporary files

Paste screenshot is an explicit clipboard read. It accepts an image, writes a bounded PNG copy to an app-owned temporary report directory, and shows a preview. Replacing or removing the image updates and deletes the prior temporary copy. The dialog provides a clear way to open the temporary file's location so the user can attach it through the GitHub form upload control. The browser handoff does not close the dialog or upload the file. Cancel/close and application shutdown clean up the staged file; a startup cleanup removes stale report files left by an unexpected exit.

Alternative: keep the image only in the Windows clipboard — rejected because copying diagnostics would replace it. Alternative: automatically upload to GitHub — rejected because it would require credentials or another service and would transmit evidence before GitHub review.

The GitHub Bug Report form gains an optional upload field and concise public-visibility/sensitive-content guidance. The repo is public, so uploaded image URLs are not private.

### Keep report orchestration in App and telemetry storage behind existing contracts

The feature is coordinated by App presentation code: a report view model owns UI state, clipboard interactions, temporary-file lifecycle, and browser handoff. It consumes existing `ITelemetryService` behavior through a narrowly extended application contract for bounded reads/deletion. SQLite remains in Integration. No Domain behavior or new package is required.

Likely code areas:

- `src/FusionCanvas.App/Views/MainWindow.axaml` and `MainWindow.axaml.cs`: utility rail action and dialog opening/focus behavior.
- New `src/FusionCanvas.App/IssueReporting/` dialog, view model, screenshot clipboard adapter, and temporary attachment store.
- `src/FusionCanvas.Application/Telemetry/ITelemetryService.cs`, `ITelemetryStore.cs`, and `WorkspaceTelemetryService.cs`: bounded report query and capture-range deletion.
- `src/FusionCanvas.Integration/Persistence/SqliteTelemetryStore.cs`: timestamp/workspace-scoped delete query.
- `src/FusionCanvas.App/AppServicesFactory.cs` or existing MainWindow composition: pass telemetry/workspace and version diagnostics dependencies.
- `.github/ISSUE_TEMPLATE/bug_report.yml` and the GitHub issue workflow delta spec.
- `tests/FusionCanvas.App.Tests`, `tests/FusionCanvas.Application.Tests`, and `tests/FusionCanvas.Integration.Tests` for UI, service, and persistence behavior.

### User-facing verification is deterministic

Use framework-free view-model tests for capture state, workspace changes, clipboard copy results, cleanup gating, and browser failures. Use application and integration tests for bounded retrieval, date boundaries, workspace scoping, ordering, and range deletion. Avalonia headless tests cover utility rail binding, dialog construction, capture toggle, screenshot preview/removal, copy-label transition, and focus/cancel behavior. No Appium journey is warranted: the handoff opens the system browser, but issue submission is an external site interaction and is not automated by the app. Manual live-browser testing is optional and must not submit a real issue.

## Risks / Trade-offs

- **[Telemetry may include private workspace activity even after secret redaction]** → Keep the capture explicit, tell users the report is public, copy only allowlisted safe fields, exclude request/response payloads, and provide a clear-before-submit action.
- **[A delete-range bug could remove older diagnostics]** → Require workspace plus capture-start/end bounds in the service/store contract and prove boundary preservation in isolated persistence tests.
- **[A user closes the app before attaching a screenshot]** → Keep the file while the report dialog is open, clean it on normal close, and remove stale report files on next startup.
- **[The Windows clipboard does not contain an image or contains an unsupported format]** → Show a recoverable status, leave any existing staged screenshot unchanged, and never replace clipboard content on failure.
- **[A report capture creates too many events]** → Bound the copied interval and result count, and disclose truncation rather than silently omitting data.
- **[GitHub is unavailable or user is not signed in]** → Preserve the dialog, screenshot, and copied diagnostics; explain that GitHub sign-in is required and offer retry/copyable URL.

## Migration Plan

No persisted schema migration is required if telemetry deletion uses the existing timestamp and workspace columns. Existing settings and telemetry remain compatible. The new form field is optional, so existing reporters retain the current workflow. Rollback removes the rail/report UI and the added GitHub field; no report service or user data migration is involved. Temporary staged images are disposable and must be cleaned on rollback/application shutdown.

## Open Questions

None. Use a ten-minute diagnostics window, preserve the report dialog through browser handoff, stage screenshots separately from the clipboard, and clear only telemetry captured by the report session after an explicit user action.

## Implementation Plan

1. Extend telemetry query/store/service contracts with capture-range reads and deletion. Query by active workspace and inclusive start/exclusive end boundaries; order newest first and cap at a documented count. Return a truncation indicator or equivalent so the report cannot imply completeness after reaching the cap. Build copied text from safe fields only and apply existing redaction before serialization.
2. Add report view model and dialog in App. Model states: idle/off, capturing/on, copied, cleared, no records, clipboard error, screenshot staged, screenshot error, browser handoff error, and workspace changed. Track the workspace identity and capture start when capture is initiated. Keep toggling off separate from clearing. Permit clear only after copy or a distinct explicit discard action; clear exactly the session range and then reset the capture state.
3. Add clipboard-image reading and staged-file lifecycle behind small App-owned collaborators. Validate format and maximum size before PNG conversion/write. Preview the image, support replace/remove, expose the staged file location for GitHub attachment, and clean it at dialog close/startup after interruption.
4. Add the utility rail button and compose dependencies through existing app services. Use the existing window coordinator pattern for one dialog, focus activation, and focus return. Open the configured public bug-report form using the platform URI launcher; do not include user text or screenshots in the URL.
5. Update the GitHub Bug Report issue form with an optional upload field and public-visibility/sensitive-content guidance. Preserve current required operating system, expected behavior, actual behavior, reproduction steps, frequency, labels, and feature request behavior.
6. Add focused tests per layer and Avalonia headless tests for the dialog and rail. No Appium journey is planned; any optional live check uses a disposable local screenshot and stops before issue submission.
7. Run strict OpenSpec validation, criterion-level verification, changed-scope review, and `dotnet test .\\FusionCanvas.sln`.

## Acceptance-to-Verification Mapping

| Acceptance area | Planned evidence |
| --- | --- |
| Utility rail action, dialog guidance, public warning, browser handoff, focus, and recoverable errors | App view-model tests plus Avalonia headless MainWindow/report dialog tests |
| Debug Mode reflects active workspace and is explicitly switched on/off | Framework-free App tests and headless control-state test |
| Ten-minute report window and capture-scoped copy | Application telemetry service tests with deterministic clock and fake store |
| Redacted allowlist excludes bodies and private payload fields | Focused formatter/service tests with sensitive sentinel values |
| No records and clipboard failure preserve existing clipboard | View-model tests with fake clipboard |
| Copy label changes to “Copied to clipboard” on successful action | App view-model and headless binding/state test |
| Report session clear preserves pre-capture and other workspace entries | Application and isolated SQLite integration tests at range boundaries |
| Workspace switch ends/clears correct report capture only | View-model/service tests with two workspaces |
| Screenshot paste, preview, replace/remove, upload handoff, cleanup | Clipboard adapter/store tests plus Avalonia headless dialog tests |
| Issue form optional upload and public warning | YAML/schema validation or repository fixture check plus spec review |
| Full change remains compatible | `dotnet test .\\FusionCanvas.sln`, strict OpenSpec validation, changed-scope drift review |

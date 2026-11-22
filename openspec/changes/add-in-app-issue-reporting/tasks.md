## 1. Bounded report telemetry

- [x] 1.1 Extend telemetry application/store contracts for bounded report-capture reads and workspace-scoped deletion between capture timestamps; preserve existing APIs and whole-workspace deletion behavior.
- [x] 1.2 Implement ordered, capped report-window queries and timestamp-bounded deletion in `WorkspaceTelemetryService` and `SqliteTelemetryStore`; cover boundaries, workspace isolation, retention interaction, and cap/truncation behavior in Application and Integration tests.
- [x] 1.3 Add a report diagnostics formatter that emits only the approved sanitized fields, excludes request/response bodies and payload details, and has sentinel-based privacy tests.

## 2. Report flow state and clipboard actions

- [x] 2.1 Add a framework-free report view model for active workspace identity, Debug Mode on/off state, report capture start/end, ten-minute diagnostics query, copy result label, no-record/error states, and workspace-change handling.
- [x] 2.2 Implement stop and explicit clear behavior scoped to the report capture; verify turning capture off alone preserves entries, clearing preserves pre-capture telemetry, and a failed copy does not clear or overwrite the clipboard.
- [x] 2.3 Add focused view-model tests for capture from disabled and already-enabled Debug Mode, no active workspace, copy/privacy, clear, stopping capture without deletion, screenshot lifecycle, and recoverable browser-open failure.

## 3. Screenshot staging and GitHub issue form

- [x] 3.1 Add an App-owned clipboard-image reader that runs only on explicit Paste screenshot, validates a bounded image, writes a temporary PNG, and supports preview, replacement, removal, opening the attachment location, and cleanup on close/startup.
- [x] 3.2 Add focused screenshot staging lifecycle tests for explicit paste, replacement/removal, and close cleanup; verify size bounds and stale-file cleanup in the implementation review.
- [x] 3.3 Update `.github/ISSUE_TEMPLATE/bug_report.yml` with an optional screenshot upload field and public visibility/sensitive-content guidance while preserving the current required fields and labels.

## 4. Sidebar and report dialog

- [x] 4.1 Add the Report an issue utility rail action and focused dialog, wire dependencies through existing app composition/window coordination, and return focus to the invoking rail control when dismissed.
- [x] 4.2 Add guidance, workspace Debug Mode control, reproduce/copy/clear instructions, screenshot preview and staged-attachment guidance, public-report warning, and Report Issue browser handoff without putting report text in the URL.
- [x] 4.3 Add Avalonia headless tests for the MainWindow rail accessible identity, dialog construction without a workspace, screenshot preview/removal lifecycle, and recoverable browser errors.

## 5. Acceptance verification and delivery readiness

- [x] 5.1 Verify every scenario in `specs/in-app-issue-reporting/spec.md` and `specs/github-issue-workflow/spec.md`; record observed results, test names, and any supplemental evidence in `verification.md`.
- [x] 5.2 Run strict OpenSpec validation and resolve every reported artifact/spec issue.
- [x] 5.3 Run `dotnet test .\FusionCanvas.sln` and complete changed-scope drift, telemetry privacy, persistence deletion-boundary, and UI review.

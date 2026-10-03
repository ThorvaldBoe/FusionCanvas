## Why

When a defect occurs, developers and users currently lack a durable, workspace-scoped record of the application decisions and service exchanges that led to it. A Debug Mode with searchable local telemetry and a live monitor makes failures easier to reproduce and diagnose while keeping capture under the user's control.

## What Changes

- Add workspace-level Debug Mode and Show Debug Window settings, with Debug Mode off by default and a one-day telemetry retention default (one hour and one week are also available).
- Capture timestamped events grouped by stable application-area identifiers, including major workflow decisions, failures, and external service request/response details with request and response bodies; redact credentials and recognized secrets.
- Store telemetry in the workspace SQLite database, support filtering by time and area, and provide explicit export and deletion actions in Settings.
- Add a resizable modeless debug window that streams newly captured telemetry. Enabling it also enables Debug Mode; disabling Debug Mode closes and disables the window. Copy copies the displayed buffer; Clear empties only that buffer and leaves database records unchanged.
- Expire telemetry automatically according to the workspace retention setting, clean up at startup and during the session, and omit telemetry records from workspace package export and import.

## Capabilities

### New Capabilities
- `workspace-telemetry`: Workspace-scoped diagnostic event capture, SQLite storage, retention, query, export, deletion, and live debug monitoring.

### Modified Capabilities
- `application-settings`: Add workspace-level Debug Mode, retention, Show Debug Window, and telemetry management controls.
- `workspace-transfer`: Explicitly exclude diagnostic telemetry records from portable workspace packages.

## Impact

- **Application:** telemetry contracts and use cases for recording, searching, exporting, deleting, and retention; concise event calls at core workflow and failure-prone boundaries.
- **Integration:** workspace SQLite schema migration, indexed telemetry persistence, bounded retention cleanup, secret redaction, and telemetry export codec.
- **App:** Workspace Settings controls and telemetry management, active-workspace-aware service composition, and a resizable floating debug window with copy and display-buffer clear actions.
- **Workspace transfer:** package readers/writers continue transferring workspace data without diagnostic records.
- **Verification:** focused Domain/Application/Integration tests, Avalonia headless coverage for Settings and debug-window control behavior, criterion-level evidence, strict OpenSpec validation, and the required solution test baseline.

The module is cohesive because its settings, persistence, capture API, and monitor all serve one outcome: controlled diagnosis of a workspace's application behavior. The first pass instruments major workflow decision points and current external service boundaries; it does not attempt exhaustive tracing of every internal method or introduce remote analytics.

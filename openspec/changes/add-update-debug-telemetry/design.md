## Context

The update surface maps several distinct package failures to one user-facing verification message. The existing workspace telemetry service already provides Debug Mode gating, workspace isolation, retention, secret redaction, and a live Debug Monitor. Update checks and downloads currently do not send events to that service.

## Goals / Non-Goals

**Goals:**

- Make update-check failures and package-download outcomes visible in the existing Debug Monitor.
- Identify which bounded package stage failed without logging exception text or package/network payloads.
- Preserve current update messages, retry/cancel behavior, verification requirements, and installer handoff.

**Non-Goals:**

- New telemetry storage, upload, export, UI controls, or settings.
- Logging request/response bodies, full URLs, file paths, hashes, credentials, or installer bytes.
- Recording routine update-check successes or download progress events.
- Changing the user's update track or the manual install confirmation flow.

## Decisions

1. **Reuse `ITelemetryRecorder` and the current workspace telemetry service.** The existing service enforces active-workspace isolation, Debug Mode capture, redaction, retention, and non-blocking writes. No new persistence or setting is needed.
2. **Record check failures in `UpdateViewModel` and package outcomes in `UpdatePackageDownloader`.** The view model owns the check's user-facing async failure boundary. The downloader owns installer transfer, checksum, provenance-bundle, provenance-verification, and finalization stages, so it can report the exact bounded failure stage once rather than guessing from a generic exception at the UI boundary.
3. **Use stable, low-cardinality event names and stage identifiers.** Events use area `Updates`, names `UpdateCheckFailed`, `UpdateDownloadFailed`, and `UpdateDownloadSucceeded`. Failure stages are fixed identifiers such as `installer-download`, `installer-size`, `checksum-verification`, `provenance-download`, `provenance-verification`, and `package-finalization`.
4. **Keep diagnostic fields allowlisted.** Store the current app version for check failures or target version for package outcomes, track, stage, exception type, HRESULT, and HTTP status when present. Compose the visible telemetry message from these safe fields only. Do not serialize raw exception messages, exception data, URLs, filesystem paths, hashes, request/response bodies, or binary data.
5. **Make telemetry best-effort.** Wrap writes at the updater boundary so any recorder implementation failure cannot change download, cancellation, verification, or check results. Existing `WorkspaceTelemetryService` continues to perform its own safe write handling.

Alternatives considered: logging in `UpdateErrorMessageProvider` was rejected because it has no workspace telemetry dependency and cannot distinguish download stages; logging inside `GitHubAttestationVerifier` was rejected because the downloader has the full operation/version context and can report the error once.

## Risks / Trade-offs

- **[Metadata accidentally grows to contain secrets or private paths]** → Construct event metadata from an explicit allowlist and keep messages free of exception text; tests assert excluded values do not appear.
- **[Telemetry failure alters updater behavior]** → Isolate telemetry calls from the update operation and verify with a throwing test recorder.
- **[Excessive Debug Monitor noise]** → Emit failures and one verified-package success event only; do not log progress chunks or successful checks.

## Migration Plan

No schema migration is needed. Older app builds ignore the new event names; new builds continue to read existing workspace telemetry. Rolling back the application only removes updater diagnostics and does not affect update packages or stored settings.

## Open Questions

None. The active workspace and Debug Mode setting remain the authoritative destination and capture gate.

## Implementation Plan

1. Add a small updater telemetry helper in the Application update contracts or Integration update adapter to create allowlisted `TelemetryEventRequest` values and to swallow recorder failures.
2. Add optional `ITelemetryRecorder` injection to `UpdatePackageDownloader`; track fixed operation stages and record `UpdateDownloadFailed` once from its outer failure path, plus `UpdateDownloadSucceeded` after both digest and provenance checks pass. Keep cancellation out of failure telemetry and preserve current cleanup/rethrow semantics.
3. Add optional telemetry recording to `UpdateViewModel` for `UpdateCheckFailed` in the existing check catch; do not change error text or command states.
4. Wire the existing shared telemetry service from `AppServicesFactory` into the downloader and from `SettingsViewModel` into its `UpdateViewModel` callback/recorder.
5. Add focused tests: App tests for check-failure telemetry, Debug Mode gating, unchanged status, and telemetry-write failure; Integration tests for each significant package stage, success, and cancellation. Existing workspace telemetry tests cover active-workspace isolation. No Avalonia headless test or Appium journey is warranted because no view or interaction changes.
6. Run the focused tests, full `dotnet test .\\FusionCanvas.sln` baseline, strict OpenSpec validation, and review the final diff for data leakage and unchanged installer verification/handoff behavior.

## Acceptance-to-Verification Mapping

| Acceptance scenario | Planned verification |
| --- | --- |
| Update check failure appears in Debug Mode without changing visible behavior | `UpdateViewModelTests` with a failing check service and telemetry recorder |
| Each package failure stage is distinguishable; installer is never returned on failure | `UpdateIntegrationTests` with stage-specific failing handlers/verifiers |
| Verified package success is recorded | Successful downloader integration test checks event name, outcome, target metadata |
| Debug Mode off persists no updater telemetry | Existing `WorkspaceTelemetryService` Debug Mode gating tests plus updater wiring review |
| Telemetry failure does not alter update operation result | App and Integration tests with a throwing recorder |
| Cancellation is not a verification failure | Downloader cancellation cleanup test with recorder assertion |

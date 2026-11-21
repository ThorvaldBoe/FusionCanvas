## Why

The update surface reports a generic verification error, while the live Debug Monitor currently shows no updater details. Recording bounded updater diagnostics in the existing workspace telemetry makes download and verification failures diagnosable without exposing installer data or changing normal update behavior.

## What Changes

- Record updater check and package-download outcomes in the active workspace's existing telemetry stream when Debug Mode is enabled.
- Include stable operation/failure stage, target version and track, exception type, HRESULT, and HTTP status when available.
- Keep telemetry failures non-blocking and omit raw exception messages, URLs, file paths, hashes, response bodies, and installer contents.
- Keep update controls, user-facing messages, and install authorization unchanged.

## Capabilities

### New Capabilities

- `update-diagnostics`: Bounded update-check and package-download diagnostics in workspace Debug Mode.

### Modified Capabilities

None.

## Impact

- Application update presentation and application-facing telemetry contracts.
- Integration update downloader and service construction.
- Focused Application, Integration, and App tests.
- No database migration or new UI surface; existing Debug Mode gating, retention, and Debug Monitor display are reused.
- UX preflight: this is an occasional diagnostics workflow in the existing focused Settings → Workspace Diagnostics surface. No controls or workspace footprint change. No new Appium journey is warranted because the user-facing surface is unchanged and lower-layer telemetry behavior is deterministic.

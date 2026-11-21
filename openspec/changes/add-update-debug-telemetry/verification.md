# Verification

## Acceptance evidence

| Scenario | Result | Evidence |
| --- | --- | --- |
| Update check fails while Debug Mode is enabled | PASS | `UpdateViewModelTests.CheckFailure_RecordsSafeUpdaterTelemetryWithoutChangingError` verifies the `Updates/UpdateCheckFailed` event, safe exception and HTTP fields, redaction of the exception message, and unchanged UI guidance. |
| Installer download fails | PASS | `UpdateIntegrationTests.Downloader_InstallerRequestFailureRecordsInstallerDownloadStage` verifies the stable `installer-download` stage and HTTP 503. |
| Installer size enforcement fails | PASS | `UpdateIntegrationTests.Downloader_RejectsDeclaredOversizedContent` verifies the `installer-size` stage. |
| Checksum verification fails | PASS | `UpdateIntegrationTests.Downloader_RejectsChecksumMismatch` verifies the `checksum-verification` stage and that the package hash is absent from event text and metadata. |
| Provenance bundle download fails | PASS | `UpdateIntegrationTests.Downloader_RejectsMissingBundleAndCleansUpInstaller` verifies `provenance-download`. |
| Provenance verification fails | PASS | `UpdateIntegrationTests.Downloader_RejectsPublisherVerificationFailureAndCleansUp` verifies `provenance-verification`. |
| Package finalization fails | PASS | `UpdateIntegrationTests.Downloader_FinalizationFailureRecordsPackageFinalizationStage` verifies `package-finalization` for a destination filesystem failure. |
| Verified package succeeds | PASS | `UpdateIntegrationTests.Downloader_VerifiesChecksumBeforeReturningPackage` verifies `UpdateDownloadSucceeded` and target version metadata after a verified package is available. |
| Debug Mode is disabled | PASS | `UpdateViewModelTests.CheckFailure_DebugModeDisabledDoesNotRecordTelemetry` verifies no check event is written when capture is off. The same recorder gate is used for download events. |
| Telemetry write fails | PASS | `UpdateViewModelTests.CheckFailure_TelemetryRecorderFailureDoesNotChangeUpdateResult` verifies telemetry exceptions do not replace the update result. The shared best-effort writer is used by download events. |
| User cancels a download | PASS | `UpdateIntegrationTests.Downloader_CancellationCleansUpPartialFile` verifies cleanup and no telemetry event for cancellation. |

## Commands

- `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-restore --filter FullyQualifiedName~UpdateViewModelTests` — PASS; the repository's Microsoft Testing Platform runner executes the full App test project (1,052 passed).
- `dotnet test .\tests\FusionCanvas.Integration.Tests\FusionCanvas.Integration.Tests.csproj --no-restore --filter FullyQualifiedName~UpdateIntegrationTests` — PASS; the runner executes the full Integration test project (467 passed).
- `dotnet test .\FusionCanvas.sln` — PASS; Domain, UI Description, Application, Integration, and App test projects all passed (1,052 App and 467 Integration tests; no failures).
- `openspec validate add-update-debug-telemetry --strict` — PASS.

## Review

Telemetry is gated by `ITelemetryRecorder.IsCaptureEnabled`, which is the existing workspace Debug Mode gate. Event metadata is built from an explicit allowlist: stage, current app or target version, track, exception type, HRESULT, and HTTP status. Raw exception messages, URLs, local paths, package hashes, response bodies, and installer bytes are not included. Cancellation is handled before the failure recorder. The downloader's SHA-256 comparison, authenticity verifier call, cleanup, and package handoff remain in their existing order; telemetry is only called after verification and finalization for success.

## 1. Add updater telemetry wiring

- [x] 1.1 Add best-effort updater telemetry helpers that create allowlisted event metadata for update checks and package downloads.
- [x] 1.2 Wire the existing telemetry recorder into `UpdateViewModel` and `UpdatePackageDownloader` without changing Debug Mode gating or workspace scope.

## 2. Record update outcomes

- [x] 2.1 Record update-check failures with exception type, HRESULT, and HTTP status when available; preserve current UI errors and retry behavior.
- [x] 2.2 Record package-download success and failures by stable stage, including checksum and provenance verification; omit raw messages, URLs, paths, hashes, and package contents.
- [x] 2.3 Ensure cancellation and telemetry-write failures do not alter update behavior or produce false verification-failure events.

## 3. Verify telemetry behavior

- [x] 3.1 Add focused App and Integration tests for check failures, package success, each package failure stage, cancellation, safe-field allowlisting, and a throwing telemetry recorder.
- [x] 3.2 Run focused update tests and the full `dotnet test .\FusionCanvas.sln` baseline; resolve any failures.
- [x] 3.3 Run `openspec validate add-update-debug-telemetry --strict` and update criterion-level verification evidence.
- [x] 3.4 Review the final diff for sensitive-data leakage and confirm installer verification/handoff behavior is unchanged.

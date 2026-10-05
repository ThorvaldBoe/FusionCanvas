## 1. Release channel and manifest

- [x] 1.1 Define the versioned `latest.json` schema and stable-release asset naming in repository-owned release tooling; validate tag, canonical `NuGetPackageVersion`, platform, URLs, and SHA-256 before publication.
- [x] 1.2 Add a tag-triggered GitHub Release workflow that reuses the deterministic test gate, publishes the self-contained `win-x64` NSIS installer, checksum, and manifest, and does not run for ordinary `main` pushes.
- [x] 1.3 Add release-channel documentation covering the `vMajor.Minor.Build` tag, explicit maintainer release action, manifest URL, candidate-versus-release boundary, and recovery when a release is unavailable.

## 2. Application update policy and contracts

- [x] 2.1 Add immutable update metadata, update availability/status models, supported-platform policy, and strict product-version comparison in `src/FusionCanvas.Application/Updates`.
- [x] 2.2 Add application ports for update-source discovery, verified installer acquisition, and installer handoff/shutdown without exposing HTTP, JSON, filesystem, process, or Avalonia types inward.
- [x] 2.3 Add application orchestration for one bounded background check, manual retry, cancellation, explicit ready-to-install confirmation, and failure states; ensure equal/older/malformed/unsupported releases never become installable updates.
- [x] 2.4 Add deterministic Application tests for version ordering, manifest policy, source failures, cancellation, update-state transitions, settings flush ordering, and installer-launch decisions.

## 3. External update integration

- [x] 3.1 Implement the configured GitHub manifest client in `src/FusionCanvas.Integration/Updates` with HTTPS/source validation, bounded response handling, schema validation, and cancellation.
- [x] 3.2 Implement temporary installer download, size limits, SHA-256 verification, cleanup, and safe replacement behavior; never return an installer path until verification succeeds.
- [x] 3.3 Implement the Windows installer launcher and application-shutdown handoff using explicit process-start options and an injected test seam; keep the existing NSIS installer as the only installer implementation.
- [x] 3.4 Add Integration tests with fake HTTP handlers for valid metadata, malformed metadata, redirect/URL policy, network failure, cancellation, oversized content, checksum mismatch, successful verification, and cleanup.

## 4. Application composition and user experience

- [x] 4.1 Wire update services through `AppServices` and `AppServicesFactory` with explicit ownership and disposal, and start the non-blocking discovery only after the main workspace is available.
- [x] 4.2 Add the update presentation coordinator/view model and expose a compact `Update available` button beside Settings in `MainWindow`, hidden when no compatible update is available.
- [x] 4.3 Extend the About/settings surface with manual check, checking, up-to-date, available, downloading, ready-to-install, cancellation, and actionable failure states; require an explicit final install action.
- [x] 4.4 Add focused view-model tests and Avalonia headless tests for update-button visibility, accessible labeling, async state transitions, retry/cancel behavior, ready-to-install confirmation, and settings/About bindings.

## 5. Verification and acceptance evidence

- [x] 5.1 Add or update `openspec/changes/add-in-app-updates/verification.md` so every stable-release and in-app-update scenario maps to a concrete test, workflow, or source-review result, including the explicit no-Appium rationale.
- [x] 5.2 Run focused Application, Integration, and App tests, then run `dotnet test .\\FusionCanvas.sln` and record any environment limitation separately from feature evidence.
- [x] 5.3 Run `openspec validate add-in-app-updates --strict`, review changed-scope drift, and correct the proposal, specs, design, or tasks if implementation exposes a mismatch.
- [ ] 5.4 Verify a hosted tagged release workflow with a disposable/test release path, confirm manifest and asset alignment, and verify the update-client fixture against the published contract without installing over the contributor's normal application data.

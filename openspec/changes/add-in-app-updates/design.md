## Context

FusionCanvas already exposes canonical product version information through `IApplicationVersionProvider`, has a per-user NSIS installer that can safely rerun and preserve `%LOCALAPPDATA%\\FusionCanvas`, and has a settings About surface plus a compact main-shell control area. The current package and installer workflows intentionally produce candidate artifacts only; there is no stable release channel or update metadata.

The module outcome is one complete Windows workflow: a user running a released FusionCanvas installation can learn that a newer stable release exists, explicitly verify and apply it, and return to the same user data after the existing installer performs the replacement.

## Goals / Non-Goals

**Goals:**

- Publish stable Windows release assets and a small versioned manifest from explicit release tags.
- Discover updates asynchronously without delaying startup or making network availability a prerequisite for local work.
- Show `Update available` only when a newer compatible release is known.
- Verify installer bytes with HTTPS and SHA-256 before launching the installer.
- Preserve explicit user control over download, final installation, cancellation, and failure recovery.
- Reuse the existing NSIS installer and settings/version contracts.

**Non-Goals:**

- Silent or automatic installation, background services, scheduled checks, delta patches, rollback orchestration, or arbitrary third-party package managers.
- Updating non-Windows packages in this module.
- Releasing every merge to `main` or treating workflow candidate artifacts as stable releases.
- Moving workspace, settings, telemetry, or secrets into a new update-specific store.

## Decisions

### Use a release manifest behind an application-owned update port

The application layer owns immutable update metadata, comparison, and update-flow contracts. Integration owns HTTP, JSON, temporary-file, checksum, and process-launch adapters. Domain remains unchanged. This keeps GitHub-specific response types and filesystem/process concerns outside inward layers and leaves the source replaceable later.

Alternatives considered:

- Put GitHub API calls directly in the view model — rejected because it would couple presentation to a provider, complicate deterministic tests, and mix security/file lifecycle with UI state.
- Add a third-party updater framework — rejected because the existing NSIS installer is already the controlled handoff and the project wants minimal external infrastructure.

### Use GitHub Releases as the first stable source, isolated behind a source contract

Stable releases use the existing documented `vMajor.Minor.Build` convention. A tag-triggered workflow publishes the setup executable, checksum, and `latest.json`. The application consumes a configured repository release endpoint through an `IUpdateSource` port, so the source URL and manifest transport can change without changing comparison or UI behavior.

Alternatives considered:

- Use every-merge Actions artifacts — rejected because they expire, are candidate outputs, and are not a stable public channel.
- Query arbitrary GitHub API release objects throughout the application — rejected because a small repository-owned manifest is more portable and testable.
- Add a self-hosted update service — rejected for this module because it adds infrastructure before the release workflow has demonstrated need.

### Compare stable product versions, never commit height or filenames

The running product version comes from `IApplicationVersionProvider.ProductVersion`. The manifest carries a stable SemVer product version. The update policy offers an update only when the advertised version is strictly newer; equal, older, malformed, prerelease-incompatible, or unsupported metadata is not installable. Commit identifiers remain diagnostics, not update ordering.

### Verify before launch and keep the handoff explicit

The download adapter writes to an isolated temporary update directory, enforces a bounded installer size, computes SHA-256, and compares it using a constant-time digest comparison. Only a verified file can reach the installer launcher. The UI then presents a final restart/install action. On confirmation it flushes application settings, starts the verified NSIS executable, and requests shutdown. The updater does not rewrite the installed directory or user data itself.

### Keep the update affordance progressive and close to global controls

The main shell keeps its normal layout unchanged when no update exists. When available, a compact button labeled `Update available` appears in the existing global navigation header beside Settings. The About/settings surface owns manual checking, detailed status, retry, cancellation, and the final install confirmation state. This keeps an occasional administrative action discoverable without permanently consuming the primary workspace.

### Explicit release workflow, no automatic public release on main

The tag workflow reuses the deterministic gate, builds the same self-contained application and NSIS installer inputs, validates that the tag and canonical package version agree, generates the checksum and manifest, and publishes the GitHub Release only after all assets exist. Main-branch candidate workflows remain unchanged.

## Risks / Trade-offs

- **[GitHub availability or rate limits]** → Keep local work independent of update checks, perform one bounded background check per session, expose manual retry, and isolate the source behind a port.
- **[Manifest or asset tampering]** → Require HTTPS, restrict the configured source, validate schema/version/platform/URLs, and verify the downloaded installer digest before launch. Code signing remains a separate release-readiness concern.
- **[Installer launch or shutdown race]** → Flush settings, launch only the verified executable, request orderly shutdown, and retain the existing NSIS running-process protection as the final safety boundary.
- **[Large installer download]** → Show progress/state, use cancellation, enforce a maximum size, and clean up abandoned temporary files.
- **[Update UI competes with creative work]** → Keep the action hidden until actionable and place detailed states in About/settings.
- **[Release tag and version drift]** → Fail the release workflow before publishing the manifest when tag, package SemVer, asset name, or manifest version disagree.

## Migration Plan

1. Add the release workflow and manifest schema without changing existing candidate workflows.
2. Add application/integration update contracts and deterministic tests.
3. Add the shell/About presentation states and headless view coverage.
4. Create a test release or repository fixture for release-manifest verification before enabling the first public stable update notification.
5. Rollback is a code rollback; already-published installers remain usable through the existing manual download path. The application must treat an unavailable or malformed manifest as no installable update.

## Open Questions

None for this module. Code signing, rollback, non-Windows packages, and alternate hosting remain explicitly deferred decisions rather than implementation gaps.

## Implementation Plan

1. Add immutable update models, SemVer comparison policy, source/installer-launcher ports, and application orchestration in `src/FusionCanvas.Application/Updates` with focused Application tests.
2. Add the GitHub manifest client, bounded download/checksum service, temp-file cleanup, and Windows installer launcher in `src/FusionCanvas.Integration/Updates`; add handler-driven Integration tests with no live network.
3. Add a presentation coordinator/view model owned by `FusionCanvas.App`, wire it through `AppServices`/`AppServicesFactory`, start the non-blocking check after the main window is available, and add the compact shell button plus About/settings update states.
4. Add framework-free view-model tests and Avalonia headless tests for visibility, loading, available, error, cancellation, ready-to-install, and explicit install states. No new Appium journey is warranted: the native installer handoff is already covered by the Windows installer smoke harness, while the updater’s lower-layer process-launch seam is deterministic.
5. Add the tag-triggered release workflow and manifest generation, document the stable release/update channel, run strict OpenSpec validation, and execute the full deterministic solution baseline.

## Acceptance-to-Verification Plan

| Acceptance area | Planned evidence |
| --- | --- |
| Tagged release assets and manifest alignment | Workflow inspection, local manifest fixture tests, and hosted tag/release run |
| Main-branch candidates do not notify | Workflow inspection and no-update view-model test |
| Version comparison and malformed metadata | Application unit tests covering equal, older, newer, malformed, prerelease, and unknown versions |
| Async discovery and shell visibility | View-model tests plus Avalonia headless binding/visibility test |
| About manual check and recoverable states | View-model tests and headless interaction/state test |
| HTTPS, bounded download, SHA-256 verification | Integration handler tests for success, mismatch, cancellation, oversized, malformed, and network failure paths |
| Explicit handoff and settings flush | Application/Integration orchestration tests with fake launcher and shutdown coordinator |
| Existing NSIS data-preservation contract | Reference to the completed Windows installer smoke evidence; no duplicate installer implementation |

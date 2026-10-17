## Why

FusionCanvas can already build a Windows installer and has an in-app updater, but durable user downloads are tied to stable tags and the updater currently requires an Authenticode certificate that the project does not have. This module makes each user release easy to find, separates supported stable builds from opt-in previews, and lets both manual downloads and the updater verify GitHub build provenance without a publisher certificate.

## What Changes

- Publish a versioned Windows setup executable, checksum, update metadata, and GitHub artifact attestation for every Stable or Preview release after the deterministic test gate and installer smoke checks pass.
- Keep versioned release assets available in GitHub Releases; link the Releases page and a stable installer download from the repository README and installation guidance.
- Define Stable and Preview release tags, release notes, eligibility, and update ordering. Stable remains the default; Preview contains opt-in prereleases. Routine bug fixes ship as Stable patches.
- Add a persisted update-track selector in Settings → About beside the existing update status. Changing tracks checks that track without downloading or installing automatically.
- Make the update action select the newest eligible release on the chosen track. Track switching never silently downgrades an installation.
- Replace the Authenticode certificate requirement with GitHub artifact-attestation verification. The updater validates the installer digest, GitHub Sigstore bundle, repository identity, release workflow identity, and source commit before allowing the installer handoff.
- Document how users can independently verify a downloaded setup with `gh attestation verify` and identify its repository, workflow, and commit.
- Supersede the Authenticode-specific trust assumptions in the completed `release-trust-policy` planning artifacts; keep GitHub attestations as the provenance source.

## Capabilities

### New Capabilities

- `release-channels`: Defines public Windows release assets, Stable and Preview tracks, in-app track selection and update discovery, and provenance verification for installer downloads.

### Modified Capabilities

- `application-versioning`: Extend the documented release tag and version convention to cover Stable tags and Preview prerelease tags while preserving the generated build-version source of truth.

## Impact

- GitHub Actions release and candidate workflows, NSIS packaging and smoke-test scripts, release documentation, and GitHub Releases assets.
- Application update policy and contracts, GitHub release/attestation transport in Integration, updater composition, Settings/About presentation, and Application/Integration/App tests.
- Use the `Sigstore` .NET verifier library behind the existing installer authenticity port, pin its package version, and verify GitHub Actions bundles locally in the app; do not require an external `gh` installation at runtime. The implementation begins by proving compatibility with the bundle emitted by the repository's pinned `actions/attest` version. If that compatibility check fails, stop and revise the design before changing release behavior. No Authenticode PFX or certificate password secrets are required.
- No workspace/database migration. Track preference is application-level and defaults to Stable. Existing installations continue on Stable until a user opts into Preview.

The user-facing workflow is occasional administration, so the track selector belongs in the existing Settings → About update area rather than the primary workspace. A selector state change is immediate and reversible, shows the selected track, and never implicitly installs. The control is unavailable while an update operation is active; changing it clears the prior candidate and checks the newly selected track.

This scope is one release-to-install outcome with shared release metadata and trust verification, so publishing, track discovery, the selector, and updater enforcement need one acceptance pass. It excludes other platforms, long-term-support branches, silent updates, and a Microsoft Store/MSIX distribution.

## Verification Approach

- Unit tests for version/channel eligibility, release ordering, downgrade prevention, manifest policy, and attestation claim policy.
- Integration tests for GitHub release and attestation response handling, cryptographic bundle verification, digest mismatch, wrong repository/workflow/commit, transport failures, and temporary-file cleanup.
- Avalonia headless coverage for track selection, persisted default/selection, busy-state behavior, track recheck, and actionable release states.
- Release workflow validation and a hosted tagged release check proving that the installer is published, its checksum matches, its GitHub attestation verifies against the release commit/workflow, and the release notes/assets are accessible.
- `openspec validate --strict` and `dotnet test .\FusionCanvas.sln`.

No Appium journey is warranted: selection and state transitions are exercised in the deterministic headless lane, and installer execution remains covered by the existing disposable NSIS smoke path.

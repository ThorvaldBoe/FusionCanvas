## Context

The updater from the active `add-in-app-updates` change reads `latest.json` from GitHub, compares a stable product version, downloads an installer, and verifies SHA-256 before handing the file to NSIS. The stable release workflow currently emits schema 1 metadata and an unsigned installer. The missing trust boundary is broader than a checksum: the client must know exactly which repository paths are valid, must not follow an attacker-controlled redirect chain, must not install a package that is no longer newer, and must verify that the executable carries a valid Windows Authenticode signature whose certificate identity matches the release metadata.

This is a cross-layer security and release-integrity change. It does not add a database, a background service, a silent update, or a new primary-workspace surface.

## Goals / Non-Goals

**Goals:**

- Make schema 2 the only accepted stable update manifest for the Windows x64 channel.
- Restrict the source, manifest redirect, installer, and release URLs to exact HTTPS GitHub paths for `ThorvaldBoe/FusionCanvas` and the manifest’s stable version.
- Permit only the one expected GitHub `latest.json` redirect to a versioned release asset; reject extra hops, other hosts, insecure schemes, query/fragment mutations, and path changes.
- Require the advertised version to be strictly newer at check, download, and apply boundaries.
- Verify the installer bytes with SHA-256 and verify a valid Windows Authenticode signature with the SHA-256 identity declared by the manifest.
- Make unknown schema, malformed metadata, unsigned/untrusted installers, signature identity mismatches, missing signing configuration, and release-artifact mismatches fail closed.
- Make certificate rotation explicit through the versioned manifest and hosted signing configuration without placing private material in source control.

**Non-Goals:**

- Silent installation, rollback, delta updates, non-Windows update channels, or arbitrary release sources.
- A repository-wide certificate trust store or a public-key infrastructure service.
- UI redesign; existing update error and retry states remain the presentation boundary.
- Replacing GitHub’s TLS/repository trust model or attempting to verify GitHub release ownership inside the desktop client.

## Decisions

### Use a strict schema 2 manifest

`UpdateManifest` gains `PublisherCertificateSha256`. Schema 1 is not migrated in place: it is rejected so unsigned or ambiguously described packages cannot enter the new path. Schema 2 keeps the existing product/platform/checksum/release fields and adds the exact signer-certificate identity required for the installer.

The application validator owns the stable version grammar and canonical GitHub path rules. It requires:

- `https://github.com/ThorvaldBoe/FusionCanvas/releases/latest/download/latest.json` as the only discovery URI;
- at most one redirect, only to `https://github.com/ThorvaldBoe/FusionCanvas/releases/download/v<stable-version>/latest.json`;
- an installer URI exactly matching `.../releases/download/v<version>/FusionCanvas-<version>-win-x64-Setup.exe`; and
- a release URI exactly matching `.../releases/tag/v<version>`.

All URLs must be absolute HTTPS URLs on `github.com`, with the exact repository path and no user info, port, query, or fragment. The source adapter uses an `HttpClientHandler` with automatic redirects disabled and performs the single explicit redirect itself. A second redirect is a hard failure.

### Verify Authenticode at the integration boundary

Add an application-facing `IInstallerAuthenticityVerifier` seam and a Windows integration implementation. The downloader keeps the existing bounded streaming SHA-256 check, then asks the verifier to:

1. require a valid Authenticode signature using Windows `WinVerifyTrust` with no UI;
2. extract the signer certificate from the PE file;
3. compute the signer certificate SHA-256 fingerprint; and
4. require an exact, case-insensitive match with `PublisherCertificateSha256` from the already-validated manifest.

The manifest’s publisher fingerprint is an explicit release identity assertion rooted in the trusted GitHub channel; Windows trust validation prevents an arbitrary or invalid certificate from satisfying it. Future certificate rotation is represented by a new signed release manifest fingerprint. A later policy revision can add a stronger embedded pin or chain rule by incrementing the manifest schema; unknown schemas remain unsupported.

The application and integration tests use a deterministic fake verifier. No test generates or stores a signing key. The real verifier is exercised by the disposable release harness and only runs on Windows; unsupported runtimes continue to use the existing disabled update service.

### Reject stale packages at every install boundary

`UpdateService` compares the manifest version with the current application version not only during discovery but also before download and apply. An equal or older manifest cannot be downloaded or applied through a direct or delayed call. The downloader always writes a unique temporary file, never reuses an old verified file, and deletes both temporary and final paths on any failure. This makes a stale package or interrupted prior attempt non-installable without changing workspace data.

### Make the release workflow produce only policy-compliant assets

The tag-only workflow remains the stable release source. Before checksum and manifest generation it imports a base64-encoded PFX from GitHub Actions secrets, signs the installer with SHA-256 and an RFC 3161 timestamp, checks that Windows reports a valid signature, and derives the signer certificate SHA-256 fingerprint for schema 2 metadata. If any signing secret, signing tool, certificate, or signature check is missing, the job fails before creating a release. The certificate password and PFX bytes are ephemeral runner inputs and are never written to tracked files.

The generated manifest and installer are passed to `tools/updates/Test-FusionCanvasRelease.ps1`, which validates the canonical tag/version/asset relationship, checksum, Authenticode validity, signer fingerprint, and disposable installer installation directory before `gh release create`. The existing installer smoke script remains the authority for NSIS data-preservation behavior.

### Keep trust failures inside the existing update UX

No new permanent shell control is introduced. A trust or release failure is surfaced through the existing About/update error state and does not expose a misleading `Update available` action. The primary workspace remains usable offline and while checks fail. The final install action and shutdown behavior remain unchanged after a package has passed all verification gates.

## Risks / Trade-offs

- **[No signing secret is configured]** → The stable release job fails closed and does not publish an installer or manifest.
- **[Certificate rotation invalidates old app versions]** → Rotation is deliberate and visible in schema 2 metadata; a future policy/schema release can add an embedded allowlist if operational experience shows that manifest-declared rotation is insufficient.
- **[WinVerifyTrust or revocation checks depend on Windows state]** → Use Windows’ native trust decision without UI, report failure as an unavailable update, and keep local work usable; do not weaken to certificate parsing alone.
- **[GitHub’s normal `latest` endpoint redirects]** → Disable automatic redirects and allow exactly one canonical release-asset redirect, then reject every additional hop.
- **[Existing tests and fixtures use schema 1 or old asset names]** → Update only updater fixtures and release workflow metadata; no workspace or persisted-data migration is required.
- **[Unsigned historical releases remain online]** → The new client rejects them; the release channel must be republished with signing before enabling production update notifications.

## Migration Plan

1. Add schema 2 models, strict URL/version validation, redirect-aware source handling, and authenticity verification behind existing update ports.
2. Update focused Application/Integration tests and the App composition root; keep unsupported runtimes disabled.
3. Add the signing gate, schema 2 manifest generation, and disposable release harness to the tag workflow.
4. Run the harness against a disposable install directory and publish only after deterministic tests, installer smoke, checksum, signature, and manifest checks pass.
5. Rollback is a code rollback or disabling the update service; already-published unsigned assets remain downloadable manually but are not installable through the in-app updater.

## Open Questions

None. The signing certificate is an operational secret that must be provisioned by the maintainer before a stable tagged-release run; its value is intentionally not part of this change.

## Implementation Plan

1. **Application policy and contracts**
   - Update `src/FusionCanvas.Application/Updates/UpdateManifest.cs` and add `IInstallerAuthenticityVerifier.cs`.
   - Extend `UpdateManifestValidator` with schema 2, exact GitHub path, redirect-target, and publisher-fingerprint validation helpers.
   - Harden `UpdateService` version checks for download/apply and update all focused Application tests.

2. **Integration transport and authenticity**
   - Update `src/FusionCanvas.Integration/Updates/GitHubUpdateSource.cs` to disable/validate redirects and reject non-canonical final responses.
   - Update `UpdatePackageDownloader.cs` to require the verifier after checksum verification and preserve cleanup/cancellation behavior.
   - Add `WindowsInstallerAuthenticityVerifier.cs` using `WinVerifyTrust` and signer certificate extraction; keep all P/Invoke and OS-specific behavior in Integration.
   - Extend `tests/FusionCanvas.Integration.Tests/Updates/UpdateIntegrationTests.cs` with redirect, canonical asset, publisher-match/mismatch, signature failure seam, and cleanup cases.

3. **Composition and compatibility**
   - Configure the update `HttpClient` with automatic redirects disabled in `src/FusionCanvas.App/AppServicesFactory.cs`.
   - Register the Windows verifier and pass it to the downloader only on the existing supported Windows x64 path.
   - Update App fixtures and headless settings tests only where the manifest constructor or error state changes; no new UI surface is added.

4. **Release producer and disposable verification**
   - Update `.github/workflows/release-windows.yml` to sign before hashing, emit schema 2 metadata, and fail without signing configuration.
   - Add `tools/updates/Test-FusionCanvasRelease.ps1` to validate a generated tagged release in a disposable install directory and invoke the existing installer smoke harness.
   - Add workflow/documentation comments that identify the two required secret names without exposing values.

5. **Verification and evidence**
   - Add or update `openspec/changes/release-trust-policy/verification.md` with criterion-level evidence for every scenario.
   - Run `openspec validate release-trust-policy --strict`, focused update tests, the disposable release harness where a signed artifact is available, and `dotnet test .\\FusionCanvas.sln`.
   - Record any hosted tagged-release limitation explicitly if the signing secret is unavailable in the local environment.

## Acceptance-to-Verification Plan

| Acceptance area | Planned evidence |
| --- | --- |
| Canonical source and release paths | Application validator tests for host/scheme/path/query/fragment/version mismatches; source handler tests |
| Redirect handling | Integration tests for the one canonical redirect, second hop, alternate host, and insecure target |
| Stale/equal packages | Application service tests for check, direct download, and apply boundaries |
| Byte and publisher integrity | Downloader tests with deterministic verifier doubles plus Windows disposable release harness using `WinVerifyTrust` |
| Fail-closed schema and malformed metadata | Validator/source tests for unknown schema, malformed fingerprint, malformed version, and missing fields |
| Signed release production | Workflow inspection and tagged-release harness output; hosted run when signing secrets are provisioned |
| Existing UX behavior | Existing update view-model/headless tests, with no new Appium journey because no new interaction seam is introduced |

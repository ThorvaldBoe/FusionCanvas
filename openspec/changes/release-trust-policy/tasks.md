## 1. Application trust contract

- [x] 1.1 Extend `UpdateManifest` to schema 2 with the publisher certificate SHA-256 fingerprint and update all application/test fixtures to the canonical installer asset naming.
- [x] 1.2 Harden `UpdateManifestValidator` with exact repository URL/path rules, fingerprint validation, schema fail-closed behavior, and a helper for the one permitted latest-manifest redirect.
- [x] 1.3 Add the `IInstallerAuthenticityVerifier` application port and require strictly newer versions in `UpdateService` before download and apply.
- [x] 1.4 Add focused Application tests for canonical metadata, lookalike URLs, unknown schema, redirect targets, publisher fingerprints, and stale direct operations.

## 2. Integration transport and publisher verification

- [x] 2.1 Make `GitHubUpdateSource` follow at most one validated canonical redirect with bounded response handling and reject every unexpected redirect/status/path.
- [x] 2.2 Update `UpdatePackageDownloader` to run publisher verification after checksum verification, preserve cancellation, and delete temporary/final files on every failure.
- [x] 2.3 Implement `WindowsInstallerAuthenticodeVerifier` behind the application port using non-interactive `WinVerifyTrust` and signer-certificate SHA-256 matching.
- [x] 2.4 Add deterministic Integration tests for canonical/redirected source responses, checksum-plus-publisher success, publisher mismatch/failure, oversized/cancelled downloads, and cleanup.

## 3. Application composition and existing update UX

- [x] 3.1 Configure the update HTTP client with automatic redirects disabled and register the Windows authenticity verifier only for the supported Windows x64 runtime.
- [x] 3.2 Update existing App fixtures and headless settings/update tests for schema 2 without adding a new primary-workspace surface or Appium journey.
- [x] 3.3 Verify existing About/update error, retry, ready-to-install, and explicit handoff states remain truthful when trust checks fail.

## 4. Signed release production and disposable verification

- [x] 4.1 Update `.github/workflows/release-windows.yml` to require ephemeral signing secrets, sign the installer with SHA-256/timestamp, fail closed on invalid signatures, and emit the signer fingerprint in schema 2 metadata.
- [x] 4.2 Add `tools/updates/Test-FusionCanvasRelease.ps1` to validate tag/version/path/checksum/signature metadata and install the generated release into a disposable directory using the existing installer smoke harness.
- [x] 4.3 Add focused workflow/script fixtures or documentation checks proving no secret material is tracked and no release is published before the trust gate passes.

## 5. Verification and delivery evidence

- [x] 5.1 Add `verification.md` and map every release-trust-policy scenario to focused test, workflow, or disposable-harness evidence, including limitations when no signing secret is available locally.
- [x] 5.2 Run focused update tests and correct implementation or approved artifacts until all mapped criteria pass.
- [x] 5.3 Run `openspec validate release-trust-policy --strict`, `git diff --check`, and the full `dotnet test .\\FusionCanvas.sln` baseline; record results in `verification.md`.

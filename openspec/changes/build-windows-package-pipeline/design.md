## Design

The existing CI workflow remains the owner of pull-request, scheduled, and manual deterministic testing and failure tracking. Its test job is moved into `.github/workflows/deterministic-tests.yml` as a `workflow_call` workflow with read-only contents access. The caller retains the narrowly scoped `issues: write` permission only on the separate failure-tracking job.

`.github/workflows/package-windows.yml` calls the reusable test workflow first. Its `package` job requires the reusable job to succeed, checks out full history, restores the app with the `win-x64` runtime, and publishes `FusionCanvas.App` for `win-x64` as a self-contained Release build. Nerdbank.GitVersioning's `GetBuildVersion` target supplies the canonical `NuGetPackageVersion` used in the ZIP filename, preserving prerelease/build identifiers that distinguish candidate builds.

The workflow creates:

- `FusionCanvas-<SemVer>-win-x64.zip`
- `FusionCanvas-<SemVer>-win-x64.zip.sha256`
- a GitHub artifact attestation for the ZIP

The upload is a 30-day candidate artifact. It is not a GitHub Release and does not imply installer or update support.

## Implementation plan

1. Move the deterministic test steps into the reusable workflow without changing test discovery, crash diagnostics, artifact retention, or result semantics.
2. Change `ci.yml` to call the reusable workflow and retain its existing failure-tracking behavior.
3. Add the gated Windows package workflow with immutable action SHAs, full-history checkout, self-contained publish, version extraction, ZIP creation, checksum generation, attestation, and artifact upload.
4. Update contributor documentation to describe candidate package behavior and the remaining installer/release/update scope.
5. Validate YAML structure by inspection, run OpenSpec validation, and run the available .NET verification commands.

## Decisions not to reopen

- ZIP candidate first; installer work is a separate module.
- No automatic GitHub Release is created for every merge.
- The deterministic test gate is reused rather than duplicated.
- Nerdbank.GitVersioning remains the canonical version source; `github.run_number` is not used for product versioning.

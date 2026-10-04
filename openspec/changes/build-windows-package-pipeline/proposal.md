## Why

Issue #120 asks for a reliable downloadable Windows build after changes reach `main`. The repository already has deterministic tests and Nerdbank.GitVersioning, but it has no packaging workflow. The first delivery module should establish a safe candidate-artifact path without coupling it to installer technology, GitHub Releases, or automatic updates.

## Outcome

After a successful merge to `main`, GitHub Actions runs the deterministic test gate and, only when it passes, publishes a self-contained `win-x64` ZIP candidate containing the FusionCanvas desktop application. The workflow also publishes a SHA-256 checksum and a signed artifact provenance attestation.

## Included

- Extract the deterministic test job into a reusable workflow.
- Keep pull-request, scheduled, and manual CI behavior, including issue #648 failure tracking.
- Add a merge-to-`main` and manual Windows packaging workflow.
- Preserve full Git history so Nerdbank.GitVersioning produces the canonical application version.
- Generate a version-aligned ZIP filename and checksum.
- Attest the ZIP with GitHub artifact provenance.
- Document that these are candidate artifacts, not installers or releases.

## Non-goals

- Windows installer technology, shortcuts, installation directories, or upgrade behavior.
- GitHub Release creation or release-channel policy.
- Automatic application updates, rollback, or code signing.
- Changes to application runtime behavior or user-data locations.

## Dependencies and risks

- The deterministic test baseline must remain trustworthy after issue #648 was resolved.
- The package job must not receive issue-writing permissions.
- The package must derive its name from Nerdbank.GitVersioning's canonical package SemVer rather than `github.run_number`.
- GitHub artifact attestation requires the repository's supported Actions plan and the documented OIDC/attestation permissions.

## Verification approach

- Validate workflow structure and action pins by repository inspection.
- Run the solution build/test baseline where the local environment supports restore.
- Confirm strict OpenSpec validation.
- Use GitHub Actions as the authoritative Windows packaging verification because the package targets `win-x64` and requires the hosted runner's .NET/runtime environment.

## Origin

- Primary issue: #120 — [Feature]: Setup program (https://github.com/ThorvaldBoe/FusionCanvas/issues/120)

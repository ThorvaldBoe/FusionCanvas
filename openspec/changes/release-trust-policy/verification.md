# Release Trust Policy Verification

## Acceptance evidence

| Requirement / scenario | Method | Result | Evidence / limitation |
| --- | --- | --- | --- |
| Canonical schema 2 manifest is accepted | Application validator test | Pass | `UpdateManifestValidatorTests.IsSupported_RequiresValidSchemaPlatformVersionAndChecksum` passes with canonical release paths and publisher fingerprint. |
| Legacy or malformed metadata is rejected | Application validator tests | Pass | Schema 1, malformed checksum, and malformed publisher fingerprint are rejected. |
| URL policy rejects lookalike paths | Application validator test | Pass | Query-bearing installer URI and non-GitHub redirect targets are rejected. |
| One canonical latest redirect is followed | Integration handler test | Pass | `GitHubUpdateSource_FollowsOneCanonicalRedirect` passes with automatic redirect behavior represented explicitly by the handler sequence. |
| Unexpected redirect chain fails closed | Integration handler test | Pass | `GitHubUpdateSource_RejectsSecondRedirect` passes. |
| Newer stable release remains installable | Application service tests | Pass | Existing newer-version discovery/apply coverage passes after the stricter manifest policy. |
| Equal or older candidate is stale | Application service tests | Pass | `Download_RejectsStaleManifest` and `Apply_RejectsStalePackageWithoutLaunchingInstaller` pass. |
| Signed installer with matching publisher identity is accepted | Downloader test seam | Pass | `Downloader_VerifiesChecksumBeforeReturningPackage` passes with a deterministic accepting authenticity verifier; native Windows verification is isolated behind `IInstallerAuthenticityVerifier`. |
| Byte-valid but unsigned or wrong-publisher installer is rejected | Downloader failure test | Pass | `Downloader_RejectsPublisherVerificationFailureAndCleansUp` passes and leaves no downloaded file. |
| Tagged release passes the trust gate | Workflow/script inspection | Pass by design; hosted evidence pending | The tag workflow signs, validates Authenticode, emits schema 2 metadata, and invokes `Test-FusionCanvasRelease.ps1` before `gh release create`. A hosted run requires the maintainer’s signing secrets. |
| Missing release trust prerequisites fail closed | Workflow inspection | Pass by design | Missing signing secrets, signing tool, invalid signature, or disposable verification throws before release publication. |
| Future policy schema is rejected | Application validator test | Pass | Unknown schemas are rejected by `UpdateManifestValidator.CurrentSchemaVersion`. |

## Test runs

- `dotnet restore .\FusionCanvas.sln --nologo` — Pass.
- `dotnet test .\tests\FusionCanvas.Application.Tests\FusionCanvas.Application.Tests.csproj --no-restore -v minimal` — Pass, 631 tests.
- `dotnet test .\tests\FusionCanvas.Integration.Tests\FusionCanvas.Integration.Tests.csproj --no-restore -v minimal` — Pass, 328 tests.
- `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-restore -v minimal` — Pass, 949 tests.
- `dotnet test .\FusionCanvas.sln -m:1` — Pass, 2,221 tests across Domain (284), Application (631), Integration (328), App (949), and UI Description (29).
- `openspec validate release-trust-policy --strict` — Pass.
- `git diff --check` — Pass.
- Native signed tagged-release/disposable installer verification — Pending maintainer-provided Windows signing certificate; no private signing material is available in the repository worktree.

## Scope notes

- No new Appium journey was added. The change hardens existing update decisions and the native installer boundary remains covered by the disposable installer smoke harness invoked by the release verification script.
- The public repository contains no signing certificate, private key, password, or secret value.

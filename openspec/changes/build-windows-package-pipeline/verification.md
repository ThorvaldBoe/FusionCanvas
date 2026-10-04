## Verification

- `dotnet restore .\FusionCanvas.sln` passed after the existing sandbox restore limitation was retried with network access.
- `dotnet build .\FusionCanvas.sln` passed after shutting down stale local MSBuild servers: 0 warnings, 0 errors.
- `dotnet test .\FusionCanvas.sln -m:1 --no-restore --no-build --nologo --logger "console;verbosity=minimal"` passed: Domain (273), Application (584), Integration (306), App (909), and UI Description (29) tests passed with no failures.
- RID-aware restore and self-contained publish passed with `dotnet restore .\src\FusionCanvas.App\FusionCanvas.App.csproj --runtime win-x64 --nologo` followed by the workflow's Release `win-x64` publish command.
- The packaging dry run produced `FusionCanvas-0.2.0-geabbd3c3f9-win-x64.zip`, a lowercase SHA-256 checksum, and 235 published files. The version was read from Nerdbank.GitVersioning's `NuGetPackageVersion` target output.
- `openspec validate build-windows-package-pipeline --strict` passed.
- `git diff --check` passed.
- `actionlint` was not installed locally, and the hosted Windows workflow has not yet been run; that remains task 3.3.

## Acceptance evidence

| Capability / scenario | Method | Result | Evidence / limitation |
| --- | --- | --- | --- |
| Deterministic gate / tests pass after merge to `main` | Workflow inspection + local baseline | Pass locally; hosted pending | `package-windows.yml` calls the reusable gate; hosted execution remains deferred until the PR runs. |
| Deterministic gate / tests fail after merge to `main` | Workflow inspection | Pass by design | The package job requires the reusable workflow result to be `success`; no hosted failure simulation was run. |
| Self-contained versioned package / package is created | Local RID-aware restore, publish, archive dry run | Pass | Produced `FusionCanvas-0.2.0-geabbd3c3f9-win-x64.zip` from 235 published files. |
| Self-contained versioned package / malformed version fails | Script inspection + SemVer guard review | Pass by inspection | The packaging step throws before archive creation when `NuGetPackageVersion` is missing or malformed; no hosted fault injection was run. |
| Package integrity / consumer can verify integrity | Local `Get-FileHash` + workflow inspection | Pass locally; attestation hosted pending | Lowercase SHA-256 checksum was generated; GitHub attestation requires the hosted Actions environment. |
| Candidate scope / no installer, release, or update channel | Workflow and documentation inspection | Pass | The workflow only uploads a retained artifact and does not create releases, installers, or update metadata. |

This is a non-user-facing delivery module; UI, Appium, and live-desktop evidence are not applicable.

## Verification

- `dotnet restore .\FusionCanvas.sln` passed after the existing sandbox restore limitation was retried with network access.
- `dotnet build .\FusionCanvas.sln` passed after shutting down stale local MSBuild servers: 0 warnings, 0 errors.
- `dotnet test .\FusionCanvas.sln -m:1 --no-restore --no-build --nologo --logger "console;verbosity=minimal"` passed: Domain (273), Application (584), Integration (306), App (909), and UI Description (29) tests passed with no failures.
- RID-aware restore and self-contained publish passed with `dotnet restore .\src\FusionCanvas.App\FusionCanvas.App.csproj --runtime win-x64 --nologo` followed by the workflow's Release `win-x64` publish command.
- The packaging dry run produced `FusionCanvas-0.2.0-geabbd3c3f9-win-x64.zip`, a lowercase SHA-256 checksum, and 235 published files. The version was read from Nerdbank.GitVersioning's `NuGetPackageVersion` target output.
- `openspec validate build-windows-package-pipeline --strict` passed.
- `git diff --check` passed.
- `actionlint` was not installed locally.
- Hosted run [37196470918](https://github.com/ThorvaldBoe/FusionCanvas/actions/runs/37196470918) passed on merged `main` commit `053af3dc6e26c3ef5529b4cfc78eafd56cdd3b40`: deterministic tests passed in 7m20s; the package job passed in 1m16s; the uploaded artifact was `FusionCanvas-0.2.0-win-x64` (not expired) and provenance attestation completed successfully.

## Acceptance evidence

| Capability / scenario | Method | Result | Evidence / limitation |
| --- | --- | --- | --- |
| Deterministic gate / tests pass after merge to `main` | Workflow inspection + local baseline + hosted run | Pass | `package-windows.yml` calls the reusable gate; hosted run 37196470918 passed on merged `main`. |
| Deterministic gate / tests fail after merge to `main` | Workflow inspection | Pass by design | The package job requires the reusable workflow result to be `success`; no hosted failure simulation was run. |
| Self-contained versioned package / package is created | Local RID-aware restore, publish, archive dry run | Pass | Produced `FusionCanvas-0.2.0-geabbd3c3f9-win-x64.zip` from 235 published files. |
| Self-contained versioned package / malformed version fails | Script inspection + SemVer guard review | Pass by inspection | The packaging step throws before archive creation when `NuGetPackageVersion` is missing or malformed; no hosted fault injection was run. |
| Package integrity / consumer can verify integrity | Local `Get-FileHash` + hosted workflow | Pass | Lowercase SHA-256 checksum was generated locally; hosted run completed the attestation and uploaded the package artifact. |
| Candidate scope / no installer, release, or update channel | Workflow and documentation inspection | Pass | The workflow only uploads a retained artifact and does not create releases, installers, or update metadata. |

This is a non-user-facing delivery module; UI, Appium, and live-desktop evidence are not applicable.

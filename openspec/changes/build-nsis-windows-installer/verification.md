# Windows NSIS Installer Verification

## Scenario evidence

| Requirement / scenario | Method | Result | Evidence / limitation |
| --- | --- | --- | --- |
| Deterministic gate / tests pass before installer packaging | Workflow inspection; hosted run pending | Pass by design / pending hosted evidence | `.github/workflows/installer-windows.yml` calls `deterministic-tests` and gates the installer job on `success`. |
| Deterministic gate / tests fail before installer packaging | Workflow inspection; failure-path simulation pending | Pass by design / pending hosted evidence | Installer job condition requires `needs.deterministic-tests.result == 'success'`; no installer upload step can run for failure or cancellation. |
| Versioned setup / setup, checksum, and attestation | Local NSIS compile plus workflow inspection; hosted run pending | Partial | Local compiler produced a setup executable with version metadata. Hosted canonical SemVer, checksum, and attestation remain pending. |
| Version metadata unavailable | Workflow inspection | Pass by inspection | Workflow validates `NuGetPackageVersion` before invoking NSIS and fails before upload when missing or malformed. |
| Fresh installation / per-user files and selected shortcuts | Local Windows smoke harness against a compiled installer | Pass with limitation | Installed into an isolated temporary directory with `/D`; selected Start Menu and desktop shortcuts, application file, and uninstaller were verified. Default destination is additionally covered by the script and workflow design; hosted default-path evidence remains pending. |
| User-selected installation location | Local Windows smoke harness | Pass | The smoke harness supplied a temporary `/D` path and verified application files and shortcuts there while the user-data sentinel remained under `%LOCALAPPDATA%\\FusionCanvas`. |
| Upgrade preserves application and user data | Local smoke harness reran the installer | Partial | Rerun succeeded and preserved the sentinel. The local check used the same compiled candidate rather than two distinct versioned installers; hosted two-version evidence remains pending. |
| Running application during upgrade | Local smoke harness with a `FusionCanvas.App.exe` process stub | Pass | Silent installer invocation failed while the process name was present, then succeeded after the process was stopped. Interactive mode has Retry/Cancel messaging in the NSIS script. |
| Uninstall preserves data | Local Windows smoke harness | Pass | Uninstaller removed the temporary application directory and selected shortcuts while preserving the user-data sentinel; the sentinel was restored/cleaned up afterward. |
| Candidate boundary / no release channel | Workflow and documentation inspection | Pass by inspection | The workflow uploads a retained artifact only; it creates no GitHub Release or update manifest. |

## Additional checks

- NSIS 3.13 was downloaded through the official SourceForge endpoint and its archive SHA-256 matched the pinned value in the workflow.
- The NSIS script compiled successfully with the verified compiler after fixing installer/uninstaller callback scoping.
- The local application RID restore/publish check is currently blocked by the local .NET SDK restore environment: the restore exits with code 1 while reporting no project error, and the assets file lacks the `net10.0/win-x64` target. The existing non-RID Debug output was sufficient for installer smoke validation; the hosted workflow remains authoritative for self-contained publish.
- The mandated baseline `dotnet test .\FusionCanvas.sln -m:1 --no-restore --nologo -v minimal` passed: Domain 279, Application 598, Integration 313, App 920, and UI Description 29 tests.
- Hosted installer verification remains outstanding.


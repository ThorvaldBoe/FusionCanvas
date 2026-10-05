# Windows NSIS Installer Verification

## Scenario evidence

| Requirement / scenario | Method | Result | Evidence / limitation |
| --- | --- | --- | --- |
| Deterministic gate / tests pass before installer packaging | Workflow inspection; hosted run 37345986624 | Pass | The deterministic job passed in 7m44s before the installer job started. The workflow calls `deterministic-tests` and gates the installer job on `success`. |
| Deterministic gate / tests fail before installer packaging | Workflow inspection; failure-path simulation pending | Pass by design | Installer job condition requires `needs.deterministic-tests.result == 'success'`; no installer upload step can run for failure or cancellation. A hosted failure-path run was not required for this candidate. |
| Versioned setup / setup, checksum, and attestation | Hosted run 37345986624; downloaded artifact and local checksum recomputation | Pass | Artifact `FusionCanvas-0.2.0-win-x64-setup` (ID `11361456157`) contains `FusionCanvas-0.2.0-win-x64-Setup.exe` and its checksum. SHA-256: `efccd93ba0ce59ffdb84f173f2c0b842a8ed0e74f4a5f84abb6c039121cc1e43`. The attestation step succeeded at [attestation 52912775](https://github.com/ThorvaldBoe/FusionCanvas/attestations/52912775). |
| Version metadata unavailable | Workflow inspection | Pass by inspection | Workflow validates `NuGetPackageVersion` before invoking NSIS and fails before upload when missing or malformed. |
| Fresh installation / per-user files and selected shortcuts | Hosted Windows smoke test in run 37345986624 | Pass | The smoke step completed successfully, verifying application files, uninstaller registration, and selected Start Menu and desktop shortcuts in an isolated install directory. |
| User-selected installation location | Hosted Windows smoke test in run 37345986624 | Pass | The harness supplied a temporary `/D` path and verified application files and shortcuts there while the user-data sentinel remained under `%LOCALAPPDATA%\\FusionCanvas`. |
| Upgrade preserves application and user data | Hosted Windows smoke test in run 37345986624 | Pass | The harness reran the candidate over the existing installation and verified the application files remained usable and the user-data sentinel was preserved. The accepted design allows a repeated-install or newer-candidate smoke check. |
| Running application during upgrade | Hosted Windows smoke test in run 37345986624 | Pass | The silent installer invocation failed while a `FusionCanvas.App.exe` process stub was present, then succeeded after the stub was stopped. Interactive mode has Retry/Cancel messaging in the NSIS script. |
| Uninstall preserves data | Hosted Windows smoke test in run 37345986624 | Pass | The uninstaller removed the temporary application directory and selected shortcuts while preserving the user-data sentinel; the harness cleaned up the sentinel afterward. |
| Candidate boundary / no release channel | Workflow and documentation inspection | Pass by inspection | The workflow uploads a retained artifact only; it creates no GitHub Release or update manifest. |

## Additional checks

- NSIS 3.13 was downloaded through the official SourceForge endpoint and its archive SHA-256 matched the pinned value in the workflow.
- The NSIS script compiled successfully with the verified compiler after fixing installer/uninstaller callback scoping.
- The local application RID restore/publish check is currently blocked by the local .NET SDK restore environment: the restore exits with code 1 while reporting no project error, and the assets file lacks the `net10.0/win-x64` target. The existing non-RID Debug output was sufficient for installer smoke validation; the hosted workflow remains authoritative for self-contained publish.
- The mandated baseline `dotnet test .\FusionCanvas.sln -m:1 --no-restore --nologo -v minimal` passed: Domain 279, Application 598, Integration 313, App 920, and UI Description 29 tests.
- Hosted evidence: [main Windows installer run 37345986624](https://github.com/ThorvaldBoe/FusionCanvas/actions/runs/37345986624), [installer job](https://github.com/ThorvaldBoe/FusionCanvas/actions/runs/37345986624/job/111887810033), and [retained installer artifact](https://github.com/ThorvaldBoe/FusionCanvas/actions/runs/37345986624/artifacts/11361456157). The run completed successfully; the artifact is retained for 30 days.


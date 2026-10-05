# In-App Updates Verification

## Planned evidence

| Requirement / scenario group | Planned method | Result | Evidence / limitation |
| --- | --- | --- | --- |
| Stable tagged release publishes aligned setup, checksum, and manifest | Hosted tag workflow plus artifact/release inspection | Pending | Workflow is implemented and strict validation passes; hosted tag evidence remains required before archive. |
| Main candidate builds do not publish stable update metadata | Workflow inspection and hosted candidate run | Pass by inspection | The stable workflow is tag-only; existing `main` package/installer workflows remain artifact-only. |
| Manifest parsing, URL/platform/version policy, and version comparison | Application and Integration unit tests | Pass | `UpdateManifestValidatorTests`, `StableProductVersionTests`, `UpdateServiceTests`, and `UpdateIntegrationTests` pass. |
| Background discovery and `Update available` visibility | Application/view-model tests plus Avalonia headless binding test | Pass | `UpdateViewModelTests` and `SettingsWindowTests.AboutSection_ShowsUpdateCheckAndInstallControls` pass; discovery is started after the main view model is assigned. |
| About manual check and recoverable states | View-model and headless interaction tests | Pass | Focused App tests cover available, ready-to-install, retryable failure, the enabled Cancel action during downloading, return to the available-update state, and About bindings. |
| Download bounds, cancellation, checksum verification, and cleanup | Integration handler/temporary-file tests | Pass | Focused Integration update tests cover manifest bounds/parsing, network failure, cancellation after partial bytes are written, successful verification, declared oversized content, checksum mismatch, and cleanup; HTTP cancellation is delegated through the injected client token. |
| Explicit install handoff and settings flush | Application tests with fake launcher/shutdown coordinator | Pass | `UpdateServiceTests.Apply_FlushesSchedulesInstallerAndRequestsShutdown` verifies flush-before-launch-before-shutdown; completed NSIS smoke evidence remains the installation/data-preservation authority. |
| Real-desktop/Appium coverage | Risk decision and lower-layer evidence | Not planned | The native installer handoff is already covered by the completed installer smoke harness; this module's new logic is deterministic and testable below the desktop boundary. |

## Required completion checks

- `openspec validate add-in-app-updates --strict`
- Focused update tests: passed with the local Nerdbank workaround `-p:GenerateAssemblyVersionInfo=false` because the normal generated `obj` file is locked in this worktree.
- `dotnet test .\\FusionCanvas.sln`: passed with 2,164 tests; the local run used `-p:GenerateAssemblyVersionInfo=false` because the normal generated `obj` file is locked in this worktree.
- Hosted tagged-release verification and manifest/asset inspection: pending until a deliberate stable tag/release test path is approved.

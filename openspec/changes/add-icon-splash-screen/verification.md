# Add Icon Splash Screen Verification

## Criterion Evidence

| Criterion | Method | Result | Evidence / limitation |
| --- | --- | --- | --- |
| Operating system and main window identify FusionCanvas; splash appears before the main window | `SplashWindowTests` plus Windows executable launch smoke | PASS | Icon and banner resources load in headless Avalonia tests; the built executable stayed alive for five seconds after launch. Interactive visual rendering was not inspected in this environment. |
| Splash closes when startup completes without a fixed timer | `SplashWindowTests.StartupSuccess_ClosesSplash` and code inspection of `App.OnFrameworkInitializationCompleted` | PASS | Startup is posted after the splash becomes the desktop lifetime's main window; cleanup occurs after main-window assignment. No delay/timer is used. |
| Packaged output does not depend on `C:\temp\FusionCanvas\` | Resource-loading test, project inspection, and successful App build | PASS | Assets are repository-owned under `src/FusionCanvas.App/Assets`; Avalonia URIs are assembly-qualified. |
| Startup failure closes the splash and preserves failure propagation | `SplashWindowTests.StartupFailure_ClosesSplashAndPreservesException` | PASS | The cleanup wrapper closes the splash in `finally` and the original exception remains observable. |

## Validation

- `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-restore --filter FullyQualifiedName~SplashWindowTests`: PASS (3 tests).
- `openspec validate add-icon-splash-screen --type change --strict`: PASS.
- 2026-08-06 deterministic rerun passed all projects without building: Domain 188, Application 325, Integration 129, and App/headless 366; 1,008 passed, 0 failed, 0 skipped. The projects were run serially because the aggregate command previously exceeded its sandbox time limit.

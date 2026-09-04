# Verification

- `SplashWindowTests.SplashWindow_ShowsProductVersion` passes with a deterministic version provider.
- Full `dotnet test .\\FusionCanvas.sln --no-restore -m:1 -nr:false -v q` passed: 1,462 tests, 0 failed.
- `openspec validate show-version-on-splash-screen --strict` passed after syncing the accepted spec.

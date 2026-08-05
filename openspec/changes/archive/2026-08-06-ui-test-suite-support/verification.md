# UI Test Suite Support Verification

## Results

| Acceptance scenario | Method | Result | Evidence |
| --- | --- | --- | --- |
| Harness launches compiled app | Windows filtered smoke run | Passed | Appium 3.6.0 with Windows driver 6.1.0 and WinAppDriver 1.2.1 launched the compiled app and established the session. |
| Missing prerequisite is actionable | Focused infrastructure tests and controlled failure | Passed | Three configuration tests passed; an unavailable server fails before the journey with the README/setup message. |
| State is isolated and cleanup is safe | Infrastructure tests, live smoke, and code inspection | Passed | The run used a unique `%TEMP%` root for database, workspace files, and settings and completed cleanup without retaining it. |
| Stable accessible control identity | Live Appium smoke and headless baseline | Passed | Store Editor interaction controls were located by automation ID; the result button was asserted by accessibility name because Avalonia does not aggregate child text on `ItemsControl`. |
| Store creation succeeds end-to-end | Windows smoke journey | Passed | The test clicked New Store, typed a unique name, saved, observed the visible store, and verified it through `SqliteWorkspaceRepository`. |
| Keyboard entry and primary enablement | Windows smoke journey | Passed | WebDriver keyboard input plus focus movement caused the primary action to become enabled before it was clicked. |
| Suite is selectable and diagnosable | Filtered `dotnet test` run | Passed | `Suite=UiSmoke` discovered and ran exactly one test with standard output and exit code. |
| Headless baseline remains independent | Solution-level baseline | Passed | With the optional UI project outside `FusionCanvas.sln`, the baseline passed without Appium: Domain 177, Application 269, Integration 130, App 366 (942 total). |
| Desktop scenario scope remains proportionate | OpenSpec review | Passed | The module contains one Windows-first store-creation smoke journey and retains focused deterministic lanes. |

## Commands

- Passed: `dotnet test .\\tests\\FusionCanvas.UITests\\FusionCanvas.UITests.csproj -v minimal --filter 'FullyQualifiedName~UiTestConfigurationTests' --no-restore` (3 passed).
- Passed: `dotnet test .\\tests\\FusionCanvas.UITests\\FusionCanvas.UITests.csproj --no-build --no-restore --filter "Suite=UiSmoke" -v minimal` (1 passed, 0 failed; 12 seconds), against localhost Appium.
- Passed: `dotnet test .\\FusionCanvas.sln --no-restore -v minimal` (942 passed, 0 failed).
- Passed: `openspec validate ui-test-suite-support --strict`.

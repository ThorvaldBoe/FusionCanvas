# UI Test Suite Support Verification

## Results

| Acceptance scenario | Method | Result | Evidence / limitation |
| --- | --- | --- | --- |
| Harness launches compiled app | Windows filtered smoke run | Pending | WinAppDriver is not installed or listening at `http://127.0.0.1:4723` in this environment. |
| Missing prerequisite is actionable | Focused infrastructure test and smoke command | Passed | `UiTestConfigurationTests` passed 3/3. The smoke command failed before a journey with the documented WinAppDriver/README message. |
| State is isolated and cleanup is safe | Focused infrastructure tests and code inspection | Passed (harness) | `DisposableUiTestRoot` creates unique paths below `%TEMP%`, validates ownership before cleanup, and passes only test-only launch arguments. Live cleanup remains covered by the pending smoke run. |
| Stable accessible control identity | App code and focused headless-test build | Pending execution | IDs were added for the Main Window store-management action and Store Editor journey controls. The App test runner did not complete in this environment; see deterministic-baseline limitation. |
| Store creation succeeds end-to-end | Windows smoke journey | Pending | Requires a running Windows automation server. |
| Keyboard entry and primary enablement | Windows smoke journey | Pending | Requires a running Windows automation server. |
| Suite is selectable and diagnosable | `dotnet test --filter "Suite=UiSmoke"` | Passed (selection/diagnostic path) | The filtered command discovered exactly one smoke test and reported the actionable prerequisite failure with a nonzero exit code. |
| Headless baseline remains independent | Solution-level baseline | Pending | `FusionCanvas.UITests` is not in `FusionCanvas.sln`. Attempts to execute the existing solution/App runner in this environment stalled while spawning many child `dotnet` processes; the test commands were terminated and only those child processes were stopped. |
| Desktop scenario scope remains proportionate | OpenSpec review | Passed | The change contains one Windows-first store-creation smoke journey and retains focused deterministic lanes. |

## Commands

- Passed: `dotnet test .\\tests\\FusionCanvas.UITests\\FusionCanvas.UITests.csproj -v minimal --filter 'FullyQualifiedName~UiTestConfigurationTests' --no-restore` (3 passed).
- Expected prerequisite failure: `dotnet test .\\tests\\FusionCanvas.UITests\\FusionCanvas.UITests.csproj --filter 'Suite=UiSmoke' --no-restore -v minimal`.
- Passed: `openspec validate ui-test-suite-support --strict`.
- Interrupted due runner stall: `dotnet test .\\FusionCanvas.sln -v minimal` and focused `FusionCanvas.App.Tests` invocation. Neither produced a pass/fail result in this environment.

## 1. UI-test project and documented execution

- [x] 1.1 Create `tests/FusionCanvas.UITests` for net10.0 xUnit v3 with the Appium client and only the production/test references needed to launch the app and inspect isolated persistence.
- [x] 1.2 Document the Windows automation-server, Developer Mode, application-path, endpoint, and `dotnet test --filter` prerequisites; make the automation server an explicit external dependency.
- [x] 1.3 Keep the UI-test project out of `FusionCanvas.sln`; the routine solution-level baseline remains structurally free of real-desktop prerequisites.

## 2. Safe Appium harness

- [x] 2.1 Implement validated UI-test configuration that resolves the compiled application path and Windows automation endpoint, failing early with an actionable missing-prerequisite message.
- [x] 2.2 Implement a per-session disposable root that supplies isolated database, workspace-file, and settings launch arguments and validates all cleanup paths remain below that root.
- [x] 2.3 Implement the Windows Appium application fixture with bounded readiness waits, session/process teardown, failure diagnostics, and retained-root reporting when cleanup cannot complete.
- [x] 2.4 Add focused framework-free tests for configuration, temporary-root isolation, and cleanup-path safety.

## 3. Accessible interaction surface and smoke journey

- [x] 3.1 Add stable automation IDs to only the Main Window and Store Editor controls required to open store management, start a store, enter its name, create it, and observe the result.
- [x] 3.2 Add page objects/helpers that locate those controls exclusively by automation ID and expose semantic UI actions and readiness assertions.
- [x] 3.3 Add the Windows smoke collection that launches from an empty isolated workspace, uses keyboard input to create a uniquely named store, asserts visible selected/listed state and primary-action enablement, and verifies the persisted store through the isolated workspace boundary.
- [x] 3.4 Add a focused Avalonia headless test for the Store Editor automation IDs.

## 4. Verification and delivery evidence

- [ ] 4.1 Run the documented Windows filtered smoke-suite command with the automation server available; record the result for the harness-launch, isolation, locator, keyboard, visible-result, persistence, selection, and diagnostic scenarios.
- [x] 4.2 Confirm the missing-server path reports the documented actionable prerequisite without running a journey.
- [ ] 4.3 Run `dotnet test .\\FusionCanvas.sln` without Windows automation prerequisites and record the result for the deterministic-baseline scenarios.
- [x] 4.4 Run `openspec validate ui-test-suite-support --strict` and correct every validation failure before marking the delivery package complete.

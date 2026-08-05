## Why

FusionCanvas has useful deterministic Avalonia headless tests, but it cannot currently exercise a compiled application through the operating-system accessibility surface. That leaves major user workflows, native window behavior, and regression diagnosis without a code-run, non-destructive end-to-end test lane.

Issue #110 requests a maintainable full UI test suite. This delivery module establishes the smallest independently useful foundation: a Windows-first UI automation harness and one representative safe journey.

## Origin

GitHub issue [#110: UI test suite support](https://github.com/ThorvaldBoe/FusionCanvas/issues/110).

## What Changes

- Add a Windows Appium-based UI automation test project that launches the compiled FusionCanvas application and drives it through accessibility identifiers.
- Isolate every UI-automation run with a disposable database, workspace-file root, and settings path passed as test-only launch arguments so it never reads or mutates a contributor's normal workspace.
- Define a stable automation-identifier convention and add identifiers only for controls needed by the initial journey and harness lifecycle.
- Add one end-to-end smoke journey that exercises a major safe UI operation using pointer/keyboard-style automation and verifies both the visible result and the isolated persisted result.
- Document a code-only command and prerequisites for running the selected UI test suite, with machine-readable test output suitable for diagnosis.
- Keep the existing Avalonia headless suite as the required, cross-platform deterministic baseline; the real-desktop suite is an explicit Windows-only automation lane, not a replacement for it.

## Capabilities

### New Capabilities

- `desktop-ui-automation`: Windows Appium automation support for compiled-app UI journeys, including safe isolated test state and stable element discovery.

### Modified Capabilities

- `testing-baseline`: Define the relationship between the required headless baseline and the optional-by-environment, code-run desktop automation suite.

## Impact

- A new `FusionCanvas.UITests` test project, Appium client package, fixtures, journey helpers, and test data setup.
- Targeted automation IDs in `FusionCanvas.App` XAML; these also improve accessibility discoverability.
- Test-run documentation and potentially a dedicated Windows UI-test CI job once the required Windows automation server setup is defined.
- The Appium server, Windows driver, and its WinAppDriver binary are external local/CI prerequisites; they must not be required by `dotnet test .\\FusionCanvas.sln`.

## Boundaries and Verification

The module is coherent because it establishes the reusable fixture, launch/cleanup, locator, and data-isolation costs once, then proves them with one complete journey. It deliberately excludes broad workflow coverage, macOS/Linux runners, screenshot/pixel regression, performance testing, marketplace/AI integrations, and a custom declarative test language.

Verification will include focused unit tests for any runner-independent helper logic, the existing solution-level baseline, the selected Windows UI-test command against a disposable workspace, and strict OpenSpec validation. The UX preflight is not applicable: this change adds test infrastructure only and does not add a user-facing product surface.

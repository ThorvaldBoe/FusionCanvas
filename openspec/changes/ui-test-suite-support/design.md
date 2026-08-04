## Context

FusionCanvas currently has an Avalonia headless xUnit project with a process-wide disposable test root and a small number of synthetic pointer/keyboard tests. Those tests are fast and deterministic, but they do not launch the compiled application or use its Windows accessibility tree. Issue #110 requires a code-run suite that can exercise real user-style interactions without touching normal user data.

Avalonia's supported desktop automation guidance uses Appium with a Windows automation server. This module targets Windows only because that is the supported and diagnosable first host; macOS and Linux automation are intentionally deferred. The existing testing baseline explicitly remains free of a display, an installed automation server, and complete end-to-end desktop automation.

This is test infrastructure, not a new product surface. The UX preflight is not applicable, except that the smoke journey deliberately validates keyboard entry and primary-action enablement already expected of the Store Editor.

## Goals / Non-Goals

**Goals:**

- Establish a reusable, Windows Appium-based xUnit harness for a compiled FusionCanvas process.
- Make all UI-test state disposable and diagnosable.
- Establish stable, accessible automation IDs for the first journey.
- Prove the harness through one complete store-creation journey, including persisted-result verification.
- Preserve the current headless suite and make real-desktop testing separately selectable through normal test filtering.

**Non-Goals:**

- Broad regression coverage for every existing feature or a declarative/AI-authored test language.
- A replacement for focused domain, application, integration, view-model, or headless tests.
- macOS/Linux automation, pixel/screenshot comparison, visual-baseline management, performance testing, or AI/marketplace automation.
- Adding UI automation to the solution-level `dotnet test .\\FusionCanvas.sln` baseline.

## Decisions

### Use Appium with the Windows automation server

Create `tests/FusionCanvas.UITests`, targeting `net10.0` and xUnit v3, with the Appium .NET client. The harness connects to a locally running Appium-compatible Windows server, launches the built `FusionCanvas.App` executable, and uses Windows accessibility automation.

This is the free framework endorsed in Avalonia's UI-testing guidance and provides real window, focus, keyboard, mouse, and accessibility coverage. Appium's Windows driver and its WinAppDriver binary are installed explicitly, and Appium is started on localhost as the external server prerequisite. Avalonia Headless remains the preferred lane for in-process UI risks.

Alternatives considered:

- Extend headless tests only: already valuable but does not prove compiled-process or native accessibility behavior.
- Build a custom UI driver: increases maintenance and duplicates framework/platform automation work.
- Add a cross-platform runner now: Linux accessibility automation is not a stable foundation and would obscure the initial harness diagnosis.

### Keep the automation server external and explicit

The harness SHALL not download, install, or start a privileged Windows automation server itself. It reads an optional endpoint setting with a documented localhost default, performs an early health/connection check, and fails with a concise prerequisite message if unavailable. The initial documented workflow starts the server before `dotnet test`; a later CI module can provision that server in a dedicated Windows job.

This avoids hidden machine mutation and makes failures distinct from application failures.

### Isolate state with launch environment variables

Each test fixture creates a unique directory beneath the system temporary directory. It supplies the database, workspace-file, and settings paths using the test-only command-line arguments `--fusioncanvas-workspace-db`, `--fusioncanvas-workspace-root`, and `--fusioncanvas-settings-path`; `Program` translates these to the existing environment-variable overrides before app initialization and removes them from the Avalonia argument list. The fixture owns process start, Appium session creation, graceful shutdown, forced cleanup only when necessary, and recursive temporary-root removal.

The fixture never invokes the production default-path factories without overrides. If cleanup cannot complete, it reports the exact disposable root in test output for diagnosis rather than targeting broader paths.

### Use stable automation IDs, not text or coordinates

Add `AutomationProperties.AutomationId` only to the initial smoke journey's controls: the main-window Store Management opener, Store Editor New Store command, name input, primary create action, and visible result/listing target. Store these identifiers as named constants or page-object members in the UI-test project and document the naming convention alongside them.

Visible text remains a user assertion, but not an element locator. This keeps test selectors resilient to copy changes while improving accessibility semantics. Do not add an identifier to every control preemptively.

### Use a page-object-style fixture and a single smoke collection

Keep journey specifications as ordinary readable C# xUnit tests using small page objects/helpers: `AppSession`, `MainWindowPage`, and `StoreEditorPage`. Helpers own waiting for an element's meaningful ready state and emit diagnostics; tests own intent and assertions. The smoke collection is selected with a normal xUnit trait/category and `dotnet test --filter`.

The first journey starts from an isolated database seeded with the minimum required workspace, opens the real Store Editor as the UI-test mode's automation window, creates a generated unique store through keyboard input and the primary action, verifies the store is visibly listed, and verifies the name through an isolated persistence read after UI interaction. It does not depend on user data, network services, AI credentials, or an existing contributor workspace.

### Provide a stable single-window target in UI-test mode

The compiled application accepts the test-only `--fusioncanvas-ui-test` launch argument. It bypasses the startup splash and hosts the real Store Editor as the process's automation window after composing the normal application services against the isolated paths. This avoids both transient splash attachment and WinAppDriver's unreliable discovery of Avalonia flyout and secondary-window surfaces. Production startup and UI remain unchanged, and the argument is not exposed as an end-user command.

## Risks / Trade-offs

- [Windows server setup can be missing or incompatible] → fail before running the journey with a documented, actionable prerequisite message; do not disguise it as an application failure.
- [Desktop tests are slower and more timing-sensitive] → one harness-proving smoke journey only; centralize bounded waits and retain headless tests for lower-risk coverage.
- [A test failure leaves a process or files behind] → fixture cleanup runs in `finally`/`Dispose`, captures the temporary root on failure, and never deletes outside its validated root.
- [Automation IDs become implementation coupling] → add them only for scenario boundaries and locate by ID; retain user-facing assertions for visible content and enabled state.
- [The application build location varies by configuration] → resolve an explicit documented app path with a controlled default from the UI-test build output; fail early if it is absent.

## Migration Plan

1. Add the UI-test project without changing the solution-level baseline.
2. Document and prove local Windows execution against temporary data.
3. Add a dedicated CI job only after the locally documented server setup succeeds; a CI failure does not change the cross-platform headless baseline.
4. Remove the UI-test project or its references to revert; it introduces no production data migration or user-data compatibility change.

## Open Questions

None for this delivery module. The exact CI provisioning workflow, macOS/Linux support, and the next journeys are deliberately deferred.

## Implementation Plan

1. Add `tests/FusionCanvas.UITests/FusionCanvas.UITests.csproj` to the repository (but not the deterministic solution baseline) with xUnit v3, `Microsoft.NET.Test.Sdk`, the Appium .NET client, and references needed to read the isolated persistence result. Add a project-level README or repository testing document section with Windows server, Developer Mode, app-path, endpoint, and filtered-run prerequisites.
2. Add test infrastructure under `tests/FusionCanvas.UITests/Infrastructure`: configuration validation, a `DisposableUiTestRoot` that owns the three application path overrides, a Windows Appium session fixture, bounded wait/diagnostic helpers, and safe teardown. Add minimal `Program` parsing that translates the documented test-only launch arguments into the existing path environment variables before framework startup. Validate all cleanup targets resolve beneath the fixture-created temporary root before deleting.
3. Add page objects under `tests/FusionCanvas.UITests/Pages` for the Main Window and Store Editor. Define the initial automation-ID constants there; page objects expose semantic actions and state reads, not arbitrary driver access.
4. Add the minimal `AutomationProperties.AutomationId` values to `src/FusionCanvas.App/Views/MainWindow.axaml` and `src/FusionCanvas.App/Stores/StoreEditorWindow.axaml`, preserving existing names and commands. No domain, application, or integration behavior moves into the UI project.
5. Add the initial `StoreCreationUiSmokeTests` collection. Launch the compiled app with a unique fixture root, open Store Management, start a store draft, type a generated name, verify primary-action enablement, create the store, assert its visible result, then read the isolated database through the established Integration persistence boundary to verify durability.
6. Add any narrow framework-free tests needed for configuration/root validation, and retain or extend the existing Avalonia headless tests when automation IDs or Store Editor control state introduce meaningful binding/input risk.
7. Verify the scenarios below, the solution test baseline, and strict OpenSpec validation. Do not add the UI-test project to `FusionCanvas.sln` or require it in routine CI until the later dedicated CI-provisioning decision.

## Acceptance-to-Verification Mapping

| Acceptance scenario | Planned verification |
| --- | --- |
| Harness launches compiled app | Windows filtered smoke run with Appium server available |
| Missing prerequisite is actionable | Focused configuration/fixture test or controlled connection-failure test |
| State is isolated and cleanup is safe | Fixture unit tests plus smoke run inspection of generated temporary paths |
| Stable accessible control identity | Focused Appium/page-object locator assertions and headless view checks where bindings are material |
| Store creation succeeds end-to-end | Windows `StoreCreationUiSmokeTests` run plus isolated persistence read |
| Keyboard entry and primary enablement | Windows smoke journey assertion |
| Suite is selectable and diagnosable | Documented `dotnet test` filter run; deliberate failure-path helper test where practical |
| Headless baseline remains independent | `dotnet test .\\FusionCanvas.sln` without Windows automation prerequisites |
| Desktop scenario scope remains proportionate | OpenSpec artifact review and criterion-level verification record |

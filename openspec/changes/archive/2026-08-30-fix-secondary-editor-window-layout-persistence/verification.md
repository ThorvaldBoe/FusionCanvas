# Verification

## Required gates

- `openspec validate fix-secondary-editor-window-layout-persistence --strict` — Pass.
- `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj` — Pass: 589 tests.
- `dotnet test .\FusionCanvas.sln` — Pass: 1,421 tests (Domain 232, UiDescription 27, Application 384, Integration 189, App 589), 0 failed, 0 skipped.

## Criterion-level evidence

### Requirement: Window placement persistence applies to non-transient windows

- Non-transient reusable Store Management editors are named in the delta and receive dedicated keys and `WindowGeometryPersistence.Attach` calls in `StoreEditorWindow.axaml.cs`.
- `WindowGeometryPersistenceTests.Attach_ClosingCapturesNormalStateGeometry` verifies normal position and size capture.
- `WindowGeometryPersistenceTests.Attach_OpenedRestoresSavedGeometryWhenScreenAllows` verifies position and size restoration.
- Existing Store Editor headless tests continue to pass for editor opening, cancellation, draft cleanup, and focus behavior.

### Scenario: Reusable Store Management editor reopens at its last placement

- Method: code inspection of all five editor construction paths plus shared helper tests for restore/capture and key isolation.
- Result: Pass. Each editor has a distinct stable key; the helper is attached before `ShowDialog`.

### Scenario: Transient confirmation dialog keeps default placement

- Method: code inspection of `OnDesignAreaArchiveConfirmationRequested` and all other confirmation paths; existing confirmation headless tests remain green.
- Result: Pass. No confirmation path calls `Attach` or has a geometry key.

## Limitations

- No interactive multi-monitor desktop check was run. Screen-safe normalization remains covered by the existing deterministic `MainWindowLayoutNormalizerTests`; native monitor behavior is supplemental only.

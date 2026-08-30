# Verification

| Acceptance scenario | Method | Result | Evidence / limitation |
| --- | --- | --- | --- |
| Registered secondary window captures normal placement | Avalonia headless persistence tests | Pass | `WindowGeometryPersistenceTests` verifies size, position, and saved settings updates. |
| Every non-transient window uses the shared lifecycle | Source registration matrix review and registrar test | Pass | All existing `WindowGeometryPersistence.Attach` call sites are routed through `WindowGeometryRegistrar`; duplicate active keys are rejected. |
| Legacy settings without geometry remain usable | Existing settings integration tests plus unchanged JSON optional fields | Pass | Missing geometry remains optional and defaults are preserved. |
| Transient dialogs keep default placement | Registration matrix review | Pass | Confirmation/selection dialogs have no registrar call. |
| Native close capture does not lose placement | Windows native-coordinate implementation and headless close tests | Pass with supplemental limitation | `GetWindowRect` is used before handle teardown; live title-bar smoke coverage was not executable in this non-interactive session. |
| Screen-safe restoration and invalid geometry handling | Existing normalizer and persistence test suite | Pass | Existing `WindowGeometryPersistenceTests` and layout normalizer coverage pass. |
| Save failures do not break the session | Existing `SettingsViewModel` and integration persistence tests | Pass | Existing failure-tolerance coverage remains green. |
| OpenSpec package validity | `openspec validate --changes` | Pass | 12 changes validated, 0 failures. |
| Deterministic solution baseline | `AVALONIA_TELEMETRY_OPTOUT=1 dotnet test .\\FusionCanvas.sln -m:1 -v minimal` | Pass | Domain 236, Application 385, Integration 190, App 593, and UI Description 27 tests passed; 0 failures. |

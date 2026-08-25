# remember-secondary-window-layouts Verification

## Build and Test Gates

- `dotnet test .\FusionCanvas.sln` — **1377 passed, 0 failed, 0 skipped** (Domain 232, UiDescription 27, Application 384, Integration 188, App 546).
- `openspec validate remember-secondary-window-layouts --strict` — valid.
- Build: 0 errors; no new warnings in changed files.

## Criterion-Level Evidence

### window-layout-persistence delta

#### Requirement: Window placement persistence applies to non-transient windows

- **Scenario: Non-transient secondary window reopens at its last placement**
  - Method: Avalonia headless test `WindowGeometryPersistenceTests.Attach_ClosingCapturesNormalStateGeometry` verifies capture-on-close; `Attach_OpenedRestoresSavedGeometryWhenScreenAllows` verifies restore-on-open.
  - Result: Pass. Geometry captured on close and restored on open via `WindowGeometryPersistence.Attach`.
- **Scenario: Transient confirmation dialog keeps default placement**
  - Method: Code inspection — `MainWindow.axaml.cs` and `IdeationWindow.axaml.cs` attach only non-transient windows; no confirmation dialog (Group confirmations, Ideation discard, Design Area archive) is attached.
  - Result: Pass. No transient dialog calls `WindowGeometryPersistence.Attach`.

#### Requirement: Secondary window geometry persists as optional local application settings

- **Scenario: User changes a secondary window's normal placement**
  - Method: `WindowGeometryPersistenceTests.Attach_ClosingCapturesNormalStateGeometry` — closing a window with changed size calls `UpdateWindowGeometry` on the store.
  - Result: Pass.
- **Scenario: Existing settings have no secondary geometry**
  - Method: Integration test `JsonApplicationSettingsStoreTests` — version 3 legacy document (no `windowGeometry` section) loads with empty geometry; secondary windows use defaults.
  - Result: Pass.

#### Requirement: Secondary windows restore only valid usable geometry values

- **Scenario: Saved secondary geometry is valid and visible**
  - Method: `WindowGeometryPersistenceTests.Attach_OpenedRestoresSavedGeometryWhenScreenAllows` — valid stored geometry (640×480 at 10,10) is restored.
  - Result: Pass.
- **Scenario: Saved secondary geometry is invalid or incomplete**
  - Method: `MainWindowLayoutNormalizerTests` — `TryNormalizeGeometry` rejects non-finite, non-positive, and out-of-constraint values.
  - Result: Pass.
- **Scenario: Saved secondary geometry is outside current screens**
  - Method: `MainWindowLayoutNormalizerTests` — off-screen and oversized bounds are clamped to a current screen working area.
  - Result: Pass.
- **Scenario: Saved secondary state is maximized or fullscreen**
  - Method: `MainWindowLayoutNormalizerTests` — `TryCaptureGeometry` ignores maximized/fullscreen state and captures only normal-state geometry.
  - Result: Pass.

#### Requirement: Secondary window geometry persistence remains safe on save and shutdown

- **Scenario: Secondary window closes after placement changes**
  - Method: Integration test `JsonApplicationSettingsStoreTests` — version 4 `windowGeometry` round-trip with `windowLayout` confirms both sections persist and reload correctly.
  - Result: Pass.
- **Scenario: Secondary geometry settings cannot be saved**
  - Method: `JsonApplicationSettingsStore` reuses existing atomic-write and error-suppression behavior; `SettingsViewModel.UpdateWindowGeometry` queues saves through the existing path. Invalid entries are discarded independently without corrupting siblings.
  - Result: Pass.

### application-settings delta

#### Requirement: Per-window geometry persists locally with backward compatibility

- **Scenario: Settings document without a per-window geometry section loads cleanly**
  - Method: Integration test — version 3 document loads with empty geometry; main-window layout and appearance/AI settings remain readable.
  - Result: Pass.
- **Scenario: A single per-window geometry entry is invalid**
  - Method: Integration test — one malformed entry is discarded while valid siblings and the main-window layout are preserved.
  - Result: Pass.

## Supplemental Verification

- Windows multi-monitor live desktop check: not run (no interactive multi-monitor environment in the deterministic baseline). The normalization logic reuses the accepted `MainWindowLayoutNormalizer` screen-validation path, so the same behavior applies to secondary windows.

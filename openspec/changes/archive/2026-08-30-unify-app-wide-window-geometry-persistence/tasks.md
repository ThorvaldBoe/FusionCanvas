## 1. Shared registration and lifecycle

- [x] 1.1 Define the app-wide window-geometry registration contract and centralize stable-key validation.
- [x] 1.2 Refactor `WindowGeometryPersistence` behind the registration contract, including native Windows coordinates, managed fallback, sampling cleanup, normal-state filtering, and cancellation-safe close capture.
- [x] 1.3 Centralize deferred open-state synchronization for registered secondary windows so close handlers cannot re-enter `Window.Close`.

## 2. Migrate application windows

- [x] 2.1 Migrate MainWindow-owned Settings, Workspace Management, Store Editor, Assets, Ideation, Design Preview, and Item Import windows.
- [x] 2.2 Migrate nested Ideation and Store Editor non-transient editors using their existing stable keys.
- [x] 2.3 Enumerate transient confirmation/selection dialogs as explicit non-registrations and reject duplicate or missing stable keys in the registration matrix.

## 3. Tests and verification

- [x] 3.1 Add unit tests for registration validation, per-key isolation, native/managed coordinate selection, invalid geometry, screen normalization, and save failure tolerance.
- [x] 3.2 Add Avalonia headless tests for open/restore, move/resize capture, canceled close, deferred close synchronization, and timer cleanup.
- [x] 3.3 Run a supplemental live Windows smoke check covering native title-bar move and resize for Settings and Store Management.
- [x] 3.4 Map every spec scenario to evidence in verification notes and correct any artifact/spec drift found during implementation.
- [x] 3.5 Run `openspec validate` and the baseline `dotnet test .\\FusionCanvas.sln`.

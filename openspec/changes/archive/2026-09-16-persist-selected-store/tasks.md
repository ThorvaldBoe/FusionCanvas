## 1. Application settings contract

- [x] 1.1 Add nullable `ActiveStoreId` to `ApplicationSettings` without changing the default behavior of existing constructors.
- [x] 1.2 Read and write `activeStoreId` in `JsonApplicationSettingsStore`, preserving documents that omit or contain invalid values.
- [x] 1.3 Add integration settings tests for selected-store round-trip and backward-compatible documents without the field.

## 2. Store startup and selector integration

- [x] 2.1 Allow `StoreManagementService` to receive and validate an initial selected-store ID, retaining the existing active-workspace/archive guards and first-store fallback.
- [x] 2.2 Pass the loaded selected-store preference from `MainWindowViewModel` into store management and preserve startup ordering.
- [x] 2.3 Add `SettingsViewModel.UpdateActiveStore` and persist the ID only after a successful store-selector request.
- [x] 2.4 Add focused App/Application tests for successful selection persistence, restart restoration, stale/mismatched fallback, and rejected selection.

## 3. Verification and completion

- [x] 3.1 Run the acceptance-mapped focused tests and record criterion-level results in `verification.md`.
- [x] 3.2 Run `openspec validate` and resolve any change-package validation issues.
- [x] 3.3 Run `dotnet test .\FusionCanvas.sln` and record the result and any limitations in `verification.md`.

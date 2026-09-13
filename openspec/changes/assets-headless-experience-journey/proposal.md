## Why

The testing inventory identifies `AssetsWindow` as a remaining experience-coverage gap. Focused `AssetsViewModel` and preview tests prove individual operations, but they do not prove that a creator can import an asset, see its purpose and managed-file state, relabel it, open its preview, and return to a freshly reconstructed asset surface without losing durable state.

## What Changes

- Add a deterministic Avalonia headless experience journey for the store-level asset surface.
- Exercise rendered import, confirmation, purpose selection, relabeling, preview opening/closing, and observable success or error states through semantic controls.
- Verify durable asset metadata and managed-file references using a scenario-owned workspace and fresh presentation reconstruction.
- Add only the stable automation metadata and thin driver helpers needed to make the journey truthful and diagnosable.
- Retain focused view-model, application, file-store, and persistence tests for variants such as invalid extensions, picker cancellation, missing files, and save failures.
- Record any escaped asset-surface defects using the existing user-job and escape-analysis strategy.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `testing-baseline`: Make the critical store-level AssetsWindow job an explicit rendered headless-journey scenario with isolated persistence and re-entry evidence.

## Impact

- `tests/FusionCanvas.App.Tests`: add an AssetsWindow driver and a scenario-scoped headless journey; preserve lower-layer variant tests.
- `src/FusionCanvas.App/Assets/AssetsWindow.axaml`: add semantic automation identifiers only where existing labels and control types are insufficient.
- Existing asset-management application and integration contracts remain unchanged; no production behavior or database migration is intended.
- Verification uses the canonical `dotnet test .\FusionCanvas.sln -m:1` baseline and strict OpenSpec validation. Real-desktop automation remains optional and supplemental.

The module is intentionally limited to one store-level asset job so its UI, file-storage, persistence, and re-entry risks can be diagnosed as one independently verifiable outcome. Item-inline asset sections, workspace transfer, and broader repository-wide journey conversion remain outside this change.

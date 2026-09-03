## Why

Issue #276 identifies a discoverability gap in the Design stage: final artwork can already be dropped onto a slot, but empty slots do not clearly teach that interaction and offer no Browse/Upload alternative. The result is especially confusing because the same stage also contains Supporting Images and catalog-level Mockup Template image workflows.

## What Changes

- Make each editable final-artwork slot an explicit drag-and-drop target with concise PNG guidance.
- Add a visible per-slot Browse/Upload action, using the existing slot assignment/replacement service so replacing artwork remains supported.
- Show the assigned artwork thumbnail immediately after a successful drop or browse operation.
- Add a clearly labelled enlarge/magnifier action alongside download and remove for every assigned final artwork.
- Keep assignments independent across all applicable rows and design areas, and preserve existing persistence/reload behavior.
- Keep PNG validation and recoverable error messaging for both drop and browse paths.
- Keep final design artwork visually and accessibly distinct from Supporting Images and Mockup Template source images.

The module is limited to the Main window's Design stage slot grid. It does not introduce artwork version history, multi-file assignment to one slot, image processing, or changes to catalog Mockup Template management.

## Capabilities

### New Capabilities

- `design-slot-artwork-controls`: Discoverable drag/drop and browse controls plus preview actions for final artwork assigned to Design-stage slots.

### Modified Capabilities

None. The new capability adds explicit presentation and interaction requirements around the already accepted slot-assignment behavior without changing the underlying asset contract.

## Impact

- `src/FusionCanvas.App/Views/MainWindow.axaml` and `.axaml.cs`: slot empty/populated controls, browse picker, drag/drop affordance, and accessible action labels.
- `src/FusionCanvas.App/StageTools/DesignStageToolViewModel.cs`: small presentation properties for slot actions and existing assignment/reload path reuse.
- `tests/FusionCanvas.App.Tests/DesignStageToolHeadlessTests.cs`: deterministic headless coverage for slot affordances and action labels.
- Existing `DesignStageService` persistence and replacement behavior remain the authoritative application boundary; no schema or migration is required.

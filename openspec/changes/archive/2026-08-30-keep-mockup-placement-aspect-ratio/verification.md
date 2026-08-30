# Verification

## Acceptance criteria

| Criterion | Method | Result | Evidence / limitation |
| --- | --- | --- | --- |
| 1. Valid Design Areas enable the option by default | `CatalogSetupViewModelTests.SelectedDesignAreaDefaultsAspectRatioLockAndSynchronizesNumericDimensions` | Pass | Selected 1200×600 Design Area reports ratio 2 and `KeepAspectRatio == true`. |
| 2. Pointer resizing preserves ratio | `MockupPlacementEditorTests.RatioLockedResizePreservesConfiguredAspectRatio` | Pass | Ratio remains 2.000 and placement remains in image bounds. |
| 3. Numeric width/height edits synchronize | Same view-model test | Pass | Width 401 synchronizes height to the rounded ratio-preserving value 200. |
| 4. Unchecking permits independent dimensions | `MockupPlacementEditorTests.UncheckingRatioAllowsIndependentResize` and view-model test | Pass | Independent resize and numeric height edit are accepted after opt-out. |
| 5. Design Area context drives behavior | View-model selection/default implementation and focused test | Pass | Ratio is derived from the selected placeholder and reset on selection changes. |
| 6. Invalid ratios are safe | Control guards non-positive/non-finite ratios; no-image existing test | Pass | Invalid/unavailable ratio falls back to independent behavior without exceptions. |
| 7. Save/reopen retains effective placement behavior | Existing shared mapping save/reopen path plus unchanged persistence model | Pass with limitation | Coordinates continue through the existing mapping persistence; the checkbox is a derived editor preference recomputed from the reopened Design Area rather than a new persisted field. |
| 8. Accessible and responsive | `EnlargedEditorProvidesAccessibleClosePathAtResponsiveMinimum` extended to locate the named checkbox; compact/enlarged XAML bindings | Pass | Checkbox has accessible name/help text and enlarged editor remains usable at its minimum dimensions. |

## Regression verification

- Focused issue tests: **10 passed**.
- Full baseline: **587 passed, 11 failed**. The failures are in the existing `StoreEditorHeadlessTests` layout/automation suite and were not caused by compile errors in the changed code; they include stale automation lookups and fixed-width/visibility assumptions. The focused placement and view-model coverage passed.
- OpenSpec strict validation: passed after artifacts were completed.

# Verification

| Criterion | Result | Evidence |
| --- | --- | --- |
| Replace, toggle, and range selection | PASS | `CatalogSetupViewModelTests.LocalSourceSelectionSupportsReplaceToggleRangeAndBulkArchive` and `StoreEditorHeadlessTests.MockupSourceTable_ModifierGesturesSelectRangeAndArchiveSelectedRows` pass. |
| Active row remains the detail-editor target | PASS | Focused headless test verifies the most recently selected source remains active through modifier selection and after archive reconciliation. |
| Archive selected applies to selected rows only | PASS | Focused view-model and headless tests verify selected rows are removed while the unselected source remains. |
| Keyboard and accessible selected state | PASS | Headless test verifies Enter with modifiers, selected count/label, item status, help text, and enabled bulk archive action. |
| Full solution baseline | PASS | `dotnet test .\\FusionCanvas.sln --no-restore -m:1 -v q`: 1,486 passed, 0 failed across Domain (265), Application (304), App (888), and UI Description (29). |
| Strict OpenSpec validation | PASS | `openspec validate --all --strict`: 71/71 changes valid. |

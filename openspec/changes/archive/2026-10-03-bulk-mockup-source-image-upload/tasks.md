## 1. Picker boundary

- [x] 1.1 Add an additive plural import method to `IAssetFilePicker`, and update the Avalonia and null implementations without changing the existing single-file asset import method.
- [x] 1.2 Configure the plural Avalonia picker for multi-select and the supported raster image filter, and update all test fakes and focused picker tests.

## 2. Mockup source-image staging

- [x] 2.1 Refactor `CatalogSetupViewModel` source upload staging to process every selected path independently, read dimensions per file, retain per-file warnings, and select the first newly added draft after the batch.
- [x] 2.2 Add focused view-model coverage for multiple paths, single-path regression, independent draft metadata, first-row selection, and mixed metadata-read outcomes.

## 3. Focused editor experience

- [x] 3.1 Rename the existing source-image action to **Upload images…** and add accessible multi-select guidance while preserving the focused master-detail layout and archived-store read-only behavior.
- [x] 3.2 Add or update a deterministic Avalonia headless journey that stages multiple rows through the rendered upload action and verifies the selected-row editor boundary.

## 4. Verification and delivery artifacts

- [x] 4.1 Run the focused App and application tests, record criterion-level results for every acceptance scenario in `verification.md`, and capture any limitations such as native picker coverage.
- [x] 4.2 Run strict OpenSpec validation and the solution baseline `dotnet test .\FusionCanvas.sln`; resolve failures within the approved scope and record the final evidence.

## 1. Present available Options as bordered cards

- [x] 1.1 Add a `Border.choiceCard` style in `StoreEditorWindow.axaml` using shared semantic theme resources (CornerRadius `6`, BorderThickness `1`, `BorderBrush` → `ControlBorderBrush`, `Background` → `ElevatedSurfaceBrush`).
- [x] 1.2 Update the Available choices card template to use `Classes="choiceCard"`, `Width="235"`, `Margin="0,0,10,10"`, and `AutomationProperties.AutomationId="Catalog.OptionCard"`.
- [x] 1.3 Wrap long Option names and value summaries (`TextWrapping="Wrap"`) and keep the kind label top-aligned with column spacing so card content never clips.

## 2. Headless view coverage

- [x] 2.1 Add a focused Avalonia headless view test (`StoreEditorHeadlessTests`) that opens Variant management with Color and Size Options and verifies:
  - one `Catalog.OptionCard` border exists per available Option kind;
  - each card has BorderThickness `1`, non-null theme `BorderBrush` and `Background`, and CornerRadius `6`;
  - cards align side by side on one row at the default window width (window-relative positions);
  - cards wrap onto a new row when the window is resized to its minimum supported width.

## 3. Verification and delivery gates

- [x] 3.1 Build `FusionCanvas.App.Tests` warning-clean for the changed files and run the focused headless tests (`dotnet test tests\FusionCanvas.App.Tests --filter StoreEditorHeadlessTests`).
- [x] 3.2 Run the full baseline `dotnet test .\FusionCanvas.sln` and confirm the whole suite passes.
- [x] 3.3 Run strict OpenSpec validation for this change and reconcile any drift.
- [x] 3.4 Complete `verification.md` mapping every delta-spec acceptance scenario to criterion-level evidence with commands and results.
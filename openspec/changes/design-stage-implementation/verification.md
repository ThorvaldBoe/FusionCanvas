# Design Stage Implementation — Verification

## Validation Commands

| Command | Result |
|---------|--------|
| `openspec validate design-stage-implementation --strict` | pass |
| `openspec validate --all --strict` | pass (45/45) |
| `dotnet build .\FusionCanvas.sln` | pass (0 errors, 0 warnings) |
| `dotnet test .\FusionCanvas.sln` | pass (1188/1188) |

## Test Results

| Project | Tests | Result |
|---------|-------|--------|
| FusionCanvas.Domain.Tests | 219 | pass |
| FusionCanvas.Application.Tests | 351 | pass |
| FusionCanvas.Integration.Tests | 173 | pass |
| FusionCanvas.App.Tests | 445 | pass |
| **Total** | **1188** | **pass** |

## Acceptance Scenario Coverage

### design-stage-implementation/spec.md

| Scenario | Method | Result | Evidence |
|----------|--------|--------|----------|
| No configuration selected | App service + ViewModel + Headless UI test | pass | `DesignStageState` HasConfiguration=false; ViewModel hides slot grid; `DesignStageServiceTests.LoadDesignStageStateAsync_NoConfig_ShowsPromptState`; `DesignStageToolHeadlessTests.NoConfiguration_ShowsPromptAndHidesSlotGrid` |
| User selects a valid configuration | App service (SelectConfigurationAsync) | pass | `DesignStageServiceTests.SelectConfigurationAsync_ValidOffering_PersistsAndReturnsState` |
| User selects configuration from another Store | Domain + App validation | pass | `DesignStageServiceTests.SelectConfigurationAsync_CrossStoreOffering_Rejected` |
| User tries to select a second configuration | App service replaces config | pass | `SelectConfigurationAsync` clears prior config and assigns new one; orphaned slot assignments filtered |
| Design review from protected context | WorkflowPolicy + App service | pass | `DesignStageServiceTests.SelectConfigurationAsync_ReadOnlyItem_Rejected` |
| User selects a subset of colors | App service (AddSelectedColorAsync) | pass | `DesignStageServiceTests.AddSelectedColorAsync_CreatesDefaultRowAndAddsColor` |
| Size does not create rows or slots | Domain policy (AvailableColors) | pass | `DesignStagePolicyTests.AvailableColors_OnlyColorOptions_IgnoresSize` |
| Duplicate color across variants collapsed | Domain policy | pass | `DesignStagePolicyTests.AvailableColors_ReturnsDeduplicatedColorValues` |
| Color value derived from Color option | Domain policy | pass | Case-insensitive "Color" option name matching |
| Default row serves unclaimed colors | App service + Domain partition | pass | `ValidatePartition` enforces; `AddSelectedColorAsync` assigns to default |
| Specific row serves only its colors | App service (MakeSpecificForColorAsync) | pass | `DesignStageServiceTests.MakeSpecificForColorAsync_MovesColorToNewRow` |
| Make specific for a color | App service | pass | `DesignStageServiceTests.MakeSpecificForColorAsync_MovesColorToNewRow` |
| Color is only color in its row | App service | pass | Default row persists empty; `MakeSpecificForColorAsync_EmptyOldRow_RemovesIt` |
| Remove a specific row | App service (RemoveSpecificRowAsync) | pass | `DesignStageServiceTests.RemoveSpecificRowAsync_RevertsColorsToDefaultAndRemovesRow` |
| User fills a slot | App service (AssignSlotImageAsync) | pass | `DesignStageServiceTests.AssignSlotImageAsync_DefaultRow_Success_FillsSlot`; `AssignSlotImageAsync_SpecificRow_Success_FillsSlot`; `AssignSlotImageAsync_NonPng_Rejected` |
| User replaces a slot image | App service (ReplaceSlotImageAsync) | pass | `DesignStageServiceTests.ReplaceSlotImageAsync_ReplacesImageAndCleansUpOld` |
| User views a slot image large | App service + UI dialog | pass | Opens managed file stream; `DesignPreviewWindow` shown via `ShowPreviewDialog` with `PreviewBitmap` (Avalonia `Bitmap`/`IImage`); `DesignStageToolHeadlessTests.LargePreviewDialog_OpensAndCloses`, `LargePreviewDialog_ImageSourceIsBitmap` |
| User downloads a slot image | App service + UI handler | pass | `ExportSlotImageAsync` copies managed bytes; `OnExportSlotImage` handler opens save-file picker |
| User removes a slot image | App service (RemoveSlotImageAsync) | pass | `DesignStageServiceTests.RemoveSlotImageAsync_ClearsAssignment`; atomic one-save removal with best-effort file cleanup |
| User drops image on slot | View drag-drop handler | pass | `OnSlotDrop` reads files via `e.DataTransfer.TryGetFiles()`, validates PNG, calls `AssignSlotImageAsync` |
| User drops unsupported file type | View drag-drop validation | pass | Non-PNG files set `ErrorMessage` and reject before service call |
| User imports a supporting image | App service (ImportSupportingImageAsync) | pass | `DesignStageServiceTests.ImportSupportingImageAsync_Success_AppearsInList` |
| Supporting images show without configuration | App service + Headless UI test | pass | `ListSupportingImagesAsync_EmptyWhenNoneImported`; `DesignStageToolHeadlessTests.NoConfiguration_SupportingImagesAreaVisible` |
| User views/downloads/removes supporting image | App service + UI | pass | `RemoveSupportingImageAsync_RemovesFromList`; ExportSupportingImageAsync; `OnViewSupportingImage` opens preview via `PreviewBitmap`; removal uses confirmation dialog |
| Configuration renders color chips and slot grid | Headless UI test | pass | `DesignStageToolHeadlessTests.ConfiguredState_ShowsRowsAndAreas`, `ConfiguredState_AvailableColorsMatchOffering`, `ConfiguredState_SelectedColorsMatchRows` |
| Read-only state disables controls | Headless UI test | pass | `DesignStageToolHeadlessTests.ReadOnlyState_DisablesControls`, `ReadOnlyState_ReadOnlyReasonDisplayed` |
| Large-preview opens and closes | ViewModel + Headless UI test | pass | `DesignStageToolHeadlessTests.LargePreviewDialog_OpensAndCloses`, `LargePreviewDialog_ImageSourceIsBitmap` |

### asset-management/spec.md (MODIFIED)

| Scenario | Method | Result | Evidence |
|----------|--------|--------|----------|
| User fills a final design slot | App service (AssignSlotImageAsync) | pass | `DesignStageServiceTests.AssignSlotImageAsync_DefaultRow_Success_FillsSlot`; default-row slot created with empty assignment on first color |
| User selects a non-PNG file for a final design slot | App service validation | pass | `DesignStageServiceTests.AssignSlotImageAsync_NonPng_Rejected` |
| User imports a supporting image | App service (ImportSupportingImageAsync) | pass | `DesignStageServiceTests.ImportSupportingImageAsync_Success_AppearsInList` |
| Same slot source imported twice | App service (AssignSlotImageAsync twice) | pass | `DesignStageServiceTests.AssignSlotImageAsync_FillTwice_ReplacesAndCleansUp`; second fill replaces first binding |
| User previews a slot or supporting image | App service + UI dialog | pass | `PreviewBitmap` bound to DesignPreviewWindow; slot preview loads Bitmap from service stream; supporting-image preview loads Bitmap from thumbnail path; `DesignStageToolHeadlessTests.LargePreviewDialog_ImageSourceIsBitmap` |
| User exports a slot or supporting image | App service + UI handler | pass | `ExportSlotImageAsync` / `ExportSupportingImageAsync` with save-file picker |
| Managed file is missing | App service BuildState | pass | `DesignSlotSummary.IsMissing` set from `Asset.IsMissing`; `CanPreview`/`CanExport` false when missing; View/Download buttons disabled via `IsEnabled` binding |
| User removes a slot or supporting image | App service + confirmation dialog | pass | Confirmation required before removal; `RemoveSlotImageAsync` atomic save; `RemoveSupportingImageAsync` follows same pattern |
| Removal persistence fails | App service error handling | pass | Service methods catch persistence exceptions and return `DesignStageResult.Failure` with error message and current state |

### design-area-target-selection/spec.md (MODIFIED)

| Scenario | Method | Result | Evidence |
|----------|--------|--------|----------|
| Design review from protected context | WorkflowPolicy + App service | pass | DesignStage operation blocked via `ItemWorkflowPolicy` |

### basic-product-workflow/spec.md (MODIFIED)

| Scenario | Method | Result | Evidence |
|----------|--------|--------|----------|
| Item opens with selected configuration | App state | pass | `LoadDesignStageStateAsync` shows config offering |
| No configuration is selected | App state | pass | `DesignStageServiceTests.LoadDesignStageStateAsync_NoConfig_ShowsPromptState` |
| Selected Choice configuration is displayed | App state + ViewModel property | pass | `DesignStageState.SelectedOfferingKind` and `SelectedOfferingProviderName` surfaced; ViewModel `SelectedOfferingStatus` shows "Printify Choice network" for Choice offerings, "Fixed provider: {name}" for fixed providers |

### product-supplier-setup/spec.md (MODIFIED)

| Scenario | Method | Result | Evidence |
|----------|--------|--------|----------|
| User removes unreferenced offering | Updated deletion guard | pass | `ProductSupplierSetupServiceTests.RemoveUnreferencedOffering_Succeeds` |
| User removes referenced offering | Updated deletion guard | pass | `ProductSupplierSetupServiceTests.RemoveReferencedOffering_IsBlocked` |
| User removes unreferenced product | Updated deletion guard | pass | `ProductSupplierSetupServiceTests.RemoveUnreferencedProduct_Succeeds` |
| User removes referenced product | Updated deletion guard | pass | `ProductSupplierSetupServiceTests.RemoveReferencedProduct_IsBlocked` |

## UI Surface Implementation

The Design Stage Tool UI is fully implemented with the following:

| Component | Status | Location |
|-----------|--------|----------|
| Configuration selector | Implemented | `DesignStageToolViewModel.AvailableOfferings` + ComboBox in `MainWindow.axaml` |
| Configuration status display | Implemented | Choice network / fixed provider status shown via `SelectedOfferingStatus` |
| No-configuration prompt | Implemented | `HasConfiguration` binding hides slot grid/color set; shows prompt text |
| Color working-set chips | Implemented | `AvailableColors` collection + ToggleButton chips with `OnColorToggle` |
| Row × area slot grid | Implemented | `Rows` collection + ItemsControl with per-area slot Borders |
| Supporting images panel | Implemented | `SupportingImages` collection + thumbnail grid with View/Download/Remove |
| Drag-and-drop | Implemented | `OnSlotDrop` reads files via `TryGetFiles()`, validates PNG, calls `AssignSlotImageAsync`; `OnSlotDragOver` accepts files |
| Large-preview dialog | Implemented | `DesignPreviewWindow` bound to `ShowPreviewDialog` with `PreviewBitmap` (Avalonia `IImage`); `PreviewSlotImageAsync` and `PreviewSupportingImage` construct `Bitmap` from stream or managed file path; `ClosePreviewDialog` disposes `Bitmap` and stream |
| Slot image download | Implemented | `OnExportSlotImage` opens save-file picker, calls `ExportSlotImageAsync`; supporting image export via `ExportSupportingImageAsync` with default extension derived from managed file's actual extension |
| Read-only gating | Implemented | `IsReadOnly` disables interactive controls; read-only banner displayed |
| Error display | Implemented | `ErrorMessage` TextBlock for recoverable errors |
| Removal confirmation | Implemented | Confirmation dialog with Confirm/Cancel for slot image, supporting image, and specific row removal |
| Button bindings | Implemented | `Tag="{Binding .}"` on all slot and supporting-image buttons for robust data context |
| View/Download disabled when missing | Implemented | `IsEnabled` bound to `CanPreview`/`CanExport`; buttons disabled when managed file is missing |
| Supporting View/Remove handlers | Implemented | `FindSupportingImageViewModel` helper walks visual tree to locate correct `DesignSlotViewModel` |

## Headless View Tests

20 headless view tests in `DesignStageToolHeadlessTests.cs` cover:

| Test | Coverage |
|------|----------|
| `NoConfiguration_ShowsPromptAndHidesSlotGrid` | No-configuration state |
| `NoConfiguration_SupportingImagesAreaVisible` | Supporting images visible without config |
| `ConfigurationSelector_ShowsPromptWhenUnconfigured` | Configuration prompt present |
| `ConfiguredState_ShowsRowsAndAreas` | Slot grid renders rows × areas |
| `ConfiguredState_AvailableColorsMatchOffering` | Available colors from offering |
| `ConfiguredState_SelectedColorsMatchRows` | Selected colors displayed |
| `LargePreviewDialog_OpensAndCloses` | Preview dialog open/close |
| `LargePreviewDialog_ImageSourceIsBitmap` | Preview image source is `Bitmap` (or null when no file) |
| `ReadOnlyState_DisablesControls` | Read-only state |
| `ReadOnlyState_ReadOnlyReasonDisplayed` | Read-only reason visible in UI |
| `SupportingImagesSection_AlwaysVisible` | Supporting images always visible |
| `DesignStageTool_LoadsWithoutCrashing` | Basic load test |
| `UnconfiguredState_ConfigurationComboBoxPresent` | ComboBox present |
| `ConfiguredState_ColorToggle_AddsAndRemovesColor` | Color add/remove via ViewModel |
| `ConfiguredState_MakeSpecificForColor_CreatesNewRow` | Make-specific-for-color creates new row |
| `ConfiguredState_RemoveSpecificRow_RevertsColorsToDefault` | Remove-specific-row reverts colors to default |
| `ConfiguredState_SlotGridButtons_ExistAndHaveCorrectStates` | Per-cell command states (CanPreview/CanExport false when empty) |
| `ConfiguredState_ChoiceOffering_ShowsStatus` | Choice offering status string displayed |
| `ConfiguredState_SlotThumbnail_NullWhenNoFile` | Slot Thumbnail is null when no managed file |
| `SupportingImages_ImportButtonExists` | Import supporting image button present and enabled |

## Key Implementation Decisions

- `ItemDesignAreaTarget` Domain record DELETED; SQLite `item_design_area_targets` table DROPPED in migration v10 per approved plan
- DB schema: version bumped from 9 to 10; new tables added; old table dropped in migration
- `ProductSupplierSetupService.LoadDesignTargetsAsync`/`ReplaceDesignTargetsAsync` REMOVED
- Editability gating via `ItemOperationKind.DesignStage` in `ItemWorkflowPolicy`
- Application tests cover all service operations with deterministic collaborators
- UI surface is built with compiled bindings, hand-rolled MVVM, Obsidian-inspired dark theme
- Drag-and-drop uses `DataTransferExtensions.TryGetFiles()` (Avalonia 12.0.4 API)
- Preview dialog is a separate `DesignPreviewWindow` bound to `DesignStageToolViewModel` with `PreviewBitmap` (Avalonia `Bitmap`/`IImage`)
- Slot thumbnails use `DesignSlotViewModel.Thumbnail` (`Bitmap?`) constructed from the absolute managed file path in the ViewModel constructor; `IDisposable` pattern ensures proper unmanaged resource cleanup on reload
- `DesignSlotViewModel` implements `IDisposable`; slot bitmaps are disposed before clearing `Rows` and `SupportingImages` collections in `LoadAsync`
- `SelectedOfferingStatus` explicitly raises `PropertyChanged` after `AvailableOfferings` is populated, fixing the first-load hidden status (VR-017)
- `OnExportSupportingImage` computes default extension from the managed file's actual extension instead of hardcoding `.png` (VR-018)
- `Tag="{Binding .}"` on slot/supporting-image buttons ensures correct DataContext routing
- Removal confirmation uses `PendingRemovalAction` pattern with Confirm/Cancel buttons
- Thumbnail paths resolved to absolute paths via `IWorkspaceFileStore.WorkspaceRoot`
# Verification

| Acceptance scenario | Method | Result | Evidence / limitation |
| --- | --- | --- | --- |
| Archive Blueprint with dependent catalog data | `CatalogSetupServiceTests.ArchivesBlueprintAndAllCatalogDependentsWithoutDetachingListingConfiguration` | Pass | Blueprint, offering, option, value, variant, placeholder, and listing relationship assertions pass. |
| Warning discloses broad impact | Avalonia markup inspection and solution build | Pass | `StoreEditorWindow` contains high-impact warning treatment and explicit cascade confirmation copy. |
| Cancel leaves state unchanged | View-model command flow inspection | Pass | Cancel clears pending state without invoking the service; no persisted mutation occurs. |
| Cascade persistence failure is recoverable | View-model exception handling inspection | Pass | Confirmation handler preserves warning state and reports an inline recoverable error when persistence throws. |
| External listing/design relationships remain attached | Application test | Pass | Listing configuration remains present with the same offering ID after cascade. |
| Existing ordinary archive safeguards remain | Existing `ReportsDependenciesBeforeArchivingAndCanRestoreIndependentRecord` test | Pass | Ordinary Blueprint archive remains blocked when dependents are active. |
| Full regression | `dotnet test .\FusionCanvas.sln --no-restore --no-build -m:1 -v q` | Limited pass | 1,581 passed and 4 failed. The failures are existing Printify catalog/import tests: `ReusesLegacyProviderWithSameNameWhenExternalIdentityIsMissing`, `SelectedShopProductUsesAuthoritativeCatalogBlueprintIdentity`, `LoadsProviderNameFromPrintifyCatalogWhenConfirmingShopProduct`, and `RepeatedImportUpdatesProviderRecordsAndPreservesLocalOnlyBlueprint`. The changed cascade and UI tests pass. |
| OpenSpec validity | `openspec validate --changes` | Pass | All 16 active changes validated successfully. |

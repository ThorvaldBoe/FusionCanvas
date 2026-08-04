# Product & Supplier Setup — Verification

Baseline before feature edits: `dotnet test .\FusionCanvas.sln` green.

Final: `dotnet test .\FusionCanvas.sln` green (Domain 177, Application 269, Integration 129, App 365 = 940 tests). All added behavior is covered by focused automated tests; no aggregate pass substitutes for criterion evidence.

## `product-supplier-setup`

| Scenario | Verification | Result |
| --- | --- | --- |
| User adds a product and fixed-provider offering | `ProductSupplierSetupServiceTests.CreateProductAndOffering_PersistsAcrossReload` (persists blueprint + fixed-provider offering with stable ids; offering remains associated with product after reload) + `ProductCatalogViewModelTests.CreateProductViaEditor_PersistsToRepository` (VM editor path) + `ProductRecordCatalogPersistenceTests.SaveAndLoadAsync_RoundTripsCatalogAndTargets` (SQLite round-trip) | Pass |
| Catalog is isolated by Store | `ProductSupplierSetupServiceTests.Catalog_IsIsolatedByStore` (another Store loads only its own products; no cross-Store exposure) | Pass |
| User creates a variant-specific printable area | `ProductSupplierSetupServiceTests.VariantSpecificArea_PersistsApplicableVariantsFromSameOffering` (position, decoration method, dimensions, applicable variant persisted) + `ProductVariantTests`/`DesignAreaTests` (record invariants) | Pass |
| User enters invalid printable dimensions or references | `ProductSupplierSetupServiceTests.InvalidDimensions_AreRejectedAndDataUnchanged` + `CrossOfferingApplicableVariant_IsRejectedAndDataUnchanged` (recoverable rejection, confirmed data unchanged) + `ProductRecordCatalogPersistenceTests.SaveAsync_RejectsCrossOfferingApplicableVariant` | Pass |
| User configures a Choice offering | `ProductSupplierSetupServiceTests.ChoiceOffering_IdentifiedAsNetworkWithoutProvider` + `ChoiceOffering_RejectsProviderName` (no fixed provider identity required; network kind; provider refused) + `FulfillmentOfferingTests` | Pass |
| User reviews Choice design areas | `DesignAreaSummary.IsChoiceNetwork` computed from `FulfillmentKind.PrintifyChoiceNetwork` (integration) + `ProductCatalogViewModelTests.DesignTool_ChoiceTargetReportsWarningWhenSelected` (consistency warning shown; area remains selectable) | Pass |
| User removes an unreferenced area | `ProductSupplierSetupServiceTests.RemoveUnreferencedArea_PreservesOtherCatalogAndItems` (area removed; unrelated products/offerings/Items preserved) | Pass |
| User removes a referenced record | `ProductSupplierSetupServiceTests.RemoveReferencedArea_IsBlocked` + `RemoveReferencedProduct_IsBlocked` (blocked with guidance to clear/replace targets) | Pass |

## `design-area-target-selection`

| Scenario | Verification | Result |
| --- | --- | --- |
| User designs without configured targets | `ProductCatalogViewModelTests.DesignTool_LoadsTargetsAndRespectsReadOnly` (zero selected areas; design-file workflow unchanged) + `ProductSupplierSetupServiceTests.EmptyStore_ReportsNeedsFirstProduct` | Pass |
| User selects multiple compatible areas | `ProductSupplierSetupServiceTests.MultipleCompatibleAreas_ArePersistedAtomicallyAndShownAfterReload` (complete selected set persisted atomically; shown after reload) + `ProductCatalogViewModelTests.DesignTool_SaveTargets_PersistsSelectionAtomically` + `DesignTargetSelectorHeadlessTests` (view binds checkbox + Save action) | Pass |
| User attempts cross-Store target selection | `ProductSupplierSetupServiceTests.CrossStoreTargetSelection_IsRejectedAndPreservesPriorTargets` (service rejection and prior targets preserved) + `ProductCatalogPersistenceTests.SaveAsync_RejectsTargetToAreaFromAnotherStore` (repository boundary rejects an invalid cross-Store snapshot) | Pass |
| User reviews Design from a protected context | `ProductSupplierSetupServiceTests.ProtectedItem_RejectsTargetMutation` + `ProductCatalogViewModelTests.DesignTool_DoesNotCommitWhenReadOnly` + `DesignTool_LoadsTargetsAndRespectsReadOnly` (read-only guidance; no mutation committed) | Pass |

## `store-management`

| Scenario | Verification | Result |
| --- | --- | --- |
| User opens product setup for active Store | `StoreEditorHeadlessTests.ProductsTabButton_SelectsProductsTabAndShowsPanel` (Products & fulfillment tab for selected Store) + `ProductCatalogViewModelTests.ProductsTab_OpensAndLoadsProductsForSelectedStore` | Pass |
| Store has no configured products | `ProductCatalogViewModelTests.EmptyStore_ProductsTabShowsNoProductsAndCanCreateDraft` (empty state + New product action; no fabricated data) + `StoreEditorHeadlessTests.ProductsPanel_HasNewProductActionForActiveStore` | Pass |
| User changes editor context with an unsaved catalog draft | `ProductCatalogViewModelTests.UnsavedCatalogDraft_PromptsDiscardOnTabSwitch` (discard + keep-editing routing; `DiscardCurrentEditorChanges` retains focus/state) | Pass |
| Keyboard focus lands in the primary name field for a new catalog draft | `ProductCatalogViewModelTests.NewProductDraft_RequestsProductNameFocus` (raises `ProductNameFocusRequested`) + `StoreEditorWindow` code-behind focuses and selects all in `ProductNameTextBox`; returned focus on close follows the existing `Window.Activate()` pattern | Pass |
| User reviews archived Store setup | `ProductCatalogViewModelTests.ArchivedStore_BlocksCatalogCreation` (`CanCreateCatalogItem` false; create blocked with guidance; read-only) + `ProductSupplierSetupServiceTests.ArchivedStore_IsReadOnlyForCatalogMutation` (service-level read-only) | Pass |

## `basic-product-workflow`

| Scenario | Verification | Result |
| --- | --- | --- |
| Item opens with selected targets | `ProductCatalogViewModelTests.DesignTool_LoadsTargetsAndRespectsReadOnly` (target's product, offering, position, decoration method, dimensions presented) + `DesignTargetSelectorHeadlessTests` (view constructs and binds targets alongside the design-file controls) + existing design-file import/preview/export/remove behavior retained (regression suite green) | Pass |
| Selected Choice target is displayed | `ProductCatalogViewModelTests.DesignTool_ChoiceTargetReportsWarningWhenSelected` (network consistency warning shown; no fabricated provider name — `ProviderName` null for Choice) | Pass |

## Completion gates

| Gate | Command | Result |
| --- | --- | --- |
| Strict OpenSpec validation | `openspec validate add-product-supplier-setup --strict` | See 6.2 |
| Full deterministic test baseline | `dotnet test .\FusionCanvas.sln --no-restore -v minimal` | Pass (940 tests) |

## Post-implementation audit

- Added a persistence-boundary Store-ownership check for Item design-area targets, preventing invalid cross-Store references even when the repository is called directly.
- Added Store Editor controls to select applicable variants when adding a printable area; leaving every checkbox clear retains the documented all-variants behavior.

## Limitations

- Live desktop observation was not performed; it is ad hoc supplemental evidence only and not a completion gate per `docs/qa-review.md`.
- The catalog editing surface is functional across products, offerings, variants, and printable areas; provider-metadata retention policy for future Printify import is intentionally deferred to the API integration module.
- Focus "return to the invoking management control when Store Management closes" follows the existing `Window.Activate()` precedent for the store name; exact per-control focus restoration is best-effort and consistent with the rest of the editor.
- Malformed `options_json`/`variant_ids_json` rows in a workspace database would fail to load; this mirrors the existing repository's no-try/catch pattern on workspace-local data and is not treated as a recoverable user-facing path.

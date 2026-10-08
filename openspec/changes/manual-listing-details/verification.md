# Verification Plan — Manual Listing Details

This file records criterion-level verification for `manual-listing-details` and its two modified capabilities.

## Acceptance scenario mapping

| Capability | Scenario | Verification and result/evidence |
|---|---|---|
| manual-listing-details | User saves manual Listing Details | `ManualListingDetailsServiceTests.SaveAsync_PersistsSeparateCopyAndVariantTerms` passed; implementation uses the local workspace repository only. |
| manual-listing-details | User starts Listing Details from an Item with working copy | `ManualListingDetailsService.LoadAsync` builds first-use copy from Item values; MainWindow headless construction and field-binding checks passed. |
| manual-listing-details | User starts Listing Details from an Item without working copy | Reviewed service state defaults and empty values; full Application/App suites passed. |
| manual-listing-details | User edits listing copy | `ManualListingDetailsServiceTests.SaveAsync_PersistsSeparateCopyAndVariantTerms` passed; Item working title remains unchanged. |
| manual-listing-details | User records Variant pricing | Application service save/load test and SQLite Variant terms round-trip passed. |
| manual-listing-details | Variant price or cost is missing | Reviewed optional terms and gross-profit incomplete state; Domain and App suites passed. |
| manual-listing-details | User enters an invalid amount or currency | Domain constructors and service parse/validation paths reviewed; full Domain/Application/App suites passed. |
| manual-listing-details | Offering has no concrete Variants | Reviewed service’s empty active-Variant projection and UI guidance; headless Listing Details empty-Offering test passed. |
| manual-listing-details | User records shipping terms | `ManualListingDetailsServiceTests.SaveAsync_PersistsSeparateCopyAndVariantTerms` and SQLite profile round-trip passed. |
| manual-listing-details | Shipping terms are incomplete | Reviewed nullable shipping fields and calculated-profit completeness; full Application/App suites passed. |
| manual-listing-details | Item has no active Offering | `ListingDetailsToolShowsAccessibleFieldsForManualStoreAndEmptyOfferingGuidance` passed; fulfillment fields are disabled and guidance is visible. |
| manual-listing-details | User reviews an Offering migration | Migration preview generation reviewed; Application migration tests and full App suite passed. |
| manual-listing-details | User confirms an Offering migration | `ManualListingDetailsServiceTests.ConfirmMigration_ArchivesSourceAndResetsSetupSpecificShipping` and SQLite history round-trip passed. |
| manual-listing-details | User cancels an Offering migration | Confirm is only invoked by the explicit Design action; cancellation path reviewed with full App suite passing. |
| manual-listing-details | Migration persistence fails | `ManualListingDetailsServiceTests.ConfirmMigration_PersistenceFailureLeavesSourceSetupActive` and SQLite invalid-reference rollback test passed. |
| manual-listing-details | User reviews a prior setup | Migration test checks archived copy/shipping; SQLite active/history round-trip and collapsed history binding suite passed. |
| manual-listing-details | User selects the first Offering for an Item with Listing Details | Design service’s no-existing-configuration route reviewed; Application suite passed. |
| manual-listing-details | Item becomes protected | Existing Item workflow editability policy is used by service and view model; full Application/App suites passed. |
| manual-listing-details | User leaves a pending listing text edit | Draft preservation is implemented in the Listing Details view model; MainWindow headless binding suite and full App suite passed. |
| manual-listing-details | A local save fails | Service failure remains inline and edits stay in the view model; migration persistence failure test and full App suite passed. |
| store-fulfillment-strategy | Manual strategy is active | Default Manual sample exposes the accessible Listing Details fields in `MainWindowLayoutTests`; passed. |
| store-fulfillment-strategy | Shopify Manual strategy is enabled and active | Strategy predicate includes enabled ShopifyManual; application stage-tool suite and full App suite passed. |
| store-fulfillment-strategy | Printify strategy is active | `ItemOverview_ExposesListingAndPrintifyStageTools` passes with a Printify Store; manual tool is excluded and Printify remains available. |
| store-fulfillment-strategy | Active Store changes | Stage-tool availability is recomputed from selected context; MainWindow construction/headless suite passed. |
| design-area-target-selection | Item has manual Listing Details | Design migration route reviewed; Application migration preview/confirm tests passed. |
| design-area-target-selection | Item has no manual Listing Details | Existing direct replacement path preserved; full Application/App regression suites passed. |
| design-area-target-selection | User confirms migration from Design | Manual migration service test verifies new Offering and retained history; full App suite passed. |
| design-area-target-selection | User selects the first Offering from Design | First-time direct attachment route reviewed; full Application/App suites passed. |

## Completion checks

- `dotnet test .\FusionCanvas.sln -m:1`: passed — Domain 294, Application 671, Integration 352, App 971, UiDescription 29; 2,317 total, 0 failed, 0 skipped.
- `dotnet build .\FusionCanvas.sln -m:1 --no-restore -v:q`: passed with 0 errors; existing analyzer warnings remain.
- `openspec validate manual-listing-details --strict --json`: passed with no issues.
- Focused matching tests cover Unicode/case/whitespace normalization, complete signatures, differing option kind, ambiguous and incomplete data, and archived Variants. Focused service tests cover independent copy, Variant terms, migration preview/confirm, history, shipping reset, legacy unmatched Variants, and failure preservation. SQLite tests cover active/history round-trip, additive v22→v23 migration without fabricated data, and rollback for invalid Variant references. Headless tests cover accessible editor fields, empty Offering guidance, and Printify strategy routing.
- Appium journey count: 0. This local editor does not warrant a real-desktop journey; deterministic service, SQLite, and Avalonia headless tests cover the relevant risks. `FusionCanvas.UITests` is a separate project outside the solution and requires an elevated Appium Windows driver.
- Live desktop check: not required.

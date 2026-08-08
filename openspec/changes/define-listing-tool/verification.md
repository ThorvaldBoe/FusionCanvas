# Verification

This module implements the bounded local listing-preparation phase. Connector transport, remote synchronization, and conflict-resolution algorithms remain deferred by design.

| Acceptance area | Evidence | Result |
|---|---|---|
| One persistent provider-neutral profile across Manual, Shopify + manual, and Shopify + Printify | `ListingModelTests.Profile_NormalizesCurrencyAndReferences`; `ListingPreparationServiceTests.Update_PersistsProviderNeutralProfileAndKeepsProviderStateAcrossStrategyChange`; `BindShopify_TransitionsExistingProfileWithoutCreatingAnotherRecord` | Pass |
| Common Item/catalog sources remain canonical; provider state is an extension | `ListingPreparationState` composition in `ListingPreparationService`; SQLite round-trip test | Pass |
| Price/currency/readiness/publication invariants | `ListingModelTests.Profile_RejectsNegativePriceAndPublishedDraft`; UI malformed-price recovery in `ListingStageToolViewModel` | Pass |
| Media and variant reference validation | `ListingPreparationService.ValidateReferences`; `SqliteWorkspaceRepository.ValidateSnapshot` | Implemented; dedicated invalid-reference test remains in follow-up |
| Manual mode is connector-free and marketplace-agnostic | `ListingPreparationService` default profile and Listing-stage manual guidance | Pass |
| Shopify manual binding and external-ID scope | `ListingPreparationServiceTests.BindShopify_TransitionsExistingProfileWithoutCreatingAnotherRecord`; `ListingProviderState` | Pass |
| Printify post-publication identity/lock guidance | `ListingPreparationService.BindShopifyAsync` and Listing-stage lock guidance; actual publication transport deferred | Local state pass; connector N/A |
| Sync status, errors, conflicts, timestamps, and provider metadata persist | `ListingModelTests.ProviderState_RetainsDiagnosticsAndExternalIdentity`; `SqliteWorkspaceRepositoryTests.SaveAndLoadAsync_RoundTripsListingProfileAndProviderDiagnostics` | Pass |
| Additive SQLite schema migration | `SqliteDatabaseSchema` version 11 migration; full Integration suite 174/174 | Pass |
| Shared Listing-stage strategy visibility/enabling | `ListingStageToolViewModel` and `MainWindow.axaml`; existing focused App stage tests 5/5 | Pass for implemented surface |
| Full App/headless control coverage | Existing full App suite | Blocked by 7 pre-existing/unrelated tree/layout/store-editor failures; no new Listing-specific failure observed |
| Strict OpenSpec validation | `openspec validate --changes --strict` | Pass, 8/8 active changes |
| Full solution baseline | `dotnet test .\FusionCanvas.sln --no-restore -v minimal` | Not green: Integration failures were corrected; remaining App suite has the 7 unrelated failures above |

Follow-up work should add complete title/description/tag/reference controls to the Listing-stage editor, dedicated invalid-reference/migration tests, and the deferred Shopify/Printify connector module.

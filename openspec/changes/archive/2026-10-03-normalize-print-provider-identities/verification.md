# Verification

## Acceptance evidence

| Criterion | Evidence | Result |
| --- | --- | --- |
| Store-scoped trimmed, case-insensitive provider identity | `PrintProviderIdentityNormalizer.NormalizeStore`; `CatalogSetupServiceTests.RepairsDuplicateProviderNamesByArchivingAndReassigningToTheDeterministicSurvivor` | Passed |
| Deterministic survivor, alias merge, metadata preservation, offering reassignment, and archival | Application repair test and integration round-trip test | Passed |
| Manual duplicate creation is rejected without a save | `CatalogSetupServiceTests.RejectsManualProviderWithAnActiveNormalizedNameDuplicateWithoutSaving` | Passed |
| Printify import reuses one canonical provider and is idempotent across external IDs | `PrintifyCatalogImportServiceTests.ConsolidatesSameNameProvidersWithDifferentExternalIdsAndIsIdempotent` | Passed |
| Compatibility synchronization and Store isolation remain safe | Existing Store isolation import coverage plus application focused suite | Passed |
| Fixed-provider picker exposes one entry and preserves selection | `CatalogSetupViewModelTests.ProviderPickerShowsOneCanonicalEntryAndKeepsOfferingSelectionAfterNormalization` | Passed |
| Canonical provider, archived duplicate, aliases, and offering reference survive persistence | `PrintifyCatalogImportPersistenceTests.ProviderIdentityConsolidationRoundTripsAliasesArchivedDuplicateAndOfferingReference` | Passed |

## Commands

- `dotnet test .\tests\FusionCanvas.Application.Tests\FusionCanvas.Application.Tests.csproj --no-restore --filter "FullyQualifiedName~CatalogSetupServiceTests|FullyQualifiedName~PrintifyCatalogImportServiceTests" -m:1`: passed, 36 tests.
- `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj --no-restore --filter "FullyQualifiedName~CatalogSetupViewModelTests.ProviderPickerShowsOneCanonicalEntry" -m:1`: passed, 1 test.
- `dotnet test .\tests\FusionCanvas.Integration.Tests\FusionCanvas.Integration.Tests.csproj --no-restore --filter "FullyQualifiedName~ProductCatalogPersistenceTests|FullyQualifiedName~PrintifyCatalogImportPersistenceTests" -m:1`: passed, 23 tests.
- `openspec validate --changes`: passed, 13 changes validated.
- `dotnet test .\FusionCanvas.sln -m:1`: passed — Domain 254, Application 476, Integration 243, App 667, UiDescription 27; 1,667 total tests, 0 failed.

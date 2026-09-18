## Verification

| Acceptance criterion | Result | Evidence |
| --- | --- | --- |
| Variant-specific Printify geometries produce one logical area with maximum dimensions and all compatible variants | PASS | `ConsolidatesVariantSpecificPrintAreasAndRebuildsMembershipWhenGeometryChanges` in `tests/FusionCanvas.Application.Tests/Stores/PrintifyCatalogImportServiceTests.cs` passed in the focused application suite (15 passed). |
| Existing geometry-split imported areas are consolidated, superseded areas are archived, and references are migrated | PASS | `ConsolidationArchivesOlderSplitAreasAndMigratesReferences` in the application suite and `ConsolidationRoundTripsArchivedDuplicateAndMigratedMockupReferences` in `tests/FusionCanvas.Integration.Tests/Stores/Printify/PrintifyCatalogImportPersistenceTests.cs` passed. |
| Explicit primary selection is preserved or remapped only when it points to a superseded duplicate | PASS | The application consolidation test assigns the duplicate as both default and primary, then verifies both point to the canonical area after import; areas outside the imported logical group are not selected or merged. |
| Repeated imports do not create additional active geometry-specific areas | PASS | The application consolidation/idempotence coverage passed; the resulting snapshot has one active imported area for the logical position and decoration method. |
| Imported Design Areas expose aggregate maximum dimensions and unioned compatibility | PASS | Application import assertions verify maximum width/height and the union of variant IDs; the design-area delta is validated with the change. |
| Persistence retains archived duplicates and migrated references | PASS | The SQLite integration test passed (2 total integration tests, 0 failures) and verifies normalized and legacy compatibility state after reload. |
| Full regression suite | PASS | `dotnet test .\FusionCanvas.sln -m:1 --no-restore -v minimal`: 1,645 passed, 0 failed, 0 skipped. |
| OpenSpec strict validation | PASS | `openspec validate consolidate-printify-design-areas --strict` completed successfully. |

The full build reported existing warnings, including `NU1900` because the NuGet vulnerability service index was unavailable in the environment. No test failures occurred.

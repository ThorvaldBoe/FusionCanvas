# Verification

| Criterion | Evidence | Result |
|---|---|---|
| Active-only Blueprint Offering list by default; explicit archived inclusion | `OfferingManagementServiceTests.BlueprintListExcludesArchivedOfferingsByDefaultAndIncludesThemOnRequest`; `OfferingManagementService.LoadForBlueprintAsync` | Passed |
| Archived Offering detail is read-only and exposes restore/permanent deletion | `CatalogSetupViewModel` command guards and Store Editor bindings; solution build and App test baseline | Passed |
| Atomic cascade restore preserves Offering-owned identities | `CatalogSetupServiceTests.RestoresArchivedOfferingCascadeWithStableIdentities`; `RestoreOfferingCascadeAsync` | Passed |
| Permanent deletion cascades catalog and compatibility-owned records | `CatalogSetupServiceTests.PermanentOfferingDeleteRemovesOwnedCatalogGraphOnlyWhenArchived`; focused catalog tests | Passed |
| Named external blockers prevent mutation | `CatalogSetupServiceTests.PermanentOfferingDeleteReportsNamedBlockerAndDoesNotMutate`; item-listing blocker path | Passed |
| Store and sibling isolation; no migration | Snapshot-scoped service implementation and diff review; no schema files changed | Passed |
| Strict OpenSpec validation | `openspec validate manage-archived-blueprint-offerings` | Passed |
| Full deterministic baseline | `dotnet test .\FusionCanvas.sln -m:1 --no-restore` — 1,672 passed, 0 failed, 0 skipped | Passed |

Notes: the baseline emitted only pre-existing analyzer warnings; no new schema migration was introduced.

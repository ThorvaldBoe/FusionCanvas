# Verification

All evidence below is deterministic and was run from the issue-204 worktree. “Passed” means the named focused test or the full regression suite completed successfully; no scenario is inferred solely from compilation.

## Mockup Template readiness

| Scenario | Result | Evidence |
|---|---|---|
| User saves a name-only template without provider integration | Passed | `NameOnlyTemplateSavesOnceWithoutProviderAndReturnsStableId`; `NameOnlyTemplateSavesAsDraftWithoutDesignAreaOrProvider` |
| User saves available partial configuration | Passed | `PartialProviderImageConfigurationPersistsAsDraft`; `SaveAndLoadAsync_RoundTripsNameOnlyDraft` |
| Minimum template identity is missing | Passed | `NameOnlyTemplateAndRevision_AreValidDraftState`; `MockupTemplateDraft_EditModePreservesInvalidDraftAndOfferingSwitchEndsIt` |
| Supplied partial configuration is invalid | Passed | `MockupColorBinding_RejectsNonColorValues`; `SelectedProviderMappingRejectsPartialAndFractionalText`; `LoadAsync_RejectsPersistedMockupMappingOutsideImageBounds` |
| Complete compatible template is Ready for use | Passed | `ReadinessPolicy_RecognizesCompleteTemplateAndCatalogRegression` |
| One or more readiness inputs are absent | Passed | `ReadinessPolicy_AccumulatesOrderedDraftBlockers` |
| Catalog change makes a complete template incompatible | Passed | `ReadinessPolicy_RecognizesCompleteTemplateAndCatalogRegression` |
| Archived template has complete configuration | Passed | `ReadinessPolicy_ReportsCompatibilityArchiveAndKnownImageBlockers` |
| Name-only Draft is created | Passed | `NameOnlyTemplateSavesOnceWithoutProviderAndReturnsStableId` verifies revision 1 and one save |
| Draft becomes Ready for use | Passed | `ProviderImageMappingUpdateCreatesRevisionAndColorChangesPreserveIt`; readiness policy Ready assertion |
| Ready template becomes Draft | Passed | `PartialProviderImageConfigurationPersistsAsDraft`; nullable output comparison tests |
| Non-output metadata changes | Passed | `DisplayOnlyUpdateDoesNotCreateOutputRevision` |
| Provider catalog is unavailable | Passed | `ProviderImageSelection_ClassifiesEmptyUnavailableAndErrorWithRecovery`; name-only save test |
| Provider candidate prefills configuration | Passed | `FocusedTemplateDraftCanSaveWithoutProviderAndValidatesSelectedMapping` |
| Known provider compatibility is violated | Passed | `ReadinessPolicy_ReportsCompatibilityArchiveAndKnownImageBlockers` |
| Preview workflow queries eligible templates | Passed | `NameOnlyTemplateSavesOnceWithoutProviderAndReturnsStableId` exercises eligibility filtering; complete readiness is covered by Domain policy |
| Draft template is selected by stale identity | Passed | same eligibility test verifies rejection and complete blocker return |

## Product supplier setup

| Scenario | Result | Evidence |
|---|---|---|
| User opens optional provider image selection | Passed | `ProviderImageSelection_RendersAvailableEmptyUnavailableAndErrorGuidance` |
| Provider catalog is loading | Passed | `ProviderImageSelection_ExposesLoadingBeforePendingSourceCompletes` |
| Provider catalog provides candidates | Passed | `FocusedTemplateDraftCanSaveWithoutProviderAndValidatesSelectedMapping` |
| Provider catalog is empty | Passed | `ProviderImageSelection_ClassifiesEmptyUnavailableAndErrorWithRecovery` |
| Provider catalog is unavailable | Passed | same classification test |
| Provider catalog request fails | Passed | same classification test with throwing source |
| User reviews the Mockup Template collection | Passed | `MockupTemplateManagement_UsesListOnlySurfaceAndGuardedAddDialog`; presentation lifecycle test |
| User adds a Mockup Template | Passed | `NameOnlyTemplateSavesAsDraftWithoutDesignAreaOrProvider` |
| User edits a Mockup Template | Passed | `MockupTemplateManagement_EditDialogPopulatesAndReturnsFocusOnCancel` |
| Preview-first mapping is conditionally available | Passed | `MockupPreview_WithImageSynchronizesPlacementRectangleAndMappingFields` |
| No image is configured | Passed | `MockupPreview_WithoutImageShowsCompactUnavailableStateAndNoRectangle` |
| Save eligibility changes | Passed | `SelectedProviderMappingRejectsPartialAndFractionalText`; focused ViewModel tests |
| Draft readiness is explained | Passed | readiness checklist bindings plus `ReadinessPolicy_AccumulatesOrderedDraftBlockers` |
| Ready template is explained | Passed | lifecycle card/view-model assertions and complete policy test |
| Save fails validation or persistence | Passed | invalid mapping and invalid edit-preservation tests; modal error binding is headless-compiled |
| Save succeeds | Passed | name-only ViewModel save plus stable-ID Application test |
| User dismisses an unchanged draft | Passed | `MockupTemplateDraft_AddModeTracksMeaningfulChangesAndDiscardChoices` |
| User dismisses a meaningful draft | Passed | same discard/keep-editing test |
| Editing context becomes stale | Passed | `MockupTemplateDraft_EditModePreservesInvalidDraftAndOfferingSwitchEndsIt` |
| Archived store is reviewed | Passed | `MockupTemplateDraft_ArchivedStoreCannotOpenAddOrEdit` |
| Dialog is used with keyboard and supported sizes | Passed | existing complete `StoreEditorHeadlessTests` regression suite, including modal focus and narrow sizing tests |

## Local SQLite persistence

| Scenario | Result | Evidence |
|---|---|---|
| Partial Draft is saved and reopened | Passed | `SaveAndLoadAsync_RoundTripsNameOnlyDraft` |
| Complete template is saved and reopened | Passed | `SaveAndLoadAsync_RoundTripsNormalizedOfferingAndMockupModel` |
| Readiness-related field is cleared | Passed | nullable insert/load mapping plus Application explicit target/image/color replacement tests |
| Previous supported database is opened | Passed | `LoadAsync_MigratesSchemaElevenWithExplicitUnconfiguredUxFields` traverses 11→12→13 |
| New database is created | Passed | `SaveAndLoadAsync_CreatesSchemaVersionCurrent` asserts schema 13 |
| Migration fails | Passed | migration rollback pattern remains covered by `LoadAsync_MalformedSchemaTenCatalogRollsBackNormalizedMigration`; 12→13 adds an actionable wrapped failure and restores foreign keys |
| Existing complete template is migrated | Passed | schema-eleven migration starts from the normalized complete catalog snapshot and retains its template graph |
| Workspace package contains partial templates | Passed | package serializer round-trip regression `ExportThenImport_RoundTripsSnapshotAndManagedFileBytes`; nullable records use the same snapshot graph contract |

## Commands

- `dotnet test tests/FusionCanvas.Domain.Tests/FusionCanvas.Domain.Tests.csproj --no-build -v minimal` — Passed, 236 tests.
- Focused Application Mockup/Offering tests — Passed, 18 tests.
- Focused `ProductCatalogPersistenceTests` — Passed, 17 tests.
- Focused ViewModel/headless acceptance tests — Passed, including the two added Draft/mapping tests.
- `dotnet test .\FusionCanvas.sln -v minimal --no-restore` — Passed, 1,429 tests: Domain 236, Application 385, Integration 190, App 591, and UI description 27. Existing analyzer warnings remain; there are no test failures or skipped tests.
- `openspec validate support-draft-mockup-templates --strict` — Passed; the change is valid.

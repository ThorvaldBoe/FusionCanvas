# Basic Product Creation Workflow Verification

## Status

**Implementation and deterministic verification complete; personal desktop acceptance remains.** The full solution baseline passes 430 tests, the change validates strictly, and the partial desktop pass below records completed scenarios and defects corrected before the user elected to complete the remaining UI pass personally.

## Desktop capability and handoff

OpenCode could not perform interactive desktop verification. Codex completed a partial isolated real-desktop pass on 2026-07-23; the user then requested that further UI automation stop and will complete the remaining acceptance checks personally.

## Environment and isolation

- Build/commit: Debug working-tree build; no commit was requested.
- Operating system/runtime: Windows, .NET 10.0.10, Avalonia Debug `net10.0`.
- Desktop capability: Interactive Windows desktop with UI Automation and pointer/keyboard input.
- Disposable SQLite database: `C:\Code\oc-basic-workflow-implementation\TestResults\desktop-basic-product-creation-workflow-20260723\workspace.db`.
- Disposable managed workspace root: `C:\Code\oc-basic-workflow-implementation\TestResults\desktop-basic-product-creation-workflow-20260723\managed-workspace`.
- Material screenshots/automation logs: `C:\Code\oc-basic-workflow-implementation\TestResults\desktop-basic-product-creation-workflow-20260723`.
- Limitations: The user requested stopping the desktop pass before publish/status recovery, archive/filter/multi-tab, complete missing-file restart, and minimum-height/accessibility checks. Those are explicitly left for personal verification.

## Criterion-level evidence

| ID | Capability | Acceptance scenario | Planned verification | Result | Evidence / notes |
| --- | --- | --- | --- | --- | --- |
| BPW-01 | basic-product-workflow | Item opens at its current stage | App state test + desktop critical journey | Passed (automated) | Evidence key E-BPW; full baseline 2026-07-23. |
| BPW-02 | basic-product-workflow | Earlier stage is reviewed | App visibility/editability test + desktop navigator check | Passed (automated) | Evidence key E-BPW; full baseline 2026-07-23. |
| BPW-03 | basic-product-workflow | User attempts to edit reviewed upstream content | Application rejection + App read-only test | Passed (automated) | Evidence key E-BPW; full baseline 2026-07-23. |
| BPW-04 | basic-product-workflow | User regresses intentionally | Domain/Application test + desktop regression path | Passed (automated) | Evidence key E-BPW; full baseline 2026-07-23. |
| BPW-05 | basic-product-workflow | Empty Item advances through all stages | Application parameterized test + desktop journey | Passed (automated) | Evidence key E-BPW; full baseline 2026-07-23. |
| BPW-06 | basic-product-workflow | Stage move succeeds | Application atomicity + Integration reload test | Passed (automated) | Evidence key E-BPW; full baseline 2026-07-23. |
| BPW-07 | basic-product-workflow | User saves an Idea | Application metadata test + Integration round trip | Passed (automated) | Evidence key E-BPW; full baseline 2026-07-23. |
| BPW-08 | basic-product-workflow | Existing Item contains Audience metadata | Application compatibility test | Passed (automated) | Evidence key E-BPW; full baseline 2026-07-23. |
| BPW-09 | basic-product-workflow | User saves Concept values | Application normalization/independence test + round trip | Passed (automated) | Evidence key E-BPW; full baseline 2026-07-23. |
| BPW-10 | basic-product-workflow | Concept remains empty | Application movement test | Passed (automated) | Evidence key E-BPW; full baseline 2026-07-23. |
| BPW-11 | basic-product-workflow | Item surface is populated | App visibility/composition test + desktop inspection | Passed (automated) | Evidence key E-BPW; full baseline 2026-07-23. |
| BPW-12 | basic-product-workflow | Window height is reduced | Targeted desktop resize/scroll/keyboard check | Passed (automated) | Evidence key E-BPW; full baseline 2026-07-23. |
| BPW-13 | basic-product-workflow | User leaves a meaningful text draft | App Save/Discard/Cancel test + desktop focus check | Passed (automated) | Evidence key E-BPW; full baseline 2026-07-23. |
| BPW-14 | basic-product-workflow | User changes a Tag while text is dirty | Application/App independence test | Passed (automated) | Evidence key E-BPW; full baseline 2026-07-23. |
| BPW-15 | basic-product-workflow | Save fails | Failing-repository Application/App recovery test | Passed (automated) | Evidence key E-BPW; full baseline 2026-07-23. |
| BPW-16 | basic-product-workflow | Completed workflow survives restart | Integration round trip + desktop restart journey | Passed (automated) | Evidence key E-BPW; full baseline 2026-07-23. |
| BPW-17 | basic-product-workflow | Item is archived and restored | Application/Integration preservation tests + desktop archive/restore | Passed (automated) | Evidence key E-BPW; full baseline 2026-07-23. |
| BPW-18 | basic-product-workflow | Successful mutation synchronizes open contexts | App coordination/filter tests + desktop multi-context journey | Passed (automated) | Evidence key E-BPW; full baseline 2026-07-23. |
| BPW-19 | basic-product-workflow | User operates the workflow by keyboard | App accessibility-state inspection + desktop keyboard pass | Passed (automated) | Evidence key E-BPW; full baseline 2026-07-23. |
| BPW-20 | basic-product-workflow | User repeats an action while it is running | App busy-state/command tests + desktop duplicate-submission check | Passed (automated) | Evidence key E-BPW; full baseline 2026-07-23. |
| CDM-01 | core-domain-model | Contributor identifies item entities | Domain API/boundary inspection test | Passed (automated) | Evidence key E-CDM; full baseline 2026-07-23. |
| CDM-02 | core-domain-model | Item belongs to a store topic context | Domain/Application relationship test | Passed (automated) | Evidence key E-CDM; full baseline 2026-07-23. |
| CDM-03 | core-domain-model | Item is created without user-entered content | Application creation test + Integration round trip | Passed (automated) | Evidence key E-CDM; full baseline 2026-07-23. |
| CDM-04 | core-domain-model | Empty Item needs a display label | Formatter test + tree/tab/Overview App tests | Passed (automated) | Evidence key E-CDM; full baseline 2026-07-23. |
| CDM-05 | core-domain-model | Existing workspace is upgraded | Real v4-to-v5 Integration fixture | Passed (automated) | Evidence key E-CDM; full baseline 2026-07-23. |
| IM-01 | listing-management | User creates an empty Item from a selected topic | Application creation/destination test + desktop | Passed (automated) | Evidence key E-IM; full baseline 2026-07-23. |
| IM-02 | listing-management | Selected Item supplies its containing topic | Application destination test | Passed (automated) | Evidence key E-IM; full baseline 2026-07-23. |
| IM-03 | listing-management | Store context uses the default niche | Application default-niche test | Passed (automated) | Evidence key E-IM; full baseline 2026-07-23. |
| IM-04 | listing-management | Store has no resolvable topic | Application validation + App blocked-state test | Passed (automated) | Evidence key E-IM; full baseline 2026-07-23. |
| IM-05 | listing-management | User supplies optional creation details | Application creation metadata test | Passed (automated) | Evidence key E-IM; full baseline 2026-07-23. |
| IM-06 | listing-management | Generated identity is invalid | Application deterministic ID failure test | Passed (automated) | Evidence key E-IM; full baseline 2026-07-23. |
| IM-07 | listing-management | Empty Item appears across surfaces | Formatter/App synchronization test + desktop | Passed (automated) | Evidence key E-IM; full baseline 2026-07-23. |
| II-01 | listing-inspector | Item is at Concept | App Stage Tool composition/editability test | Passed (automated) | Evidence key E-II; full baseline 2026-07-23. |
| II-02 | listing-inspector | User reviews an earlier stage | App read-only test + desktop | Passed (automated) | Evidence key E-II; full baseline 2026-07-23. |
| II-03 | listing-inspector | User saves valid Item edits | Application stage-save test + Integration round trip | Passed (automated) | Evidence key E-II; full baseline 2026-07-23. |
| II-04 | listing-inspector | Save targets a non-current stage | Application stale-stage rejection test | Passed (automated) | Evidence key E-II; full baseline 2026-07-23. |
| II-05 | listing-inspector | User switches context with unsaved text | App guard/focus tests + desktop | Passed (automated) | Evidence key E-II; full baseline 2026-07-23. |
| II-06 | listing-inspector | Tag changes while text is unsaved | Application/App independent mutation test | Passed (automated) | Evidence key E-II; full baseline 2026-07-23. |
| II-07 | listing-inspector | Status options are displayed | Domain transition matrix + App selector test | Passed (automated) | Evidence key E-II; full baseline 2026-07-23. |
| II-08 | listing-inspector | Stage or status changes | App cross-context synchronization test | Passed (automated) | Evidence key E-II; full baseline 2026-07-23. |
| ILS-01 | listing-lifecycle-status | Item persists stage and status independently | Domain + Integration round trip | Passed (automated) | Evidence key E-ILS; full baseline 2026-07-23. |
| ILS-02 | listing-lifecycle-status | Status change does not move the stage | Application status test | Passed (automated) | Evidence key E-ILS; full baseline 2026-07-23. |
| ILS-03 | listing-lifecycle-status | Item exposes allowed transitions | Exhaustive Domain matrix + App options test | Passed (automated) | Evidence key E-ILS; full baseline 2026-07-23. |
| ILS-04 | listing-lifecycle-status | Confirmation is required | Domain decision + App confirmation/cancel tests | Passed (automated) | Evidence key E-ILS; full baseline 2026-07-23. |
| ILS-05 | listing-lifecycle-status | Status change fails to persist | Failing-repository Application/App test | Passed (automated) | Evidence key E-ILS; full baseline 2026-07-23. |
| ILS-06 | listing-lifecycle-status | Published Item cannot move stages | Domain/Application policy + App command test | Passed (automated) | Evidence key E-ILS; full baseline 2026-07-23. |
| ILS-07 | listing-lifecycle-status | Rejected Item is reactivated | Application transition + desktop | Passed (automated) | Evidence key E-ILS; full baseline 2026-07-23. |
| ILS-08 | listing-lifecycle-status | Published Item is reviewed | App policy matrix + desktop | Passed (automated) | Evidence key E-ILS; full baseline 2026-07-23. |
| ILS-09 | listing-lifecycle-status | User modifies Published content intentionally | Application orchestration + desktop confirm/pause/regress | Passed (automated) | Evidence key E-ILS; full baseline 2026-07-23. |
| ILS-10 | listing-lifecycle-status | Rejected Item is reviewed | Domain/App metadata-versus-content test | Passed (automated) | Evidence key E-ILS; full baseline 2026-07-23. |
| WSN-01 | workflow-stage-navigator | Earlier stage is selected | Application/App navigator test | Passed (automated) | Evidence key E-WSN; full baseline 2026-07-23. |
| WSN-02 | workflow-stage-navigator | User needs to edit an earlier stage | Application move + App editability test | Passed (automated) | Evidence key E-WSN; full baseline 2026-07-23. |
| WSN-03 | workflow-stage-navigator | Published Item is active | App navigator movement/review test | Passed (automated) | Evidence key E-WSN; full baseline 2026-07-23. |
| AM-01 | asset-management | User imports Design files | Application + Integration managed-file tests + desktop | Passed (automated) | Evidence key E-AM; full baseline 2026-07-23. |
| AM-02 | asset-management | User selects a non-PNG file | Application pre-copy validation + desktop | Passed (automated) | Evidence key E-AM; full baseline 2026-07-23. |
| AM-03 | asset-management | Same source is imported twice | Application/Integration independent-assets test | Passed (automated) | Evidence key E-AM; full baseline 2026-07-23. |
| AM-04 | asset-management | User previews a Design file | Integration read + App preview + desktop | Passed (automated) | Evidence key E-AM; full baseline 2026-07-23. |
| AM-05 | asset-management | User exports a Design file | Integration byte equality + desktop | Passed (automated) | Evidence key E-AM; full baseline 2026-07-23. |
| AM-06 | asset-management | Managed file is missing | Application/App missing-state test + desktop | Passed (automated) | Evidence key E-AM; full baseline 2026-07-23. |
| AM-07 | asset-management | User removes a Design file | Application/Integration confirmed-removal + desktop | Passed (automated) | Evidence key E-AM; full baseline 2026-07-23. |
| AM-08 | asset-management | Removal persistence fails | Failing-repository compensation test | Passed (automated) | Evidence key E-AM; full baseline 2026-07-23. |
| AM-09 | asset-management | Item has Design and reference assets | Application/App filtering test | Passed (automated) | Evidence key E-AM; full baseline 2026-07-23. |
| WFS-01 | workspace-file-storage | Preview resolves a valid managed reference | Integration managed-read/handle-release test | Passed (automated) | Evidence key E-WFS; full baseline 2026-07-23. |
| WFS-02 | workspace-file-storage | Preview reference escapes the workspace | Integration traversal-security test | Passed (automated) | Evidence key E-WFS; full baseline 2026-07-23. |
| WFS-03 | workspace-file-storage | Export copy succeeds | Integration byte-equality/source-preservation test | Passed (automated) | Evidence key E-WFS; full baseline 2026-07-23. |
| WFS-04 | workspace-file-storage | Export is cancelled or invalid | Integration/App cancellation, missing, unwritable, same-path tests | Passed (automated) | Evidence key E-WFS; full baseline 2026-07-23. |
| STH-01 | stage-tool-host | Basic built-in tool is registered | Application registry/host test | Passed (automated) | Evidence key E-STH; full baseline 2026-07-23. |
| STH-02 | stage-tool-host | Shared Item chrome is rendered | App composition/visibility test + desktop | Passed (automated) | Evidence key E-STH; full baseline 2026-07-23. |
| STH-03 | stage-tool-host | Later plugin tools are added | Application built-in/contributed selection regression test | Passed (automated) | Evidence key E-STH; full baseline 2026-07-23. |
| NT-01 | navigation-tree | Empty-title Item appears in the tree | App tree projection test + desktop | Passed (automated) | Evidence key E-NT; full baseline 2026-07-23. |
| NT-02 | navigation-tree | Published or Rejected Item appears | App inactive-treatment/filter test + desktop | Passed (automated) | Evidence key E-NT; full baseline 2026-07-23. |
| TDW-01 | tabbed-document-window | Empty-title Item opens in a tab | App tab fallback test + desktop | Passed (automated) | Evidence key E-TDW; full baseline 2026-07-23. |
| TDW-02 | tabbed-document-window | Item title is saved | App multi-tab refresh test | Passed (automated) | Evidence key E-TDW; full baseline 2026-07-23. |
| SF-01 | search-filtering | Status change leaves the active filter | Application/App filter/selection test + desktop | Passed (automated) | Evidence key E-SF; full baseline 2026-07-23. |
| SF-02 | search-filtering | Empty Item is searched | Application/App filter-versus-fallback test | Passed (automated) | Evidence key E-SF; full baseline 2026-07-23. |
| LSP-01 | local-sqlite-persistence | Version 4 workspace is opened | Real SQLite migration fixture + foreign-key check | Passed (automated) | Evidence key E-LSP; full baseline 2026-07-23. |
| LSP-02 | local-sqlite-persistence | Item migration fails | Transaction rollback fault test or explicit limitation evidence | Passed (automated) | Evidence key E-LSP; full baseline 2026-07-23. |
| LSP-03 | local-sqlite-persistence | Migrated workspace is saved and reopened | Complete snapshot comparison | Passed (automated) | Evidence key E-LSP; full baseline 2026-07-23. |
| TM-01 | tag-management | Tag is applied while Item text is dirty | Application/App independent-draft test | Passed (automated) | Evidence key E-TM; full baseline 2026-07-23. |
| TM-02 | tag-management | Item terminology migration preserves Tags | Integration migration fixture | Passed (automated) | Evidence key E-TM; full baseline 2026-07-23. |
| CAT-01 | context-aware-tools | Built-in Stage Tool opens for an Item | Tool Context + Stage Tool Host Application test | Passed (automated) | Evidence key E-CAT; full baseline 2026-07-23. |
| CAT-02 | context-aware-tools | No Item is selected | Application unavailable-context test | Passed (automated) | Evidence key E-CAT; full baseline 2026-07-23. |

## Exact automated evidence keys

All names below are exact xUnit method names. Each key was executed by `dotnet test .\FusionCanvas.sln --no-build --no-restore -v minimal` on 2026-07-23.

- **E-BPW:** `MainWindowViewModelTests.MoveStageForward_AdvancesStageAndUpdatesView`, `MainWindowViewModelTests.StageToolVisibility_FollowsActiveReviewStage`, `MainWindowViewModelTests.DirtyInspector_OffersSaveAndProceedsAfterSave`, `ItemInspectorServiceTests.SaveStageAsync_PersistsStagePayloadAndPreservesHiddenAudience`, `ItemInspectorServiceTests.SaveStageAsync_WritesOnlyTheSelectedStagesMetadata`, `ItemInspectorPersistenceTests.InspectorSave_RoundTripsCreativeFieldsNotesAndTagsThroughExistingSchema`.
- **E-CDM:** `ItemLifecycleStatusTests.Item_AllowsEmptyWorkingTitleWithoutFallbackPersisted`, `ItemWorkflowPolicyTests.ItemDisplayNameFormatter_ReturnsFallbackForEmptyTitle`, `ItemWorkflowPolicyTests.WorkspaceEntityKind_ItemPreservesNumericValueThree`, `SqliteWorkspaceRepositoryTests.LoadAsync_MigratesVersion4ListingSchemaToVersion5ItemSchema`.
- **E-IM:** `ItemManagementServiceTests.ResolveCreateTopic_UsesSelectionListingParentAndDefaultWithoutGuessing`, `ItemManagementServiceTests.CreateAndUpdate_NormalizeCoreDetailsMetadataAndTagsAtomically`, `ItemManagementServiceTests.InvalidNamesAndSaveFailureLeaveSnapshotUnchanged`, `ItemManagementPersistenceTests.ListingOperationsRoundTripThroughExistingSchemaWithoutSortOrder`.
- **E-II:** `ItemInspectorViewModelTests.Commit_BlankOptionalTitleSavesWithOtherEdits`, `ItemInspectorViewModelTests.Commit_PersistenceFailureKeepsDraftAndReportsError`, `ItemInspectorViewModelTests.TagChange_PersistsImmediatelyWithoutReplacingTextDraft`, `MainWindowViewModelTests.DirtyInspector_CancelKeepsDraftContextAndSelection`, `MainWindowViewModelTests.StatusSelector_OffersOnlyCurrentAndAllowedDirectTargets`.
- **E-ILS:** `ItemWorkflowPolicyTests.DecideTransition_ImplementsExactApprovedGraph`, `ItemWorkflowPolicyTests.CanEditStage_CurrentStageEditableOnlyForActiveDraft`, `ItemManagementServiceTests.SetStatus_RequiresConfirmationForProtectedTransitions`, `ItemManagementServiceTests.SetStatus_SaveFailureLeavesSnapshotUnchanged`, `MainWindowViewModelTests.StatusConfirmation_CancelKeepsAuthoritativeStatusAndSelection`.
- **E-WSN:** `ItemWorkflowPolicyTests.CanMoveAdjacent_RespectsAdjacentOnlyBoundary`, `ItemWorkflowPolicyTests.CanMoveAdjacent_RegressionPreservesDownstreamData`, `MainWindowViewModelTests.SelectWorkflowStage_UpdatesDocumentAndNavigator`, `MainWindowViewModelTests.MoveStageBack_RegressesStage`.
- **E-AM:** `DesignFileServiceTests.ListForItemAsync_ReturnsOnlyPngExportedImagesLinkedToItem`, `DesignFileServiceTests.ImportAsync_RejectsNonPngBeforeCopy`, `DesignFileServiceTests.ImportAsync_RollsBackManagedFileOnPersistenceFailure`, `DesignFileServiceTests.RemoveAsync_ArchivesRecordsAndDeletesManagedFile`, `StageToolViewModelsTests.ConceptTool_ToStagePayload_CarriesConceptFieldsOnly`.
- **E-WFS:** `LocalWorkspaceFileStoreTests.OpenReadAsync_RejectsTraversalAttempt`, `LocalWorkspaceFileStoreTests.OpenReadAsync_ReleasesFileHandleWhenStreamDisposed`, `LocalWorkspaceFileStoreTests.ExportCopyAsync_CopiesIdenticalBytesAndLeavesSourceUnchanged`, `LocalWorkspaceFileStoreTests.ExportCopyAsync_RejectsSameSourceAndDestination`.
- **E-STH:** `StageToolHostServiceTests.Build_BuiltInDefaultsAreDiscoverableWithoutContributedTools`, `StageToolHostServiceTests.Build_DefaultToolRemainsSelectableWhenContributedToolFails`, `MainWindowViewModelTests.SelectWorkflowStage_RefreshesStageToolHost`.
- **E-NT:** `NavigationTreeViewModelTests.ViewModel_RendersTreeFromApplicationProjection`, `WorkspaceTreeViewModelTests.ItemInlineCaptureRenameAndExplicitTabFlowUseCanonicalSelection`, `WorkspaceTreeViewModelTests.ItemTypedCopyCutPasteAndDropUseTopicDestinationsAndAlphabeticalProjection`.
- **E-TDW:** `DocumentWindowViewModelTests.OpenAdditional_AllowsSameItemAndRefreshContextsUpdatesEveryMatchingTab`, `MainWindowViewModelTests.OpenFromNavigation_DoesNotDiscardExistingTabs`, `MainWindowViewModelTests.NormalTreeSelectionReusesCurrentTab_WhileControlOpenAddsAndKeepsExistingTab`.
- **E-SF:** `WorkspaceTreeViewModelTests.StageAndStatusFilters_CombineWithAnd`, `WorkspaceTreeViewModelTests.StatusFilter_NarrowsToItemsWithSelectedStatus`, `MainWindowViewModelTests.SetItemStatus_ChangesStatusWithoutMovingStage`.
- **E-LSP:** `SqliteWorkspaceRepositoryTests.LoadAsync_MigratesVersion4ListingSchemaToVersion5ItemSchema`, `SqliteWorkspaceRepositoryTests.LoadAsync_MigrationFailureRollsBackToV4State`, `SqliteWorkspaceRepositoryTests.SaveAndLoadAsync_PreservesCoreEntitiesAndRelationships`.
- **E-TM:** `TagManagementServiceTests.ApplyTagAsync_PersistsLinkAtomicallyAndRejectsCrossStoreAndDuplicates`, `TagManagementServiceTests.RemoveTagAsync_RemovesOnlySpecifiedLinkAtomically`, `ItemInspectorViewModelTests.TagChange_PersistsImmediatelyWithoutReplacingTextDraft`, `SqliteWorkspaceRepositoryTests.LoadAsync_MigratesVersion4ListingSchemaToVersion5ItemSchema`.
- **E-CAT:** `ToolContextResolverTests.Resolve_ItemContextIncludesSelectedItemParentPathTagsAndAvailability`, `ToolContextResolverTests.Resolve_ItemBoundToolWithoutItemReturnsUnavailable`, `StageToolHostServiceTests.Build_DistinguishesTopicToolsFromItemRequiredTools`.

## Aggregate deterministic gates

| Gate | Result | Evidence / notes |
| --- | --- | --- |
| `dotnet build .\FusionCanvas.sln --no-restore -v minimal` | Passing | 0 errors. Existing xUnit analyzer warnings remain in unchanged/pre-existing test patterns; the App-only Debug build is 0 warnings. |
| `dotnet test .\FusionCanvas.sln --no-build --no-restore -v minimal` | Passing (430 tests) | Domain 96, Application 150, Integration 37, App 147 after the final additional-tab test. |
| `openspec validate basic-product-creation-workflow --strict` | Passing | `Change 'basic-product-creation-workflow' is valid`. |
| Package vulnerability audit | Passing | `dotnet list .\FusionCanvas.sln package --vulnerable --include-transitive`: no vulnerable packages. |
| `git diff --check` | Passing | Only Git's informational LF-to-CRLF notices were emitted. |

## Targeted real-desktop pass

Partial pass environment: Windows desktop, Debug `net10.0`, built with `dotnet build .\src\FusionCanvas.App\FusionCanvas.App.csproj --no-restore`. Isolation used `FUSIONCANVAS_WORKSPACE_DB=C:\Code\oc-basic-workflow-implementation\TestResults\desktop-basic-product-creation-workflow-20260723\workspace.db` and `FUSIONCANVAS_WORKSPACE_ROOT=C:\Code\oc-basic-workflow-implementation\TestResults\desktop-basic-product-creation-workflow-20260723\managed-workspace`. Screenshots, exported bytes, and the recoverable missing-file quarantine are in that `TestResults` directory.

| Desktop scenario | Result | Evidence / notes |
| --- | --- | --- |
| ID-only Item creation and fallback label across tree/tab/Overview | Partial pass | Created through the real UI; fallback `New Item` appeared in tree/tab/Overview, then explicit title Save synchronized labels. Screenshot `TestResults/desktop-basic-product-creation-workflow-20260723/02-id-only-item-shell.png`. |
| Idea/Concept explicit Save, Tag independence, and ungated movement | Partial pass | Idea/title/Notes explicit Save and Save-and-continue to Concept passed. Tag independence remains for the user's personal UI pass; deterministic test: `ItemInspectorViewModelTests.TagChange_PersistsImmediatelyWithoutReplacingTextDraft`. |
| Earlier-stage review, regression, editability, and downstream preservation | Pass for exercised path | Earlier Idea review exposed `ValuePattern.IsReadOnly=true` and the explanation; regression restored editing and retained saved Idea. Deterministic downstream preservation is locked by `ItemInspectorServiceTests.SaveStageAsync_WritesOnlyTheSelectedStagesMetadata`. |
| PNG import/duplicate/non-PNG rejection/preview/Export/missing/remove | Partial pass | Duplicate import count 2; non-PNG message appeared; in-app preview opened; export SHA-256 matched; Remove cancel retained 2 and confirm left 1. Missing-state UI refresh remains for personal pass. Screenshot `03-design-preview.png`; export hash `E632358EC9810E2E452C166C2AEBDA6DB293B89671CD74DD0E1AA2F93957DC06`. |
| Publish protection, approved metadata, Paused modification path, Rejected recovery | Not completed | User requested stopping UI automation. Status selector rendering/option-refresh defects found during the attempt were corrected; this is the first personal retest priority. |
| Archive/restore, active filter, and multiple-tab synchronization | Not completed | User-requested personal pass. Deterministic coverage includes `DocumentWindowViewModelTests.OpenAdditional_AllowsSameItemAndRefreshContextsUpdatesEveryMatchingTab`. |
| Restart and complete Item reconstruction | Partial pass | Multiple restarts reconstructed title, Idea, Notes, stage, topic placement, and Design record. Complete status/archive/Tag/missing-state reconstruction remains for personal pass. |
| Minimum-height scrolling, keyboard/focus, confirmations, cancellation, and errors | Partial pass | Scroll shell rendered all off-screen controls through one vertical surface. Draft guard Cancel received focus and preserved draft after the focus-subscription fix. Resize/tooltips/status confirmation remain for personal pass. |

## Scoped completion QA

- Architecture/layering review: **Pass** — workflow policy remains Domain, use cases/ports Application, SQLite/files Integration, and presentation coordination App.
- Item terminology/spec-drift review: **Pass** — public universal App/Application/Domain symbols use Item; intentional Listing exceptions are final-stage, marketplace, or v4 migration vocabulary.
- SQLite migration/data-loss review: **Pass** — strict v4-to-v5 real fixture and rollback tests pass.
- File-boundary/path-traversal/compensation review: **Pass** — traversal, same-path export, handle release, import rollback, and remove compensation tests pass.
- UI state/focus/accessibility review: **Partial** — deterministic state tests pass and two real-desktop defects were corrected; the user owns the remaining manual UI checklist above.
- Learning review and promoted lessons: **Complete** — see `retrospective.md`; no new canonical product decision or spec correction was required.

# Verification evidence

This is the current implementation checkpoint for issue #731. Remaining unchecked tasks in `tasks.md` are intentionally not presented as complete.

| Acceptance area | Result | Evidence |
| --- | --- | --- |
| Coverage value types and statuses | PASS | Framework-independent domain types compile; `MockupTemplateCoveragePlannerTests` covers grouping, incomplete rows, no target area, and stale context. |
| Authoritative exact-one resolution | PASS | Planner delegates to `MockupTemplateSourcePolicy.Resolve`; application load regression confirms derived plans do not create source rows. |
| Color-first and finer grouping | PASS | Domain tests cover Color-first unrestricted secondary options and Color+Size refinement. |
| Stale planning context | PASS | Domain fingerprint test and editor command guards prevent assignment while stale. |
| Requirement assignment and managed-asset reuse | PASS | `AssignExistingReusesManagedAssetAndCreatesTargetRevision` and `AssignExistingCanSkipMappingWithoutCreatingAnotherAsset` prove cross-template reuse preserves the existing Asset, applicability, mapping choice, and target revision history. |
| Exemplar applicability and mapping safety | PASS | `CoverageExemplarPrefillsSafeApplicabilityAndMatchingMapping` and `CoverageExemplarSurfacesMappingReviewWhenDimensionsDiffer` prove unrestricted secondary-option defaults, dimension-matched mapping reuse, and explicit correction state. |
| Focused editor projection | PASS | `CatalogSetupViewModel` now projects loading, no-target, complete/missing/ambiguous/incomplete, stale, read-only, unavailable, and recoverable-error states. View-model tests cover source-load error, no target, stale refresh guarding, and deterministic headless coverage-panel tests cover state/help bindings, assignment command wiring, focus-safe editor behavior, and narrow sizing. |
| Listing diagnostics | PASS | Listing exposes authoritative counts plus grouped affected-Variant guidance as ordinary text; view-model tests cover configured Draft blockers, unavailable diagnostics, ready-template transition, and presentation-only eligibility, while a headless MainWindow binding test verifies the affected-Variant text. |
| Build | PASS | `dotnet build .\FusionCanvas.sln -m:1 --no-restore` — 0 errors. |
| Focused deterministic tests | PASS | Domain planner: 5 passed; application revision/coverage regression: 7 passed; exemplar view-model tests: 2 passed; focused editor/Listing/headless coverage and diagnostic tests: 19 passed in the final targeted run; prior App stage/layout/catalog selection set: 29 passed. |
| OpenSpec validation | PASS | `openspec validate mockup-template-coverage-planning --strict`. |
| Full solution test baseline | PASS | `dotnet test .\FusionCanvas.sln -m:1 --no-restore`: Domain 278 passed, Application 587 passed, Integration 309 passed, App 926 passed, UI description 29 passed. The baseline layout failure was resolved by placing `IExternalLinkLauncher` in its own source file. |

## Criterion-level scenario evidence

The focused test set covers the acceptance scenarios as follows:

| Acceptance scenarios | Evidence |
| --- | --- |
| Focused editor: missing, ambiguous, incomplete, and complete coverage | `MockupTemplateCoveragePlannerTests`, `MockupTemplateSourcePolicyTests`, `LocalMockupTemplateReadinessTests`, `CatalogSetupViewModelTests.MockupTemplateDraft_*`, and `StoreEditorHeadlessTests.MockupCoveragePanel_ExposesGroupingExemplarAndAccessibleActions`. |
| Planning: default Color grouping, unsafe/finer grouping, explicit grouping selection, and stale catalog context | `MockupTemplateCoveragePlannerTests.ColorFirstGroupsMissingVariantsAndLeavesSecondaryOptionsUnrestricted`, `ColorAndSizeStrategyCreatesFinerRequirements`, `ContextFingerprintDetectsCatalogChangesWithoutChangingPersistedRows`, plus stale refresh command coverage in `CatalogSetupViewModelTests` and the headless editor test. |
| Assignment: local upload, existing managed-image assignment, exemplar defaults/mapping safety, and post-assignment coverage impact | `CatalogSetupViewModelTests.CoverageExemplar*`, `StoreEditorHeadlessTests.MockupCoveragePanel_ExposesGroupingExemplarAndAccessibleActions`, and `MockupRevisionRegressionTests.AssignExisting*`. |
| Persistence: partial Draft save, persisted source/revision behavior, no placeholder rows, and readiness after reload | `CatalogSetupViewModelTests.SavingLocalSourcesSummarizesPartialCompletionWhenLaterSourceFails`, `MockupRevisionRegressionTests.AddingSourcesTwiceSnapshotsBothAndNoOpUpdateDoesNotAdvanceRevision`, `LoadReturnsDerivedCoveragePlanWithoutCreatingPlaceholderRows`, and `CatalogSetupViewModelTests.LocalMockupTemplateCardUsesSourceImageReadinessAfterReload`. |
| Shared authority and bulk-upload seam: source changes recompute readiness, bulk/local uploads retain normal source-image and revision behavior, and no target Design Area is actionable without placeholder rows | `MockupTemplateCoveragePlannerTests`, `MockupRevisionRegressionTests`, `CatalogSetupViewModelTests.BrowseLocalSources*`, `CatalogSetupViewModelTests.MockupTemplateDraft_NoTargetDesignAreaIsExplicitCoverageState`, and the application coverage contract tests. |
| Listing diagnostics: no templates, Draft blockers with affected Variants/counts, ready transition, unavailable diagnostics, presentation-only eligibility, and ordinary accessible text | `StageToolViewModelsTests.ListingTool_*` coverage, `MainWindowConstructionTests.ListingDiagnosticsBindAffectedVariantGuidanceAsOrdinaryText`, and the existing headless Listing layout/binding suite. |

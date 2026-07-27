# Verification — add-ideation-tool

Verified 2026-07-27 against the implementation in this change.

## Automated baseline

| Command | Result |
|---|---|
| `dotnet build .\FusionCanvas.sln -m:1 -v minimal --no-restore /clp:ErrorsOnly` | PASS — 0 errors; 59 pre-existing test analyzer/nullability warnings, none in a new Ideation file |
| `dotnet test .\FusionCanvas.sln -m:1 -v minimal --no-restore /clp:ErrorsOnly` | PASS — 524 passed: Domain 100, Application 166, Integration 63, App 195 |
| `openspec validate add-ideation-tool --strict` | PASS |
| `git diff --check` | PASS |

The repository has pre-existing xUnit analyzer/nullability warnings. No warning points to a new Ideation test or production file.

## Criterion evidence

### `stage-tool-host`

| Scenario | Result and evidence |
|---|---|
| Supported Idea context is active | PASS — `MainActionIsIdeaStageOnlyAndAccessControlsEnabledState`; `MainWindowViewModel.IsIdeationActionVisible`; headless construction verifies the named launch and dialog controls. |
| Another stage is active | PASS — `MainActionIsIdeaStageOnlyAndAccessControlsEnabledState` changes to Concept and verifies the action is hidden. |
| Placeholder access is unavailable | PASS — the same theory verifies disabled state and safe guidance; `EnvironmentAccess_UsesPresenceOnly` covers null, empty, and whitespace values. |
| Dialog closes | PASS — `IdeationWindow` is owned and modal, `MainWindow.SyncIdeationWindow` prevents a second instance, and its closed handler restores owner activation and launch-button focus. |

### `context-aware-tools`

| Scenario | Result and evidence |
|---|---|
| Ideation runs for a selected group | PASS — `Generate_GroupScopeUsesDirectActiveAndRejectedIdeasAndSanitizesMetadata` proves exact-group inclusion and parent/child/root exclusion. |
| Ideation runs without a selected group | PASS — `Generate_NicheScopeIncludesRootAndDescendantIdeas` proves whole-niche inclusion. |
| Active Item has no Idea text | PASS — the exact-group test includes an applicable Item with no `idea` metadata and proves it contributes no fabricated text. |
| Recorded rejection has no reason | PASS — `Rejection_RoundTripsAndNewSchemaIsVersionSeven` round-trips a null reason; context assembly preserves nullable reasons. |
| Generation payload is assembled | PASS — exact-group and niche tests inspect store, niche, group, guidance, active Ideas, rejected Ideas, and reasoning. |
| Operational fields exist | PASS — the sanitization test excludes API key and inheritance provenance; `IdeationService.IsOperationalKey` also excludes IDs, timestamps, archive/status fields, paths, credentials, secrets, and tokens. |

### `ideation`

| Scenario | Result and evidence |
|---|---|
| User opens Ideation from a selected group | PASS — `IdeationScopeResolverTests` and the Main-window action theory verify group scope and opening. |
| User opens Ideation from an Item | PASS — `IdeationScopeResolverTests` proves Item scope equals its parent group scope. |
| Context has no active niche | PASS — `IdeationScopeResolverTests` rejects inactive niche context; scope revalidation also handles missing or stale context. |
| Placeholder API access is present | PASS — `EnvironmentAccess_UsesPresenceOnly` and Main-window action theory. |
| Placeholder API access is absent | PASS — access adapter and Main-window action theory; no key value appears in returned state. |
| Generator request is assembled | PASS — application context tests inspect the generator inputs. |
| Dialog opens for a group | PASS — `Open_UsesSafeDefaultsAndValidatesBoundedCount` verifies frozen visible scope and defaults. |
| Dialog opens at niche root | PASS — scope resolver plus `WindowConstructsWithScopeInputModeCountAndAccessibleCandidateList`. |
| User enters an invalid count | PASS — default/count view-model test proves validation and disabled command; bounded service validation is independently tested. |
| User requests grumpy pug ideas | PASS — `FakeGenerator_UsesGuidanceAndGroupInBasicMode`. |
| Guidance is empty | PASS — `IdeationViewModel.Open` defaults to empty; fake generation accepts null guidance and uses scoped context. |
| Snowclone candidate is generated | PASS — `FakeGenerator_FillsSnowcloneTemplate` and `FakeGenerator_FillsAllVariablesAndRemainsConcise`. |
| Batch fits within the catalog | PASS — `SnowcloneCatalog_ExhaustsUniqueTemplatesBeforeRepeating`. |
| Batch exceeds the catalog | PASS — the same test requests 13 from a 12-entry catalog and proves exhaust-before-repeat. |
| Batch is running | PASS — `Generate_NeverExceedsFourConcurrentOperations`; view-model tests cover progress and confirmed cancellation; XAML binds an indeterminate progress indicator and status text. |
| Some operations fail | PASS — `Generate_DeduplicatesAndReportsPartialFailure`. |
| All operations fail | PASS — `Generate_TotalFailureAndCancellationReturnExplicitResults`. |
| Generator returns duplicates | PASS — application and view-model duplicate tests cover whitespace/case normalization and request-order output. |
| Candidate is generated | PASS — view-model generation tests prove candidate rows are appended; headless view verifies the candidate ListBox and named actions. |
| Dialog session ends | PASS — confirmed-close test proves candidates are cleared and late results ignored; no transient candidate enters `WorkspaceSnapshot`. |
| Candidate is created in a selected group | PASS — `Create_WritesFullIdeaAndUsesFirstSentenceInExactGroup`. |
| Candidate is created without a selected group | PASS — `Create_UsesNicheRootWhenNoGroupIsSelected`. |
| Candidate creation succeeds | PASS — service and view-model tests prove durable creation precedes row removal and raises authoritative refresh. |
| Candidate creation fails | PASS — `CreateFailureKeepsRowAndRejectSuccessRemovesIt`. |
| User confirms rejection with a reason | PASS — view-model decision test plus SQLite round trip. |
| User confirms rejection without a reason | PASS — SQLite group rejection round-trips a null reason. |
| User cancels rejection | PASS — `IdeationViewModel.CancelReject` preserves the row; nested dialog has explicit named Cancel. |
| Rejection persistence fails | PASS — `Decisions_RevalidateStaleScopeAndRejectSaveIsAtomic` and view-model row-retention behavior. |
| User confirms Clear All | PASS — `IdeationViewModel.ConfirmDiscard` clears only transient candidates. |
| User cancels Clear All | PASS — cancellation path leaves input/candidates untouched; declined-close test exercises the same preservation state. |
| User closes with candidates | PASS — view-model confirmation test and native-window close routing; close restores launch focus when available. |
| User closes during generation | PASS — confirmed-close test proves cancellation token invalidation and late-result suppression. |
| User declines Close | PASS — `DeclinedClosePreservesStateAndConfirmedCloseIgnoresLateGeneration`. |
| User operates Ideation with a keyboard | PASS — logical XAML order, meaningful automation names, and initial Guidance focus; nested destructive confirmation focuses Cancel. |
| Candidate action completes | PASS — `IdeationWindow.FocusNextCandidate` selects the next row or returns to Guidance. |
| Application theme changes | PASS — all Ideation surfaces use shared dynamic semantic resources; headless construction runs under the application theme and minimum dimensions. |

### `local-sqlite-persistence`

| Scenario | Result and evidence |
|---|---|
| Rejection is saved and reloaded | PASS — `Rejection_RoundTripsAndNewSchemaIsVersionSeven`. |
| Group-scoped rejection is stored | PASS — the same test round-trips a group ID and null reason. |
| Niche-root rejection is stored | PASS — the same test round-trips null group and a reason. |
| Rejection save fails | PASS — atomic-failure application test keeps the repository snapshot unchanged. |
| Previous supported database is opened | PASS — `VersionSixDatabase_MigratesWithoutChangingExistingData` plus the updated repository migration suite. |
| New database is created | PASS — schema-version suite expects v6; round-trip test queries `PRAGMA user_version`. |
| Migrated rejection data is saved | PASS — v5 migration test loads the new collection, after which standard round-trip persistence covers writes. |
| Migration fails | PASS — `VersionSixMigrationFailure_RollsBackTableAndVersion` proves both the v7 table creation and version remain rolled back; newer-version refusal uses v8. |

## Architecture, security, and changed-scope QA

- PASS — Domain contains only the stable mode and validated rejection model.
- PASS — Application owns scope resolution, sanitization, orchestration, concurrency, and decision boundaries without UI, SQLite, or network dependencies.
- PASS — Integration owns the environment-variable adapter, fake generator/catalog, and SQLite v7 mapping/migration.
- PASS — App owns composition and presentation. Its only SQLite reference remains the existing composition root (`AppWorkspaceFactory`); Domain has none.
- PASS — `FUSIONCANVAS_AI_API_KEY` is read only by `EnvironmentIdeationAccessStatus`. The adapter returns a boolean/safe message, never the value. Sanitization excludes credential-like metadata before generation.
- PASS — fake generation uses no HTTP/network API and persists no undecided candidates.
- PASS — group moves update rejection niche/store ownership; permanent group deletion nulls the optional association; workspace deletion removes owned rejections and store/niche deletion treats them as connected data.
- PASS — compiled Avalonia bindings, minimum window size, scrollable candidate list, accessible names, modal ownership, nested confirmations, and native-close interception are present and covered at the lowest reliable automated layer.

## Limitations

No optional live-desktop observation was performed. Native modality, operating-system close interception, spinner animation smoothness, and visual density are supported by implementation and deterministic tests but were not manually observed on this run.
> Post-merge integration note (2026-07-27): the environment gate, fake generator,
> and in-memory Snowclone catalog evidence below records this module's original
> isolated implementation. Production composition now uses the saved OpenRouter
> configuration and persisted Snowclone Library through
> `integrate-ideation-openrouter-snowclones`. SQLite's shared schema authority is
> v7, and the reconciliation change owns current cross-feature verification.

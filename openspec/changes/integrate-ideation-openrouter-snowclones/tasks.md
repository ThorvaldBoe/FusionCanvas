## 1. Protect Ideation Rejection History During Workspace Transfer

- [x] 1.1 Extend `WorkspaceSnapshotFilter.ForWorkspace` to include only Ideation rejections owned by the exported workspace’s included store, niche, and optional group identities.
- [x] 1.2 Extend workspace transfer identity preflight and entity counting to include stable Ideation rejection identities and `ideationRejections` manifest counts.
- [x] 1.3 Update import preparation and merge behavior to retain all live destination rejections and atomically add every packaged rejection.
- [x] 1.4 Add Domain and Application tests for scoped filtering, other-workspace exclusion, rejection-ID collisions, older empty packages, manifest counts, and preservation of unrelated destination history.
- [x] 1.5 Add Integration package round-trip tests proving rejection fidelity, destination preservation, schema-v7 embedding/migration, and exclusion/preservation of global Snowclones.

## 2. Establish One Canonical Persisted Snowclone Source

- [x] 2.1 Refactor `SnowcloneTemplatePolicy` to parse and expose canonical brace-delimited named tokens while preserving current validation and duplicate-normalization behavior.
- [x] 2.2 Add immutable Ideation Snowclone selections carrying record identity, phrase, guidance, and parsed tokens; replace the synchronous string-only catalog contract with an asynchronous one.
- [x] 2.3 Implement `PersistedSnowcloneCatalog` over the confirmed `ISnowcloneLibraryService` state with injected deterministic selection/shuffle behavior, unique-per-cycle selection, and explicit empty-library results.
- [x] 2.4 Update Ideation orchestration to load persisted selections for Snowclones mode, pass phrase plus guidance to generation, and reject results that retain any selected template token.
- [x] 2.5 Add Domain and Application tests for named/repeated tokens, phrase/guidance propagation, within-cycle uniqueness, post-exhaustion repetition, empty library, unresolved tokens, cancellation, and load failures.

## 3. Connect Ideation to Configured OpenRouter AI

- [x] 3.1 Add provider-neutral AI availability categories/results and `GetAvailabilityAsync(AiRequestPurpose, CancellationToken)` to the Application AI boundary.
- [x] 3.2 Implement availability resolution from the effective profile, cached catalog, privacy policy, and native credential state without returning or logging secret material.
- [x] 3.3 Replace the Ideation access contract with an asynchronously refreshable cached adapter exposing current availability and change notifications suitable for synchronous UI bindings.
- [x] 3.4 Add an Application-owned Ideation prompt builder that delimits Snowclone guidance and other user-authored context as untrusted creative input and excludes identifiers, timestamps, paths, archive state, credentials, and operational metadata.
- [x] 3.5 Implement `AiIdeaGenerator` over `IAiTextGenerationService` using `AiRequestPurpose.Ideation`, one request per candidate, typed secret-safe failures, blank-response rejection, and no automatic retry.
- [x] 3.6 Update `IdeationService` to preserve at-most-four concurrent calls, partial successes, duplicate suppression, progress, cancellation, and categorized all/partial failure reporting with the typed generator result.
- [x] 3.7 Add Application tests covering every availability category, secret absence, Basic and Snowclone prompts, profile purpose, success/blank/failure translation, exact call counts, no retry, partial failure, duplicates, and cancellation.

## 4. Unify Production Composition and Remove Placeholder Paths

- [x] 4.1 Extend `AppServices`/`AppServicesFactory` to expose the configured AI generation and availability dependencies through the application lifetime without duplicating credential or HTTP clients.
- [x] 4.2 Update `App`, `MainWindow`, `MainWindowViewModel`, and `AppWorkspaceFactory` composition so production Ideation receives `AiIdeaGenerator`, configured access, and `PersistedSnowcloneCatalog`.
- [x] 4.3 Refresh cached Ideation availability at startup and after relevant credential, model/profile, catalog, or privacy changes; marshal presentation notifications to the Avalonia UI thread.
- [x] 4.4 Remove production use of `EnvironmentIdeationAccessStatus`, `FakeIdeaGenerator`, and `InMemorySnowcloneCatalog`; retain explicit deterministic fakes only in test code where useful.
- [x] 4.5 Add composition and view-model tests proving a saved ready OpenRouter configuration enables Ideation, removal/inaccessibility/incomplete profiles disable it with distinct guidance, environment placeholders have no effect, and no production path bypasses `IAiTextGenerationService`.

## 5. Make Ideation Own Snowclone Library Management

- [x] 5.1 Extend `IdeationViewModel` with confirmed-library availability, `ManageSnowclonesCommand`, child-dialog state, generation blocking while the child is open, and refresh after child close.
- [x] 5.2 Add a compact `Manage Snowclones…` action and empty-library/error guidance visible only in Snowclones mode without adding persistent main-window or Settings controls.
- [x] 5.3 Update `IdeationWindow` to open exactly one `SnowcloneLibraryWindow` with Ideation as modal owner, coordinate protected close ordering, and return keyboard focus to the invoking action.
- [x] 5.4 Remove or repurpose the now-redundant standalone dialog factory and replace the no-launcher test with exact single-owner launcher assertions.
- [x] 5.5 Add Avalonia headless tests for Basic-mode hiding, Snowclones-mode launcher/blocked states, modal ownership, duplicate-open prevention, parent-state preservation, unsaved child close, library-change refresh, command disabling, and focus return.

## 6. Reconcile Compatibility Evidence and Complete Verification

- [x] 6.1 Confirm `SqliteWorkspaceRepository.CurrentSchemaVersion` and package manifests remain at shared schema v7 with no migration, and add regression assertions for current, older, and newer database/package behavior.
- [x] 6.2 Update the active Ideation, Snowclone Library, OpenRouter, and workspace-transfer verification/artifact statements that still describe schema v6, environment access, fake production generation, or an ownerless dialog; do not edit accepted specs directly.
- [x] 6.3 Run targeted Domain, Application, Integration, and App test projects after their corresponding task groups and correct any failed acceptance criterion rather than waiving it through aggregate results.
- [x] 6.4 Run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`; record project-level counts, warnings, and any sandbox-only rerun in `verification.md`.
- [x] 6.5 Run `openspec validate integrate-ideation-openrouter-snowclones --strict` and `openspec validate --all --strict`.
- [x] 6.6 Perform changed-scope architecture, security/privacy, persistence/data-loss, UI/focus, and specification-drift review, including inspection that no credentials or submitted prompts appear in settings, workspace databases, packages, logs, or fixtures.
- [x] 6.7 Create `verification.md` mapping every scenario in all six delta specs to the exact automated test or explicit supplemental evidence and document any remaining limitation.
- [ ] 6.8 Confirm the base changes are synchronized/archived before this reconciliation change is synchronized or archived, then obtain final human acceptance for the integrated workflow.

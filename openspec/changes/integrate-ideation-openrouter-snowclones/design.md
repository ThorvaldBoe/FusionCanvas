## Context

Four independently implemented modules now coexist:

- Ideation owns contextual candidate generation, approval/rejection, transient dialog state, and durable `IdeationRejection` records, but production composition still uses an environment-variable gate, `FakeIdeaGenerator`, and `InMemorySnowcloneCatalog`.
- OpenRouter configuration owns native credential storage, model/profile settings, model cache, strict request dispatch, and `IAiTextGenerationService`, but the service has no production caller.
- Snowclone Library owns validated brace-delimited records, global SQLite persistence, starter initialization, CSV interchange, and a focused dialog, but its factory has no owner and Ideation does not consume it.
- Workspace transfer filters and merges the positional `WorkspaceSnapshot` fields that existed when it was implemented. `IdeationRejections` was later added as an init-only property, so export omits it and import constructs a merged snapshot with an empty rejection collection. `SqliteWorkspaceRepository.SaveAsync` then rewrites the full rejection table, causing destination-wide data loss.

The shared SQLite authority is now schema v7. Snowclones are application-wide records in the same database but outside `WorkspaceSnapshot`; rejection records are workspace-owned through their store/niche/group relationships. OpenRouter credentials are application-wide secrets outside SQLite. The module must preserve those ownership boundaries.

The primary workflow is frequent Idea-stage generation. AI setup and Snowclone maintenance are occasional. Ideation remains a compact Idea-stage action opening one focused dialog; Snowclone maintenance is progressively disclosed only while Snowclones mode is selected and opens as a child modal. No persistent main-window or Settings Snowclone launcher is added.

## Goals / Non-Goals

**Goals:**

- Make the saved OpenRouter credential and effective Ideation profile the sole production availability source.
- Dispatch Basic and Snowclone candidate requests through the existing provider-neutral text service.
- Make the persisted Snowclone Library the sole production template source, including required guidance and canonical brace syntax.
- Give the existing Snowclone dialog one clear owner inside Ideation and preserve nested-dialog focus and draft behavior.
- Preserve rejection history through export/import and preserve unrelated destination rejection history during any import.
- Keep schema v7, secret boundaries, global Snowclone ownership, bounded concurrency, cancellation, and non-persistence of undecided candidates intact.
- Add deterministic cross-feature tests at Domain, Application, Integration, and Avalonia headless layers.

**Non-Goals:**

- Additional AI providers, streaming, structured output, tools, web search, images, automatic generation retries, or automatic model selection.
- Prompt/response history, provider diagnostics on Items, or persistence of undecided candidates.
- Snowclone categories, tags, archive state, cloud synchronization, or inclusion in workspace packages.
- Whole-application backup or changes to the existing workspace package container version.
- A new SQLite schema version or destructive migration.
- Redesign of candidate Create/Reject, Snowclone CRUD, or AI Settings beyond integration state refresh.

## Decisions

### 1. Production Ideation uses `IAiTextGenerationService`; fakes remain test-only

Add an Application-owned `AiIdeaGenerator` implementing the Ideation generator boundary. It builds provider-neutral system/user messages from `IdeationGenerationContext`, submits `AiTextRequest(AiRequestPurpose.Ideation, ...)`, and converts `AiTextResult` into a typed Idea-generation success/failure. Basic asks for one concise working direction. Snowclones supplies exactly one confirmed phrase, its guidance, and creative context and asks for one completed phrase with no explanation unless needed for clarity.

The generator must not receive or read credentials. `IAiTextGenerationService` remains the only component that reads the native credential before dispatch. No generation request is automatically retried because it may incur cost.

Alternatives rejected:

- Keep the fake generator but gate it with the real key: it would charge nothing, ignore the configured profile, and make the key requirement misleading.
- Inject OpenRouter transport directly into Ideation: it would violate the provider-neutral Application boundary and duplicate configuration/security logic.
- Generate the full requested batch in one provider call: it would weaken per-candidate progress, partial failure, cancellation, and existing bounded-parallel behavior.

### 2. Availability is an asynchronous cached application result

Extend the provider-neutral AI text boundary with `GetAvailabilityAsync(AiRequestPurpose, CancellationToken)`, returning a stable category (`Checking`, `Ready`, `MissingCredential`, `CredentialUnavailable`, `MissingModel`, `InvalidConfiguration`) and secret-safe guidance. The implementation resolves the current profile and cached catalog, then reads credential state without returning the secret.

Replace the environment implementation of `IIdeationAccessStatus` with an Application adapter backed by that availability method. Because Avalonia command enablement is synchronous, the adapter exposes a cached current value, `RefreshAsync`, and an `AvailabilityChanged` event. Initial state is `Checking`/disabled. Refresh occurs at startup and whenever `AiSettingsViewModel` changes credential state, effective profiles, model catalog, or privacy policy. `MainWindowViewModel` and `IdeationViewModel` listen to the adapter event and raise action/message/command properties on the UI thread.

`EnvironmentIdeationAccessStatus` is removed from production composition and its environment variable no longer affects behavior. A simple mutable access fake remains available to tests.

Alternatives rejected:

- Synchronously block on native credential reads from `CanOpenIdeation`: this risks UI stalls and deadlocks.
- Let App read `IAiCredentialStore` and settings directly: this duplicates readiness rules and crosses the credential boundary.
- Cache availability only at startup: saving/removing a key would require restart and leave visible state stale.

### 3. Persisted Snowclone selection is async and carries phrase plus guidance

Replace `ISnowcloneCatalog.GetTemplates(int)` with an asynchronous contract returning immutable selections containing record ID, phrase, guidance, and parsed placeholder tokens. Implement `PersistedSnowcloneCatalog` over `ISnowcloneLibraryService.LoadAsync`. Selection shuffles confirmed records using an injected index/random strategy for deterministic tests, uses each record once per cycle, and repeats only after exhausting the library.

The same `SnowcloneTemplatePolicy` that validates library input gains a pure placeholder-token parser used by both validation and Ideation response checking. Tokens retain braces and support named and repeated variables. AI performs contextual filling; the local application does not naïvely replace the letters X/Y/Z. A Snowclone response is rejected if any token from the selected template remains in the normalized output. Phrase guidance is included as untrusted user-authored creative context, not as system authority.

Alternatives rejected:

- Adapt only the phrase string: required guidance would remain dead data and named placeholders would be underspecified.
- Locally substitute X/Y/Z: it is incompatible with `{X}`, named placeholders, repeated variables, and context-sensitive phrasing.
- Copy persisted records into a second Ideation cache: it would reintroduce two sources of truth.

### 4. Ideation owns the Snowclone Library child dialog

Inject `ISnowcloneLibraryService` into `IdeationViewModel` and give it a nested `SnowcloneLibraryViewModel` plus an `IsSnowcloneLibraryOpen`/request command presentation state. `IdeationWindow` observes that state, creates `SnowcloneLibraryWindow` with the nested view model, and calls `ShowDialog(this)` so the Ideation window is the owner. Only one child can be open.

`Manage Snowclones…` is visible only for Snowclones mode, positioned beside the mode/blocked-state area rather than in the main candidate action row. Closing the child resolves its existing unsaved-change flow, refreshes the catalog state, recalculates Generate availability, and returns focus to the invoking button. While the child is open, the parent does not start generation. Closing the parent while a child is open first closes/resolves the child through its normal protection.

The current main-window and Settings no-launcher behavior remains. `SnowcloneLibraryDialogFactory` may be removed if nested view-model ownership makes it redundant.

Alternatives rejected:

- Put Snowclone management in Settings: maintenance is contextual to Snowclones generation and would create a second owner.
- Add a persistent main-window control: it consumes workspace area for occasional administration.
- Embed full CRUD inside Ideation: it would overload the frequent generation workflow and duplicate the tested focused dialog.

### 5. Workspace transfer treats rejection records as workspace-owned

`WorkspaceSnapshotFilter.ForWorkspace` filters `IdeationRejections` by the included store and niche identities and requires any non-null group identity to be included. The filtered snapshot sets the init-only collection explicitly. `CountEntities` includes `ideationRejections`. `WorkspaceImportPreflight` checks rejection IDs.

`WorkspaceTransferService.Merge` returns a snapshot whose rejection collection concatenates live and imported rejections. `PrepareImportedSnapshot` retains packaged rejections. Because preflight rejects ID collisions before files are copied, concatenation is safe. The one repository save remains the atomic record commit. Older packages naturally load with an empty collection after migration.

Global Snowclone tables remain excluded because package writing uses only `WorkspaceSnapshot`; `SqliteWorkspaceRepository.SaveAsync` continues not to clear them. Credential data remains outside both repositories.

Alternatives rejected:

- Preserve only live rejections and continue omitting packaged records: restored workspaces would lose feedback that directly affects later generation.
- Export all rejection records: that would leak unrelated workspace creative history.
- Add a separate rejection package entry: the filtered SQLite snapshot already supplies transactional schema migration and referential integrity.

### 6. Composition is unified without moving business behavior into App

`AppServices` remains the application-lifetime owner of the long-lived HTTP client, AI settings, and `IAiTextGenerationService`. `AppWorkspaceFactory.Create*` accepts the provider-neutral text service (and availability service if separated) and composes:

- `AiIdeaGenerator`
- configured `IIdeationAccessStatus`
- `SnowcloneLibraryService`
- `PersistedSnowcloneCatalog`
- `IdeationService`

`MainWindow` receives `AppServices`, not only `SettingsViewModel`, and passes the required provider-neutral dependencies into `MainWindowViewModel.CreateForDefaultWorkspace`. Test constructors retain explicit optional collaborators and deterministic defaults that never contact native storage or the network.

The Domain remains independent of AI, SQLite, and Avalonia. Application owns prompts/orchestration/contracts. Integration owns native credentials, OpenRouter, SQLite, CSV, and embedded resources. App owns nested-window and focus state.

## UX Preflight

- **User/outcome:** A creator working at the Idea stage wants a short batch of relevant options and occasionally maintains reusable phrase templates.
- **Frequency/placement:** Generate/Create/Reject remain in the focused Ideation dialog. Snowclone administration is occasional and appears only as `Manage Snowclones…` in Snowclones mode.
- **Workspace footprint:** No new persistent main-window or Settings control. One compact child action and one blocked-state message are added to Ideation.
- **Progressive disclosure:** Snowclone management and empty-library guidance are hidden in Basic mode. Provider configuration guidance links the creator conceptually to AI Settings without embedding settings controls.
- **States:** Initial availability checking, missing key, inaccessible credential store, incomplete model/profile, empty library, ready, generation busy, partial/all provider failure, nested library busy/error, and successful refresh are explicit.
- **Selection/focus:** Opening the library preserves Ideation scope, guidance, count, candidates, and mode. Closing returns focus to `Manage Snowclones…`; closing Ideation returns focus to its stage action.
- **Drafts/destructive actions:** Existing Snowclone unsaved-change and delete confirmation behavior remains authoritative. Existing Ideation candidate discard and rejection confirmation behavior remains authoritative.
- **Cancellation:** Generation cancellation propagates to in-flight AI calls without retry. Closing/clearing behaves as currently specified. Library operations use their existing cancellation and draft protection.

## Implementation Plan

### Domain

- Update `FusionCanvas.Domain/Snowclones/SnowcloneTemplatePolicy.cs` and validation result types to expose canonical placeholder tokens from the same parse used for validation. Preserve duplicate normalization and current error messages.
- Update `Workspace/Transfer/WorkspaceSnapshotFilter.cs` to filter and attach `IdeationRejections`.
- Update `Workspace/Transfer/WorkspaceImportPreflight.cs` to detect rejection-ID collisions.
- Tests:
  - `FusionCanvas.Domain.Tests/Snowclones/SnowcloneTemplatePolicyTests.cs`: named/repeated token extraction, brace preservation, invalid syntax.
  - `FusionCanvas.Domain.Tests/Workspace/Transfer/WorkspaceTransferPolicyTests.cs`: exact rejection filtering and collision behavior.

### Application

- Add AI availability records/categories and `GetAvailabilityAsync` to the provider-neutral text service contract in `Application/AI`.
- Implement readiness using the existing profile resolver, cache, and credential result types without returning secret data.
- Replace synchronous string-only `ISnowcloneCatalog` with async phrase/guidance/token selections.
- Add `AiIdeaGenerator` and an Application prompt builder under `Application/Ideation`; change the generator result boundary to preserve secret-safe failure category/message rather than using arbitrary provider exceptions.
- Update `IdeationService.GenerateAsync` to await template selection, preserve at-most-four parallel operations, translate per-operation results, suppress duplicates, validate unresolved placeholders, retain partial successes, and never retry.
- Add `PersistedSnowcloneCatalog` over `ISnowcloneLibraryService` in Application unless a narrower read contract is warranted during implementation.
- Update `WorkspaceTransferService` merge, entity counts, and summaries for rejection history.
- Tests:
  - `Application.Tests/AI/AiTextGenerationServiceTests.cs`: all availability categories and secret absence.
  - `Application.Tests/Ideation/IdeationServiceTests.cs`: persisted phrase/guidance, cycles, empty library, unresolved tokens, provider categories, no retry, context privacy.
  - `Application.Tests/Workspaces/WorkspaceTransferServiceTests.cs`: preserve live rejections, add imported rejections, older empty packages, counts.

### Integration

- Remove production dependence on `EnvironmentIdeationAccessStatus`, `FakeIdeaGenerator`, and `InMemorySnowcloneCatalog`; retain/move deterministic fakes to tests if still useful.
- Reuse `NativeAiCredentialStore`, `JsonAiModelCatalogCache`, and `OpenRouterClient`; no new HTTP behavior or package is required.
- Confirm `SqliteWorkspaceRepository.CurrentSchemaVersion` remains 7 and package reader/writer use that authority.
- Add package round-trip fixtures containing rejections and a destination with unrelated rejections. Assert global Snowclones survive import and are absent from the embedded package. Assert no credential material appears in package fixtures.
- Tests:
  - `Integration.Tests/Workspaces/WorkspacePackageIntegrationTests.cs`
  - existing SQLite Ideation/Snowclone suites for regression.

### App

- Extend `AppServices`/`AppServicesFactory` to expose the configured availability service as needed and pass the full application-lifetime services to `MainWindow`.
- Update `AppWorkspaceFactory`, `MainWindow`, and `MainWindowViewModel` composition so production Ideation receives AI, access, and persisted Snowclone dependencies while tests remain deterministic.
- Subscribe to AI settings/availability changes and marshal property/command refresh to the Avalonia UI thread.
- Extend `IdeationViewModel` with Snowclone library state, `ManageSnowclonesCommand`, blocked message, nested-dialog state, and refresh after child close.
- Add the compact action and empty-library/provider-state messaging to `IdeationWindow.axaml`; update code-behind to own `SnowcloneLibraryWindow` modally and restore focus.
- Remove or repurpose `SnowcloneLibraryDialogFactory` and replace the test that asserts no launcher anywhere with one that asserts exactly one Ideation-owned launcher and none in main/Settings.
- Headless tests:
  - saved/removed/inaccessible credential and profile changes refresh `CanOpenIdeation`;
  - Basic mode hides library management;
  - Snowclones mode shows the action and empty-library block;
  - child dialog has Ideation ownership, preserves parent state, prevents duplicates, and returns focus;
  - library changes refresh Snowclones generation availability;
  - categorized provider failures render without secret content.

### Compatibility, sequencing, and documentation

1. Implement rejection-safe filter/merge/preflight and tests first because it fixes the data-loss path independently.
2. Add canonical token parsing and persisted async catalog.
3. Add AI availability and generator adapter.
4. Unify production composition.
5. Add nested Snowclone dialog interaction and headless tests.
6. Reconcile the active base artifacts/verification statements with schema v7 and integrated behavior without editing accepted specs directly.
7. Run strict OpenSpec validation, build, full deterministic tests, changed-scope architecture/security/spec-drift review, and criterion-level verification.

No schema migration is added. Rollback is a code rollback: v7 data remains readable by the pre-change branch, though workspace packages created with rejection records would be read with those rows ignored by the old transfer filter/merge behavior. Do not archive this change before the four base changes it modifies have been synchronized or archived.

Decisions not to reopen during implementation:

- OpenRouter remains the sole provider.
- Production Ideation uses `IAiTextGenerationService`; the environment gate and fake production generator are removed.
- Candidate operations remain one AI call each with concurrency capped at four and no automatic retry.
- Persisted Snowclones use brace-delimited named tokens and required guidance.
- Snowclone management is owned only by Ideation Snowclones mode.
- Rejections are workspace-owned package content; Snowclones and credentials are global/excluded.
- SQLite remains schema v7.

## Risks / Trade-offs

- **[Parallel generation may incur several charges quickly]** → Keep count 1–20, concurrency at four, visible requested count/progress, cancellation, and no automatic retry.
- **[Availability checks could stall or churn]** → Cache current presentation state, refresh asynchronously on meaningful settings changes, coalesce concurrent refreshes, and never block property getters.
- **[Provider output is nondeterministic]** → Use concise prompts, one candidate per request, normalization, duplicate suppression, unresolved-token validation, and human Create/Reject control.
- **[User-authored Snowclone guidance could contain adversarial instructions]** → Delimit it as untrusted creative data beneath fixed system instructions; never include credentials or operational metadata.
- **[Nested modal windows can create focus/close defects]** → Let Ideation own one child, reuse existing draft protection, and cover ownership, duplicate prevention, close ordering, and focus return headlessly.
- **[Snapshot init-only fields are easy to omit again]** → Add explicit rejection assertions to filter, merge, count, preflight, package round-trip, and preservation tests; consider a future non-positional snapshot builder separately.
- **[Active changes overlap before archival]** → Record base-change dependencies, validate the combined change set, and archive/sync in dependency order.

## Migration Plan

1. Ship the code against existing schema v7 with no database migration.
2. On first run, availability starts disabled/checking and resolves from existing native credential/profile/cache state.
3. Existing persisted Snowclones immediately become the production Ideation source; the one-time starter initialization remains unchanged.
4. New workspace exports include rejection records. Existing packages without them remain valid and import an empty rejection set.
5. If rollback is required, revert application code; no schema downgrade or data rewrite is needed.

## Verification Mapping

| Acceptance area and scenarios | Planned evidence |
| --- | --- |
| Ideation availability: ready, missing key, inaccessible credential, incomplete profile, ignored environment placeholder | Application availability tests plus MainWindow/Ideation view-model and headless binding tests |
| Basic generation: grumpy-pug context, empty guidance, provider-neutral/private request | Prompt-builder and `AiIdeaGenerator` tests with recording `IAiTextGenerationService` |
| Snowclone generation: persisted phrase/guidance, unique cycle, repeat cycle, empty library, unresolved token | Domain token tests and Application catalog/Ideation service tests |
| Async generation: busy state, partial/all failure, duplicates, no retry | Existing Ideation service/view-model tests updated with typed AI fakes and exact call counts |
| AI availability observation and live refresh | AI service tests plus App tests that save/remove/change profile through fakes and observe action state |
| AI result handling: success, blank, categorized failure, undecided non-persistence | `AiIdeaGenerator` and Ideation orchestration tests; repository/settings recording fakes |
| Snowclone dialog: Ideation owner, close refresh/focus, busy/error, only launcher | Avalonia headless `IdeationWindow`/`SnowcloneLibraryWindow` tests and visual-tree launcher assertions |
| Workspace export: scoped rejections/counts, other-workspace exclusion, global/secret exclusion | Domain filter tests, Application manifest tests, Integration ZIP inspection/round-trip tests |
| Workspace import: rejection restoration, destination preservation, older empty package | Application merge tests and Integration SQLite/package round trips |
| Import collision: rejection ID and same-package refusal | Domain preflight tests and Application no-mutation/no-file-copy tests |
| Schema v7 coherence: current open, full save, package DB, old migration | Integration SQLite workspace/Snowclone/Ideation/package suites |

Routine completion gates are `openspec validate integrate-ideation-openrouter-snowclones --strict`, `openspec validate --all --strict`, `dotnet build .\FusionCanvas.sln`, and `dotnet test .\FusionCanvas.sln`. A live OpenRouter call and interactive desktop pass are optional supplemental checks only.

## Open Questions

None. The integration ownership, provider choice, placeholder syntax, transfer ownership, schema version, UX placement, and retry behavior are resolved above.

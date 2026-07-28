## Context

FusionCanvas currently has an accepted context-aware Stage Tool Host and, through `basic-product-creation-workflow`, a lightweight Item-bound Idea editor plus topic and Item creation services. The current `ToolContext` exposes store, niche, selected topic or Item, inherited values, and bounded nearby-work summaries, but those summaries do not carry original Idea text or rejection reasoning. The current workspace snapshot and SQLite schema have no durable record for a generated candidate rejected before Item creation.

Ideation is a frequent creative batch operation but not persistent document content. It must stay close to the Idea stage without consuming permanent workspace area or replacing the manual Idea editor. The operation begins from an immutable active niche/group scope, may run several asynchronous generations, and ends only when the creator explicitly creates or rejects candidates. Undecided candidates are intentionally session-only.

This module deliberately uses a fake local generator, a small in-memory Snowclone catalog, and an environment-variable availability placeholder. These seams exercise the complete user workflow without committing the product to a provider SDK, credential format, secret store, prompt protocol, or Snowclone database design.

Implementation depends on the Item names, stage surfaces, metadata keys, and SQLite migration from `basic-product-creation-workflow`. If those contracts change before implementation, reconcile this package to their final accepted names without changing the product decisions here.

## Goals / Non-Goals

**Goals:**

- Launch a discoverable, modal Ideation workflow from supported Idea-stage contexts.
- Make current store, niche, optional exact group, active Ideas, and rejected directions explicit generator inputs.
- Generate 1 through 20 concise Basic or Snowclone candidates without blocking the UI.
- Keep candidates transient until Create or Reject succeeds.
- Create normal Draft Idea-stage Items through the existing Item application boundary.
- Persist rejections and optional reasoning locally so later batches receive negative guidance.
- Preserve selection, input, candidates, focus, and recoverability through cancellation and failure.
- Keep secret values and irrelevant operational fields out of generation payloads and logs.
- Verify UI-owned behavior with deterministic Avalonia headless tests and all decision logic below the UI where practical.

**Non-Goals:**

- Calling a real AI service or selecting providers/models.
- Adding an API-key field, persisting credentials, or designing a production secret store.
- Persisting, importing, editing, or managing Snowclone templates.
- Persisting undecided candidates, sessions, raw prompts, fake-generator outputs, or cost/token history.
- Editing candidate text, scoring/ranking candidates, trend research, marketplace validation, or automatic approval.
- Changing the existing manual Idea editor or the Item workflow policy.
- Treating a rejected candidate as a normal Rejected Item.

## Decisions

### 1. Expose Ideation as an auxiliary Idea-stage action

Add one `Ideation…` command in the Stage Tool Host header/action area. Its visibility is driven by active view stage and resolvable niche context; its enabled state additionally depends on placeholder access. The action opens a modal window and does not become a `StageToolDescriptor` selection.

Rationale: descriptors represent hosted surfaces with persistent selection. Ideation is a transient batch operation, and selecting it as a hosted tool would either replace the manual editor or leave confusing selection state after the dialog closes. A stage-local action follows the existing compact-command guidance and can later become an `Idea tools` menu if more auxiliary actions appear.

Alternative considered: add a permanent section between the navigator and Item sections. Rejected because it consumes daily workspace area for an occasional batch operation.

### 2. Resolve and freeze scope when the dialog opens

Application code resolves an `IdeationScope` containing Store ID, Niche ID, optional Group ID, display path, and creation topic. A selected niche yields niche-root scope. A selected group yields that exact group. An Item yields its parent group when present, otherwise its parent niche. An inactive, missing, cross-store, or niche-less context is unavailable.

The modal dialog prevents main-window selection changes, and its view model retains the resolved scope for its lifetime. Each generation request re-loads authoritative workspace data for that scope so newly created or rejected ideas from the same dialog inform later batches, while the target itself cannot drift.

Exact-group context includes direct Items and direct ideation rejections only; it does not imply subtree scope. Niche-root context includes the niche root and every group in that niche. Creation at niche scope places the Item at the niche root.

Alternative considered: reuse the generic bounded `NearbyWork` list. Rejected because “every active” Idea and every scoped rejection are explicit requirements and need Idea text/reason fields that the generic summary deliberately lacks.

### 3. Use an environment access checker without exposing the value

Application owns an `IIdeationAccessStatus` port with an availability result and user-safe unavailable reason. Integration implements it by checking whether `FUSIONCANVAS_AI_API_KEY` is non-whitespace. The adapter returns only a Boolean/capability result; no downstream request can access the value.

Read availability whenever active context is refreshed and again before generation so a stale enabled button cannot bypass the gate. Tests inject deterministic available/unavailable implementations and never mutate the process environment in parallel test execution.

Alternative considered: add a fake key to JSON application settings. Rejected because it would introduce an intentionally insecure credential persistence shape that a later module would have to migrate or remove.

### 4. Put orchestration in Application and fake generation in Integration

Application owns:

- `IdeationMode`-aware request/result contracts;
- `IIdeaGenerator`, `ISnowcloneCatalog`, and `IIdeationAccessStatus` ports;
- `IIdeationService` and `IdeationService`;
- scope/context assembly;
- desired-count validation;
- at-most-four concurrency;
- cancellation and late-result rejection;
- normalization and duplicate suppression;
- candidate Create and Reject orchestration.

Integration owns:

- `FakeIdeaGenerator`;
- `InMemorySnowcloneCatalog`;
- `EnvironmentIdeationAccessStatus`;
- SQLite serialization for ideation rejections.

The fake generator must be genuinely asynchronous and cancellation-aware, but its content is deterministic for a supplied seeded/random collaborator so tests do not depend on timing or ambient randomness. Basic mode selects among small contextual sentence/phrase patterns. Snowclones mode receives a chosen template and fills X/Y/Z placeholders from normalized guidance, group, niche, and store terms. The catalog contains enough varied templates to exercise non-repetition, including examples such as `Talk to me about X` and `Whatever X your Y`.

Alternative considered: generate strings directly in the view model. Rejected because that would mix creative policy, concurrency, test seams, and UI state and would make later provider replacement unnecessarily invasive.

### 5. Use a dedicated sanitized generator payload

`IdeationGenerationContext` is not a domain entity and is not the general `ToolContext`. It contains only:

- store name, optional description, and user-authored metadata;
- niche name, optional description, and user-authored metadata;
- optional group path/name/description/user-authored metadata;
- optional user guidance;
- mode and optional selected Snowclone template;
- every applicable non-empty active Item original Idea;
- applicable Rejected Item Ideas;
- applicable `IdeationRejection` text and optional reason.

The assembler reads current entities and parses metadata through application-owned helpers. It excludes entity IDs from the generator DTO, timestamps, archive/status flags, file paths, `inheritedFrom:` provenance keys, credential/configuration values, and diagnostic representations of the original entities. Items without Idea text add nothing. Rejected Items without Idea text add nothing. Rejection reasons remain optional.

Alternative considered: serialize Store/Niche/Group/Item records. Rejected because it would leak operational fields, couple prompts to persistence models, and make privacy behavior difficult to test.

### 6. Treat one Generate invocation as a cancellable batch

`IdeationService.GenerateAsync` validates count 1–20, access, mode, and scope before starting. It creates one logical operation per requested candidate and schedules at most four concurrently. For Snowclones it requests an ordered randomized template sequence that exhausts unique catalog entries before repeating. It reports progress after each operation, whether successful or failed.

Candidate normalization:

1. trim leading/trailing whitespace;
2. collapse internal whitespace runs to one space for the comparison key;
3. compare keys ordinal-ignore-case;
4. preserve the trimmed original text for display/storage;
5. reject empty results;
6. deduplicate against the current batch and existing undecided dialog candidates;
7. do not retry merely to reach the requested count.

The result distinguishes complete success, partial failure, total failure, validation/access failure, and cancellation. Cancellation is cooperative. The dialog increments a batch generation token; results with an obsolete token are ignored so late callbacks cannot repopulate a closed or replaced session.

Alternative considered: launch all 20 calls simultaneously. Rejected because uncontrolled provider-style concurrency would teach the wrong architectural behavior and make later replacement prone to rate-limit failures.

### 7. Keep candidate state in the dialog view model

`IdeationViewModel` owns observable `IdeaCandidateViewModel` rows, selected mode, guidance, desired-count text/value, progress, busy state, recoverable message, and confirmation state. Candidates have a session-local stable ID and text; they are not added to `WorkspaceSnapshot`.

Generate controls are immutable during a batch. Existing candidates remain actionable unless a conflicting dialog-wide transition is active; individual Create/Reject actions are disabled only for the affected candidate while its mutation runs. A second command invocation is ignored by command predicates and guarded again in application code.

Successful Create/Reject removes the row and moves focus to the next row or a stable dialog action. Failure retains the row. A later Generate appends unique candidates instead of replacing prior undecided work.

Alternative considered: persist candidates with a Candidate status. Rejected because the user explicitly chose close/discard behavior and persistent candidates would add tree and cleanup complexity.

### 8. Create candidates through Item management and persist rejections separately

Application derives the working title as the first non-empty sentence, normalized to one line. If sentence punctuation is absent, it uses the full normalized candidate. It does not truncate unless the final Item contract defines a title limit; if such a limit exists, use the same central title policy and preserve the unabridged Idea metadata.

Create calls `IItemManagementService.CreateItemAsync` with:

- `ItemTopicReference(Group, groupId)` or `ItemTopicReference(Niche, nicheId)`;
- derived working title;
- `ItemContext.Metadata` containing the full original Idea under the existing Idea metadata key;
- existing creation defaults/inherited metadata and tag behavior.

`IdeationRejection` is a domain workspace record with `Id`, `StoreId`, `NicheId`, nullable `GroupId`, `Text`, nullable `Reason`, `IdeationMode`, and `CreatedAt`. It is intentionally not a general Item and has no archive/status workflow. `IdeationService.RejectAsync` reloads the snapshot, revalidates the scope, appends one record, and saves atomically through `IWorkspaceRepository`.

The SQLite table uses store and niche foreign keys with workspace-owned cascade behavior and a nullable group foreign key that becomes null if a group is permanently removed. The rejection remains niche-owned and usable as negative context after group removal. Group archive does not change the association.

Alternative considered: store rejection in Prompt metadata or create a Rejected Item. Rejected because Prompt history is not an accepted ideation outcome model, and rejected candidates must not clutter normal navigation.

### 9. Use nested owned dialogs and explicit discard state

`IdeationWindow` is modal and owned by `MainWindow`. `RejectIdeaWindow` is modal and owned by `IdeationWindow`. Clear All and Close confirmations may use one reusable App-owned confirmation view/window model, but their copy and confirm result are explicit.

Confirmation rules:

- Clear All with zero candidates executes without a prompt and has no effect.
- Clear All with candidates asks before clearing.
- Close with neither candidates nor an active batch closes immediately.
- Close with candidates and/or an active batch asks once with combined copy.
- Confirmed Close cancels the batch, invalidates its token, clears candidates, then closes.
- Declined confirmation changes nothing.
- Operating-system close routes through the same Close request and can cancel native closing.

Initial focus enters Guidance. Reject confirmation focuses Reason. Discard confirmation focuses Cancel because the action loses creative work. After final close, focus returns to `Ideation…` if visible and enabled; otherwise to the stable Stage Tool Host.

Alternative considered: use inline overlays inside `MainWindow`. Rejected because nested ownership, focus isolation, native close interception, and modal context stability are clearer with focused windows and already have repository patterns.

### 10. Refresh authoritative workspace state without changing the dialog scope

After Create or Reject succeeds, `MainWindowViewModel` reloads workspace/navigation state through the existing refresh path. The dialog keeps its frozen creation scope but uses the service's authoritative mutation result for subsequent work. Creating an Item may change normal service selection internally; the main navigation selection remains the pre-dialog context until the modal closes, avoiding disruptive movement during batch triage.

If the underlying scope becomes invalid through an unexpected external mutation, the next operation fails with a recoverable “scope no longer available” result; no fallback topic is guessed.

Alternative considered: automatically select/open each created Item. Rejected because batch ideation requires staying in the candidate review loop.

## Functional Design

### Primary workflow and frequency

Creators may invoke Ideation repeatedly during Idea-stage exploration, but each invocation is a bounded session rather than permanent document content. The only main-workspace footprint is one stage action. The focused dialog owns generation and triage; the rejection dialog owns the less-frequent reason capture.

```text
Idea-stage context
        |
        v
 [Ideation…] -- unavailable --> disabled guidance
        |
        v
  Modal session -- Generate --> fake Basic/Snowclone batch
        |                            |
        |                      progress / partial error
        v                            v
  Candidate list <------------- unique results
      |       |
   Create   Reject
      |       |
      v       v
 Draft Item  durable IdeationRejection
```

### Dialog states

- **Ready/empty:** scope, mode, guidance, count, Generate, disabled Clear All, Close.
- **Generating:** spinner, `completed / requested`, immutable request controls, cancellable Close, prior candidates retained.
- **Ready/populated:** candidate rows with Create/Reject, enabled Clear All.
- **Partial result:** successful candidates plus concise batch warning.
- **Blocked:** access or scope error; Generate unavailable, existing candidates retained where safe.
- **Candidate mutation:** affected row actions disabled; other state preserved.
- **Discard confirmation:** Cancel receives initial focus; underlying state remains untouched until confirmation.
- **Closed:** batch cancelled, late results invalidated, candidates released, durable outcomes preserved.

### Fake output rules

Fake output exists to exercise workflow, not emulate high-quality AI. Each result is preferably one phrase or sentence and at most a few short sentences. It may combine guidance with group/niche terms and supporting store descriptors. Snowclone results lead with the completed phrase and may include one brief explanation. The full returned text remains the candidate Idea.

No fake output is asserted verbatim in UI tests. Generator unit tests use seeded collaborators and assert contextual inclusion, template completion, uniqueness rules, concision bounds, and cancellation.

## Risks / Trade-offs

- **Fake output may be mistaken for the eventual AI quality bar** → Label unavailable configuration as placeholder access in diagnostics and document the adapter as replaceable; acceptance verifies workflow and context, not semantic quality.
- **Environment-variable gating is awkward for normal desktop users** → Keep it explicitly temporary and do not add credential UI; a later real provider/settings module replaces only the access adapter.
- **“Every active” Idea can produce a large in-memory payload** → Build the complete required context but avoid duplicate strings and unnecessary entity fields; do not introduce silent truncation that violates scope. Revisit provider-specific token budgeting with the real provider module.
- **Whole-snapshot persistence can create stale-write risk** → Reload immediately before rejection append, return authoritative state, and follow the repository's existing atomic save pattern.
- **Parallel completion can reorder candidates** → Assign request indexes and emit final/appended candidates in request order, independent of completion timing.
- **Late cancellation callbacks could repopulate a closed dialog** → Combine cancellation tokens with a session/batch generation token checked before UI mutation.
- **Group deletion can orphan historical feedback** → Use nullable group association with niche ownership; set group to null on permanent deletion while preserving the rejection.
- **First-sentence title derivation may create long tree labels** → Use the central Item title validation/formatting policy and preserve full text in Idea metadata; do not invent a second arbitrary truncation policy.
- **Nested modal windows can regress focus or theme behavior** → Cover ownership, initial focus, close interception, focus return, and theme changes with Avalonia headless tests.
- **Concurrent active changes reorganize capability folders** → Implement against the final folder conventions from the reorganization changes and avoid mixing unrelated moves into this change.

## Migration Plan

1. Complete or stabilize the relevant `basic-product-creation-workflow` Item/stage contracts.
2. Add the `IdeationRejection` collection to `WorkspaceSnapshot` with empty/default compatibility for tests and in-memory fixtures.
3. Increment SQLite to the next free schema version at implementation time.
4. Create `ideation_rejections` for new databases and add a transactional migration from the then-current prior version.
5. Backfill no rows; existing workspaces start with an empty rejection collection.
6. Verify row counts and foreign-key integrity for all pre-existing tables before committing the migration.
7. On migration failure, roll back the transaction and retain the prior schema version/data.
8. No downgrade writer is provided. Rollback requires restoring a pre-migration database copy; the application must never partially downgrade or silently drop rejection data.

## Implementation Plan

### 1. Domain model

- Add `src/FusionCanvas.Domain/Ideation/IdeationMode.cs` with stable `Basic = 0` and `Snowclones = 1` values.
- Add `src/FusionCanvas.Domain/Ideation/IdeationRejection.cs` as an immutable validated record. Require non-empty IDs, Store/Niche IDs, normalized non-empty text, optional normalized reason, defined mode, and creation timestamp.
- Extend `WorkspaceSnapshot` with `IReadOnlyList<IdeationRejection> IdeationRejections`. Preserve source compatibility through deliberate constructor overload/default updates rather than scattered test-only workarounds.
- Add domain tests under `tests/FusionCanvas.Domain.Tests/Ideation/` for invariants and workspace retention.

### 2. Application contracts and context assembly

- Add `src/FusionCanvas.Application/Ideation/` containing:
  - `IIdeationService`;
  - `IIdeaGenerator`;
  - `ISnowcloneCatalog`;
  - `IIdeationAccessStatus`;
  - scope/request/context/candidate/progress/result records;
  - `IdeationService`;
  - a focused context assembler or private service responsibility.
- Keep generator DTOs free of domain entities and IDs. Add an internal metadata sanitizer that excludes `inheritedFrom:` and any configuration/operational keys identified by accepted metadata conventions.
- Resolve scope from `ToolContext` plus authoritative snapshot. Do not inspect Avalonia controls or `MainWindow` selection directly.
- Query exact-group or whole-niche Items/rejections according to the specs. Extract original Idea through the existing Item metadata codec; expose a focused read helper if necessary instead of duplicating JSON parsing.
- Validate access, scope, mode, and count in Application even when the UI disables invalid commands.
- Add application tests under `tests/FusionCanvas.Application.Tests/Ideation/` for scope, sanitization, all-active/all-rejected assembly, concurrency, order, deduplication, partial/total failure, cancellation, Create, Reject, and atomic failure behavior.

### 3. Placeholder Integration adapters

- Add `src/FusionCanvas.Integration/Ideation/EnvironmentIdeationAccessStatus.cs`.
- Add `src/FusionCanvas.Integration/Ideation/InMemorySnowcloneCatalog.cs` with a small immutable catalog and injectable shuffle/random strategy.
- Add `src/FusionCanvas.Integration/Ideation/FakeIdeaGenerator.cs` with cancellation-aware asynchronous Basic and Snowclone branches. Inject delay/random collaborators so production can demonstrate progress while tests run without real sleeps.
- Test adapters in `tests/FusionCanvas.Integration.Tests/Ideation/`, including whitespace access values, no key exposure, seeded catalog cycling, placeholder filling, and cancellation.

### 4. Persistence and migration

- Update `SqliteWorkspaceRepository` schema creation, table clear/insert order, snapshot validation, save/load mapping, and current schema version for `ideation_rejections`.
- Use columns for stable ID, Store/Niche IDs, nullable Group ID, text, nullable reason, integer mode, and created timestamp.
- Add store/niche foreign keys consistent with workspace ownership and nullable group handling consistent with permanent group removal.
- Add migration fixtures at the actual prior schema version and tests proving pre-existing data/relationships survive, new rows round-trip, nullable reasons/groups round-trip, failed migration rolls back, and newer schemas remain refused.
- Update repository contract/in-memory fixtures across tests to include rejection collections without changing unrelated accepted behavior.

### 5. App view models and composition

- Add `src/FusionCanvas.App/Ideation/IdeationViewModel.cs`, `IdeaCandidateViewModel.cs`, rejection/discard confirmation view models as focused types, and a small dialog coordinator contract only if needed to keep view-model tests framework-free.
- Expose `CanShowIdeation`, `CanOpenIdeation`, and unavailable guidance from `MainWindowViewModel` or an Idea-stage action view model based on active view, resolved scope, and access status.
- Compose `IdeationService` with the existing repository and Item management service in `AppWorkspaceFactory`/`MainWindowViewModel.CreateForDefaultWorkspace` following final project composition conventions.
- Preserve main navigation selection during dialog decisions. Refresh workspace/tree state from successful mutation results.
- Add framework-free tests for dialog state, command predicates, batch tokens, partial errors, per-row busy state, removal-after-success, confirmation decisions, and focus-target intent.

### 6. Avalonia windows and Stage Tool action

- Add `IdeationWindow.axaml/.cs`, `RejectIdeaWindow.axaml/.cs`, and a reusable discard confirmation only if an existing confirmation pattern cannot represent the required copy/focus.
- Place `Ideation…` in the Stage Tool Host header/action area of `MainWindow.axaml`, not as a stage-tool selector entry.
- Have `MainWindow.axaml.cs` own one Ideation window instance, show it modally with `ShowDialog`, prevent duplicates, intercept native close through the view model, and restore focus.
- Build the dialog with a compact scope summary, labeled mode selector, optional multi-line Guidance, bounded numeric input, Generate, accessible progress, candidate `ListBox`/`ItemsControl`, Clear All, and Close. Candidate text wraps; row actions remain content-sized.
- Use shared semantic resources for every surface and meaningful `AutomationProperties.Name`, tooltip, and live/status semantics supported by Avalonia.
- Add headless tests under `tests/FusionCanvas.App.Tests/Ideation/` for visual-tree construction, action visibility/enabling, ownership/single-instance behavior, initial and returned focus, tab order, invalid count, busy controls, list updates, nested rejection dialog, native close confirmation, clear/close preservation, theme changes, and compact/minimum layout scrolling.

### 7. Documentation and wiring cleanup

- Document `FUSIONCANVAS_AI_API_KEY` as a non-secret placeholder switch for development/demo use without showing an example real key.
- Make clear that the fake generator is local and no workspace content is sent over the network.
- Update current UI/architecture documentation only where the accepted implementation introduces a durable pattern; do not revive historical LifeOS requirements or describe future real-provider behavior as implemented.

### 8. Verification and completion

- Run focused project tests while implementing each layer.
- Run strict OpenSpec validation and reconcile all scenario text with implemented behavior.
- Run `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln`.
- Perform changed-scope architecture/security/spec-drift review, including a search proving App/Domain do not reference SQLite and the placeholder key is never persisted/logged/passed to generator DTOs.
- Optional live desktop observation is useful only for native modal ownership, native close interception, spinner smoothness, and visual density; it supplements but does not replace deterministic completion gates.

### Decisions not to reopen during implementation

- Ideation is an auxiliary Idea-stage modal action, not a hosted selector replacement.
- Active niche is required; group is optional.
- No group means whole-niche context and niche-root creation.
- Group means exact direct-group context, not subtree context.
- Every applicable active Item contributes non-empty Idea text; Rejected lifecycle Items and ideation rejections provide negative context.
- Placeholder access uses `FUSIONCANVAS_AI_API_KEY`; the value is never stored or passed downstream.
- Generation is fake/local, count is 1–20/default 5, and concurrency is at most four.
- Snowclones use a small in-memory catalog and exhaust unique templates before reuse.
- Candidates are transient; Clear All and lossy Close require confirmation.
- Create makes a normal Draft Idea-stage Item; Reject makes a separate durable rejection.
- Candidate removal happens only after successful durable Create/Reject.
- Real AI, credential UI/storage, and persisted Snowclone management are not part of this module.

## Acceptance-to-Verification Mapping

| Capability / requirement | Scenarios | Planned verification |
|---|---|---|
| `ideation` / focused dialog | User opens from selected group; opens from Item; context has no active niche | Application scope tests plus Avalonia headless host/window ownership and unchanged-context tests |
| `ideation` / placeholder access | Access present; access absent; request assembled | Integration access-status tests, application gate tests, headless enabled/disabled test, and source/payload security assertion |
| `ideation` / controls and scope | Opens for group; opens at niche root; invalid count | View-model tests and headless binding, label, focus, and validation tests |
| `ideation` / Basic mode | Grumpy pug request; empty guidance | Seeded fake-generator integration tests and application payload tests |
| `ideation` / Snowclones | Candidate generated; batch fits catalog; batch exceeds catalog | Seeded catalog/generator integration tests |
| `ideation` / asynchronous progress | Batch running; some fail; all fail; duplicates | Application concurrency/progress tests with controllable generators plus view-model/headless busy-state tests |
| `ideation` / transient candidates | Candidate generated; session ends | View-model tests and repository-spy assertion that undecided candidates are never saved |
| `ideation` / Create | Selected group; no group; success; failure | Application Item-service collaboration tests, repository persistence test, view-model row retention/removal tests, and main-window refresh test |
| `ideation` / Reject | With reason; without reason; cancel; persistence failure | Domain/application/persistence tests plus headless rejection-dialog focus and command tests |
| `ideation` / discard confirmation | Confirm/cancel Clear All; Close with candidates; Close during generation; decline Close | View-model cancellation/token tests and headless confirmation/native-close/focus-preservation tests |
| `ideation` / accessibility/theme | Keyboard operation; candidate action focus; theme changes | Avalonia headless tab/focus/accessibility-name/theme-resource tests; optional live modal observation |
| `context-aware-tools` / scoped history | Selected group; no group; active Item lacks Idea; rejection lacks reason | Application context-assembler tests using mixed stores/niches/groups/statuses |
| `context-aware-tools` / sanitized payload | Payload assembled; operational fields exist | Application DTO/sanitizer tests and security-focused reflection/serialization assertions |
| `stage-tool-host` / auxiliary action | Supported Idea context; another stage; access unavailable; dialog closes | Stage-host/MainWindow view-model tests plus Avalonia headless action placement, enabled state, editor preservation, and focus return |
| `local-sqlite-persistence` / rejection storage | Reload; group scope; niche root; save failure | SQLite round-trip and atomic-failure integration tests |
| `local-sqlite-persistence` / migration | Previous database; new database; post-migration save; migration failure | Versioned SQLite fixture tests, foreign-key integrity checks, and rollback assertions |

## Open Questions

None. The real provider, credential management, provider-specific context limits, and persisted Snowclone management are intentionally deferred modules rather than unresolved decisions in this one.

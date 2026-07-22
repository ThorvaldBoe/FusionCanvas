## Context

FusionCanvas currently has the structural foundations for this module: a universal `Listing` record, persisted workflow stage and lifecycle status, archive/restore, navigation and tab coordination, a generic Listing Inspector, Stage Tool descriptors and host selection, reusable Tags, managed workspace Assets, and a schema-version-4 SQLite repository. The application can move a record through Idea, Concept, Design, and Listing, but the built-in Stage Tools are placeholders and the generic inspector exposes all creative fields together.

Discovery established a different durable model:

- `Item` is the universal stage-agnostic record; Listing is the final stage.
- ID is the only required Item value.
- only current-stage content is editable; upstream changes require regression;
- stage movement has no completion gates and never deletes downstream data;
- Design files reuse Item-linked exported PNG Assets without versioning;
- Published and Rejected protect product content while approved shared metadata remains available;
- built-in stage content is rendered through the Stage Tool Host;
- working title, current-stage text, and Notes use explicit guarded Save, while Tags remain immediate.

This is a cross-cutting change across all four Clean Architecture layers and requires an identity-preserving database migration. It remains one delivery module because every part supports one independently verifiable user outcome: follow one Item from Idea through a locally tracked published Listing.

## Goals / Non-Goals

**Goals:**

- Make the four-stage workflow useful for real local product tracking with the minimum durable content for each stage.
- Establish `Item` consistently in domain, application, UI, serialized contracts, and physical storage before the legacy term spreads further.
- Preserve all existing data, IDs, relationships, managed files, unknown metadata, and inherited provenance.
- Enforce workflow, status, confirmation, and editability rules in domain/application code rather than only through disabled controls.
- Reuse existing Tag, Asset, file-storage, navigation, tab, lifecycle, and Stage Tool foundations.
- Give a bounded implementation agent explicit types, responsibilities, sequence, tests, and escalation conditions.
- Verify every acceptance scenario with focused deterministic evidence and a risk-based real-desktop pass.

**Non-Goals:**

- Actual Shopify, Printify, Etsy, or other marketplace publication or synchronization.
- Marketplace title, description, price, variants, SEO, readiness validation, or credentials.
- Mockup attachment, viewing, generation, editing, or store mockup setup.
- Raster editing, resizing, conversion, generation, source-design workflows, or non-PNG Design files.
- Design entities, ordering, roles, selected-final flags, version history, or concept history.
- External OS image launch.
- AI assistance, batch operations, custom stages/statuses, or plugin discovery changes.
- Redesigning unrelated management surfaces or fixing unrelated existing behavior.

## Decisions

### 1. Rename the universal model to Item at every durable boundary

Rename the domain record, status helpers, snapshot collections, relationship properties, public application contracts, UI types and strings, serialized contract names, and physical SQLite structures. Preserve the persisted numeric value `3` when `WorkspaceEntityKind.Listing` becomes `WorkspaceEntityKind.Item`, so existing generic `asset_links` rows keep their meaning.

Rationale: keeping legacy storage or public names would leave two meanings of “Listing” and force every future stage or plugin feature to translate between them. The application is early enough that one explicit migration is cheaper than permanent semantic debt.

Alternative considered: rename only UI/domain symbols and keep `listings` tables internally. Rejected because the approved term is universal and a weaker implementation agent should not perpetuate mixed vocabulary.

### 2. Centralize workflow and edit policy

Add one focused `ItemWorkflowPolicy` domain type that returns decisions rather than mutating persistence. It owns:

- ordered and viewable stages;
- adjacent movement eligibility;
- whether active-view content is editable;
- the complete direct status transition graph;
- stage preconditions for Published;
- whether one confirmation is required;
- Published, Rejected, archived, and effectively archived protection;
- classification of product-content operations versus approved shared metadata/lifecycle operations.

Application services call this policy before every save, stage move, status mutation, Design-file mutation, and protected lifecycle action. View models consume application-owned decisions to present controls, but UI state is not the authority.

Alternative considered: duplicate booleans in each view model and service. Rejected because cross-tab stale state or a future tool could bypass protection.

### 3. Keep Item core fields simple and put evolving stage values in metadata

Rename `Listing` to `Item` while retaining its current identity, store/topic placement, optional `Name`, legacy `Description`, status, stage, archive, timestamps, and `MetadataJson` shape. Use metadata keys:

- existing `idea` for original Idea;
- new `concept.idea` for Concept idea;
- existing `phrase` for Phrase;
- existing `graphicDirection` as the stored compatibility key for the user-facing Graphics description;
- existing `notes` for Notes;
- preserve `idea.audience` but do not expose it;
- preserve generic Description, unknown keys, and `inheritedFrom:*` provenance untouched.

An empty working title is persisted as an empty string. One `ItemDisplayNameFormatter` returns the trimmed title when present; otherwise `Untitled item · ` plus the first eight characters of `item.Id.ToString("N").ToLowerInvariant()`. Never write the fallback to the Item.

Alternative considered: create relational Idea/Concept/Design tables now. Rejected because versioning and advanced history are explicitly out of scope and metadata already protects forward compatibility.

### 4. Separate shared Item chrome from built-in Stage Tools

The right document surface becomes one vertical scroll region:

```text
Tabs
Workflow navigator
Scrollable Item document
  Overview (working title, path, current stage, one status selector)
  Stage Tool Host
    Idea | Concept | Design | Listing built-in tool
  Notes
  Tags
  Related assets (excluding Design PNGs)
  Lifecycle actions
```

The existing `BuiltInStageTools` descriptors remain the registration source. Their `DetailViewKey` selects a concrete App-owned built-in view/view-model, while tool availability and explicit `ToolContext` still come from Application. Shared Item fields are not duplicated inside tools.

Alternative considered: keep stage sections inside one inspector and leave the Stage Tool Host as a placeholder. Rejected because accepted architecture explicitly reserves the host for built-in and contributed stage surfaces.

### 5. Distinguish current stage from active view at the mutation boundary

`DocumentContext` and navigator continue to carry both persisted current stage and active view stage. A stage save request includes Item ID and expected current stage. Application reloads the latest snapshot and rejects the write unless the expected stage equals the persisted current stage and policy permits the operation.

Reviewing an earlier stage changes only the active view and renders its tool read-only. Regression persists the adjacent earlier stage, makes it active, and then enables that tool. No regression deletes or clears later data.

Alternative considered: allow upstream edits with a warning. Rejected because the user explicitly chose regression as the intentional safety action.

### 6. Use a complete explicit status state machine

Allowed transitions:

```text
Draft -----> Published   (Listing current only)
  |             |
  v             v
Rejected <---- Paused
  ^  ^           |
  |  |           v
  |  +--------- Draft
  +------------ Draft (reactivation direction is Rejected -> Draft)

Published -> Rejected
Paused -> Published (Listing current only) | Draft | Rejected
Rejected -> Draft
```

No other direct transition is valid. Entering Published or Rejected and every transition away from Published requires one confirmation; when conditions overlap, show one combined confirmation. A protected-content edit request on Published offers the specific Published-to-Paused confirmation. Stage remains Listing until the user deliberately regresses.

Published and Rejected lock stage content, Design files, generic related-asset links, and stage movement. They allow working title, Notes, Tags, topic placement, archive, and allowed status transitions. Archived/effectively archived Items remain entirely read-only until restoration.

Alternative considered: status as informational only. Rejected because Published was explicitly intended to protect live product content.

### 7. Reuse Assets for Design files with a workflow-specific filter

A Design file is an active Asset that:

- is linked to the current Item through `AssetLink`;
- has `AssetKind.ExportedImage`;
- has a managed workspace-relative path ending in `.png` case-insensitively.

Do not create a Design domain entity or add ordering/version columns. Import delegates to existing Asset management after pre-validating `.png`. Repeated import remains independent. Remove delegates to existing confirmed removal and compensation behavior. Shared Related assets filter these Design files out; the Design tool is their single presentation owner.

Alternative considered: add a Design table and variants. Rejected as premature and outside the approved module.

### 8. Extend the file boundary narrowly for preview and Export copy

Extend `IWorkspaceFileStore` with these managed-reference operations:

- `Task<Stream> OpenReadAsync(string workspaceRelativePath, CancellationToken)`; the caller owns and disposes the returned readable stream;
- `Task ExportCopyAsync(string workspaceRelativePath, string destinationPath, CancellationToken)`.

`LocalWorkspaceFileStore` normalizes the reference, resolves it below `WorkspaceRoot`, rejects traversal, verifies source existence, and copies bytes. Export rejects a source/destination path identity match and never mutates the Asset. App owns Avalonia storage pickers and image decoding; Application/Integration never depend on Avalonia storage objects. The preview consumes/disposes the stream so the file remains removable.

Alternative considered: expose `WorkspaceRoot` and concatenate paths in the view model. Rejected because it bypasses traversal and managed-boundary enforcement.

### 9. Preserve explicit text drafts and immediate Tags

One Item draft tracks optional working title, only the active current-stage text, and Notes. Explicit Save sends one stage-aware request and preserves unrelated/hidden metadata. Context change, active-view change, lifecycle change, or tab close invokes Save/Discard/Cancel. Cancel keeps context, draft, selection, and focus.

Tags retain their current independent immediate service calls. A Tag result refreshes authoritative Tag state without replacing the text draft. This is an intentional exception to the generic blur-autosave recommendation in `docs/ui-guidelines.md`; that guidance must be clarified so Item workflow fields follow the approved guarded draft behavior.

Alternative considered: blur-autosave all fields. Rejected because a multi-field creative stage needs a coherent intentional commit and the user approved explicit Save.

### 10. Synchronize from authoritative mutation results

Every successful Item mutation returns or triggers reload of authoritative Item state. `MainWindowViewModel` coordinates updates to:

- reusable and explicitly opened tabs for the same Item;
- navigator current/active stages;
- Stage Tool Host context and selected tool;
- tree row label and inactive treatment;
- stage/status/text/Tag filters;
- Item draft baseline and command availability.

Each write reloads the latest repository snapshot before validation. Stage-aware expected values prevent a stale tab from silently overwriting newer stage data. If a filter removes the active row, canonical selection follows existing sensible-replacement rules without reverting the mutation.

## Functional Design

### Primary workflow and frequency

- Item selection, stage review, stage text editing, stage movement, Tags, Notes, and Design-file review are frequent primary-workspace actions.
- Status transitions and Design import/export/remove are less frequent but remain close to the stage or state they affect.
- Archive remains a deliberate shared lifecycle action; permanent Item deletion remains existing secondary management behavior and is not expanded.
- Marketplace setup, mockup management, and external integrations remain outside the primary workspace and outside this module.

### Interaction states

- **Initial/empty:** an ID-only Item opens at Idea; fallback title is stable; fields and file lists show quiet empty states; movement remains available.
- **Loading/busy:** Save, stage/status mutation, import, preview load, export, and removal expose focused busy state and prevent duplicate/conflicting submissions.
- **Success:** authoritative state refreshes without extra tabs; new Design file is selected; removed file selects a remaining file or empty state; stage movement focuses the destination tool's first meaningful control.
- **Blocked:** read-only tool state explains current-stage, Published, Rejected, archived, missing-file, or context requirements and names the safe action.
- **Validation/error:** non-PNG import is rejected before copy; failed saves retain drafts; failed status reverts selector; failed removal keeps record/file; missing preview/export stays recoverable.
- **Cancellation:** pickers cause no change; status/removal/archive confirmations retain context; Save/Discard/Cancel cancellation retains draft and focus.
- **Accessibility:** essential controls are keyboard reachable; icon-only file actions expose tooltips/accessible names; tab order follows Overview → tool → Notes → Tags → assets → lifecycle.

## Implementation Plan

### Phase 1 — Specifications, naming inventory, and test anchors

1. Keep this proposal, all delta specs, this design, tasks, and verification mapping aligned. Update `docs/ui-guidelines.md` to document the explicit Item-workflow save exception without changing unrelated blur-autosave surfaces.
2. Build a repository-wide Listing-name inventory before edits. Classify every occurrence as universal Item terminology, final Listing-stage terminology, marketplace-future terminology, or historical migration SQL. Do not blindly replace stage names.
3. Rename test files/types along with production contracts so new failures use Item language. Add domain policy tests before changing application/UI behavior.

### Phase 2 — Domain Item model and policy

Likely files:

- `WorkspaceEntities.cs`: `Listing` → `Item`; allow empty `Name`.
- `WorkspaceRelationships.cs`: `ListingStatus` → `ItemStatus`, `ListingStatuses` → `ItemStatuses`, `WorkspaceEntityKind.Listing` → `Item` with numeric value `3`, `ListingTag` → `ItemTag`.
- `WorkspaceSnapshot.cs`: `Listings`/`ListingTags` → `Items`/`ItemTags`.
- `ListingHierarchy.cs` → `ItemHierarchy.cs`.
- `Prompt.ListingId` → `ItemId`.
- new `ItemWorkflowPolicy.cs`, `ItemStatusTransitionDecision`, `ItemEditDecision`, and `ItemOperationKind` domain types.

Policy implementation:

1. Represent allowed transitions as explicit switch/lookup pairs; do not infer from enum ordering.
2. `ItemStatusTransitionDecision` contains `IsAllowed`, `RequiresConfirmation`, and an actionable `Reason`.
3. `ItemEditDecision` contains `IsAllowed` and an actionable `Reason`; it is evaluated for an `ItemOperationKind` so shared metadata and protected product content cannot be conflated.
4. Stage editability requires active/effective Item, non-Published/non-Rejected status, and `activeViewStage == currentStage`.
5. Shared metadata policy separately permits working title, Notes, Tags, topic placement, archive, and status recovery for Published/Rejected.
6. Add exhaustive parameterized tests for all 16 status pairs at every relevant stage, stage boundaries, and archive/effective-archive combinations.

### Phase 3 — SQLite schema version 5 migration

Primary file: `SqliteWorkspaceRepository.cs`; integration coverage in `SqliteWorkspaceRepositoryTests.cs` and renamed persistence tests.

New schema uses `items`, `item_tags`, `prompts.item_id`, and Item-named load/insert methods. Keep `asset_links.entity_kind = 3` compatible.

Migration algorithm:

1. Open the version-4 database with foreign keys enabled and begin one transaction.
2. Create final `items` with the same columns/values as `listings`, allowing the chosen empty-string title representation.
3. Copy every `listings` row to `items` without transforming IDs, timestamps, stage/status integers, archive, text, or metadata.
4. Create `item_tags` referencing `items`; copy `listing_tags` unchanged.
5. Create `prompts_v5` referencing `items(item_id)`; copy every prompt and map `listing_id` to `item_id` unchanged.
6. Retain `asset_links` rows unchanged because the entity-kind numeric value remains `3`.
7. Verify row counts and run `PRAGMA foreign_key_check` before removing old child tables.
8. Drop old `listing_tags`, old `prompts`, and `listings` in child-first order; rename `prompts_v5` to `prompts`.
9. Set `PRAGMA user_version = 5` only inside the successful transaction and commit.
10. On any error, roll back completely and surface a recoverable open failure. After successful migration, downgrade to older application versions is unsupported; backup/restore is the rollback route.

Tests create a real v4 fixture containing active/archived Items, every status/stage, empty and named records, Tags, Prompts, Design/reference Assets, generic links, unknown metadata, and nested topic placement. Compare complete snapshots before migration and after migrate-save-reopen.

### Phase 4 — Application contracts and services

Rename:

- `ListingManagement.cs` → `ItemManagement.cs` and all requests/results/summaries/methods.
- `ListingInspector.cs` → `ItemInspector.cs`.
- `ListingMetadataCodec.cs` → `ItemMetadataCodec.cs`.
- universal `ListingId`/`Listings` references throughout Store, Niche, Group, Workspace, Tag, Asset, tree, navigation, tool-context, and Stage Tool services.

Behavior:

1. Add application contract `IItemIdGenerator` with `Guid NewId()` and production `GuidItemIdGenerator`; `CreateItemAsync` accepts optional title, obtains and validates the ID through that contract, resolves destination through existing canonical/default-niche rules, and persists Draft/Idea/not-archived.
2. Add `ItemDisplayNameFormatter`; reuse it in navigation/document projections only.
3. `ItemInspectorState` exposes shared fields, current stage, active view, read-only reasons, stage-specific records, Tags, generic non-Design assets, and Design files.
4. `ItemSaveRequest` contains Item ID, expected current stage, optional title, Notes, and exactly one stage payload union/record. Service reloads snapshot, validates policy, updates only allowed keys, preserves legacy/unknown/provenance, saves once, and returns authoritative state.
5. Status and stage mutation services reload latest state, call policy, require explicit confirmed-request flags for protected transitions, save once, and return authoritative Item.
6. Rename Tag request/link contracts to Item and keep their existing independent immediate behavior.
7. Add `IDesignFileService` or a focused Asset service facade with query/import/preview descriptor/export/remove eligibility. Reuse `AssetManagementService` mutation logic rather than duplicating record/link compensation.
8. Extend `IWorkspaceFileStore` with managed read and Export copy operations; add App-facing picker/preview abstractions separately.
9. Update `ToolContext.SelectedItem` to type `Item`; allow empty names by applying the display formatter where a nonblank context label is required.

### Phase 5 — Built-in Stage Tool presentation

Likely App types:

- rename `Listings/ListingInspectorViewModel.cs` to an Item document/shell view model;
- add focused Idea, Concept, Design, and Listing tool view models or one discriminated stage-tool presentation state, each backed by Application contracts;
- update `DocumentContext`, `DocumentWindowViewModel`, `WorkflowStageNavigatorViewModel`, `WorkspaceTreeViewModel`, `MainWindowViewModel`, and `MainWindow.axaml`;
- extend file-picker abstractions for multi-PNG import and export destination;
- add an App preview state/control that decodes the managed PNG from a disposed stream.

Composition:

1. Keep `BuiltInStageTools` descriptor IDs/detail keys stable unless a deliberate migration requires change.
2. Render selected tool content by `DetailViewKey` through the Stage Tool Host, not as four inspector sections.
3. Place Overview, Notes, Tags, Related assets, and lifecycle controls once around the host inside one `ScrollViewer`.
4. Overview owns the only status selector. Options and confirmations come from Application policy results.
5. Visibility follows active view; editability follows current-stage equality plus policy. Show read-only explanation rather than hiding fields.
6. Idea tool: multi-line Idea only; no Audience.
7. Concept tool: Concept idea, Phrase, Graphics description.
8. Design tool: multi-select/import loop as supported by picker, list, selection, in-app preview, Export copy, missing state, confirmed Remove.
9. Listing tool: compact local lifecycle explanation/status summary; no duplicate selector and no mockups/marketplace fields.
10. Shared Related assets filter out Design PNGs.
11. Explicit Save covers title, current-stage text, and Notes; Tag commands remain immediate. Context-leave guard includes active view changes and lifecycle actions.
12. Ensure minimum-height scrolling, compact buttons, accessible names/tooltips, logical tab order, and predictable focus.

### Phase 6 — Synchronization and recovery

1. Refactor `MainWindowViewModel` coordination so every successful mutation applies one authoritative Item refresh to tree, tabs, navigator, tool host, filters, and draft baseline.
2. Preserve canonical selection and reusable-tab behavior. Explicit additional tabs for the same Item refresh together.
3. Reject stale expected-stage saves and keep their drafts with actionable reload guidance.
4. During busy Item/file mutation, disable conflicting commands and duplicate submission; do not globally freeze unrelated navigation without a guard decision.
5. Preserve existing rollback/compensation conventions: copy then save with best-effort cleanup on failed import; save removal then best-effort file delete; export never changes structured state.

### Phase 7 — Deterministic verification and documentation

1. Update all affected Domain, Application, Integration, and App tests instead of deleting coverage during rename.
2. Add focused tests named or traceable to the delta scenarios.
3. Update canonical UI/UX/domain/data documentation where Item terminology or explicit-save behavior changed.
4. Run build, targeted tests during development, then `dotnet test .\FusionCanvas.sln`, strict OpenSpec validation, formatting/whitespace verification where configured, and `git diff --check`.

### Phase 8 — Targeted desktop verification

Use a disposable database and managed workspace root. Exercise the critical journey: ID-only creation; Idea/Concept saves; ungated advance/regress; read-only earlier review; multiple PNG import; preview/export/missing/remove; publish protection and confirmed Paused regression; Rejected recovery; archive/restore; multi-tab/filter synchronization; restart; short-window scrolling; keyboard/focus; cancellation and representative recovery.

This set is sufficient because it covers every novel framework boundary and high-risk behavior: schema migration is additionally covered by deterministic fixtures; equivalent status-pair variants remain exhaustive domain tests; low-risk field empty/populated combinations remain application tests.

## Acceptance-to-Verification Mapping

Every delta scenario is planned below. Final command names, test names, desktop evidence, results, and limitations will be recorded in `verification.md`.

| Capability | Scenarios | Planned evidence |
| --- | --- | --- |
| `basic-product-workflow` | Item opens at its current stage; Earlier stage is reviewed; User attempts to edit reviewed upstream content; User regresses intentionally; Empty Item advances through all stages; Stage move succeeds | Domain/application workflow tests, App visibility/editability tests, critical desktop journey |
| `basic-product-workflow` | User saves an Idea; Existing Item contains Audience metadata; User saves Concept values; Concept remains empty | Application metadata/save tests, Integration round trip, desktop representative Idea/Concept save |
| `basic-product-workflow` | Item surface is populated; Window height is reduced | App state tests, XAML inspection, desktop resize/scroll/keyboard evidence |
| `basic-product-workflow` | User leaves a meaningful text draft; User changes a Tag while text is dirty; Save fails | App leave-guard/focus tests, Application failing-repository tests, desktop Save/Cancel representative path |
| `basic-product-workflow` | Completed workflow survives restart; Item is archived and restored; Successful mutation synchronizes open contexts | Integration round trip, Application/App coordination tests, desktop restart/archive/multi-context journey |
| `basic-product-workflow` | User operates the workflow by keyboard; User repeats an action while it is running | App accessibility/busy-state tests and desktop keyboard/duplicate-submission checks |
| `core-domain-model` | Contributor identifies item entities; Item belongs to a store topic context; Item is created without user-entered content; Empty Item needs a display label | Domain compile/boundary tests, Application creation tests, formatter tests |
| `core-domain-model` | Existing workspace is upgraded | v4-to-v5 Integration fixture and snapshot comparison |
| `listing-management` | User creates an empty Item from a selected topic; Selected Item supplies its containing topic; Store context uses the default niche; Store has no resolvable topic; User supplies optional creation details; Generated identity is invalid | Item management application tests with a deterministic `IItemIdGenerator` |
| `listing-management` | Empty Item appears across surfaces | Formatter, navigation, tab, and Item surface App tests plus desktop check |
| `listing-inspector` | Item is at Concept; User reviews an earlier stage; Save targets a non-current stage; User saves valid Item edits | App visibility/editability tests and Application stage-aware save tests |
| `listing-inspector` | User switches context with unsaved text; Tag changes while text is unsaved | App draft-guard tests, Tag/application tests, desktop cancellation/focus check |
| `listing-inspector` | Status options are displayed; Stage or status changes | Domain transition matrix, App option/synchronization tests, desktop status/filter path |
| `listing-lifecycle-status` | Item persists stage and status independently; Status change does not move the stage; Item exposes allowed transitions; Confirmation is required; Status change fails to persist | Domain exhaustive matrix, Application status service and failure tests, desktop representative confirmations |
| `listing-lifecycle-status` | Published Item cannot move stages; Rejected Item is reactivated; Published Item is reviewed; User modifies Published content intentionally; Rejected Item is reviewed | Domain edit-policy tests, App command/read-only tests, desktop publish/pause/regress/reject path |
| `workflow-stage-navigator` | Earlier stage is selected; User needs to edit an earlier stage; Published Item is active | Application navigator tests, App navigator tests, desktop pointer/keyboard representative path |
| `asset-management` | User imports Design files; User selects a non-PNG file; Same source is imported twice | Application validation tests, Integration managed-file tests, desktop multi-import/validation |
| `asset-management` | User previews a Design file; User exports a Design file; Managed file is missing | File-store security/byte tests, App preview/command tests, desktop preview/export/missing evidence |
| `asset-management` | User removes a Design file; Removal persistence fails; Item has Design and reference assets | Application compensation/filter tests, Integration file tests, desktop cancel/confirm removal |
| `workspace-file-storage` | Preview resolves a valid managed reference; Preview reference escapes the workspace; Export copy succeeds; Export is cancelled or invalid | Integration file-store boundary, path traversal, cancellation, same-path, and byte-equality tests |
| `stage-tool-host` | Basic built-in tool is registered; Shared Item chrome is rendered; Later plugin tools are added | Application registry/host tests, App composition tests, desktop active-tool inspection |
| `navigation-tree` | Empty-title Item appears in the tree; Published or Rejected Item appears | App tree projection/status tests and desktop navigation evidence |
| `tabbed-document-window` | Empty-title Item opens in a tab; Item title is saved | App tab coordination tests and desktop multi-tab refresh |
| `search-filtering` | Status change leaves the active filter; Empty Item is searched | Application/App filter tests and desktop representative filtered status change |
| `local-sqlite-persistence` | Version 4 workspace is opened; Item migration fails; Migrated workspace is saved and reopened | Isolated real SQLite migration, rollback injection where practical, foreign-key check, complete snapshot comparison |
| `tag-management` | Tag is applied while Item text is dirty; Item terminology migration preserves Tags | Application independent-draft test and Integration migration fixture |
| `context-aware-tools` | Built-in Stage Tool opens for an Item; No Item is selected | Tool-context and Stage Tool Host application tests |

## Migration Plan

1. Build and test the v5 migrator against disposable copies/fixtures before changing normal application composition.
2. On first open of a v4 workspace, run the single transactional migration before loading a `WorkspaceSnapshot`.
3. Preserve v4 data on failure through transaction rollback; report that the workspace could not be upgraded.
4. After successful v5 migration, old FusionCanvas versions are not expected to open the database. Users needing downgrade must restore a pre-upgrade backup/copy.
5. Managed files are not moved or renamed; only structured relationships and terminology change.
6. No separate user action is required after successful migration.

## Risks / Trade-offs

- **[Cross-cutting rename misses a legacy public contract]** → Use a classified repository-wide name inventory and fail completion on stale universal `Listing` symbols outside migration/history/final-stage contexts.
- **[SQLite table recreation loses relationships]** → Migrate in one transaction, preserve IDs/integers verbatim, check counts and `PRAGMA foreign_key_check`, and compare complete fixture snapshots after save/reopen.
- **[Old app cannot read v5]** → State downgrade boundary explicitly and use backup/restore rather than pretending schema rollback is safe.
- **[Current-stage protection exists only in UI]** → Enforce expected stage and edit policy in application services after reloading latest state.
- **[Explicit Save conflicts with general UI guidance]** → Document the approved Item-workflow exception and test leave guards and immediate Tags separately.
- **[Stage Tool composition duplicates shared controls]** → Keep one surrounding Item shell and assert shared chrome is absent from built-in tools.
- **[Preview locks files or allows traversal]** → Decode from a short-lived validated managed stream and dispose it; cover traversal and subsequent removal in Integration tests.
- **[Design Asset filter misclassifies images]** → Require Item link + ExportedImage kind + `.png` managed extension; test reference/Mockup/non-PNG assets.
- **[Multi-tab refresh overwrites unsaved drafts]** → Apply authoritative state selectively, detect stale stage/baseline, and retain/reconcile drafts rather than silently replacing them.
- **[Module size becomes hard to diagnose]** → Implement in ordered layer gates with passing focused tests after each phase; do not combine the database, policy, file, and UI rewrites in one unverified step.

## Open Questions

None. Product, UX, data, architecture, migration, save, and verification decisions required for implementation are resolved. Any newly discovered decision that would change accepted behavior or the implementation plan must stop the affected task and return to specification review rather than being guessed.

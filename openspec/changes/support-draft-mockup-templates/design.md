## Context

Issue 204 exposed a contract mismatch rather than only a disabled-button defect. `MainWindowViewModel` composes the production catalog editor with `UnavailableProviderCatalogCandidateSource`, but `CatalogSetupViewModel.CanCreateTemplate()` requires `SelectedProviderMockup`. The only passing creation path injects a test provider source. The modal also omits `CatalogSetup.ErrorMessage`, so failures reported by the view model appear only behind the owned dialog.

The current model assumes complete configuration at identity boundaries: `MockupTemplate.TargetPlaceholderId` and `MockupTemplateRevision.TargetPlaceholderId` are non-null; `MockupTemplateRevision` couples provider reference and mapping; `CreateFocusedMockupTemplateRequest` requires target, provider image, Colors, and mapping; and schema version 12 declares both target foreign keys `NOT NULL`. Color bindings can already be empty structurally. Provider candidate support is transient application data and has no production adapter.

The product decision supersedes provider-gated creation: a manually named template is a valid persisted Draft, provider integration is optional, and only a derived Ready-for-use template may enter customer-facing or design-preview rendering. The existing Store Editor-owned modal remains the right focused surface for this occasional setup workflow.

## Goals / Non-Goals

**Goals:**

- Persist a named Mockup Template from any editable Offering without provider data or other render configuration.
- Keep supplied partial values structurally valid and same-Offering while treating missing values as readiness blockers rather than persistence blockers.
- Derive Draft/Ready from one Domain policy and expose all blockers to UI and future render consumers.
- Preserve immutable attributable revisions when output configuration is added, removed, or changed.
- Make the modal's Save state, readiness guidance, failure handling, selection, and focus coherent and deterministically testable.
- Migrate schema version 12 data transactionally without changing existing complete template values.
- Remain local-first and fully testable without network access or external accounts.

**Non-Goals:**

- A Printify/provider API adapter, synchronization workflow, credentials, or network retry system.
- Local image import/upload, drag/drop, managed-file creation, image editing, composition, or rendering.
- A detailed future rendering UI or listing workflow.
- Persisting a manually editable lifecycle/status flag.
- Persisting malformed numeric input or cross-Offering/archived relationships merely because a template is a Draft.
- Reworking unrelated catalog lifecycle and dependency safeguards.

## Decisions

### 1. Separate persistence validity from render readiness

Two evaluations serve different purposes:

- **Save validity** requires an editable, current Offering; a nonblank name; and valid values for every optional field that is actually supplied. Missing optional configuration is valid.
- **Render readiness** requires the complete current render contract and produces an ordered set of blockers.

The primary action is therefore permitted for a valid Draft even when readiness blockers remain. Invalid supplied mapping text, an out-of-bounds mapping, or an invalid supplied relationship still blocks persistence because storing corrupt structured state would weaken Domain invariants.

Alternative considered: remove provider image from the existing `CanCreateTemplate` predicate and leave other requirements unchanged. Rejected because it still prevents saving before Design Area/Color configuration and would leave no authoritative render gate.

### 2. Derive readiness in Domain; do not persist status

Add a focused Domain model, tentatively:

```text
MockupTemplateLifecycle: Draft | ReadyForUse
MockupTemplateReadiness
  Lifecycle
  Blockers[]

MockupTemplateReadinessBlockerCode
  MissingTargetDesignArea
  InvalidTargetDesignArea
  MissingColors
  InvalidColors
  MissingCompatibleVariants
  IncompatibleVariants
  MissingImage
  InvalidImageDimensions
  MissingMapping
  InvalidMapping
  KnownImageColorIncompatibility
  Archived
```

`MockupTemplatePolicy.EvaluateReadiness(...)` accepts the template/current-revision configuration plus active Offering relationships and optional currently known image/Color support. It returns all applicable blockers in a stable presentation order. `ReadyForUse` means an active template has no blockers. The App maps blocker codes to concise user text; Domain does not depend on Avalonia or provider SDK types.

Absence of live provider metadata is not itself a blocker once an image reference and valid dimensions/mapping are persisted. A known incompatibility is a blocker when such metadata is available. This keeps reload and offline behavior deterministic and avoids turning a network/source outage into a lifecycle mutation.

Alternative considered: store `Draft`/`ReadyForUse` in SQLite. Rejected because catalog archive/compatibility changes could make the value stale and allow consumers to bypass current invariants.

### 3. Make readiness configuration nullable but keep template identity strict

Change `MockupTemplate.TargetPlaceholderId` and `MockupTemplateRevision.TargetPlaceholderId` to `Guid?`. Keep template ID, Offering ID, nonblank name, revision number, timestamps, and archive state required. Allow the current revision's image reference and `MockupImageSpaceMapping` to be independently absent so a user may add or remove them incrementally. A supplied mapping remains an immutable `MockupImageSpaceMapping`, preserving positive dimensions and in-bounds coordinates.

Color applicability remains represented by active `MockupTemplateColorVariant` rows and revision-color snapshots; zero bindings is a valid Draft. Explicit replacement with an empty set removes active applicability and records an empty snapshot in the next output-affecting revision.

The initial name-only save creates revision 1 with nullable target/image/mapping and no revision-color rows. Output-affecting updates compare nullable target, Color set, image reference, and mapping. Name/description-only edits do not advance the revision.

Alternative considered: create templates without a revision until configuration exists. Rejected because a saved Draft is already attributable user work and later history needs a stable revision origin.

### 4. Use one provider-independent application save path

Create and update shall use one focused application owner rather than the current split where create uses `OfferingManagementService` and edit uses `MockupTemplateSetupService`. Extend `IOfferingManagementService` with explicit partial create/update requests (or replace `CreateFocusedMockupTemplateRequest` with equivalent optional fields) and a shared internal validator/revision builder. Both operations:

1. load one snapshot and resolve Store/Blueprint/Offering;
2. reject archived or stale context;
3. normalize the name and optional values;
4. validate each supplied Design Area, Color, and mapping;
5. create or update the template and at most one new revision;
6. call the repository save once;
7. return authoritative Offering state plus readiness.

The save path does not call `IProviderCatalogCandidateSource`. Optional candidates remain a presentation aid; supported-Color metadata already loaded for the draft may be passed as optional evidence for readiness/validation, but lack of that evidence never blocks persistence. Archive/restore operations may remain in `IMockupTemplateSetupService`; speculative service consolidation is out of scope.

Alternative considered: teach `UnavailableProviderCatalogCandidateSource` to fabricate a candidate. Rejected because it would invent external data and violate local ownership and accepted unavailable-state behavior.

### 5. Represent absent or malformed mapping input explicitly in the view model

The current non-null `double` fields default Width/Height to `100`, which cannot distinguish “not configured” from actual input and relies on binding conversion for invalid text. Replace the four numeric draft bindings with string-backed editor values (or an equivalent nullable input model) and one parse result:

```text
all four blank                     -> mapping omitted; save-valid Draft
some but not all populated         -> invalid supplied mapping; Save disabled
non-numeric/non-integral value     -> invalid supplied mapping; Save disabled
positive integral in-bounds values -> valid mapping
negative/zero/out-of-bounds        -> invalid supplied mapping; Save disabled
```

The placement control is visible/enabled only when an image with positive dimensions exists and a valid mapping can be initialized. Pointer/keyboard changes update the textual fields through a focused adapter. Selecting a provider candidate may initialize a sensible valid mapping; clearing the image does not fabricate one and may retain user-entered mapping as partial configuration if structurally valid.

Alternative considered: keep non-null doubles and treat zero image dimensions as absence. Rejected because it cannot preserve empty/partial editor intent or explain conversion failures completely.

### 6. Present readiness and save errors inside the focused dialog

`CatalogSetupViewModel` exposes:

- current lifecycle label (`Draft` / `Ready for use`);
- an observable/read-only ordered readiness blocker collection;
- `HasReadinessBlockers`;
- a separate save-validation message/collection for malformed supplied input;
- the existing persistence `ErrorMessage` inside the modal;
- Save eligibility based only on save validity, `CanEdit`, current context, and busy state.

Every relevant setter and child Color-selection notification re-evaluates parsed input, readiness, and command state. `StartAddTemplateCommand` requires only `CanEdit` and a current Offering, not an available Design Area. New drafts start with Name focused. The optional provider selector is labelled **Provider mockup image (optional)** and unavailable/empty/error copy says Save Draft remains available.

On success, the view model invokes one application mutation, applies returned state, selects by returned stable template ID rather than name, ends the draft, and lets existing dialog ownership restore focus. On failure, it leaves the modal, all draft fields, readiness guidance, and focus intact. The dialog renders the error rather than relying on the inaccessible parent surface.

### 7. Migrate schema 12 to 13 by rebuilding affected tables transactionally

SQLite cannot remove `NOT NULL` in place. Add a 12 → 13 migration that, inside one transaction:

1. creates replacement `mockup_templates` and `mockup_template_revisions` tables with nullable `target_placeholder_id` while retaining all other columns, checks, and foreign keys;
2. copies rows without transforming non-null values;
3. validates row counts, identities, template/revision ownership, revision uniqueness, and `PRAGMA foreign_key_check`;
4. replaces old tables and recreates dependent indexes/constraints;
5. advances `user_version` only after validation succeeds.

Insertion/loading mappings use nullable GUID helpers. New-database schema is created directly at version 13. Snapshot validation permits null targets but continues rejecting non-null targets outside the template Offering and invalid supplied mappings. Existing schema-12 complete templates remain byte-for-value equivalent at the model level and keep their derived readiness.

Rollback is the SQLite transaction: any failure leaves schema version 12 and its tables intact. Application rollback to an older binary after a successful migration is not supported because the older binary cannot safely understand version 13; users must restore a pre-migration backup, consistent with existing schema-version policy.

### 8. Rendering eligibility enters through an application contract

Add an application query/result that returns readiness for a template and filters eligible active templates for an Offering. Future preview/render code consumes that contract and receives blockers when a specifically requested Draft is rejected. No renderer is implemented in this module; tests establish the boundary so “persisted” cannot later be mistaken for “renderable.”

## UX Preflight

- **User/outcome:** a Store owner incrementally records Mockup Template setup without losing work or needing provider connectivity.
- **Frequency:** create/edit is occasional Store administration; readiness review happens whenever configuration is refined.
- **Surface:** the existing Store Editor-owned modal remains the sole editor; cards show compact lifecycle context. No main-workspace area is added.
- **Progressive disclosure:** identity and lifecycle/checklist remain visible; provider assistance and advanced provider data remain secondary; placement appears only with an image.
- **Initial/empty:** a new name-only Draft is valid to save; no-image preview is compact; no Design Areas or Colors are explained as readiness blockers.
- **Loading/unavailable:** provider loading or failure never disables Draft save; it only affects optional choices.
- **Success:** exactly one persistence call, authoritative refresh, saved template selected, modal closes, focus returns to Add or Edit.
- **Validation/error:** all readiness blockers are visible; malformed supplied values separately block Save; persistence errors appear in the modal and preserve the draft.
- **Cancellation:** existing unchanged-close and meaningful-discard/keep-editing behavior remains.
- **Context/read-only:** stale context closes/discards the editor; archived Store prevents opening an editable modal; busy state disables Save and editing.
- **Keyboard/sizing:** Name receives initial focus; checklist, controls, and action row remain reachable in deterministic tab order and through the existing scroll viewer at normal/narrow supported sizes.

## Implementation Plan

### Domain

- Update `src/FusionCanvas.Domain/Mockups/MockupTemplate.cs` and `MockupTemplateRevision.cs` for nullable target configuration and independently optional image/mapping values while retaining strict validation for supplied IDs and mappings.
- Add focused readiness types under `src/FusionCanvas.Domain/Mockups/` and extend `MockupTemplatePolicy` with one all-blockers evaluator and nullable output-change comparison.
- Update catalog dependency policies so null targets and zero Colors are valid Draft state, while non-null relationships retain current deletion/archive safeguards.
- Add tests in `tests/FusionCanvas.Domain.Tests/Mockups/` (or the existing catalog model test location if kept cohesive) for minimum identity, every blocker, multi-blocker accumulation, transitions, archive behavior, and revision comparison.

### Application

- Update `CreateFocusedMockupTemplateRequest`, add/update request contracts, `MockupTemplateSetupSummary`, and related state/results to carry nullable configuration, returned template identity, lifecycle, and blockers.
- Refactor `OfferingManagementService` create/update into a shared provider-independent save pipeline with one repository write and authoritative state return. Remove provider lookup as a persistence precondition; keep optional provider-candidate loading for UI assistance and bulk workflows that genuinely require it.
- Update `MockupTemplateSetupService` and workspace snapshot validation/mapping code for nullable target and independently optional revision values; keep archive/restore and historical revision behavior.
- Add an application-facing readiness/eligible-template query for future preview/render consumers.
- Extend `OfferingManagementServiceTests` and `MockupTemplateSetupServiceTests` for name-only create, partial update, supplied-invalid rejection, exactly-once write, Draft↔Ready revision transitions, metadata-only updates, failure preservation, no-provider behavior, and eligibility filtering.

### Integration and compatibility

- Raise `SqliteDatabaseSchema.CurrentVersion` from 12 to 13 and implement the transactional table-rebuild migration in `SqliteWorkspaceRepository`/schema helpers.
- Update create-table SQL, insert/load mapping, nullable GUID helpers, snapshot validation, and foreign-key ordering for nullable target values.
- Extend `ProductCatalogPersistenceTests` with schema-12 fixtures for complete templates, migration row/value equality, name-only Draft round-trip, clear-field update, fresh-version-13 database, failed-migration rollback, and newer-version refusal.
- Extend `WorkspacePackageIntegrationTests` to round-trip mixed Draft/Ready templates without provider connectivity.

### App and focused editor

- Update `CatalogSetupViewModel` so Add is Offering/editability-gated, mapping draft values represent absence/invalid text, every relevant setter and selection re-evaluates save validity/readiness, and create/update use the unified application path.
- Update `CatalogPresentationModels` and card rendering in `StoreEditorWindow.axaml` to show Draft/Ready without conflating lifecycle with archive state.
- Update `MockupTemplateEditorWindow.axaml` to render lifecycle, all readiness blockers, save-validation text, and `CatalogSetup.ErrorMessage`; relabel provider selection as optional; retain compact no-image and guarded discard regions.
- Limit code-behind changes in `StoreEditorWindow.axaml.cs` to existing dialog/focus ownership; keep policy and persistence out of the view.
- Extend `CatalogSetupViewModelTests`, `CatalogPresentationModelsTests`, and `StoreEditorHeadlessTests` for each command transition, optional-provider states, validation text, successful headless Save, exactly-once selection/close behavior, failure preservation, focus, and narrow sizing.

### Sequencing and decisions not to reopen

1. Implement and test Domain nullability/readiness first.
2. Update Application contracts/save/query behavior and tests.
3. Add schema 13 migration and persistence/package tests before changing UI construction paths.
4. Update ViewModel/presentation behavior and framework-free tests.
5. Update XAML/headless interaction tests.
6. Run focused suites, strict OpenSpec validation, then the full solution baseline.

Implementation SHALL NOT add provider synchronization, fabricate candidates, add local image upload, persist a lifecycle flag, permit malformed supplied values, or introduce a second save mutation. The minimum saved identity remains Offering plus nonblank name.

## Acceptance-to-Verification Mapping

### `mockup-template-readiness`

| Scenario | Planned verification |
|---|---|
| User saves a name-only template without provider integration | Application test with `UnavailableProviderCatalogCandidateSource` plus SQLite reload assertion |
| User saves available partial configuration | Parameterized Application tests for valid target-only, Colors-only, image-only, and mapping-only subsets |
| Minimum template identity is missing | Domain/Application tests for blank name and stale/missing Offering with repository write count zero |
| Supplied partial configuration is invalid | Application tests for cross-Offering/archived targets, non-Color values, and invalid mapping; assert state unchanged |
| Complete compatible template is Ready for use | Domain readiness test covering the complete valid fixture |
| One or more readiness inputs are absent | Domain theory removing each input plus a multi-removal assertion that all blocker codes return |
| Catalog change makes a complete template incompatible | Domain/Application test mutating active catalog relationships and re-evaluating without status persistence |
| Archived template has complete configuration | Domain readiness and eligibility-query tests |
| Name-only Draft is created | Application test asserting revision 1 nullable snapshot and exactly one repository save |
| Draft becomes Ready for use | Application test asserting one new immutable revision and Ready result |
| Ready template becomes Draft | Application test clearing each output field and asserting one new Draft revision |
| Non-output metadata changes | Application test asserting unchanged revision number and one template update |
| Provider catalog is unavailable | Application/ViewModel tests proving name-only Save remains executable and succeeds |
| Provider candidate prefills configuration | ViewModel test for optional candidate initialization without provider lookup during mutation |
| Known provider compatibility is violated | Domain readiness test with optional support evidence and eligibility exclusion |
| Preview workflow queries eligible templates | Application query test with Ready, Draft, and archived fixtures |
| Draft template is selected by stale identity | Application query test asserting blocker-bearing rejection and unchanged Draft |

### `product-supplier-setup`

| Scenario | Planned verification |
|---|---|
| User opens optional provider image selection | Avalonia headless accessibility/text test |
| Provider catalog is loading | ViewModel pending-source test and headless visible-state test; Save Draft remains executable after naming |
| Provider catalog provides candidates | ViewModel/headless selector population and readiness-transition test |
| Provider catalog is empty | ViewModel/headless state text and Draft-save eligibility test |
| Provider catalog is unavailable | Production-composition ViewModel or headless test proving Save Draft succeeds without candidates |
| Provider catalog request fails | ViewModel/headless failure-state test preserving manual save |
| User reviews the Mockup Template collection | Headless collection-width/Add availability test with no Design Areas and no provider source |
| User adds a Mockup Template | Headless modal ownership/title/Name-focus test from empty readiness context |
| User edits a Mockup Template | ViewModel/headless population test for both partial and complete templates |
| Preview-first mapping is conditionally available | Existing placement synchronization headless tests updated for optional mapping |
| No image is configured | Headless compact-state test with hidden placement and reachable Save |
| Save eligibility changes | ViewModel theory covering name, image, Design Area, Color, every mapping field, busy, read-only, and stale context |
| Draft readiness is explained | ViewModel all-blocker assertion plus headless visible checklist test |
| Ready template is explained | ViewModel transition and headless lifecycle/checklist-resolution test |
| Save fails validation or persistence | ViewModel fake-repository failure and headless in-dialog error/draft-preservation test |
| Save succeeds | Headless command execution with counting repository; assert one write, selected stable ID, close, and focus return |
| User dismisses an unchanged draft | Existing headless cancellation test retained/updated |
| User dismisses a meaningful draft | Existing ViewModel/headless discard and keep-editing tests retained/updated |
| Editing context becomes stale | Existing context-change test plus partial-field coverage |
| Archived store is reviewed | Existing read-only ViewModel/headless test retained |
| Dialog is used with keyboard and supported sizes | Headless focus/tab/scroll/narrow-size test including readiness and error regions |

### `local-sqlite-persistence`

| Scenario | Planned verification |
|---|---|
| Partial Draft is saved and reopened | Isolated SQLite round-trip test for name-only and representative partial configurations |
| Complete template is saved and reopened | Isolated SQLite complete-fixture round-trip plus readiness evaluation |
| Readiness-related field is cleared | SQLite update/round-trip test with preserved prior revision |
| Previous supported database is opened | Hand-built schema-12 migration fixture with row/value/count assertions and `foreign_key_check` |
| New database is created | Fresh-database schema/version/nullability test |
| Migration fails | Malformed schema-12 fixture asserting rollback and unchanged `user_version` |
| Existing complete template is migrated | Schema-12 complete fixture before/after equality and no duplicate revisions/bindings |
| Workspace package contains partial templates | Workspace package export/import integration test for mixed Draft/Ready records |

The deterministic baseline is `dotnet test .\FusionCanvas.sln`. No live desktop test is required for acceptance: modal bindings, command state, focus, selection, visual-tree visibility, and successful Save are representable with Avalonia headless tests. A later live visual check may supplement review but cannot replace the mapped evidence.

## Risks / Trade-offs

- **[Nullable targets increase state combinations]** → Keep all readiness evaluation in one Domain policy and use exhaustive blocker/transition tests.
- **[Schema table rebuild can lose foreign keys or rows]** → Perform a transaction, copy without transformation, compare counts/values, run `foreign_key_check`, and advance version last.
- **[Provider metadata disappears after reload]** → Derive from persisted configuration and current catalog; absence of optional live metadata never downgrades readiness, while known incompatibility does.
- **[Save and readiness messages become confusing]** → Visually separate blocking save validation/errors from the non-blocking readiness checklist and label lifecycle explicitly.
- **[String-backed numeric fields complicate placement synchronization]** → Centralize parsing/formatting in one focused adapter and retain pointer, keyboard, numeric, and bounds headless tests.
- **[Future consumers bypass readiness]** → Provide one Application eligibility contract and test rejection of direct Draft selection.
- **[Existing complete templates change revision unexpectedly]** → Migration never rewrites model values; only explicit output-affecting edits create revisions.

## Migration Plan

1. Ship Domain/Application support and schema 13 migration in the same release so no version-13 partial record is read by non-null model code.
2. On first open, back up according to the existing workspace practice, execute the 12 → 13 transaction, validate, then set `user_version = 13`.
3. Existing complete templates retain all values and derive readiness under the new policy; incomplete states become Draft without data synthesis.
4. If migration fails, roll back and report the existing actionable upgrade failure; do not open the database for unsafe writes.
5. After successful migration, rollback requires restoring the pre-migration database with an older application version.

## Open Questions

None. Local image acquisition and provider synchronization are intentionally deferred capabilities; they are not delegated to implementation in this module.

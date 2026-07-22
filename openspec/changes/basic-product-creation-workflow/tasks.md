## 1. Delivery package and terminology inventory

- [ ] 1.1 Review `proposal.md`, every delta spec, `design.md`, and `verification.md` before code changes; correct artifacts first if implementation would require a product, UX, data, architecture, migration, or acceptance decision not already present.
- [ ] 1.2 Build a repository-wide inventory of `Listing`/`listing` occurrences and classify each as universal Item terminology, final Listing-stage terminology, future marketplace terminology, or historical v4 migration SQL; preserve the inventory in implementation notes or the change verification record.
- [ ] 1.3 Update `docs/ui-guidelines.md` to document the approved explicit-Save exception for Item working title, current-stage text, and Notes while retaining immediate Tags and leaving unrelated blur-autosave surfaces unchanged.
- [ ] 1.4 Run `openspec validate basic-product-creation-workflow --strict` after any artifact correction and do not proceed while it fails.

## 2. Domain Item model and workflow policy

- [ ] 2.1 Rename the universal `Listing` record to `Item` and update `WorkspaceSnapshot.Listings` to `Items`, preserving field meanings and allowing an empty working title.
- [ ] 2.2 Rename `ListingStatus`/`ListingStatuses` to Item terminology and implement the exact approved direct transition graph without relying on enum ordering.
- [ ] 2.3 Rename `WorkspaceEntityKind.Listing` to `Item` while preserving numeric value `3`; rename `ListingTag`, `ListingId`, hierarchy helpers, Prompt references, and other universal domain relationships to Item terminology.
- [ ] 2.4 Add the focused Item workflow/edit policy that decides stage availability, adjacent movement, current-stage editability, allowed status transitions, one-confirmation requirements, and Published/Rejected/archive operation permissions.
- [ ] 2.5 Add the single Item display-name formatter returning a normalized title or `Untitled item · <first eight lowercase hexadecimal ID characters>` without mutating Item data.
- [ ] 2.6 Update domain boundary and hierarchy code to Item terminology while preserving archive-aware topic behavior and all existing invariants.
- [ ] 2.7 Add exhaustive Domain tests for all status-pair/stage combinations, confirmation flags, stage boundaries, current-versus-view editability, Published/Rejected/archive protection, optional title, fallback formatting, stable entity-kind value, and downstream-data preservation decisions.
- [ ] 2.8 Run `dotnet test .\tests\FusionCanvas.Domain.Tests\FusionCanvas.Domain.Tests.csproj` and resolve failures before persistence work.

## 3. SQLite version 5 Item migration

- [ ] 3.1 Update new-database schema creation to physical `items`, `item_tags`, and `prompts.item_id` structures and Item-named insert/load/validation methods while retaining generic `asset_links.entity_kind = 3` compatibility.
- [ ] 3.2 Implement one transactional v4-to-v5 migration that creates Item structures, copies all values unchanged, recreates child relationships and foreign keys, verifies row counts and `PRAGMA foreign_key_check`, removes old child-first Listing structures, and advances `user_version` only on success.
- [ ] 3.3 Ensure migration failure rolls back to the complete v4 schema/data state and reports a recoverable workspace-open error; do not add unsupported in-place downgrade behavior.
- [ ] 3.4 Create a real v4 fixture containing named and empty records, every stage/status, archive state, unknown/provenance metadata, Tags, Prompts, Design/reference Assets, generic links, and nested topic placement.
- [ ] 3.5 Add Integration tests comparing complete pre-migration, post-migration, and post-save/reopen snapshots, including IDs, values, counts, relationships, entity-kind values, and managed references.
- [ ] 3.6 Add migration rollback/failure coverage using the narrowest practical fault point and record any uninjectable SQLite failure path for desktop/manual evidence rather than faking it.
- [ ] 3.7 Run `dotnet test .\tests\FusionCanvas.Integration.Tests\FusionCanvas.Integration.Tests.csproj --filter FullyQualifiedName~SqliteWorkspaceRepository` and resolve failures before application contract migration.

## 4. Application Item contracts and services

- [ ] 4.1 Rename Listing management, inspector, metadata codec, request/result/summary, Tag-link, navigation, tree, Store/Niche/Group/Workspace, Asset, Prompt, Tool Context, and Stage Tool universal contracts to Item terminology; do not rename the `WorkflowStage.Listing` value or final-stage display text.
- [ ] 4.2 Add `IItemIdGenerator.NewId()` with production `GuidItemIdGenerator`, change Item creation to accept an optional title and validate the generated non-empty ID, preserve existing canonical/default-niche destination rules and inherited context, and create Draft/Idea/not-archived Items with no fabricated content.
- [ ] 4.3 Add metadata keys/mapping for distinct `concept.idea`, retain `idea`, `phrase`, and `graphicDirection`, hide but preserve `idea.audience` and generic Description, and preserve unknown keys and `inheritedFrom:*` provenance.
- [ ] 4.4 Replace the generic inspector save request with a stage-aware Item save contract containing Item ID, expected current stage, shared working title/Notes, and exactly the current stage payload; reject stale/non-current-stage writes after reloading the latest snapshot.
- [ ] 4.5 Implement explicit Item text Save as one atomic snapshot mutation that updates only approved fields, returns authoritative Item state, and retains recoverable drafts on validation or persistence failure.
- [ ] 4.6 Implement stage movement through the central policy with adjacent-only, ungated behavior, Published/Rejected/archive blocking, downstream preservation, atomic persistence, and authoritative result state.
- [ ] 4.7 Implement status changes through the central policy with exact transitions, stage preconditions, one confirmed-request flag, cancellation/no-change behavior, failure rollback, and no stage mutation.
- [ ] 4.8 Keep working title, Notes, Tags, topic placement, archive, and allowed status recovery available under the approved Published/Rejected policies; reject product-content and related-asset mutations from those states.
- [ ] 4.9 Rename Tag service requests and links to Item and preserve their independent immediate persistence while an Item text draft remains pending.
- [ ] 4.10 Update Tool Context to expose selected Item, current stage, active view stage, status, fallback display label, metadata, and capability decisions without requiring nonblank persisted title or UI scraping.
- [ ] 4.11 Add/rename focused Application tests for ID-only creation, destination resolution, fallback label use, metadata compatibility, stage-aware saves, stale writes, movement, transitions/confirmations, edit policy, Tags-versus-draft independence, Tool Context, and failure recovery.
- [ ] 4.12 Run `dotnet test .\tests\FusionCanvas.Application.Tests\FusionCanvas.Application.Tests.csproj` and resolve failures before UI composition.

## 5. Managed PNG Design-file behavior

- [ ] 5.1 Extend `IWorkspaceFileStore` with narrowly scoped managed read and Export-copy operations; keep Avalonia storage objects and UI concerns out of Application/Integration contracts.
- [ ] 5.2 Implement managed reference normalization, workspace-boundary enforcement, missing/read failure handling, short-lived preview reads, distinct-source/destination validation, cancellation/no-change, and byte-identical Export copy in `LocalWorkspaceFileStore`.
- [ ] 5.3 Add the Design-file application facade over existing Asset management: filter Item-linked `ExportedImage` Assets by `.png`, import PNG only, retain independent duplicate imports, expose missing state/eligibility, and reuse confirmed removal/compensation behavior.
- [ ] 5.4 Exclude Design files from shared Related assets without hiding other Item-linked Assets.
- [ ] 5.5 Add Integration tests for valid preview reads, disposed file handles, traversal attempts, missing source, non-PNG rejection before copy, duplicate import, Export byte equality, cancelled/invalid/same-path export, confirmed removal, and persistence-failure compensation.
- [ ] 5.6 Add Application tests for Design-file filtering, Published/Rejected/non-current Design mutation rejection, missing-state commands, and authoritative result state.
- [ ] 5.7 Run Design/Asset/file-focused Application and Integration test filters and resolve failures before wiring Avalonia controls.

## 6. Item document shell and built-in Stage Tools

- [ ] 6.1 Rename universal Listing App types, bindings, commands, automation labels, and tests to Item terminology while retaining final Listing-stage names.
- [ ] 6.2 Update `DocumentContext`, tabs, navigation projections, and Tool Context labels to use the shared Item display formatter so an empty title is never rejected or inconsistently labeled.
- [ ] 6.3 Refactor `MainWindow.axaml` into one vertically scrollable Item document shell with shared Overview, Stage Tool Host, Notes, Tags, non-Design Related assets, and lifecycle areas in the approved order.
- [ ] 6.4 Keep the existing built-in descriptor IDs/detail-view keys and render concrete Idea, Concept, Design, and Listing views through Stage Tool Host selection; do not duplicate shared Item controls inside tools.
- [ ] 6.5 Implement Idea tool state/bindings for optional multi-line original Idea only; hide Audience and show read-only explanation during earlier-stage review or lifecycle protection.
- [ ] 6.6 Implement Concept tool state/bindings for Concept idea, single-line-normalized Phrase, and Graphics description with current-stage editability.
- [ ] 6.7 Implement Design tool list/selection, multi-PNG import flow, empty and missing states, in-app preview from disposed managed data, Export-copy destination picker, confirmed Remove, busy state, and recovery UI.
- [ ] 6.8 Implement the basic Listing tool as compact local lifecycle guidance/status summary without a second status selector, marketplace fields, or mockups.
- [ ] 6.9 Put the only Item-level status selector in Overview/header, source options and confirmation text from application policy, and revert to authoritative state after failure.
- [ ] 6.10 Implement explicit Save for working title, current-stage text, and Notes plus Save/Discard/Cancel guards for Item/tab/active-view/lifecycle transitions; preserve immediate independent Tags.
- [ ] 6.11 Bind tool field editability to both current-versus-active stage and authoritative lifecycle policy; keep Application enforcement even when UI controls are disabled.
- [ ] 6.12 Ensure the outer Item surface reaches every section at minimum supported height and uses compact command sizing, accessible names/tooltips, logical tab order, and predictable focus after stage/status/file/save transitions.

## 7. Cross-context synchronization and App tests

- [ ] 7.1 Refactor main-window coordination so each successful Item mutation refreshes all tabs for that Item, navigator current/active state, Stage Tool Host context, tree label/treatment, filters, shared metadata, Design list, and draft baseline from one authoritative result.
- [ ] 7.2 Preserve canonical tree selection, reusable-tab behavior, explicit additional tabs, and sensible replacement selection when filters/archive remove the active row; prevent feedback loops and stale state from reverting mutations.
- [ ] 7.3 Preserve unsaved drafts during independent Tag refreshes and stale-stage failures; provide actionable reload/retry state instead of silently overwriting input.
- [ ] 7.4 Add/rename App tests for four-stage visibility, current-versus-review read-only matrix, fallback labels, status options/confirmations, Published/Rejected/archive commands, save guards, Tags independence, Design preview/export/remove state, busy duplicate prevention, synchronization, filters, focus, and selection.
- [ ] 7.5 Run `dotnet test .\tests\FusionCanvas.App.Tests\FusionCanvas.App.Tests.csproj` and resolve failures.

## 8. Documentation, deterministic baseline, and criterion evidence

- [ ] 8.1 Update canonical architecture, data-model, design-pipeline, UI/UX, product, and roadmap wording only where this implemented module makes current Listing terminology or behavior stale; do not import historical LifeOS scope.
- [ ] 8.2 Rename affected test files/types and confirm no stale universal `Listing` symbols remain outside approved final-stage, future-marketplace, migration, archived-change, or historical contexts; document intentional exceptions.
- [ ] 8.3 Populate `verification.md` for every delta-spec scenario with exact automated test names/commands, desktop evidence plan or result, and limitations; never replace criterion evidence with an aggregate pass.
- [ ] 8.4 Run `dotnet build .\FusionCanvas.sln` and correct all errors and warnings introduced by the change.
- [ ] 8.5 Run `dotnet test .\FusionCanvas.sln`; if the known Avalonia generated-output lock occurs, stop only processes created by that failed run and rerun serialized with `-m:1`.
- [ ] 8.6 Run `openspec validate basic-product-creation-workflow --strict` and correct artifacts or implementation when behavior drifts.
- [ ] 8.7 Run the repository's configured whitespace/format verification where applicable and `git diff --check`.

## 9. Targeted real-desktop verification

- [ ] 9.1 Build a Debug desktop application and launch it with a disposable SQLite database and disposable managed workspace root; record build, environment, isolation paths, and evidence locations in `verification.md`.
- [ ] 9.2 Create an ID-only Item; verify stable fallback labels in tree/tab/Overview, optional working title Save, Idea/Concept explicit saves, Audience/legacy-data preservation as observable, and ungated progression.
- [ ] 9.3 Review earlier stages read-only, regress to edit Concept, verify downstream Design data remains, and verify keyboard/pointer navigator behavior and focus.
- [ ] 9.4 Import two PNGs including a duplicate, reject a non-PNG, preview in-app, Export copy and compare output, exercise missing state, cancel Remove, confirm Remove, and verify resulting selection.
- [ ] 9.5 Publish at Listing, verify one confirmation and protected content, edit approved metadata, confirm Published-to-Paused modification path, regress/edit/return, and exercise Rejected-to-Draft recovery.
- [ ] 9.6 Archive and restore an Item, exercise a representative active status filter and multiple tabs for the same Item, and verify authoritative synchronization without stale overwrite.
- [ ] 9.7 Restart the disposable workspace and verify Item ID, title/fallback, stage, status, archive state, Idea, Concept, Notes, Tags, Design files, related assets, and topic placement.
- [ ] 9.8 Reduce the window to minimum supported height and verify scroll reachability, tab order, tooltips/accessibility names, confirmation cancellation, Save/Discard/Cancel focus, and blocked/error explanations.
- [ ] 9.9 Record why these desktop scenarios cover the module's distinct high-risk wiring while exhaustive transition combinations and migration failures remain deterministic; if interactive desktop becomes unavailable, mark the lane not applicable and preserve the exact handoff rather than claiming a pass.

## 10. Scoped completion QA and handoff

- [ ] 10.1 Review every acceptance scenario against `verification.md`; for each failure, correct implementation or approved artifacts and rerun that criterion plus relevant regression checks.
- [ ] 10.2 Perform changed-scope architecture, security/path traversal, migration/data-loss, persistence compensation, UI state/focus, and specification-drift review according to `docs/qa-review.md`.
- [ ] 10.3 Confirm proposal scope, specs, design Implementation Plan, tasks, code, tests, documentation, and verification evidence agree and contain no unresolved implementation decision.
- [ ] 10.4 Record the learning review/retrospective, including any reusable lesson promoted to canonical guidance and any deliberately deferred future opportunity.
- [ ] 10.5 Prepare the implementation handoff with change name `basic-product-creation-workflow`, exact task range, validation commands, prohibited scope expansion, and instruction to escalate any newly discovered product/architecture ambiguity rather than guessing.

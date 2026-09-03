## 1. Domain model and readiness policy

- [x] 1.1 Update `MockupTemplate` and `MockupTemplateRevision` to permit nullable target Design Area and independently absent image/mapping configuration while retaining required template identity, nonblank name, positive revision number, and validation for every supplied value.
- [x] 1.2 Add `MockupTemplateLifecycle`, stable readiness blocker codes, and an immutable readiness result under `FusionCanvas.Domain.Mockups`.
- [x] 1.3 Implement one `MockupTemplatePolicy` readiness evaluator that returns every ordered blocker for target ownership/activity, Colors, implied Variants, Design Area compatibility, image/dimensions, mapping, known image/Color compatibility, and archive state.
- [x] 1.4 Extend output-affecting change comparison and catalog dependency policies for nullable target/image/mapping and empty Color sets without weakening safeguards for non-null relationships.
- [x] 1.5 Add focused Domain tests for minimum identity, nullable Draft construction, valid supplied mappings, each readiness blocker, multi-blocker accumulation, complete Ready state, catalog-driven regression to Draft, archive exclusion, and output/non-output revision decisions.

## 2. Provider-independent application save and eligibility

- [x] 2.1 Update Mockup Template create/update requests, summaries, state, and results to carry nullable configuration, returned stable template identity, lifecycle, and readiness blockers.
- [x] 2.2 Refactor `OfferingManagementService` to share one provider-independent validation/revision builder across partial create and update, validating only submitted values and current same-Offering relationships.
- [x] 2.3 Make each create/update operation load once, persist through exactly one repository save, return authoritative Offering state, and never re-fetch or require `IProviderCatalogCandidateSource` for authorization.
- [x] 2.4 Update `MockupTemplateSetupService`, snapshot handling, archive/restore behavior, and revision-color replacement for null targets, empty Colors, independently optional image/mapping, and immutable prior revisions.
- [x] 2.5 Add the application readiness/eligible-template query that returns only active Ready templates and rejects specifically requested Drafts with their complete blocker set.
- [x] 2.6 Add Application tests for name-only no-provider creation, representative partial subsets, invalid supplied values with zero writes, exactly-once persistence, stable-ID return, Draft→Ready and Ready→Draft revisions, metadata-only edits, failure preservation, optional provider prefilling, known incompatibility, and render eligibility filtering.

## 3. SQLite schema 13 and workspace compatibility

- [x] 3.1 Raise the current SQLite schema version from 12 to 13 and update new-database DDL so template and revision target Design Area foreign keys are nullable while all non-null foreign keys remain enforced.
- [x] 3.2 Implement the transactional 12→13 rebuild/copy/replace migration for `mockup_templates` and `mockup_template_revisions`, including row/value validation, recreated constraints/indexes, `foreign_key_check`, version-last advancement, and actionable rollback failure.
- [x] 3.3 Update SQLite insert/load mapping and snapshot validation for nullable target IDs, independently optional revision image/mapping, empty Color sets, and unchanged rejection of invalid supplied relationships.
- [x] 3.4 Add isolated persistence tests for fresh schema 13, name-only and representative partial Draft round-trips, complete Ready round-trip, explicit clearing, schema-12 complete-data migration equality, no duplicate revisions/bindings, newer-version refusal, and malformed-migration rollback.
- [x] 3.5 Extend workspace package integration tests to export/import mixed Draft and Ready templates with stable IDs, nullable configuration, revision history, and no provider connectivity.

## 4. ViewModel and presentation behavior

- [x] 4.1 Change `StartAddTemplateCommand` to require only an editable current Offering and initialize a name-focused Draft without fabricating Design Area, Color, image, or mapping values.
- [x] 4.2 Replace non-null default mapping fields with a focused nullable/string-backed mapping draft parser that distinguishes all-empty omission from partial, non-numeric, non-integral, non-positive, and out-of-bounds supplied input.
- [x] 4.3 Expose Draft/Ready lifecycle, ordered readiness guidance, blocking save-validation guidance, and in-dialog persistence error state from `CatalogSetupViewModel`, re-evaluating after Name, provider image, Design Area, every Color choice, every mapping field, busy, read-only, and context changes.
- [x] 4.4 Route Add and Edit through the unified application create/update path; on success apply authoritative state, select by returned stable ID, close once, and preserve existing focus return; on failure preserve every editor field and the open modal.
- [x] 4.5 Update `MockupTemplateSetupSummary`/`MockupTemplateCardViewModel` and collection rendering to distinguish Draft, Ready for use, and Archived without treating persistence as readiness.
- [x] 4.6 Add ViewModel/presentation tests for every Save transition, all blocker aggregation/resolution, no-provider success, optional candidate initialization, partial/complete edit population, invalid mapping text, busy/read-only/stale context, exactly-once save/selection, and failure preservation.

## 5. Focused Mockup Template dialog

- [x] 5.1 Update `MockupTemplateEditorWindow.axaml` to display lifecycle, the complete readiness checklist, separate blocking validation, and `CatalogSetup.ErrorMessage` within the modal using accessible names and automation IDs.
- [x] 5.2 Relabel provider selection as optional and update loading, available, empty, unavailable, and failure guidance so none presents provider setup/synchronization as a Draft-save prerequisite.
- [x] 5.3 Keep the no-image preview compact with no active placement rectangle; show/synchronize placement only with usable image dimensions and valid mapping while retaining accessible numeric editing.
- [x] 5.4 Preserve existing modal ownership, scrollable normal/narrow layout, initial Name focus, deterministic keyboard traversal, meaningful-draft discard/keep-editing flow, stale-context closure, archived read-only behavior, and Add/Edit focus restoration.
- [x] 5.5 Add Avalonia headless tests covering Add with no Design Areas/provider source, optional-provider states, Draft Save enabled and visible, every readiness blocker, invalid supplied mapping disabled with guidance, Ready transition, in-dialog failure, successful exactly-once Save/close/selection/focus, edit population, cancellation, keyboard access, and supported narrow sizing.

## 6. Criterion-level verification and completion gates

- [x] 6.1 Create `verification.md` and map every scenario in all three delta specs to its focused automated result/evidence; mark no scenario complete from aggregate suite status alone.
- [x] 6.2 Run the focused Domain, Application, Integration, ViewModel, and Avalonia headless tests; correct implementation or approved artifacts for every failed criterion and rerun the affected criterion plus relevant regressions.
- [x] 6.3 Review changed scope for architecture direction, nullable/reference safety, transactionality, provider/network independence, stale-context protection, accessible modal behavior, and absence of Draft templates from render eligibility.
- [x] 6.4 Run strict OpenSpec validation with `openspec validate support-draft-mockup-templates --strict` and correct every artifact/spec error.
- [x] 6.5 Run the deterministic solution baseline `dotnet test .\FusionCanvas.sln` and record command, result, limitations, and final per-scenario evidence in `verification.md`.

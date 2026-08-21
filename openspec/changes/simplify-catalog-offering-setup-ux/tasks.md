## 1. Baseline reconciliation and traceability

- [x] 1.1 Reconcile this change against the final accepted `support-printify-store-catalog-mockup-setup` model and specs, correcting only conflicts while preserving the authority hierarchy and five-capability scope.
- [x] 1.2 Create `verification.md` with one row for every exact acceptance scenario in the five delta specs and its planned automated evidence or explicit optional-live rationale.
- [x] 1.3 Identify the final existing catalog/mockup types, schema version, Store Editor navigation states, and draft-guard utilities to extend; record any implementation-discovered high-impact ambiguity before changing production behavior.

## 2. Domain and persistence extensions

- [x] 2.1 Add nullable stable provider Design Area reference support to the authoritative Offering Placeholder model without changing Store/Offering ownership or compatibility invariants.
- [x] 2.2 Add provider-specific recommended artwork guidance representation while keeping maximum pixel dimensions authoritative and physical dimensions derived only from reliable metadata.
- [x] 2.3 Add a revision-owned mockup image-space mapping value object/configuration with provider image identity, source image dimensions, X/Y/width/height validation, equality, and snapshot semantics.
- [x] 2.4 Extend Mockup Template revision policy so source-image, target Design Area, color applicability, or mapping changes create a new revision while color-level applicability and historic attribution remain intact.
- [x] 2.5 Add focused Domain tests for provider references, pixel/physical guidance behavior, mapping bounds, revision changes, compatibility, and absence of per-concrete-Variant overrides.
- [x] 2.6 Add an ordered SQLite migration and repository read/write support for the additive Design Area and template-revision fields or records, preserving existing IDs and explicit unconfigured mapping state.
- [x] 2.7 Add Integration tests for prior-schema migration, empty/populated round-trip, invalid reference or bounds rejection, Store isolation, workspace-package compatibility, and transactional rollback.

## 3. Offering summaries and focused application contracts

- [x] 3.1 Add an Offering summary projection that reports actual fixed Print Provider or Provider-Network context, lifecycle/readiness state, and Variant/Design Area/Mockup Template setup counts without calling Printify the Provider.
- [x] 3.2 Add focused Offering, Variant, Design Area, and Mockup Template query/command contracts that carry stable Store/Blueprint/Offering identities and do not resolve context from labels or first-record fallback.
- [x] 3.3 Add a read-only provider-catalog candidate descriptor boundary with a deterministic unavailable implementation and no external SDK, credentials, network access, or upload behavior.
- [x] 3.4 Implement and test validity-aware color-plus-enabled-sizes preview and atomic bulk Variant creation, including duplicate elimination, partial invalid-size reporting, no-op results, cancellation, and cross-Offering rejection.
- [x] 3.5 Implement and test all-current-compatible-Variants Design Area expansion, explicit subset validation, pixel-first/secondary measurement projection, artwork guidance, and provider-reference persistence.
- [x] 3.6 Implement and test provider mockup descriptor selection, same-Offering Design Area compatibility, color-derived concrete Variant summaries, mapping validation, and revision creation.

## 4. Blueprint Offering list and Offering overview

- [x] 4.1 Refactor Store catalog navigation so Blueprint detail owns one Blueprint-scoped Offering list and stable add/open routes, with no duplicate Offering selector or relationship editors.
- [x] 4.2 Implement populated, empty, loading, archived/read-only, validation, and persistence-error states for the Offering list, including concise setup summaries and correct Provider terminology.
- [x] 4.3 Implement explicit new-Offering draft save/cancel behavior, initial required-field focus, unsaved-transition guarding, and post-save selection.
- [x] 4.4 Replace the dense Offering detail composition with a concise Offering overview containing Basics, fulfillment context, lifecycle/readiness status, setup summaries, and focused management routes.
- [x] 4.5 Implement prerequisite guidance, Provider-Network warning behavior, Advanced technical disclosure, and return-to-overview context/focus restoration.
- [x] 4.6 Add framework-free ViewModel and Avalonia headless tests for list/overview ownership, context identity, keyboard opening, focus, empty/read-only states, draft guards, wording, focused routing, and absence of the giant all-relationships form.

## 5. Focused Variant management

- [x] 5.1 Implement an Offering-scoped Variant surface that presents provider-catalog Options/Values separately from explicit sellable Variants and keeps Provider context visible.
- [x] 5.2 Implement individual valid Variant creation and enabled-choice editing without treating all mathematical combinations as sellable.
- [x] 5.3 Implement the bulk color-plus-valid-sizes preview/confirmation interaction with deterministic result and exclusion reporting.
- [x] 5.4 Implement unavailable-catalog, empty, duplicate, validation, archive/dependency, cancel, unsaved-change, selection-aftermath, keyboard, and focus states.
- [x] 5.5 Add focused ViewModel and Avalonia headless tests for choice/Variant separation, individual and bulk interaction, invalid/no-op outcomes, draft guards, lifecycle behavior, and no global Store-setup leakage.

## 6. Focused Design Area management

- [x] 6.1 Implement an Offering-scoped Design Area list and selected editor using existing Offering Placeholder identities and no parallel Design Area entity.
- [x] 6.2 Implement all-Variants default compatibility, explicit subset management, same-Offering validation, and clear compatibility summaries.
- [x] 6.3 Implement pixel-first maximum dimensions, reliable secondary inches/millimetres, unavailable conversion state, recommended artwork guidance, and Advanced provider-reference disclosure.
- [x] 6.4 Implement create/edit drafts, validation and persistence errors, selection/navigation guards, archive/dependency confirmation, read-only behavior, selection aftermath, keyboard flow, and focus restoration.
- [x] 6.5 Add focused ViewModel and Avalonia headless tests for list/editor ownership, measurements/guidance hierarchy, all/subset compatibility, Advanced data, invalid dimensions, draft guards, and template dependency blocking.

## 7. Focused Mockup Template management

- [x] 7.1 Implement an Offering-scoped template list and editor with provider mockup descriptor, one authoritative Design Area target, color-level applicability, derived compatible Variants, lifecycle state, and Advanced provider reference.
- [x] 7.2 Implement blocked/no-Design-Area, no-provider-descriptor, incompatible-target, empty, loading, error, and archived/read-only states without fabricating source images or relationships.
- [x] 7.3 Implement one accessible visual placement editor bound bidirectionally to numeric X/Y/width/height draft values in source-image pixel space.
- [x] 7.4 Implement mapping bounds guidance, explicit save/cancel, revision creation, unsaved selection/navigation guards, destructive safeguards, selection aftermath, keyboard operation, and focus restoration.
- [x] 7.5 Add focused ViewModel and Avalonia headless tests for visual drag/resize and numeric synchronization, mapping validation, target/color compatibility, revision saves, Provider reference stability, blocked states, drafts, and read-only behavior.
- [x] 7.6 Confirm through schema inspection and tests that no renderer, composition execution, upload flow, per-size override, listing artwork selection, Shopify publication, credential, or external-network behavior was introduced.

## 8. Completion verification and learning

- [x] 8.1 Run focused Domain, Application, Integration, ViewModel, and Avalonia headless tests after each affected layer and correct changed-scope failures.
- [x] 8.2 Run `dotnet build .\FusionCanvas.sln` and resolve all changed-scope errors and warnings.
- [x] 8.3 Run the deterministic baseline `dotnet test .\FusionCanvas.sln` without external services, credentials, network access, or an interactive desktop.
- [x] 8.4 Run the repository-supported strict OpenSpec validation command and correct every proposal, design, delta-spec, task, and traceability error.
- [x] 8.5 Perform scoped completion QA from `docs/qa-review.md`, including criterion evidence, spec drift, architecture, security, Store isolation, migration/rollback, persistence, UI states, accessibility, and excluded-scope checks.
- [x] 8.6 Review whether an optional disposable live desktop pass would add information unavailable from deterministic tests; it was not run because headless coverage exercises visual placement drag/resize, keyboard input, numeric synchronization, navigation, and density-bearing view construction.
- [x] 8.7 Update `verification.md` with final criterion-level evidence and create a retrospective capturing implementation-discovered lessons or ambiguities before archive.

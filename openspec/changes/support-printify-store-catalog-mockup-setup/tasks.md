## 1. Domain strategy and catalog model

- [ ] 1.1 Add the Store `FulfillmentStrategy` values, Manual compatibility default, availability policy, and focused Domain tests for valid values and Store identity preservation.
- [ ] 1.2 Introduce `Blueprint`, `PrintProvider`, `BlueprintOffering`, and offering-kind/provider-network identity types in a cohesive catalog namespace, with invariants for fixed-provider versus Provider-Network ownership and stable `printify-choice` identity.
- [ ] 1.3 Introduce `OfferingOption`, required `OptionKind`, `OfferingOptionValue`, explicit `OfferingVariant`, and Variant/value memberships, including duplicate-combination and cross-offering rejection policies.
- [ ] 1.4 Replace `DesignArea` responsibilities with `OfferingPlaceholder` and explicit concrete-Variant compatibility, including positive-dimension, Store/offering ownership, optional default-Placeholder, and dependency policies.
- [ ] 1.5 Introduce `MockupTemplate`, required authoritative `TargetPlaceholderId`, `MockupTemplateColorVariant`, immutable template revisions, and revision-color snapshots with nullable future source-asset state and no placement or override schema.
- [ ] 1.6 Add Domain lifecycle policies for archive/deactivation, active template/color uniqueness, referenced Placeholder and Color-value protection, template revision creation, and future selected-Variant compatibility validation.
- [ ] 1.7 Update `WorkspaceSnapshot`, workspace filtering/transfer, Item listing configuration, selected target, design row, and design slot domain records to use Blueprint Offering and Placeholder relationships while preserving stable IDs.
- [ ] 1.8 Add focused Domain tests covering every new ownership, OptionKind, duplicate, compatibility, color-binding, revision, archive, and deletion invariant, including proof that size changes require no template mapping repair.

## 2. Application setup use cases

- [ ] 2.1 Define focused application contracts, requests, results, summaries, and Store-scoped state for fulfillment strategy and Blueprint catalog loading without using labels as relationship keys.
- [ ] 2.2 Implement Blueprint, Print Provider/Provider Network, Blueprint Offering, Option/Value, concrete Variant, and Placeholder create/update/archive/restore/delete orchestration with atomic validation and authoritative refresh.
- [ ] 2.3 Implement Mockup Template and template-color draft/save/archive/restore flows, same-offering target and Color validation, active uniqueness, and revision creation for output-affecting changes.
- [ ] 2.4 Implement actionable dependency reporting for referenced Placeholders, Color Option Values, offerings, and templates; keep archived Store configuration read-only.
- [ ] 2.5 Update Design-stage target/query services to resolve selected Offering Placeholders, fixed Print Providers, and Provider-Network warnings from the normalized catalog.
- [ ] 2.6 Add application tests for Store isolation, strategy availability, Manual no-network behavior, all CRUD/lifecycle outcomes, dependency errors, revision behavior, target resolution, and cancellation-safe state refresh.

## 3. SQLite schema and migration

- [ ] 3.1 Add schema-version-11 normalized tables, foreign keys, indexes, active uniqueness enforcement, repository mappings, snapshot validation, and safe insert/delete ordering for strategy, catalog, Placeholders, templates, colors, and revisions.
- [ ] 3.2 Implement transactional Store, Blueprint, fixed-provider, and Printify Choice migration from schema 10 with Store IDs, Blueprint IDs, offering IDs, fields, and relationships preserved.
- [ ] 3.3 Implement one-time normalization of legacy inline Variant Options into typed Options/Values and explicit Variant memberships, mapping case-insensitive Color/Size names and classifying all other names as Other.
- [ ] 3.4 Migrate design areas to same-ID Offering Placeholders, convert restricted Variant IDs, expand unrestricted areas to current offering Variants, and preserve Item target and design-slot references.
- [ ] 3.5 Validate migrated counts, ownership, offering-kind fields, Option kinds, Variant memberships, Placeholder compatibility, and every Item/design relationship before advancing the version; roll back completely on any failure.
- [ ] 3.6 Add isolated schema-10 migration fixtures for fixed providers, Choice, mixed Options, restricted/unrestricted areas, archived Stores, Item targets, design slots, empty template storage, and malformed-reference rollback.
- [ ] 3.7 Add schema-11 new-database, round-trip, invalid-snapshot, package-database compatibility, and newer-schema-refusal tests, including assertions that no coordinate, override, generated-mockup, Shopify-mapping, credential, or binary asset structures exist.

## 4. Store Editor strategy and catalog UX

- [ ] 4.1 Rename and restructure the focused tab as Catalog & mockups, place fulfillment strategy at the top level, and show Manual enabled with both future Shopify strategies visibly disabled and explained.
- [ ] 4.2 Replace visible Product/design-area terminology with Blueprint, Print Provider, Provider Network, Blueprint Offering, Option, Variant, and Placeholder, adding visible first-use helper text and accessible tooltips for non-intuitive terms.
- [ ] 4.3 Extend progressive navigation through Blueprint overview, Blueprint detail, offering detail, and ordered Basics, Options/Values, Variants, Placeholders, Mockup Templates, and Advanced sections with one clear creation owner per level.
- [ ] 4.4 Add focused forms and collection states for typed Options/Values, explicit Variants, Placeholder compatibility, Provider-Network identity, and optional offering default Placeholder.
- [ ] 4.5 Add Mockup Template detail and color configuration using only authoritative target Placeholder and Color Option Value identities; expose a clear unconfigured future source-image state and no upload, placement, renderer, or override controls.
- [ ] 4.6 Preserve draft guards, cancellation, selection, focus, busy state, validation/persistence errors, blocked dependency guidance, destructive aftermath, and archived Store read-only behavior across every new navigation level.
- [ ] 4.7 Split or extract catalog/template presentation responsibilities from `StoreManagementViewModel` where required to keep focused ownership without duplicating application orchestration.
- [ ] 4.8 Add ViewModel tests for navigation, strategy availability, draft/guard behavior, dependent selections, authoritative refresh, blocked actions, read-only state, and post-mutation selection.
- [ ] 4.9 Add Avalonia headless tests for compiled bindings, helper text/tooltips, keyboard reachability, focus, disabled strategy choices, empty/populated/error/blocked states, progressive visibility, and absence of upload/placement/override UI.

## 5. Design-stage target continuity

- [ ] 5.1 Update Design Stage Tool ViewModel and view bindings to display selected Offering Placeholders with Blueprint, offering, position, decoration method, dimensions, and Provider-Network warning terminology.
- [ ] 5.2 Preserve editable/read-only target selection, multiple-selection atomicity, Store isolation, design-file behavior, and stable migrated target IDs.
- [ ] 5.3 Update focused application, ViewModel, and Avalonia headless regression tests for normal, Choice-network, cross-Store, and protected-context Placeholder targets.

## 6. Current documentation alignment

- [ ] 6.1 Update `docs/data-model.md` and directly affected current product/architecture guidance with fulfillment strategy, Printify terminology, normalized catalog relationships, color-level templates, revisions, and lifecycle rules.
- [ ] 6.2 Update `docs/ui-guidelines.md` to remove the superseded numeric-placement recommendation and state that a later visual click-and-point editor will define placement semantics.
- [ ] 6.3 Document the future Shopify adapter boundary and explicit cross-system option/Variant mapping without adding Shopify records or detailed future-module behavior.
- [ ] 6.4 Review current docs and accepted-spec terminology for remaining user-visible Product, printable-area, or design-area references in this capability and correct only the affected scope.

## 7. Criterion-level verification and completion gates

- [ ] 7.1 Run focused Domain, Application, Integration, ViewModel, and Avalonia headless test projects after each affected layer and correct failures without expanding scope.
- [ ] 7.2 Create and maintain `verification.md` mapping every acceptance scenario to its automated result or explicit future-contract not-applicable rationale, including migration IDs/counts and absence-of-scope evidence.
- [ ] 7.3 Run `dotnet build .\FusionCanvas.sln` and resolve all changed-scope errors and warnings.
- [ ] 7.4 Run the deterministic baseline `dotnet test .\FusionCanvas.sln` without external services, credentials, network access, or an interactive desktop.
- [ ] 7.5 Run strict OpenSpec validation using the installed CLI's supported invocation and correct every proposal, design, delta-spec, task, or traceability error.
- [ ] 7.6 Perform scoped completion QA per `docs/qa-review.md`: acceptance evidence, changed-scope drift review, architecture, security, migration/rollback, Store isolation, UI state, and excluded-scope checks.
- [ ] 7.7 Confirm no unresolved high-impact decision was delegated to implementation and record any implementation-discovered ambiguity for review before proceeding.

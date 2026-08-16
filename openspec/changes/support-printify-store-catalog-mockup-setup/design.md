## Context

FusionCanvas currently persists `StoreProduct`, `FulfillmentOffering`, `ProductVariant`, inline `VariantOption` values, and `DesignArea` records. The Store Editor exposes those records through a progressive Products & fulfillment surface, and Design-stage Item configuration refers to offering and area identities. The model is directionally close to Printify but lacks a Store-level integration strategy, separate Print Providers, stable Provider Network identity, explicit Option semantics and values, accurate Placeholder terminology, and Mockup Template configuration.

This module establishes the local, manual configuration foundation for GitHub issue #185. The creator performs this work occasionally in the focused Store Editor. No external account, API, credential, listing selection, image upload, renderer, or publishing workflow participates. Existing schema-version-10 databases and embedded workspace-package databases must migrate through the same ordered SQLite chain without losing Store, catalog, Item-target, or design-slot relationships.

The approved product language follows Printify: Blueprint, Print Provider, Variant, Option, and Placeholder. Printify Choice is a Provider Network rather than an ordinary Print Provider. Helper text explains non-intuitive terms without replacing them. Current UI guidance suggesting numeric placement fields conflicts with the settled scope and must be corrected: future placement will use a separate visual click-and-point editor, and this module defines no coordinate, slot, transform, or composition contract.

## Goals / Non-Goals

**Goals:**

- Persist one Store-level fulfillment strategy, migrate existing Stores to Manual, and visibly reserve the two future Shopify strategies without enabling them.
- Normalize the catalog into Blueprints, Print Providers or Provider Networks, Blueprint Offerings, typed Options and Values, explicit concrete Variants, and concrete Placeholders with Variant compatibility.
- Keep Printify Choice identifiable through an immutable stable network code while allowing its display label to change.
- Add Placeholder-targeted Mockup Templates with color-level configuration bound to stable Color Option Values.
- Make one active template-color record apply to every compatible concrete Variant sharing that Color value, independent of size changes.
- Preserve template revision attribution and safe archive/dependency behavior for future generated-output provenance.
- Provide a focused, progressively disclosed, keyboard-accessible Store Editor workflow with complete empty, draft, success, validation, blocked, read-only, and persistence-error states.
- Migrate schema version 10 to the normalized model transactionally with concrete mappings and relationship validation.
- Leave the package implementation-ready with deterministic criterion-level verification.

**Non-Goals:**

- Listing-stage color or size selection, listing Mockup Template selection, mockup generation, generated-mockup records, readiness checks, or galleries.
- Template source-image upload/import, Asset creation, image editing, rendering, composition, coordinates, slots, transforms, scale, rotation, or placement UI.
- Per-size, per-concrete-Variant, per-option-dimension, or generalized mockup overrides; no extension structure is reserved for them.
- Shopify or Printify credentials, authentication, API calls, synchronization, product creation, publication, order routing, or remote validation.
- Shopify option/Variant mapping. A future adapter must map FusionCanvas colors and concrete Variants explicitly and must not assume labels or identifiers match.
- Resolving undocumented Printify Choice API identifiers. The local stable network code keeps that adapter concern outside the domain.

## Decisions

### Fulfillment strategy is a structured Store property

Add a closed `FulfillmentStrategy` value (`Manual`, `ShopifyManual`, `ShopifyPrintify`) to the Store domain and SQLite Store row. It is stable, queryable behavior that gates integrations, not flexible metadata. The Store Editor shows all values but enables only Manual. Application policy rejects unavailable selections; Manual performs no credential lookup or network call.

Changing strategy retains Store identity and strategy-neutral catalog data. Later modules may add warned transitions and strategy-specific configuration. This module does not delete dormant data or invent downgrade cleanup.

Alternative rejected: Store metadata JSON. That would make a core capability gate weakly typed and easy for UI, services, and adapters to interpret inconsistently.

### Catalog records use Printify terminology and stable relationships

Use cohesive catalog domain types, expected under `FusionCanvas.Domain/Catalog`:

```text
Store (FulfillmentStrategy)
  Blueprint
    BlueprintOffering
      Kind = FixedPrintProvider | ProviderNetwork
      PrintProviderId?       required only for FixedPrintProvider
      ProviderNetworkCode?   required only for ProviderNetwork
      DefaultPlaceholderId?  optional new-template convenience
      OfferingOption
        OfferingOptionValue
      OfferingVariant
        OfferingVariantOptionValue
      OfferingPlaceholder
        OfferingPlaceholderVariant
      MockupTemplate
```

`BlueprintOffering` is the local relationship record joining a Blueprint to one fulfillment choice. The UI hierarchy can describe this relationship while using the exact underlying nouns. `Product` is reserved for a future artwork-added sellable product. Existing external IDs remain optional local values under Manual and do not imply synchronization.

Alternative rejected: preserve `StoreProduct`, `ProductVariant`, and `DesignArea` aliases. Aliases would keep the same conceptual ambiguity and contradict the requirement for consistent terminology.

### Provider Networks are not Print Providers

A Provider-Network offering stores a stable, normalized `ProviderNetworkCode` and a mutable display label; Printify Choice uses `printify-choice`. Fixed offerings reference a Store-scoped `PrintProvider`. Domain invariants require exactly the fields appropriate to the offering kind.

Alternative rejected: create a virtual Print Provider named Printify Choice. Printify Choice routes through an undisclosed network and is not one ordinary provider; label-based identity would also break relationships when wording changes.

### Option semantics and concrete Variants are explicit

`OfferingOption` requires `OptionKind` with `Color`, `Size`, and `Other`. Editable names are display values only. `OfferingOptionValue` owns stable values such as Black or Large. `OfferingVariant` remains an explicit concrete row whose memberships link to Option Values; the model never assumes a complete Cartesian product. Domain policy rejects values from another offering and duplicate active value combinations.

Alternative rejected: infer Color and Size from current option names. Renaming an Option would silently change behavior and future adapters could not trust it.

### Placeholders own explicit concrete-Variant compatibility

Rename and normalize `DesignArea` to `OfferingPlaceholder`. A Placeholder stores position, decoration method, positive dimensions, and explicit compatible Variant relationships. An optional `DefaultPlaceholderId` on the offering may prefill a template draft but never retargets an existing template.

Item selected targets and design slot assignments retain their current identifiers because existing design-area IDs become Placeholder IDs during migration. Future template application validates that every selected listing Variant is in the target Placeholder's compatibility set; incompatibility is reported or rejected, never silently rendered.

Alternative rejected: bind by a free-text position key. Labels and provider-facing position text may change and cannot provide durable identity or distinguish multiple decoration methods at a similar position.

### Mockup Templates bind authoritatively to Placeholder and Color identities

Use a stable `MockupTemplate` with required `BlueprintOfferingId` and `TargetPlaceholderId`. The target must belong to the same offering. `PositionKey`, if retained for display/cache use, is optional and non-authoritative.

`MockupTemplateColorVariant` binds exactly one `ColorOptionValueId`. The value must be owned by a Color Option under the template's offering. A filtered uniqueness rule plus domain validation allows at most one active row for `(MockupTemplateId, ColorOptionValueId)`. Compatible concrete Variants are derived through their Option Value memberships, so adding or removing sizes requires no mapping repair.

No join from template colors to concrete Variants exists. No size-specific or generalized override table, JSON field, polymorphic target, or extension interface is reserved. If evidence later establishes that another dimension needs different imagery, it receives a separate OpenSpec change.

### Template revisions preserve future output attribution

`MockupTemplate` is the stable current configuration and has a monotonic current revision number. Output-affecting changes create immutable `MockupTemplateRevision` snapshots. A revision snapshots at least the target Placeholder and the active color/source state through immutable revision-color rows. Current `MockupTemplateColorVariant` records hold active configuration; each has a nullable future `SourceAssetId`, which remains null because this module has no import flow. The UI presents that absence as “Source image setup is not available yet.”

Name, description, and archive presentation changes need not create an output revision. Target changes, color-set changes, and any later source-asset change do. A future generated mockup must persist the exact `MockupTemplateRevisionId` and `ColorOptionValueId` used. No generated-mockup entity is introduced here; the requirement fixes the future provenance boundary.

Alternative rejected: mutate one template row with no revision history. Historical output could no longer identify the configuration that produced it.

### Archival is the normal lifecycle for referenced catalog data

Blueprints, Print Providers, offerings, Options/Values, Variants, Placeholders, templates, and template colors expose active/archive state where their identities can become dependencies. Active selectors omit archived records; archived Store review remains read-only. Permanent deletion is allowed only when no Item, design, template, revision, or active child depends on the record. Application services return actionable dependency guidance and perform related changes atomically.

Specifically:

- A targeted Placeholder cannot be deleted until templates and Item targets are reassigned or removed.
- A Color Option Value cannot be retired/deleted while active template-color records depend on it without explicit handling.
- An archived template color cannot serve future generation but remains attributable.
- Prior immutable revisions are never rewritten to follow renamed or reassigned current records.

Alternative rejected: cascading deletion through catalog dependencies. It would be hard to diagnose and violate local-first ownership and historical attribution.

### The Store Editor extends the existing focused progressive flow

Rename the tab to Catalog & mockups. Keep strategy configuration at the top level, then use focused navigation:

```text
Catalog & mockups
  -> Blueprint overview
  -> Blueprint detail / Blueprint Offerings
  -> Blueprint Offering detail
       Basics
       Options & values
       Variants
       Placeholders
       Mockup Templates
       Advanced
  -> Mockup Template detail / Colors
```

Blueprint and Placeholder helper text is visible in first-use/empty contexts; repeated compact controls may use accessible tooltips. Each level has one clear creation owner. Starting a draft focuses its first required control. Navigation, selection changes, Store changes, tab changes, and closing use the existing guarded-draft pattern. Successful mutations refresh from persisted state and retain the closest valid selection. Blocked dependency actions explain the required reassignment/archive step. Archived Stores render all configuration read-only.

On Blueprint detail, the opened Blueprint is the authoritative context for offering creation. The surface must not expose a second Blueprint selector, normalized-model implementation label, provider-network identity field, or duplicate create-offering action alongside the focused `Add Blueprint Offering` flow.

On Blueprint Offering detail, the opened offering identity is likewise authoritative. The compatibility editor and normalized catalog presentation synchronize by the preserved offering ID. Dependent normalized sections never fall back to the first unrelated offering, and the surface does not expose a second offering selector. If no normalized record with that identity is loaded, normalized Options/Values and Mockup Template controls remain hidden behind one explanatory unavailable state. This correction does not invent an automatic repair or duplicate-record flow; missing normalized data remains a visible integrity condition to address through the catalog model.

The strategy and catalog editor are occasional administration and consume no persistent area in the main creative workspace. Avalonia headless tests cover construction, compiled bindings, visibility, disabled choices, focus, selection, cancellation, read-only state, and blocked confirmation surfaces.

### Placement and source-image behavior are intentionally absent

The data model contains no coordinate, slot, transform, compositor, renderer, or placement configuration, including opaque JSON pretending to be a future contract. The only future asset marker is a nullable source-asset identity in revisionable color source state, and this module provides no command that can populate it. Durable UI/data-model guidance is updated to remove the superseded recommendation for initial numeric placement fields and to describe the later visual click-and-point editor as future work.

### Future Shopify publication is an adapter concern

The domain records one primary template source per template and Color Option Value. A future Shopify publisher will expand that color-level choice across mapped Shopify color-and-size Variants using explicit adapter-owned identity mappings. The current schema contains no Shopify IDs or assumptions that option names, values, or identifiers match across systems.

## Risks / Trade-offs

- [Risk] The module changes a large existing data graph and multiple UI consumers → Mitigation: preserve equivalent IDs, sequence work domain-to-persistence-to-application-to-UI, use migration fixtures, and verify each relationship count and target before commit.
- [Risk] Legacy inline Options have no stable semantics → Mitigation: migration maps case-insensitive `Color` and `Size` names once, maps all others to `Other`, and requires explicit OptionKind for every new edit afterward.
- [Risk] Existing unrestricted design areas implied applicability to all Variants → Mitigation: migration expands them to all concrete Variants present at migration time and records explicit compatibility; later Variants require intentional compatibility updates.
- [Risk] Strict dependency blocking can make cleanup feel cumbersome → Mitigation: make archive/deactivate the normal action, show dependency counts, and explain exact reassignment/removal steps.
- [Risk] Revision tables arrive before rendering → Mitigation: keep the revision boundary minimal and tied only to settled provenance requirements; do not add renderer or placement fields.
- [Risk] Nullable source-asset state could be mistaken for upload support → Mitigation: no import command or picker exists, UI labels the state as future, and tests assert the absence of upload controls and stored paths.
- [Risk] Disabled future strategies may appear unfinished → Mitigation: show concise guidance explaining which future integration enables each option and keep Manual clearly selected.
- [Risk] The accepted UI guideline currently mentions numeric placement → Mitigation: update that guidance in the same change so implementation and review do not follow conflicting direction.
- [Risk] Printify Choice API behavior remains undocumented → Mitigation: isolate the stable local network code from adapter IDs and defer authenticated API investigation to the Printify integration module.

## Migration Plan

Implement an ordered schema 10 → 11 migration in one SQLite transaction:

1. Create new strategy, Print Provider, Blueprint Offering, Option, Option Value, concrete Variant membership, Placeholder compatibility, Mockup Template, template-color, revision, and revision-color structures with foreign keys and indexes. Do not remove old tables yet.
2. Add/backfill every Store's strategy as `Manual` while preserving Store rows.
3. Copy `product_blueprints` to Blueprints with unchanged IDs and values.
4. For each existing fixed-provider offering, create/reuse a Store-scoped Print Provider by case-insensitive normalized provider name, then copy the offering with its existing ID and provider relationship. Copy each Choice offering with its existing ID, Provider-Network kind, and stable code `printify-choice` without a Print Provider.
5. For each offering's inline Variant option pairs, create an Option per normalized name and Value per normalized value. Map names equal to Color or Size ignoring case to the corresponding OptionKind; map the rest to Other. Preserve Variant IDs and timestamps and create explicit membership rows.
6. Copy each design area to an Offering Placeholder with the same ID and field values. Convert explicit Variant IDs to compatibility rows; for an empty unrestricted list, create compatibility rows for every concrete Variant present in that offering at migration time.
7. Preserve `ItemListingConfiguration`, selected target, design row, and design slot relationships by retaining offering and converted Placeholder IDs. Create empty template/revision tables; do not fabricate Mockup Templates or assets.
8. Validate row counts, Store/Blueprint/offering ownership, offering-kind fields, OptionKind presence, Variant memberships, Placeholder ownership/compatibility, and every Item/design foreign reference. Commit and advance the version only after all checks pass.
9. On any failure, roll back the complete transaction and leave schema 10 readable by the prior application. New schema-11 databases are created directly in the normalized shape.

Rollback after a successful user migration is application rollback only if the prior application can refuse the newer schema safely; no destructive down-migration is provided. The workspace-package importer continues to inspect embedded schema version before changing live state and uses this same migration chain for supported older packages.

## Open Questions

None. Per-Variant/size mockup overrides and placement-coordinate semantics are explicitly deferred, not unresolved implementation choices.

## Implementation Plan

### 1. Domain and workspace model

- Add `FulfillmentStrategy` to `FusionCanvas.Domain/Stores/Store.cs` and all Store construction paths with Manual as the compatibility default until migration materializes it.
- Introduce cohesive catalog types under a `FusionCanvas.Domain.Catalog` namespace: `Blueprint`, `PrintProvider`, `BlueprintOffering`, `BlueprintOfferingKind`, `OptionKind`, `OfferingOption`, `OfferingOptionValue`, `OfferingVariant`, Variant/value membership, `OfferingPlaceholder`, and Placeholder/Variant compatibility.
- Introduce `FusionCanvas.Domain.Mockups` types: `MockupTemplate`, `MockupTemplateColorVariant`, `MockupTemplateRevision`, and immutable revision-color snapshot records. Keep each top-level type in its own file.
- Add focused domain policies for ownership, option-kind validation, duplicate Variant combinations, target compatibility, active template-color uniqueness, revision creation, and dependency-safe lifecycle operations. Do not place these rules in ViewModels or SQLite.
- Replace the relevant `WorkspaceSnapshot` collections and update snapshot filtering/transfer logic while preserving Item/design references to offering and Placeholder IDs.
- Update `ItemListingConfiguration`, `DesignStagePolicy`, selected target records, and design-slot records/properties to use Blueprint Offering and Placeholder terminology without changing their accepted Design-stage behavior.

### 2. Application use cases and contracts

- Replace the oversized product/supplier setup responsibilities with focused application orchestration, likely `StoreCatalogSetupService` plus `MockupTemplateSetupService`, and strategy availability/change policy. Retain one authoritative load/refresh state per Store.
- Define separate requests/results/summaries for Blueprints, providers/networks, offerings, Options/Values, concrete Variants, Placeholders, templates, and template colors. Requests carry stable IDs; presentation labels never serve as relationship keys.
- Implement atomic create/update/archive/restore/delete flows with dependency reports and recoverable validation. Ensure archived Store catalogs are read-only.
- Implement revision creation for output-affecting template changes and color-set changes. SourceAssetId stays null because no application command can set it.
- Update Design-stage target/query services to resolve selected Placeholders and Provider-Network warnings through the normalized catalog.
- Ensure Manual strategy paths have no dependency on external credentials, HTTP, provider SDKs, or network services.

### 3. SQLite schema, migration, and repository mapping

- Advance `SqliteDatabaseSchema.CurrentVersion` from 10 to 11 and add the ordered migration described above to `SqliteWorkspaceRepository` or a focused migration helper if extraction improves responsibility without introducing a generic framework.
- Create normalized tables and indexes, including filtered or equivalent active uniqueness for template/color, offering-kind checks, and foreign keys for all Store/offering ownership relationships. Update snapshot delete/insert ordering and validation.
- Preserve existing catalog IDs and Item/design references exactly; generate new IDs only for newly normalized Print Provider, Option, and Option Value rows.
- Add isolated schema-10 fixture tests covering fixed provider, Choice, mixed Options, restricted/unrestricted areas, Item targets, design slots, archived Stores, and rollback on malformed ownership.
- Add new-schema round-trip and invalid-snapshot rejection tests. Verify no coordinate, override, generated-mockup, Shopify-mapping, credential, or binary asset storage is introduced.
- Verify workspace-package databases use the same migration path and newer schemas remain safely refused.

### 4. Store Editor presentation

- Refactor `StoreManagementViewModel` catalog state into focused presentation collaborators if needed to keep responsibilities bounded; reuse existing guarded-transition and authoritative-refresh patterns.
- Rename the tab and visible terminology to Catalog & mockups, Blueprint, Print Provider, Provider Network, Option, Variant, and Placeholder. Add concise helper text/tooltips for Blueprint and Placeholder.
- Add a top-level strategy selector with Manual enabled and the two future strategies disabled with accessible explanatory text.
- Extend progressive navigation through Blueprint Offering details and Mockup Template detail. Keep one clear create action at each level and disclose Options, Variants, Placeholders, templates, and colors only inside their owner.
- Use the opened Blueprint directly on Blueprint detail; do not expose a second normalized-catalog Blueprint selector or duplicate raw offering form.
- Synchronize the opened compatibility offering to normalized presentation state by stable offering ID, remove the second offering selector, suppress dependent normalized controls when no matching record exists, and show a concise integrity message without selecting an unrelated fallback.
- Provide initial/empty, populated, draft, busy, success, validation-error, persistence-error, blocked-dependency, archived/read-only, and post-delete states. Focus the first required field for new drafts; preserve drafts/selection/focus on cancelled transitions.
- Show the future source image as an unconfigured informational state. Add no file picker, upload/import action, coordinates, placement fields, rendering command, preview compositor, or override UI.
- Update Design Stage Tool labels and bindings from design areas/printable areas to selected Placeholders while retaining existing network warnings and editability.

### 5. Documentation reconciliation

- Update `docs/data-model.md`, `docs/ui-guidelines.md`, and other directly affected current documentation to use the accepted terminology and normalized relationships.
- Remove the current recommendation that the first mockup setup uses numeric placement fields. State that placement and composition belong to a future visual click-and-point editor and remain undefined here.
- Document the future Shopify adapter boundary without adding Shopify domain or persistence records.

### 6. Tests and verification

- Add Domain tests for every ownership, OptionKind, duplicate, target, color-binding, compatibility, revision, archive, and deletion invariant.
- Add Application tests for Store isolation, strategy availability, Manual no-network behavior, draft mutations, dependency messages, authoritative refresh, and Design-stage Placeholder resolution.
- Add Integration tests for schema 10 → 11 migration, rollback, new-schema creation, round-trip, package compatibility, and invalid snapshot rejection.
- Add ViewModel tests for progressive navigation, draft guards, disabled strategies, template/Color selection, read-only state, and post-mutation selection.
- Add Avalonia headless tests for compiled bindings, visibility, helper text/tooltips, keyboard reachability, focus, disabled strategy controls, empty/error/blocked states, and the absence of upload/placement/override controls.
- Run focused test projects during implementation, then `dotnet build .\FusionCanvas.sln`, `dotnet test .\FusionCanvas.sln`, `openspec validate --strict` using the CLI-supported invocation, and the scoped completion QA required by `docs/qa-review.md`.

## Acceptance-to-Verification Mapping

| Capability / requirement | Acceptance scenarios | Planned verification |
|---|---|---|
| store-fulfillment-strategy / Every Store has one fulfillment strategy | Existing Store is migrated; Strategy survives reload | SQLite migration fixture plus Store domain/round-trip tests |
| store-fulfillment-strategy / Initial strategy availability is manual-only | User configures an existing Store; User operates the editor by keyboard | ViewModel policy tests plus Avalonia headless enabled-state, tooltip, and keyboard tests |
| store-fulfillment-strategy / Strategy transitions preserve Store identity | Future enabled strategy is changed; User cancels a strategy warning | Domain/application transition contract tests; cancellation ViewModel test |
| store-fulfillment-strategy / Manual performs no marketplace communication | User edits a Manual Store catalog; Manual Store records external terminology or identifiers | Application tests with deterministic collaborators proving no external port/credential dependency |
| product-supplier-setup / Store catalog maintains structure | User adds a Blueprint and fixed-Print-Provider offering; User adds a Provider-Network offering; Catalog is isolated by Store | Domain ownership tests, application CRUD tests, SQLite round-trip tests |
| product-supplier-setup / Offerings retain provider-compatible variants and design areas | User creates typed Options and a concrete Variant; User creates a variant-compatible Placeholder; User enters invalid dimensions or references; User creates a duplicate Variant | Domain invariant tests and focused application rejection tests |
| product-supplier-setup / Printify Choice variable network | User configures a Choice offering; Choice display label changes; User reviews Choice Placeholders | Domain stable-code tests, round-trip tests, ViewModel/headless warning tests |
| product-supplier-setup / Catalog edits preserve selected Item targets | User removes an unreferenced Placeholder; removes Item-referenced Placeholder; removes template-referenced Placeholder; removes referenced Color value; archives referenced catalog record | Domain dependency policy and application lifecycle tests; blocked-action headless tests |
| product-supplier-setup / Progressive disclosure | User opens catalog editor; opens Blueprint; opens Blueprint Offering | ViewModel navigation tests and Avalonia headless visibility/selection tests |
| product-supplier-setup / Terminology and ownership | User encounters Blueprint; encounters Placeholder; creates records; removes records | Avalonia headless visible helper/accessibility/action-label tests |
| product-supplier-setup / Offering detail order | User reviews offering; adds Option; adds Variant; adds Placeholder; reviews Choice offering | ViewModel mutation tests and headless section/form/focus tests |
| product-supplier-setup / Navigation safeguards | Navigates with unsaved Blueprint edits; starts nested draft; completes destructive action; reviews archived Store | ViewModel guarded-transition, cancellation, aftermath, and read-only tests |
| product-supplier-setup / Local and listing-independent scope | User completes Store setup; Contributor reviews cross-system mapping | Application persistence test plus schema/source review asserting no external/listing/Shopify records |
| mockup-template-setup / Concrete target | User creates template; selects cross-offering Placeholder; offering default prefills draft | Domain target-ownership tests and ViewModel prefill/save tests |
| mockup-template-setup / Stable Color binding | Configures one color; chooses non-Color; chooses cross-offering Color; adds duplicate active color | Domain/application validation, filtered uniqueness, and SQLite round-trip tests |
| mockup-template-setup / Color-level only | Multiple sizes share one color; offering sizes change; user reviews configuration | Domain derivation tests, application state tests, headless absence-of-override tests |
| mockup-template-setup / Revision lifecycle | User changes template configuration; Future generated mockup records provenance | Revision domain/round-trip tests; future generated-record scenario is design-contract review only because rendering is explicitly out of scope |
| mockup-template-setup / Lifecycle protection | Removes referenced Placeholder; retires referenced Color; archives template color | Domain dependency and application lifecycle tests plus blocked-action ViewModel tests |
| mockup-template-setup / Placeholder compatibility | Target covers selected Variants; target misses selected Variant | Compatibility policy unit tests; future listing application is contract-only because Listing work is out of scope |
| mockup-template-setup / Empty source and no placement | Creates template; contributor inspects storage | Headless empty-state tests plus schema/source assertions for absent upload/coordinate/slot/renderer structures |
| mockup-template-setup / Focused Store Editor | Offering has no templates; starts draft; leaves draft; reviews archived Store | ViewModel and Avalonia headless empty/focus/guard/read-only tests |
| local-sqlite-persistence / Catalog migration | Existing Store; Blueprint; fixed offering; Choice offering; inline options; design area; Item/design references; no templates | Isolated schema-10 migration fixture with field/ID/count/relationship assertions |
| local-sqlite-persistence / Migration validation | Migrated data is valid; invalid legacy reference | Successful migration and forced-failure rollback integration tests |
| local-sqlite-persistence / Atomic round-trip | Configured Store reopened; invalid snapshot; new database | Integration round-trip, transaction rejection, schema inspection tests |
| store-management / Store Editor ownership | Opens active Store setup; Store has no Blueprints; changes context with draft; reviews archived Store | ViewModel tests plus Avalonia headless tab, empty, guard, and read-only tests |
| design-area-target-selection / Optional Store targets | Designs without targets; selects multiple Placeholders; attempts cross-Store target | Existing target-service tests updated for Placeholder identities plus focused regression tests |
| design-area-target-selection / Editability | Reviews Design from protected context | Domain/application edit policy and ViewModel tests |
| basic-product-workflow / Selected target guidance | Item opens with selected targets; selected Choice target displayed | Design Stage ViewModel tests and Avalonia headless target/warning tests |

No live desktop check is mandatory: the interaction risks are construction, binding, focus, selection, disabled state, and visual-tree behavior covered by deterministic Avalonia headless tests. An optional live check may supplement visual density review but cannot determine module completion.

## Exact Scenario Traceability Index

Every exact acceptance-scenario title is listed below. Each scenario inherits the planned verification method from its capability/requirement row in the mapping above; this index prevents shortened labels from obscuring criterion-level coverage in `verification.md` during implementation.

### basic-product-workflow

- `Item opens with selected targets`
- `Selected Choice target is displayed`

### design-area-target-selection

- `User designs without configured targets`
- `User selects multiple compatible Placeholders`
- `User attempts cross-Store target selection`
- `User reviews Design from a protected context`

### local-sqlite-persistence

- `Existing Store is migrated to Manual`
- `Existing product blueprint is migrated`
- `Existing fixed-provider offering is migrated`
- `Existing Printify Choice offering is migrated`
- `Existing inline Variant options are normalized`
- `Existing design area is migrated to a Placeholder`
- `Existing Item and design relationships are migrated`
- `Existing database has no mockup templates`
- `Migrated data is valid`
- `Migration encounters an invalid legacy reference`
- `Configured Store is reopened`
- `Save would violate a catalog invariant`
- `New database is created`

### mockup-template-setup

- `User creates a template for an offering`
- `User selects a Placeholder from another offering`
- `Offering default prefills a new template`
- `User configures one template color`
- `User chooses a non-Color value`
- `User chooses a Color from another offering`
- `Duplicate active template color is added`
- `Multiple sizes share one template color`
- `Offering sizes change`
- `User reviews template configuration`
- `User changes template configuration`
- `Future generated mockup records provenance`
- `User removes a referenced Placeholder`
- `User retires a referenced Color Option Value`
- `User archives a template color`
- `Template target covers selected Variants`
- `Template target does not cover every selected Variant`
- `User creates a template in this module`
- `Contributor inspects template storage`
- `Offering has no templates`
- `User starts a template draft`
- `User leaves a meaningful template draft`
- `Archived Store is reviewed`

### product-supplier-setup

- `User adds a Blueprint and fixed-Print-Provider offering`
- `User adds a Provider-Network offering`
- `Catalog is isolated by Store`
- `User creates typed Options and a concrete Variant`
- `User creates a variant-compatible Placeholder`
- `User enters invalid Placeholder dimensions or references`
- `User creates a semantically duplicate concrete Variant`
- `User configures a Choice offering`
- `Choice display label changes`
- `User reviews Choice Placeholders`
- `User removes an unreferenced Placeholder`
- `User removes a Placeholder referenced by an Item`
- `User removes a Placeholder referenced by a Mockup Template`
- `User removes a referenced Color Option Value`
- `User archives a referenced catalog record`
- `User opens the catalog editor`
- `User opens a Blueprint`
- `User opens a Blueprint Offering`
- `User encounters Blueprint for the first time`
- `User encounters Placeholder for the first time`
- `User creates catalog records`
- `User removes catalog records`
- `User reviews an offering`
- `User adds an Option`
- `User adds a concrete Variant`
- `User adds a Placeholder`
- `User reviews a Choice offering`
- `User navigates with unsaved Blueprint edits`
- `User starts a nested draft`
- `User completes a destructive action`
- `Archived Store catalog is reviewed`
- `User completes Store catalog setup`
- `Contributor reviews cross-system mapping`

### store-fulfillment-strategy

- `Existing Store is migrated`
- `Strategy survives reload`
- `User configures an existing Store`
- `User operates the editor by keyboard`
- `Future enabled strategy is changed`
- `User cancels a strategy warning`
- `User edits a Manual Store catalog`
- `Manual Store records external terminology or identifiers`

### store-management

- `User opens catalog setup for active Store`
- `Store has no configured Blueprints`
- `User changes editor context with an unsaved catalog draft`
- `User reviews archived Store setup`

## Context

Issue #208 originally described an empty provider-mockup selector. Discovery established that the immediate creator workflow is independent of Printify: a named Mockup Template such as “Flatlay no. 1” contains several locally imported product images, normally one per color, and later templates such as “Flatlay no. 2” or “Lifestyle no. 1” provide alternatives for the same Offering and Placeholder.

The current production UI exposes `ProviderMockupCandidateDescriptor` through an unavailable application source. That contract represents a single provider image for the whole draft and cannot represent managed local files or an image per condition. The domain and SQLite schema already contain nullable `SourceAssetId` fields on current color bindings and revision-color snapshots, but no use case writes them. A current `MockupTemplateRevision` has one provider reference and mapping, which cannot correctly snapshot a distinct mapping per local source image.

The existing asset and workspace-file boundaries can make a local copied file authoritative and preserve it across workspace transfer. Standard Asset links only target Store, Niche, Group, or Item, so a source Asset remains Store-linked while the Mockup Template source model provides its specific stable relationship.

The creator performs this work occasionally in the focused Store Editor dialog. The dialog keeps the main workspace context intact and uses progressive disclosure: a compact source-image collection, a selected-entry details area with its preview and applicability conditions, and explicit Save/Cancel. Browse is the required accessible interaction; drag-and-drop is intentionally deferred.

## Goals / Non-Goals

**Goals:**

- Allow one Template to own multiple local managed raster source images.
- Associate each image with one or more Option Values from its Offering without hardcoding Color, while making Color the obvious common route.
- Define an unambiguous exact-one source resolution rule for every compatible concrete Variant.
- Preserve local source, mapping, applicability, and revision provenance atomically and independently of the source file's later location.
- Reuse the existing managed-file and Asset infrastructure without introducing a provider SDK, API key, remote URL, or second file store.
- Provide deterministic domain, application, persistence, view-model, and meaningful Avalonia headless evidence.

**Non-Goals:**

- Printify or any external provider catalog/API, credentials, remote image URLs, downloading/caching remote images, or synchronization.
- Drag-and-drop, batch import, source-image editing, image conversion, image metadata editing, rendering/composition, generated mockups, Listing integration, or marketplace publication.
- Arbitrary Boolean applicability expressions, precedence rules, “best match” selection, per-Variant manual overrides, or size/color labels as identity keys.
- Automatic deletion of unused historical source files or a retention/cleanup policy beyond reference-safe removal guards.

## Decisions

### 1. Source images are separate current entities with value-condition joins

Introduce a cohesive `Mockups` model for a current local source image and its immutable revision snapshot, rather than extending the existing color-only binding:

```text
MockupTemplate
  ├─ MockupTemplateSourceImage (AssetId, Mapping, active/archived state)
  │    └─ MockupTemplateSourceImageOptionValue (OptionValueId)
  └─ MockupTemplateRevision
       └─ MockupTemplateRevisionSourceImage (AssetId, Mapping)
            └─ MockupTemplateRevisionSourceImageOptionValue (OptionValueId)
```

The current source entity stores a managed `AssetId` and one `MockupImageSpaceMapping`; its conditions are a non-empty set of active Offering Option Value IDs. Revision source rows copy current state rather than refer to mutable current rows. This permits Black, Navy + XL, or another real Offering condition without changing the resolution model.

The existing nullable `SourceAssetId` fields on `MockupTemplateColorVariant` and `MockupTemplateRevisionColor`, plus `ProviderMockupReference` and revision-level mapping, are a planned but inadequate shape for multiple arbitrary conditional images. Migrate their data defensively: preserve all pre-existing template/color/revision records; where a legacy source asset exists, create an equivalent source entry/snapshot using its color condition and any valid legacy mapping. Legacy provider-reference-only configurations remain historical metadata and are not treated as a current local source.

Alternative rejected: retain only `MockupTemplateColorVariant.SourceAssetId`. It cannot represent Size or another Offering option, and one template-level mapping cannot correctly describe multiple images.

### 2. Conditions match by stable Option Value identity using conjunction

An image applies to a concrete Offering Variant only when the Variant contains every selected Option Value for that image. A one-value Color condition is therefore the common case; Navy + XL is a more specific case. Every source entry requires at least one value. Values must be active, owned by the Template Offering, and unique within the entry.

The readiness evaluator examines only non-archived Variants that are compatible with the Template target Placeholder. It produces a stable, sorted result for each Variant: no match, one match, or multiple matching source entries. Ready means precisely one match per eligible Variant.

Alternative rejected: color-only behavior. It simplifies the first screen but permanently encodes a product assumption that becomes incorrect when usable source imagery depends on Size, material, or another catalog dimension.

Alternative rejected: choose the “most specific” matching image. That hides ambiguous creator setup and makes later image additions alter results silently.

### 3. A local import is one application-owned atomic use case

Define an application-facing `IMockupTemplateSourceImageService` or extend the focused template service with explicit source-draft commands. It coordinates: validate Template/Offering/Store editability; import a file via `IWorkspaceFileStore`; decode safe supported raster metadata through an application-owned image-inspection port; create the Store-owned Asset and a Store AssetLink; create/update current source records and revision snapshots; and save the snapshot once. If repository persistence fails after a copy, it best-effort deletes only the newly created managed file. A cancelled picker, decode failure, or validation failure creates no persisted source records.

The Integration layer implements image inspection using a bounded, supported local decoder and returns only width/height. It treats pixels and metadata as untrusted, imposes finite positive dimension and input-size limits, does not execute embedded content, and never uses an external file path after import. The App file picker limits this flow to raster formats the decoder supports (initially PNG, JPEG, WebP, TIFF if the selected decoder safely supports it); the file-store's broader creative-file list does not define source-image eligibility.

Alternative rejected: import through the generic Asset window then ask the creator to attach an existing asset. That splits one setup action across unrelated surfaces, makes source applicability harder to discover, and risks selecting an unrelated Store asset.

### 4. The focused dialog owns drafts; files are imported only on explicit save

`CatalogSetupViewModel` holds a source-image draft collection. Choosing Browse stages a local path and inspected dimensions in memory; it does not copy a file or mutate the repository. The collection identifies one selected source entry; the preview uses the staged path for a new draft and the managed file for an existing confirmed entry. On Save, the application command imports every newly staged file and commits the entire Template change atomically.

The selected source's mapping initializes to the full image bounds. The existing placement control becomes source-entry-specific and receives its selected image preview. Switching entries preserves each draft's values. The UI shows Color choices first and a compact “Add option condition” path for other active Offering Options. It reports missing/ambiguous Variants near the source collection and cannot report the Template ready until the evaluator succeeds; saving a draft remains allowed so a creator can progressively complete configuration.

Existing Template create/edit discard handling covers staged sources, selected conditions, and mappings. Any source-affecting save creates the next revision. Archived Stores expose confirmed source rows and readiness results but no Browse, edit, placement, or Save command. Dialog close returns focus to the invoking Template action when practical.

Alternative rejected: copy the file as soon as Browse completes. Cancellation and unsaved-change behavior would leave orphan managed files or require opaque cleanup.

### 5. Source Asset removal is dependency-aware

The Asset deletion policy queries both current source entities and immutable revision source snapshots. A referenced source Asset cannot be permanently removed through the generic Asset surface. Removing a current source entry is an explicit Template edit; it creates a new revision and may make the Template incomplete, while historical snapshots retain their references and keep their Assets protected.

Alternative rejected: permit deletion and show a broken source later. That destroys revision provenance and undermines the local-first promise.

## Risks / Trade-offs

- [Multiple conditions can confuse a creator who only needs Color] → Make Color the initial visible chooser, keep additional conditions progressively disclosed, and show per-Variant readiness feedback.
- [A generic asset type may accept files the preview cannot decode] → The source-specific picker and inspection contract use a constrained raster allowlist and verify decoded dimensions before save.
- [Schema refactor could lose provisional source fields] → Use an ordered migration with fixtures for null fields, a valid legacy source Asset, and legacy provider-reference-only records; never reinterpret an external reference as a local Asset.
- [Historical assets can accumulate] → Preserve references and block unsafe deletion in this module; retention policy is deferred so no history is silently removed.
- [Large or malformed images may consume resources] → Bound file size and decoded dimensions, fail recoverably, and test hostile dimensions/decoder failures with fakes.

## Migration Plan

1. Advance the SQLite schema through one transactional migration that introduces current/revision source-image and condition tables plus indexes and foreign-key validation.
2. Preserve existing templates, current color bindings, revisions, and revision colors. Convert only rows with a non-null valid local `SourceAssetId` into equivalent source rows using their Color Option Value; copy a valid legacy mapping when present.
3. Retain legacy provider references as history/compatibility data but do not expose them as selectable current local images. Do not fabricate Assets, dimensions, or conditions.
4. Roll back the entire migration if any structure, copy, or referential check fails; do not advance the schema version.
5. New databases create the source-image schema at the current version. Workspace packages round-trip source images through existing managed-file transfer and the new structured tables.

## Open Questions

None. The first implementation chooses a safe decoder-supported raster allowlist and records it in the implementation notes; expanding formats is a later compatibility decision, not a product-model change.

## Implementation Plan

1. **Domain model and policy**
   - Add one primary type per file under `FusionCanvas.Domain.Mockups` for current source entries, current source conditions, revision source entries, revision source conditions, and a deterministic readiness/result policy.
   - Preserve the existing `MockupTemplate`, `MockupTemplateRevision`, color binding, and snapshot types for migration compatibility; remove or stop using superseded source fields only after migration and compatibility readers are reconciled.
   - Validate non-empty stable IDs, unique conditions, positive mappings, Template/Offering ownership, active Option Values, Placeholder compatibility, and exact-one Variant resolution.
   - Add Domain tests for color-only coverage, multiple conditions, cross-offering/archived values, missing and ambiguous Variant matches, mappings, and immutable revision copy behavior.

2. **Application use case and contracts**
   - Add source-image draft/request/result/state types and an image-metadata inspection port in `FusionCanvas.Application.Mockups` or the existing focused Catalog capability without leaking Avalonia or decoder types.
   - Refactor `OfferingManagementService.CreateMockupTemplateAsync` and Template edit flows into a focused source-aware operation that loads current Template state, validates staged drafts, imports only new files on explicit save, writes the Asset/Store link/source graph/revision graph in one snapshot save, and cleans copied files on save failure.
   - Add readiness summaries and dependency reports to Template state; use stable IDs, not labels, for selection and applicability.
   - Extend Asset removal orchestration to block any current or historical Template-source reference.
   - Add deterministic Application tests using repository, workspace-file, and image-inspection fakes for successful create/replace, cancellation, decode/import/save failures, revision provenance, conditions, readiness, and removal blockers.

3. **Integration and persistence**
   - Add a bounded raster metadata adapter in `FusionCanvas.Integration.Files` and wire it through the App composition root; verify no external path, remote URL, or provider dependency crosses inward.
   - Add schema migration, save/load mappings, relationship validation, indexes, and workspace-package coverage for current and revision source/condition tables.
   - Add isolated SQLite tests for new creation, migration compatibility, rollback, and exact relationship reconstruction; add workspace-package round trips for source Assets and snapshots.

4. **Focused Store Editor experience**
   - Remove `UnavailableProviderCatalogCandidateSource` and provider-candidate loading from production Mockup Template composition; retain only tests or transitional code justified by the migrated behavior, without provider-network calls.
   - Update `CatalogSetupViewModel` and `MockupTemplateEditorWindow` to stage sources through an injected picker, manage selected-entry drafts, Color-first and additional-option conditions, source-specific preview/mapping, readiness/gap/error messages, busy protection, cancellation/discard, and archived read-only state.
   - Adapt the placement editor to render the selected staged or managed local image safely; use actual inspected dimensions and source-specific mapping.
   - Add framework-free ViewModel tests for selection, draft retention, state transitions, and commands; add headless dialog tests for bindings, enabled/disabled controls, selected source/condition presentation, keyboard focus, and discard behavior. No live desktop run is required for completion.

5. **Documentation and verification**
   - Reconcile the directly affected Mockup Setup section in `docs/ui-guidelines.md` and any current source-image guidance so it describes local source images and flexible Option Value applicability instead of an empty future state.
   - Create `verification.md` mapping every delta scenario to evidence; run strict OpenSpec validation, the affected focused test projects, and `dotnet test .\FusionCanvas.sln`.

## Planned Acceptance-to-Verification Map

| Acceptance area | Planned evidence |
| --- | --- |
| Local import, failure cleanup, and replacement provenance | Application service tests with fake repository/file store/inspector |
| Option-value ownership and flexible conditions | Domain and Application policy tests |
| Exact-one Variant resolution | Domain parameterized tests and Application readiness-state tests |
| Mapping validity and source-specific dimensions | Domain mapping tests, Application inspection tests, ViewModel tests |
| Revisions and asset-removal protection | Domain/Application tests plus SQLite relationship round trip |
| Dialog browse, selection, incomplete/ready/error/read-only states, focus, and discard | ViewModel tests and meaningful Avalonia headless `MockupTemplateEditorWindow` tests |
| Migration, persistence, and package transfer | Isolated SQLite migration/round-trip tests and workspace-package integration tests |
| No network/credential path | Composition/changed-scope inspection and deterministic solution baseline |

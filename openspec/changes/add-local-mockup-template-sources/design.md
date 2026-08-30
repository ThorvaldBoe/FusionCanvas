## Context

Issue #208 originally described an empty provider-mockup selector. Discovery established that the immediate creator workflow is independent of Printify: a named Mockup Template such as “Flatlay no. 1” contains several locally imported product images, normally one per color, and later templates such as “Flatlay no. 2” or “Lifestyle no. 1” provide alternatives for the same Offering and Placeholder.

The current production UI exposes `ProviderMockupCandidateDescriptor` through an unavailable application source. That contract represents a single provider image for the whole draft and cannot represent managed local files or an image per condition. The domain and SQLite schema already contain nullable `SourceAssetId` fields on current color bindings and revision-color snapshots, but no use case writes them. A current `MockupTemplateRevision` has one provider reference and mapping, which cannot correctly snapshot a distinct mapping per local source image.

The existing asset and workspace-file boundaries can make a local copied file authoritative and preserve it across workspace transfer. Standard Asset links only target Store, Niche, Group, or Item, so a source Asset remains Store-linked while the Mockup Template source model provides its specific stable relationship.

The creator performs this work occasionally in the focused Store Editor dialog. The dialog keeps the main workspace context intact and uses a master-detail composition: the Template name and shared target Design Area remain above an always-visible source-image table, while the selected entry's applicability and placement appear below. Upload is independent of metadata editing, and explicit Save/Cancel owns the complete draft. A file picker is the required accessible interaction; drag-and-drop is intentionally deferred.

## Goals / Non-Goals

**Goals:**

- Allow one Template to own multiple local managed raster source images.
- Persist uploaded images before their applicability or placement is complete, while keeping incomplete state explicit and recoverable.
- Associate each image with grouped Option Values from its Offering without hardcoding Color, while optimizing the initial controls for one Color and all Sizes.
- Define an unambiguous per-Variant exact-one source resolution rule that retains successful outcomes when other Variants are unresolved.
- Preserve local source, mapping, applicability, and revision provenance atomically and independently of the source file's later location.
- Reuse the existing managed-file and Asset infrastructure without introducing a provider SDK, API key, remote URL, or second file store.
- Provide deterministic domain, application, persistence, view-model, and meaningful Avalonia headless evidence.

**Non-Goals:**

- Printify or any external provider catalog/API, credentials, remote image URLs, downloading/caching remote images, or synchronization.
- Drag-and-drop, batch import, source-image editing, image conversion, image metadata editing, rendering/composition, generated mockups, Listing integration, or marketplace publication.
- Arbitrary nested Boolean expressions beyond OR-within/AND-between Option groups, precedence rules, “best match” selection, per-Variant manual overrides, or size/color labels as identity keys.
- Automatic deletion of unused historical source files or a retention/cleanup policy beyond reference-safe removal guards.

## Decisions

### 1. Source images are separate current entities with value-condition joins

Introduce a cohesive `Mockups` model for a current local source image and its immutable revision snapshot, rather than extending the existing color-only binding:

```text
MockupTemplate
  ├─ MockupTemplateSourceImage (AssetId, optional Mapping, active/archived state)
  │    └─ MockupTemplateSourceImageOptionValue (OptionValueId)
  └─ MockupTemplateRevision
       └─ MockupTemplateRevisionSourceImage (AssetId, optional Mapping)
            └─ MockupTemplateRevisionSourceImageOptionValue (OptionValueId)
```

The current source entity stores a managed `AssetId` and an optional `MockupImageSpaceMapping`; zero conditions and an absent mapping are valid incomplete draft state. Condition rows store active Offering Option Value IDs and are grouped at evaluation time by each value's owning Option. Revision source rows copy both complete and incomplete current state rather than refer to mutable current rows. This permits the normal Black/all-Sizes case, alternatives such as Black-or-Navy, and cross-Option constraints such as (Black or Navy) AND (M or L) without label-based keys.

The existing nullable `SourceAssetId` fields on `MockupTemplateColorVariant` and `MockupTemplateRevisionColor`, plus `ProviderMockupReference` and revision-level mapping, are a planned but inadequate shape for multiple arbitrary conditional images. Migrate their data defensively: preserve all pre-existing template/color/revision records; where a legacy source asset exists, create an equivalent source entry/snapshot using its color condition and any valid legacy mapping. Legacy provider-reference-only configurations remain historical metadata and are not treated as a current local source.

Alternative rejected: retain only `MockupTemplateColorVariant.SourceAssetId`. It cannot represent Size or another Offering option, and one template-level mapping cannot correctly describe multiple images.

### 2. Conditions use OR within one Option and AND across Options

Group selected values by their owning stable Option identity. A Variant satisfies a group when it contains any selected value in that group, and applies to the image only when it satisfies every configured group. A single Color value with no Size group is the optimized common case and naturally covers all compatible Sizes. Values must be active, owned by the Template Offering, and unique within the entry. An entry with no groups is incomplete and matches nothing rather than acting as an unsafe wildcard.

The readiness evaluator examines only non-archived Variants compatible with the Template's one shared target Placeholder and only complete active images with valid mappings. It produces a stable, sorted result for each Variant: no match, one match, or multiple matching source entries. Ready means precisely one match per eligible Variant, but the result object retains every per-Variant outcome so a later mockup-generation consumer can skip/report Navy without failing independently resolved Black or White work.

Alternative rejected: color-only behavior. It simplifies the first screen but permanently encodes a product assumption that becomes incorrect when usable source imagery depends on Size, material, or another catalog dimension.

Alternative rejected: choose the “most specific” matching image. That hides ambiguous creator setup and makes later image additions alter results silently.

### 3. A local import is one application-owned atomic use case

Define an application-facing `IMockupTemplateSourceImageService` or extend the focused template service with explicit source-draft commands. It coordinates: validate Template/Offering/Store editability; import a file via `IWorkspaceFileStore`; decode safe supported raster metadata through an application-owned image-inspection port; create the Store-owned Asset and a Store AssetLink; create/update current source records and revision snapshots; and save the snapshot once. If repository persistence fails after a copy, it best-effort deletes only the newly created managed file. A cancelled picker, decode failure, or validation failure creates no persisted source records.

The Integration layer implements image inspection using a bounded, supported local decoder and returns only width/height. It treats pixels and metadata as untrusted, imposes finite positive dimension and input-size limits, does not execute embedded content, and never uses an external file path after import. The App file picker limits this flow to raster formats the decoder supports (initially PNG, JPEG, WebP, TIFF if the selected decoder safely supports it); the file-store's broader creative-file list does not define source-image eligibility.

Alternative rejected: import through the generic Asset window then ask the creator to attach an existing asset. That splits one setup action across unrelated surfaces, makes source applicability harder to discover, and risks selecting an unrelated Store asset.

### 4. The focused dialog separates the image collection from selected-image metadata

`CatalogSetupViewModel` holds a source-image draft collection and one selected entry. Choosing Upload stages only the local path, inspected dimensions, and source identity in memory; it does not copy current applicability or placement from another row, copy a file, or mutate the repository. The preview uses the staged path for a new draft and the managed file for an existing confirmed entry. On Save, the application command imports newly staged files and commits the Template, complete and incomplete source entries, archive changes, and revision graph atomically.

The upper table shows file name, applicability summary, shared Design Area, mapping state, and actionable completion status for each active entry, with Upload and confirmed Archive commands owned by that collection. The lower editor receives the selected image preview and exposes that entry's grouped applicability and placement. It shows Color first, defaults to no Size restriction, and progressively discloses Size or another active Option. Upload assigns neither applicability nor a mapping. Switching rows preserves every draft. Per-row completeness remains distinct from Template-level missing/ambiguous Variant coverage, and incomplete Templates remain saveable but not ready.

The Template retains one shared Design Area; every source mapping places that same area within its own image dimensions. Existing Template create/edit discard handling covers staged sources, selected conditions, mappings, and archives. Any source-affecting save creates the next revision. Archived Stores expose confirmed rows and readiness results but no Upload, archive, edit, placement, or Save command. Upload selects the new row; archiving selects a sensible remaining row or the empty state; dialog close returns focus to the invoking Template action when practical.

Alternative rejected: copy the file as soon as Upload selection completes. Cancellation and unsaved-change behavior would leave orphan managed files or require opaque cleanup.

### 5. Source Asset removal is dependency-aware

The Asset deletion policy queries both current source entities and immutable revision source snapshots. A referenced source Asset cannot be permanently removed through the generic Asset surface. Removing a current source entry is an explicit Template edit; it creates a new revision and may make the Template incomplete, while historical snapshots retain their references and keep their Assets protected.

Alternative rejected: permit deletion and show a broken source later. That destroys revision provenance and undermines the local-first promise.

## Risks / Trade-offs

- [Multiple conditions can confuse a creator who only needs Color] → Make one Color and no Size restriction the optimized initial route, keep additional Option groups progressively disclosed, and summarize the resulting applicability in the image table.
- [Persisted incomplete entries could be mistaken for usable sources] → Give every row a derived completion state, exclude incomplete entries from matching, and keep Template readiness and per-Variant outcomes visible without blocking draft save.
- [A generic asset type may accept files the preview cannot decode] → The source-specific picker and inspection contract use a constrained raster allowlist and verify decoded dimensions before save.
- [Schema refactor could lose provisional source fields] → Use an ordered migration with fixtures for null fields, a valid legacy source Asset, and legacy provider-reference-only records; never reinterpret an external reference as a local Asset.
- [Historical assets can accumulate] → Preserve references and block unsafe deletion in this module; retention policy is deferred so no history is silently removed.
- [Large or malformed images may consume resources] → Bound file size and decoded dimensions, fail recoverably, and test hostile dimensions/decoder failures with fakes.

## Migration Plan

1. Treat schema 13's current/revision source-image tables as the implemented baseline; add the next ordered transactional migration so mapping columns can represent absent configuration and zero condition rows remain valid.
2. Preserve every existing schema-13 source row, mapping, condition, archive flag, and revision snapshot exactly; existing complete entries remain complete after upgrade.
3. Preserve earlier templates, color bindings, revisions, and provider-reference-only history through the already-defined compatibility path; never fabricate Assets, dimensions, mappings, or conditions.
4. Roll back the entire new migration if any structure, copy, or referential check fails; do not advance the schema version.
5. New databases create the revised optional-state schema at the new current version. Workspace packages round-trip complete and incomplete source images through existing managed-file transfer and structured tables.

## Open Questions

None. The first implementation chooses a safe decoder-supported raster allowlist and records it in the implementation notes; expanding formats is a later compatibility decision, not a product-model change.

## Implementation Plan

1. **Domain model and policy**
   - Add one primary type per file under `FusionCanvas.Domain.Mockups` for current source entries, current source conditions, revision source entries, revision source conditions, and a deterministic readiness/result policy.
   - Preserve the existing `MockupTemplate`, `MockupTemplateRevision`, color binding, and snapshot types for migration compatibility; remove or stop using superseded source fields only after migration and compatibility readers are reconciled.
   - Permit absent mappings and zero applicability rows as explicit incomplete state; validate stable IDs, uniqueness within grouped conditions, positive assigned mappings, Template/Offering ownership, active Options and Values, Placeholder compatibility, and exact-one Variant resolution.
   - Resolve conditions by OR within each owning Option and AND across configured Options; exclude incomplete entries from matches while retaining resolved results for unaffected Variants.
   - Add Domain tests for one-Color/all-Sizes coverage, multiple values within one Option, multiple Option groups, incomplete entries, cross-offering/archived values, missing and ambiguous Variant matches, optional mappings, and immutable revision copy behavior.

2. **Application use case and contracts**
   - Add source-image draft/request/result/state types and an image-metadata inspection port in `FusionCanvas.Application.Mockups` or the existing focused Catalog capability without leaking Avalonia or decoder types.
   - Refactor `OfferingManagementService.CreateMockupTemplateAsync` and Template edit flows into a focused source-aware operation that loads current Template state, validates staged drafts, imports only new files on explicit save, writes the Asset/Store link/source graph/revision graph in one snapshot save, and cleans copied files on save failure.
   - Add per-entry completeness, per-Variant resolution, readiness summaries, and dependency reports to Template state; use stable IDs, not labels, for selection and applicability.
   - Extend Asset removal orchestration to block any current or historical Template-source reference.
   - Add deterministic Application tests using repository, workspace-file, and image-inspection fakes for successful create/replace, cancellation, decode/import/save failures, revision provenance, conditions, readiness, and removal blockers.

3. **Integration and persistence**
   - Add a bounded raster metadata adapter in `FusionCanvas.Integration.Files` and wire it through the App composition root; verify no external path, remote URL, or provider dependency crosses inward.
   - Add the next ordered schema migration for optional current/revision mappings and persisted zero-condition entries; preserve existing complete rows and revisions. Extend save/load mappings, relationship validation, indexes, and workspace-package coverage.
   - Add isolated SQLite tests for new creation, migration compatibility, rollback, and exact relationship reconstruction; add workspace-package round trips for source Assets and snapshots.

4. **Focused Store Editor experience**
   - Remove `UnavailableProviderCatalogCandidateSource` and provider-candidate loading from production Mockup Template composition; retain only tests or transitional code justified by the migrated behavior, without provider-network calls.
   - Update `CatalogSetupViewModel` and `MockupTemplateEditorWindow` to implement the approved UI-language master-detail design: Template name and shared Design Area above; upper table for upload, archive, selection, summaries, and completion; lower selected-entry preview, Color-first grouped applicability, and source-specific mapping.
   - Keep upload independent of metadata, select newly uploaded rows, retain per-row drafts across selection changes, select a sensible replacement after archive, and expose empty, incomplete, complete, invalid, busy, import-error, discard, and archived read-only states.
   - Adapt the placement editor to render the selected staged or managed local image safely; use actual inspected dimensions and source-specific mapping.
   - Add framework-free ViewModel tests for selection, draft retention, state transitions, and commands; add headless dialog tests for bindings, enabled/disabled controls, selected source/condition presentation, keyboard focus, and discard behavior. No live desktop run is required for completion.

5. **Documentation and verification**
   - Reconcile the directly affected Mockup Setup section in `docs/ui-guidelines.md` and any current source-image guidance so it describes local source images and flexible Option Value applicability instead of an empty future state.
   - Create `verification.md` mapping every delta scenario to evidence; run strict OpenSpec validation, the affected focused test projects, and `dotnet test .\FusionCanvas.sln`.

## Planned Acceptance-to-Verification Map

| Acceptance area | Planned evidence |
| --- | --- |
| Local import, failure cleanup, and replacement provenance | Application service tests with fake repository/file store/inspector |
| Option ownership, OR-within/AND-between matching, and one-Color/all-Sizes default | Domain and Application policy tests |
| Exact-one per-Variant resolution without aggregate failure | Domain parameterized tests and Application readiness-state tests |
| Persisted incomplete entries, optional mapping validity, and source-specific dimensions | Domain mapping tests, Application inspection/persistence tests, ViewModel tests |
| Revisions and asset-removal protection | Domain/Application tests plus SQLite relationship round trip |
| Dialog upload/metadata independence, table selection/archive, completion states, focus, error/read-only states, and discard | ViewModel tests and meaningful Avalonia headless `MockupTemplateEditorWindow` tests plus validated UI-language artifact |
| Migration, persistence, and package transfer | Isolated SQLite migration/round-trip tests and workspace-package integration tests |
| No network/credential path | Composition/changed-scope inspection and deterministic solution baseline |

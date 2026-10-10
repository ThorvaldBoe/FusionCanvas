## Context

The Store setup already has a Printify catalog import. It builds local Blueprint, Offering, variant, and print-area records from products in the connected Printify shop. A separate listing lifecycle is being implemented for mapping an Item to one Printify product and managing that product from the Listing stage. This module adds the complementary remote-to-local bootstrap path: import existing shop listings as Items without mutating Printify.

The mapping model is Store-scoped and currently enforces one Printify product per Item per Store and one Item per Printify product identity. It stores publication/synchronization state and a normalized product snapshot. Existing workspace file storage copies imported images into managed local paths and stores them as Item-linked Assets.

Primary workflow: a creator with a connected Printify shop opens `Import from Printify…` from a Niche context menu, reviews and selects products, optionally checks title/description matches, chooses whether each close match is new or connected to an unlinked Item, confirms, and reviews per-product outcomes. The imported Item is immediately linked and retains listing data/artwork. When the user needs local variant/design setup, they invoke the explicit Listing-stage download action. This is an occasional setup/recovery workflow, so import belongs in a focused dialog and should preserve the main workspace context.

## Goals / Non-Goals

**Goals:**

- Discover every product from the Store's selected Printify shop, across API pages, and preview import eligibility before local mutation.
- Keep already-linked products out of the default list while allowing users to reveal them for reference.
- Let users select products, run text-based duplicate checks separately, and explicitly import new Items or connect unlinked existing Items.
- Preserve local title, description, workflow state, and topic placement when connecting a remote product to an existing Item.
- Import Printify product data and original print-area artwork, persist the external mapping, and make per-product success/failure visible and retryable.
- Place newly created Items in a dated `Printify yyyy-MM-dd` group under the invoking Niche, suffixing the name when needed.
- Keep the remote shop read-only and keep direct Shopify import and image-based duplicate detection out of scope.
- Keep initial import lightweight by preserving the listing snapshot and original artwork as Item-linked data, with no catalog or Design mutation.
- Offer a Listing-stage action to download/reconstruct the variant setup when the user is ready to edit the imported listing locally.

**Non-Goals:**

- Importing products directly from Shopify or adding Shopify API support.
- Ongoing background synchronization or automatic duplicate resolution.
- Image-based duplicate detection.
- Allowing multiple Printify products to map to one local Item in the same Store.
- Changing Printify products, publication state, or shop configuration.
- Replacing the existing Store setup flow for importing the Printify catalog.
- Reconstructing catalog or Design state during the initial shop-listing import.
- Adding user-facing controls to edit Printify placement transforms in FusionCanvas.

## Decisions

### Keep initial linking separate from optional variant-setup download

Initial shop import creates the Item-level link, listing snapshot, and local artwork assets. It does not mutate the Store catalog or Design setup. When the user wants to change the listing locally, a Listing-stage action downloads the linked product's variant setup, reuses or imports the Store catalog structure, and initializes the Item's Listing configuration and Design rows/assignments. This preserves a quick setup path while making edit readiness an explicit choice.

V1 assumes each Printify print area/variant uses one finished uploaded image, matching the user's workflow. Reconstruct that image into the corresponding Design slot, grouping colors with equivalent artwork into the same row where the local model allows it. Layered/text artwork remains preserved as Item assets but is outside the reconstruction path; report affected slots instead of flattening layers or discarding files. Ignore Printify-specific image position, scale, and rotation during import and use FusionCanvas's standard calculated placement. The user accepts that Printify-only placement adjustments will not transfer to FusionCanvas and may be reset to FusionCanvas defaults by a later upload.

Reuse `PrintifyCatalogImportService`'s selected-product catalog mapping behavior through a focused orchestration boundary rather than duplicating catalog mapping logic. Refactor the catalog importer to expose its snapshot transformation separately from persistence so catalog, Item configuration, and Design setup can be composed into one repository save. The orchestration must coordinate catalog and Item Design changes so failures do not leave a partially configured Item, and it must be idempotent when catalog entities or Item setup already exist. Do not overwrite conflicting user-edited catalog entities silently; reconcile only matching Printify-owned records and return an actionable conflict.

### Reuse the external listing mapping and existing one-to-one invariant

Save imported products through the existing `ExternalListingMapping` model and `external_listing_mappings` table. Persist the selected Store, shop, and product IDs, publication state, imported snapshot, and successful operation state. Stable product identity excludes the product on subsequent scans. A close match that already has a Printify mapping is shown as non-linkable; users can still import the remote product as a separate Item.

Alternative considered: add another import-only mapping table or broaden mappings to many products per Item. A second table would split lifecycle identity, while many-to-one would expand the in-progress listing lifecycle and is not needed for v1.

### Preserve local Item identity when connecting a duplicate

When the user connects a remote product to an existing active Item in the selected Store, preserve its local title, description, workflow stage, status, and topic. Attach the remote mapping, imported listing data, and assets to it. Candidate matching searches active Items throughout that Store, including other Niches; an Item already mapped to a Printify product is a visible, non-linkable candidate.

Alternative considered: limit matching to the invoking Niche or replace local fields with Printify values. Store-wide matching catches cross-Niche duplicates; replacing user-authored fields would be surprising. New Items receive the Printify title and description.

### Keep duplicate checking explicit and advisory

The dialog lists unlinked products with individual checkboxes. The user runs duplicate checking explicitly against the selected subset. Application logic normalizes title and description (Unicode normalization, case folding, whitespace/punctuation normalization) and computes normalized edit similarity separately for each field. A score of at least 0.90 in either non-empty field is a possible duplicate, not a block. The review surface names the matched Item and its Niche/group path and offers `Import as new` or `Connect to this Item` when eligible.

Alternative considered: run matching on every listing as it loads or treat a text match as a hard duplicate. Explicit checking keeps list retrieval fast and avoids preventing legitimate similar designs. A text-only candidate is never attached or excluded without the user's choice.

### Save artwork through managed workspace files

After confirmation, retrieve each product's full detail and the original print-area artwork. Copy each source image into the workspace through the existing managed-file boundary, then link it as an Item Asset and retain source print-area identity/order in import metadata. Validate the URL and response before accepting bytes; enforce HTTPS, supported image media types, bounded image size, cancellation, and cleanup of uncommitted files. Do not trust arbitrary API-provided URLs or filenames as local paths.

Alternative considered: retain only remote image URLs. That would not satisfy local ownership and offline access. Downloading mockup previews is excluded unless a source is also identified as original print-area artwork.

### Keep lifecycle snapshot fields compatible and preserve imported product data

The current listing lifecycle compares a normalized snapshot (title, description, shipping profile, and variant retail prices) against a local projection. Keep that canonical `SnapshotJson` shape so refresh does not report import-only fields as drift. Preserve additional imported Printify product fields—visibility, Blueprint/Provider identity, complete variant information, print-area relationships, and artwork provenance—in a versioned typed value serialized in the existing JSON integration/import metadata field. Do not retain or replay Printify image position/scale/rotation in v1. Extend the metadata codec compatibly if needed; do not create a new SQL table solely for provider-specific import data. Before variant setup is downloaded, the Listing tool must explain that setup is missing and offer the download action.

Alternative considered: put the full provider response into `SnapshotJson`. The current drift comparer compares every snapshot key and would report import-only data as remote/local drift.

### Commit each product independently and retry safely

Treat one selected product as the commit unit. Prepare and validate listing data and all artwork first; then persist its Item (if new), group membership (if newly needed), assets/links, and mapping in one workspace snapshot save. If retrieval or persistence fails, remove staged files and report the product-specific error. Successful products from the same run stay committed. A later run skips those product IDs and can retry failures. If an import is cancelled after processing begins, finish the current product safely and stop before starting another, then report partial outcomes.

Alternative considered: make the entire shop import all-or-nothing. A single bad listing or transient download error would then discard unrelated successes and make the bootstrap workflow harder to resume.

### Use one dated group per run for newly created Items

Create a group under the invoking Niche only when at least one new Item succeeds. Use the local date from the injected application clock and a deterministic suffix such as `(2)`, `(3)` when the base name already exists. New Items from a run share that group; connected existing Items remain in their current topics.

Alternative considered: create a group for every product or move connected Items into the group. One group keeps bulk imports navigable while preserving existing organization for adopted Items.

### Keep the import UI in the Niche context menu and a focused dialog

Add the action alongside existing Niche-level tree actions. The dialog owns loading, preview, linked-item visibility, selection, duplicate review, progress, cancellation, empty, and error states. Keep the invoking Niche visible as context. Linked products are hidden by default; revealing them never enables their checkboxes. The dialog does not need an unsaved-draft warning because it does not edit local Item fields during preview.

Alternative considered: place import under the Listing stage or Store setup. It begins with no local Item and is a Niche-scoped placement action, so neither surface provides the correct context.

## Risks / Trade-offs

- [A shop has many products or many API pages] → Paginate through the full result, show loading/progress, and retain stable product IDs across pages; do not require all artwork to download during preview.
- [Printify returns a malformed or inconsistent product] → Validate required identity, visibility, listing data, and artwork references before committing that product; show a safe product-specific error.
- [One artwork download fails after others succeeded] → Stage all product files, save the workspace snapshot only after all required artwork for that product is available, and delete staged files on failure.
- [An image URL points outside the expected service or returns excessive/unexpected content] → Require HTTPS and an approved Printify image host, validate content type and byte limit, and reject unsupported payloads before writing a managed file.
- [A title or description is common across distinct products] → Use a 0.90 normalized edit-similarity threshold only to suggest candidates, show the candidate path, and require an explicit new-item or connect choice.
- [A user retries after a partial run] → Enforce remote identity uniqueness and exclude already mapped products so retries only affect failed/unlinked products.
- [The current mapping supports only one Printify product per Item] → Mark mapped candidates non-linkable; preserve current lifecycle data invariants and leave multi-product Items to a separately scoped change.
- [A new group would be empty after failures or cancellation] → Create it only in the successful product's atomic save; do not leave empty groups.
- [Catalog or Design setup is incomplete after a failed on-demand download] → Stage the Item-level setup changes and publish them only after all required catalog/configuration changes are ready; report a safe retryable error and leave the original linked Item intact.
- [A listing falls outside the v1 single-finished-image assumption] → Preserve all original artwork assets, leave affected assignments empty, and report incomplete reconstruction; do not flatten or discard remote artwork.
- [Printify placement has a non-default position, scale, or rotation] → Ignore those values during import and use FusionCanvas's standard placement calculation. A later upload can reset Printify-only adjustments; this is an accepted v1 limitation.
- [An imported listing has not downloaded local variant setup] → Keep the Item linked and editable as a listing record, but offer the explicit setup action before local listing edits that require catalog/Design projection.

## Migration Plan

No schema migration is expected if the current external listing mapping contains the needed snapshot and state fields. Keep the canonical Listing lifecycle snapshot shape and retain additional Printify product data in the existing JSON integration/import metadata field. Use the existing Asset and AssetLink tables and managed workspace file store.

If implementation discovers that no compatible place exists for required listing data or artwork provenance, stop and revise this OpenSpec change before adding a new persistence table or migration. Rollback can disable the import command while retaining imported Items, assets, and mappings; it must never delete or alter Printify products.

## Open Questions

None. Resolved decisions: initial import links the Printify listing and saves listing data/artwork; a user-triggered Listing-stage action downloads variant setup when local listing changes are needed; v1 assumes one finished image per print area/variant; layered artwork is preserved but outside the supported reconstruction path; Printify-specific position/scale/rotation values are ignored and FusionCanvas defaults are used, even though a later upload can reset provider-only adjustments.

## Implementation Plan

### Application contracts and import orchestration

- Add focused request/result/state records and an `IPrintifyListingImportService` under `src/FusionCanvas.Application/Listings/` (or a focused `Listings/Import/` folder) for loading the product preview, running selected duplicate checks, and importing selected products.
- Keep Printify IDs and raw/provider-specific fields outside Domain. Application results should expose stable product identity, title, description, visibility, preview image, linked state, duplicate candidates, selected outcome, progress, and safe per-product errors.
- Add a pure duplicate matcher for normalized title/description comparison against active Items in the Store. Return scored candidate records; do not mutate or auto-connect from the matcher.
- Add a per-product application workflow that revalidates Store/Niche scope and current mappings before download and commit. Use the repository snapshot for Item/group/mapping/asset changes and the workspace file store for managed artwork.
- Add an idempotent Listing-stage variant-setup use case that invokes the selected-product catalog import, resolves the matching Printify-owned Blueprint/Offering, then initializes `ItemListingConfiguration`, selected colors, Design rows, row-color membership, and representable `DesignSlotAssignment` values in the same coordinated workspace update. Preserve local Item fields and existing user-edited setup; do not add duplicate rows or overwrite conflicting local choices.
- Gate Listing operations that require `ListingProjectionBuilder` readiness with a clear missing-setup state and an action to download variant setup. Keep imported listing data visible and linked before that action.
- Generate IDs and timestamps through injected dependencies for deterministic tests. Reuse existing mapping policy and persistence constraints.

### Printify integration

- Extend the focused Printify listing read port/client rather than the broad catalog import service. Add paginated shop-product discovery, full product detail retrieval, and original print-area image download support using the existing Store-scoped credential and selected shop.
- Preserve pagination cursors/pages and reject duplicate product IDs or malformed relationships. Model product visibility separately from remote mutation state.
- Reuse or add narrowly scoped external response models for product title, description, blueprint/provider identity, variants/prices, visibility, mockup image metadata, and ordered print-area/placeholder image metadata.
- Keep all external calls read-only; do not add direct Shopify requests or image-recognition services.

### Persistence and files

- Reuse `ExternalListingMapping` with provider `Printify`, Store ID, selected shop ID, and opaque product ID. Store canonical lifecycle values in `SnapshotJson`; preserve visibility, Blueprint/Provider identity, complete variants, print-area relationships, and provenance in a versioned value serialized through the existing JSON integration/import metadata field. Set publication state from remote visibility and mark the import operation successful.
- For a new import, create the Item at Listing stage, map visible products to `ItemStatus.Published` and hidden products to `ItemStatus.Draft`, and initialize name/description from Printify. For adoption, preserve every existing local Item field except its update timestamp and attach the mapping/assets.
- Save every original print-area image as an `ExportedImage` managed Asset linked to the Item, retaining source area/placeholder order, source image identity, and safe remote provenance metadata without signed query strings or credentials. Do not treat mockup images as original artwork. On explicit variant-setup download, assign an imported finished image only where Printify area/variant identity maps unambiguously to a local Design slot. Do not store or use Printify x/y/scale/angle values; `ListingProjectionBuilder` uses the current standard calculated placement. Preserve unrepresentable layered/text artwork as assets and report incomplete assignments.
- Create or uniquely suffix `Printify yyyy-MM-dd` for the import run only when creating new Items. Include a newly created Group in the same snapshot commit as the first successful new Item, then add further successful new Items to it.
- Stage managed files before `IWorkspaceRepository.SaveAsync`; delete all staged files if any required download or snapshot save fails. Never expose remote filenames as local paths.

### Avalonia UI

- Add the command to the Niche context menu in `src/FusionCanvas.App/Views/MainWindow.axaml` and route it through the main-window/tree view model, preserving the invoking Store and Niche.
- Add a focused import dialog and view model in `src/FusionCanvas.App/` following existing Store/import dialog patterns. Provide product preview rows, individual selection, linked-product visibility toggle, explicit duplicate-check action and candidate-resolution choices, progress/cancel, and per-product result/error rows.
- Use compact labels and text status rather than color alone; maintain keyboard access and focus when the dialog closes. Disable import for linked products and mapped duplicate candidates. No local Item draft is modified before confirmation.
- Keep error messages actionable and safe: distinguish setup, retrieval, malformed product, artwork download, persistence, and cancellation/partial completion without showing credentials, headers, or raw response bodies.

### Sequencing and verification

1. Implement deterministic application contracts, duplicate matcher, pagination/detail client contracts, and focused tests.
2. Implement integration reads and safe artwork retrieval with mocked HTTP fixtures; cover all pages, malformed responses, content validation, and cancellation.
3. Implement atomic per-product workspace persistence and retry/linking behavior with temporary repository/file-store tests.
4. Add context-menu command and dialog states; add Avalonia headless coverage for construction, selection, linked toggle, duplicate choices, loading/error/progress, and focus return.
5. Add one Appium scenario pack with one real-desktop journey because the entry point is a native tree context menu followed by a multi-step selection dialog and a Listing-stage action. Use one fresh disposable workspace/database for the pack and a mocked Printify service; cover preview → import a selected product → verify the initial link → download variant setup → verify reconstructed configuration. Duplicate candidate connection and error variants remain in deterministic lower-layer/headless coverage.
6. Map each acceptance scenario in the delta spec to a focused test or the Appium journey; run `dotnet test .\FusionCanvas.sln` and strict `openspec validate` before completion review.

### Acceptance-to-verification map

| Spec scenarios | Planned evidence |
| --- | --- |
| Import action and unavailable connection | Main-window/tree view-model tests and Avalonia headless context-menu/dialog test |
| Paginated preview, linked visibility, empty and retrieval errors | Mocked Printify client pagination tests, application state tests, and headless dialog state tests |
| Explicit selection and duplicate check | Pure duplicate matcher and application tests, including cross-Niche candidates and zero-selection behavior |
| Connect/new choices and linked/non-linkable candidates | Application persistence tests for preservation, one-to-one mapping, and already-linked exclusion |
| Visible/hidden Item state, listing data, artwork, group collision, and no initial catalog/Design mutation | Application and persistence tests with an isolated repository and temporary workspace file store |
| On-demand catalog and Design setup download, idempotency, conflict behavior, and unrepresentable artwork reporting | Application/catalog/Design tests with an isolated repository and temporary workspace file store |
| Imported artwork uses standard FusionCanvas placement, not Printify-specific transforms | Projection and Printify payload tests confirming the standard calculated placement is emitted |
| Per-product failure, retry, cancellation, no partial Item, no empty group | Application/file-store tests with deterministic staged failures and repeat-import fixtures |
| Remote read-only behavior and safe image handling | Integration tests asserting HTTP methods, pagination, URL/media/size validation, malformed data, and cleanup |
| Critical native user journey | One Appium scenario pack: Niche context menu → preview → import selected product → verify link → Listing-stage variant-setup download → verify configuration. One fresh disposable workspace/database per pack; Printify service mocked. |

### Decisions not to reopen during implementation

- Printify is the only external listing source in this module; no direct Shopify importer or image-based duplicate check.
- Import is initiated from a Niche and remains read-only toward Printify.
- Already mapped products are ineligible; linked candidates are non-linkable under the current one-to-one mapping invariant.
- Duplicate checks are explicit, advisory, title/description-based, and Store-wide. Connecting preserves local Item identity and fields.
- New Items are Listing stage, with Published/Draft status derived from Printify visibility. Original print-area artwork is stored as managed local assets.
- Per-product successes survive neighboring failures; retry relies on stable product identity; group creation is dated, unique, and limited to newly created Items.
- Initial import leaves catalog and Design setup unchanged; an explicit Listing-stage action downloads the variant setup on demand.
- V1 reconstructs one finished image per print area/variant; layered/text artwork remains preserved as linked assets and the UI reports incomplete setup for affected slots.
- Printify position, scale, and rotation are ignored; imported artwork uses FusionCanvas's standard calculated placement. Later uploads may reset Printify-only adjustments.

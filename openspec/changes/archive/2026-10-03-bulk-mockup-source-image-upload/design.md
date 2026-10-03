## Context

The Mockup Template editor already owns a collection of independent local source-image drafts. A single upload action currently calls `IAssetFilePicker.PickImportFileAsync`, whose Avalonia implementation sets `AllowMultiple = false`; the view-model then reads dimensions, creates one draft, selects it, and later persists drafts sequentially through `IMockupTemplateSourceImageService`.

The generic picker is also used by the Store Assets surface, so changing its existing single-file behavior would unnecessarily widen the change. The source-image service already creates one managed Asset, source-image entry, and revision per saved draft, so multi-select does not require a domain or persistence migration.

## Goals / Non-Goals

**Goals:**

- Let creators select multiple supported raster source images from the existing focused Mockup Template upload action.
- Preserve independent per-image applicability, placement, dimensions, warnings, selection, and unsaved edits.
- Keep the current draft-first save lifecycle and partial-save diagnostics.
- Make the action discoverable and accessible as a multi-file action.
- Keep Store Assets import behavior unchanged.

**Non-Goals:**

- Batch applicability or placement assignment.
- Folder import, drag-and-drop, upload queues, cancellation of individual staged files, or reordering.
- Atomic multi-file persistence or a new batch application-service API.
- Changes to managed-file, revision, Asset, or source-image schemas.

## Decisions

### Add an additive multi-file picker method

Add `PickImportFilesAsync` to the existing `IAssetFilePicker` boundary while retaining `PickImportFileAsync` for Store Assets. The Avalonia implementation will use `OpenFilePickerAsync` with `AllowMultiple = true` and a raster-only filter matching the currently supported metadata reader. This keeps native picker ownership in the App layer and avoids a second picker abstraction for one focused workflow.

Alternatives considered:

- Replacing the single-file method with a plural return type would force unrelated Store Assets callers and all existing test fakes to change semantics.
- Adding a separate upload button would duplicate the existing workflow and make the meaning of two upload actions unclear.
- Adding a new mockup-only picker interface would protect naming purity but introduce an additional boundary without a distinct implementation or variation point.

### Stage all selected files as independent drafts

`CatalogSetupViewModel` will iterate the picker result in returned order. For each path it will independently read metadata, create a `LocalMockupSourceDraftViewModel`, and add it to `LocalSourceDrafts`. A metadata exception will be captured on that draft using the existing warning state. After the batch is staged, the first newly added draft will be selected once, avoiding repeated focus/selection churn.

No applicability or mapping values will be copied between drafts. Existing Save logic will continue to process drafts one at a time, which preserves current behavior and diagnostics when a later import fails.

### Use the existing focused editor surface

The existing `MockupTemplateEditorWindow` remains the only upload surface. Its action text becomes **Upload images…**, with accessible help text explaining that multiple supported images may be selected. The master-detail editor and archive/read-only behavior remain unchanged.

## Risks / Trade-offs

- [Native picker differences] Avalonia and Windows may expose different multi-select affordances → keep the native call simple, cover the contract with deterministic fakes, and perform an optional live Windows check.
- [Partial persistence] Existing sequential Save can save earlier files before a later failure → preserve and test the current count-and-failure diagnostic rather than implying atomicity.
- [Unsupported files] A picker extension filter is not a complete decoder guarantee → retain the existing per-draft metadata warning and service validation path.
- [Interface fan-out] Adding one method requires existing picker fakes and null implementations to implement it → update all compile-time implementations and retain the old method for unrelated callers.

## Migration Plan

No data migration is required. Existing templates and single-file imports remain compatible. The application update changes only the picker interaction and draft staging path; existing saved source-image rows are loaded and edited unchanged.

Rollback is a code rollback. Any drafts already saved before rollback remain valid because the persistence model is unchanged.

## Open Questions

None. The approved behavior is to use the existing action with multi-select, select the first newly staged row, preserve independent metadata, and retain existing partial-save semantics.

## Implementation Plan

1. Extend `IAssetFilePicker`, `AvaloniaAssetFilePicker`, `NullAssetFilePicker`, and test fakes with a plural import method using a raster-only filter and `AllowMultiple = true`; leave the existing single-file method unchanged.
2. Refactor `CatalogSetupViewModel` source staging so metadata reading and draft creation can be applied to each returned path, add all drafts, then select the first new draft and notify the existing draft/command state once.
3. Update `MockupTemplateEditorWindow.axaml` upload text and accessible help text without changing the focused master-detail layout or archive/read-only bindings.
4. Add framework-free view-model tests for multiple paths, independent drafts, first-row selection, per-file metadata warnings, and the existing single-file behavior.
5. Add or update a deterministic Avalonia headless journey proving the rendered upload action stages multiple rows and preserves the selected-row editor boundary. The native Windows picker itself is supplemental live evidence only.
6. Run focused tests, the solution baseline `dotnet test .\FusionCanvas.sln`, and strict OpenSpec validation. Record criterion-level results in `verification.md`.

## Planned Verification Mapping

| Acceptance scenario | Planned verification |
| --- | --- |
| Creator selects multiple source images | View-model test with a deterministic plural picker; headless editor journey observes multiple rows. |
| Creator selects one source image | Existing single-file view-model test plus regression run. |
| Bulk selection preserves per-image independence | View-model draft-state assertions and selected-row headless interaction. |
| One selected file has unreadable metadata | View-model test with per-path metadata failure. |
| Creator saves a mixed bulk selection | Existing partial-save test extended to a bulk-staged set; focused application tests. |
| Archived Store is reviewed | Existing headless/read-only coverage plus source inspection of disabled command state. |

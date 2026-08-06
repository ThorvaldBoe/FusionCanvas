## Why

Store management currently has no place to record the store's URL, so users cannot capture the storefront address alongside other store context in the store editor. Issue #142 requests a URL field in store settings so the storefront link is retained as part of the store's persisted context.

## Origin

Primary issue: #142 — https://github.com/ThorvaldBoe/FusionCanvas/issues/142

## What Changes

- Add an optional `Url` field to the store editor's Basic info tab, so a store can capture its storefront URL when created or edited.
- Persist the URL with the store as part of its store-level context (metadata), surviving workspace reload and separate from child niches, groups, and listings.
- Wire the URL through create and update flows, including unsaved-changes detection, draft preview, and restoring fields when switching stores.
- No new capability or marketplace integration is introduced; this is a single optional text field on the existing store context surface.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `store-management`: extend the store-editor Basic info tab and store context so users can record and edit an optional storefront URL alongside the existing description, notes, target market, brand direction, and planning context.

## Impact

- `FusionCanvas.Domain` — no structural change; the URL is opaque store context carried through `MetadataJson` via the existing `Store` metadata mechanism.
- `FusionCanvas.Application` — `StoreContext` gains an optional `Url` member; `StoreManagementService` reads/writes the URL in `ToContext`/`ToMetadataJson` as a metadata key (same pattern as `notes`, `targetMarket`, etc.).
- `FusionCanvas.App` — `StoreManagementViewModel` adds a `Url` editor property and includes it in `EditorState`, `CurrentContext()`, `ApplySelectedStoreFields`, `ClearEditorFields`, and the draft summary; `StoreEditorWindow.axaml` adds a URL text box in the Basic info tab following the existing `field` styling.
- Tests — `FusionCanvas.Application.Tests` store management create/update/round-trip coverage for the URL; `FusionCanvas.App.Tests` view-model coverage for URL field binding, unsaved-changes detection, and field restore.
- Workspace databases created without a URL remain compatible: the URL key is simply absent from `MetadataJson`, matching existing optional context fields.
## Context

The store editor's Basic info tab currently captures an optional description, notes, target market, brand direction, and planning context for a store. These context values are stored in `Store.MetadataJson` as a JSON object keyed by string constants (`notes`, `targetMarket`, `brandDirection`, `planningContext`); description is a first-class column. The same metadata mechanism is used consistently for create (`StoreManagementCreateRequest`), update (`StoreManagementUpdateRequest`), and read back in `StoreContext`.

Issue #142 is satisfied by adding an optional storefront URL to the same editor surface and persisting it with the store. No marketplace integration, validation policy, or new capability is needed.

## Goals / Non-Goals

**Goals:**
- Add an optional `Url` member to the Application `StoreContext` record.
- Persist the URL as a `url` key in `Store.MetadataJson`, matching the existing optional context fields.
- Surface a URL text box in the store editor Basic info tab following the existing `field` styling and `PlaceholderText` convention.
- Include the URL in the view model's editor state so unsaved-changes detection, draft preview, field restore, and create/update requests behave consistently with the other context fields.
- Verify persistence round-trips (create stores a URL, update changes it, reload preserves it) and that the URL is optional.

**Non-Goals:**
- Storefront URL validation/format checking (matching the current non-validating context fields).
- Linking or opening the URL from the app, displaying the URL in the store selector, or any marketplace integration.
- Reordering or redesigning the Basic info tab.

## Decisions

**Decision: Reuse the existing `MetadataJson` key-value context mechanism for the URL.**
Rationale: The URL is opaque, optional, store-scoped context exactly like notes, target market, brand direction, and planning context. Storing it as a `url` key avoids a schema migration and a new column, matches the established pattern, and keeps existing workspace databases fully backward compatible (a missing key simply reads back as `null`).
Alternative considered: A dedicated `Url` column on the store. Rejected because it introduces a migration and diverges from how the sibling context fields are handled without benefit; the URL is not queried or indexed, and no requirement needs a first-class column.

**Decision: Add `Url` to the `StoreContext` record last (as a new optional positional parameter).**
Rationale: Adding it as the final parameter keeps existing constructor call sites that pass context positionally source-compatible, and `null` default preserves behavior for callers that do not set it. This minimizes churn across the application layer.

**Decision: Store the URL as metadata key `url` and normalize it like other optional context fields.**
Rationale: `StoreManagementService.ToMetadataJson`/`ToContext` already own the key mapping and normalization (`NormalizeOptional` trims and treats whitespace-only as `null`). Adding one key follows the exact same path, so empty input is stored as absent and whitespace input is trimmed.

**Decision: Wire the URL through `StoreManagementViewModel`'s existing editor-state plumbing.**
Rationale: The view model already centralizes the field lifecycle (`EditorState` for dirty detection, `CurrentContext()` for the request payload, `ApplySelectedStoreFields` for restore/select, `ClearEditorFields` for drafts, and `DraftStore()`/`CurrentEditorState()` for preview). Adding a `_url`/`Url` property into these existing points means unsaved-changes detection, discard prompts, and field restore work automatically without special-casing.

## Risks / Trade-offs

- [Positional-parameter string drift] New optional parameter at record end is low risk, but future additions after it could churn call sites. → Mitigation: keep additions to the tail and rely on existing tests to pin the `StoreContext` shape.
- [Wide editor stays compatible but URL field could be missed visually] → Mitigation: place the URL text box adjacent to the other Basic info fields with a clear `PlaceholderText` ("Store URL") and the standard `field` class, consistent with the existing layout; no width/behavior change.
- [No validation means arbitrary text is accepted as a URL] → Mitigation: explicitly out of scope and consistent with existing optional context fields; no acceptance scenario requires URL format checking.

## Implementation Plan

### Affected layers and files

- **Application**
  - `src/FusionCanvas.Application/Stores/StoreContext.cs` — add `string? Url = null` as the final positional parameter.
  - `src/FusionCanvas.Application/Stores/StoreManagementService.cs` — add `private const string UrlKey = "url";`, read it in `ToContext`, and write it in `ToMetadataJson` via `SetOptional(metadata, UrlKey, context.Url)`.
- **App**
  - `src/FusionCanvas.App/Stores/StoreManagementViewModel.cs`:
    - Add `private string _url = string.Empty;` field.
    - Add `public string Url { get; set; }` property that calls `RaiseEditorStateProperties()` on change (mirroring `Notes`, `TargetMarket`, etc.).
    - In `EditorState` record (line ~99) add `string Url`; update `EmptyEditorState()` and `CurrentEditorState()` accordingly.
    - In `CurrentContext()` include `EmptyToNull(Url)` at the end.
    - In `ApplySelectedStoreFields` set `Url = store.Context.Url ?? string.Empty;`.
    - In `ClearEditorFields` set `Url = string.Empty;`.
  - `src/FusionCanvas.App/Stores/StoreEditorWindow.axaml` — add a `<TextBox Text="{Binding Url}" PlaceholderText="Store URL" Classes="field" />` in the Basic info tab near the other context fields (e.g., after the Description field or grouped logically; consistent with existing field placement).

### Sequencing

1. Application layer: `StoreContext` + `StoreManagementService` (key round-trip).
2. App layer: view model field + editor-state wiring, then XAML field.
3. Tests: application service round-trip, view model field handling.

### Edge cases

- `null`/whitespace URL → normalized to not persisted (key absent), read back as `null`.
- Previously saved stores with no `url` key → `GetValueOrDefault($"{UrlKey}")` returns `null` → `Url` empty string in the editor.
- Editing only the URL on an existing store → update persists the URL while other context is preserved (the full `StoreContext` is rebuilt from current editor state, matching existing save behavior).
- Archived stores: URL editable only through the same Basic info flow that existing context uses; no special handling added.

### Verification approach

| Acceptance scenario | Verification method |
| --- | --- |
| User creates a store with a URL | Application service test asserting created store's `Context.Url` equals input; `StoreManagementViewModelTests` create flow. |
| User edits a store URL | Application service update test changing the URL and asserting persistence; view-model test editing the field and saving. |
| Store URL is optional | Application service test creating a store without URL yields `Context.Url == null` and success; view-model test that an unchanged existing store with an empty URL keeps Save disabled, and a new-store draft saves successfully with an empty URL. |
| Store URL is workspace- and store-scoped | Application service test asserting URL belongs to the created store only (no other store gets it); existing store-scoping tests continue to pass. |
| Persistence survives reload | Use an existing round-trip persistence test/in-memory or SQLite workspace repository test demonstrating the URL is present after re-load, consistent with how other context fields are covered. |

- Deterministic `dotnet test .\FusionCanvas.sln` is the baseline.
- Headless Avalonia view tests: the URL field is a plain `TextBox` bound to a view-model string with no construction, focus, selection, or visual-tree risk beyond what other fields already exercise; the existing `StoreEditorHeadlessTests` cover editor construction. Add a binding/state assertion for the URL only if it carries meaningful framework risk — otherwise rely on the framework-free view-model tests and the existing headless suite. No live desktop check is required.

## Open Questions

None.
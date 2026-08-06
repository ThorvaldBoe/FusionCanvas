## 1. Application layer

- [x] 1.1 Add `string? Url = null` as the final positional parameter on `StoreContext` in `src/FusionCanvas.Application/Stores/StoreContext.cs`.
- [x] 1.2 In `src/FusionCanvas.Application/Stores/StoreManagementService.cs`, add `private const string UrlKey = "url";`.
- [x] 1.3 In `StoreManagementService.ToContext`, read the URL via `GetValueOrDefault(UrlKey)` and pass it as the final argument of the returned `StoreContext`.
- [x] 1.4 In `StoreManagementService.ToMetadataJson`, write the URL via `SetOptional(metadata, UrlKey, context.Url)`.

## 2. App layer (view model + UI)

- [x] 2.1 In `src/FusionCanvas.App/Stores/StoreManagementViewModel.cs`, add `private string _url = string.Empty;` and a `public string Url` property using the `if (SetField(ref _url, value)) { RaiseEditorStateProperties(); }` getter/setter guard pattern (mirroring `Notes`/`TargetMarket`).
- [x] 2.2 Add `string Url` to the `EditorState` record and update `EmptyEditorState()` and `CurrentEditorState()` to include it.
- [x] 2.3 Add `EmptyToNull(Url)` to `CurrentContext()` as the final argument.
- [x] 2.4 Set `Url = store.Context.Url ?? string.Empty;` in `ApplySelectedStoreFields` and `Url = string.Empty;` in `ClearEditorFields`.
- [x] 2.5 In `src/FusionCanvas.App/Stores/StoreEditorWindow.axaml`, add `<TextBox Text="{Binding Url}" PlaceholderText="Store URL" Classes="field" />` in the Basic info tab alongside the other context fields.

## 3. Tests

- [x] 3.1 Add/extend `FusionCanvas.Application.Tests` store-management coverage: creating a store with a URL persists it; updating a store's URL changes it; omitting the URL leaves it `null` and saves successfully; URL belongs only to the created store.
- [x] 3.2 Add/extend `FusionCanvas.App.Tests` view-model coverage: the URL field is bound, edits mark unsaved changes and are included in the create/update payload, applying a store restores its URL, and clearing editor state blanks the URL.
- [x] 3.3 Verify persistence round-trip (URL present after workspace reload) using the deterministic persistence/repository test used for the other context fields.

## 4. Verification

- [x] 4.1 Run criterion-level checks mapped in `design.md` Verification approach for each acceptance scenario.
- [x] 4.2 Run strict OpenSpec validation: `openspec validate add-url-to-store --strict` and `openspec validate --all --strict`.
- [x] 4.3 Run the solution baseline: `dotnet build .\FusionCanvas.sln` and `dotnet test .\FusionCanvas.sln` pass.
- [x] 4.4 Record final acceptance-scenario evidence in `verification.md`.
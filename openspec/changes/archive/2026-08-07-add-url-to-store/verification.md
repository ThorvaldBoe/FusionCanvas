# add-url-to-store Verification

## Criterion-level evidence

| Acceptance scenario | Method | Result | Evidence |
|-|-|-|-|
| User creates a store with a URL | Application service test `CreateStoreAsync_PersistsUrlInMetadata`; view-model test `StoreEditor_UrlIncludedInCreatePayload` | Pass | URL persisted as `"url":"https://mystore.example.com"` in metadata; `result.Store?.Context.Url` equals input; view-model `SelectedStore?.Context.Url` equals input after save |
| User edits a store URL | Application service test `UpdateStoreAsync_ChangesUrl`; view-model test `StoreEditor_UrlIncludedInUpdatePayload` | Pass | Updated URL `"https://updated.example.com"` present in metadata and `result.Store?.Context.Url`; view-model field updates payload correctly |
| Store URL is optional | Application service test `CreateStoreAsync_OmittingUrl_LeavesContextUrlNullAndSavesSuccessfully`; view-model tests `StoreEditor_UnchangedExistingStoreWithEmptyUrl_KeepsSaveDisabled` and `StoreEditor_NewStoreDraftSavesSuccessfullyWithEmptyUrl` | Pass | `result.Store?.Context.Url` is `null`; `"url"` key absent from metadata; empty-Url existing store keeps Save disabled; new-store draft with empty URL saves successfully with `Context.Url == null` |
| Store URL is workspace- and store-scoped | Application service test `StoreUrl_IsScopedToCreatedStore` | Pass | Two stores each retain their own distinct URL; no cross-contamination |
| Persistence survives reload | Application service test `StoreUrl_SurvivesReload` (loads from InMemoryWorkspaceRepository after store creation) | Pass | `store.Context.Url` matches the URL set at creation time after `LoadAsync` |
| URL field is bound and editing marks unsaved changes | View-model test `StoreEditor_UrlFieldIsBoundAndEditingMarksUnsavedChanges` | Pass | Default `Url == string.Empty` and `HasUnsavedChanges == false`; after setting URL, `HasUnsavedChanges == true`, `CanSaveSelectedStore == true` |
| Applying a store restores its URL into the field | View-model test `StoreEditor_ApplyingStoreRestoresUrlIntoField` | Pass | After `SelectStoreForEditing`, `Url` property equals the store's persisted URL |
| Clearing editor state blanks the URL | View-model test `StoreEditor_ClearingEditorStateBlanksUrl` | Pass | After `StartCreateStore`, `Url == string.Empty` |

## Validation gates

| Gate | Result | Evidence |
|-|-|-|
| `openspec validate add-url-to-store --strict` | Pass | Change is valid |
| `openspec validate --all --strict` | Pass | 45/45 passed |
| `dotnet build .\FusionCanvas.sln` | Pass | 0 errors |
| `dotnet test .\FusionCanvas.sln` | Pass | Domain: 188/188, Application: 334/334, Integration: 173/173, App: 437/437 |

## File changes

| File | Change |
|-|-|
| `src/FusionCanvas.Application/Stores/StoreContext.cs` | Added `string? Url = null` as final positional parameter |
| `src/FusionCanvas.Application/Stores/StoreManagementService.cs` | Added `UrlKey = "url"` constant; read in `ToContext`; write via `SetOptional` in `ToMetadataJson` |
| `src/FusionCanvas.App/Stores/StoreManagementViewModel.cs` | Added `_url` field, `Url` property; added to `EditorState`, `EmptyEditorState()`, `CurrentEditorState()`, `CurrentContext()`, `ApplySelectedStoreFields`, `ClearEditorFields` |
| `src/FusionCanvas.App/Stores/StoreEditorWindow.axaml` | Added `<TextBox Text="{Binding Url}" PlaceholderText="Store URL" Classes="field" />` in Basic info tab |
| `tests/FusionCanvas.Application.Tests/Stores/StoreManagementServiceTests.cs` | Added 5 tests: URL persistence, URL update, URL omitted, URL scoped, URL reload |
| `tests/FusionCanvas.App.Tests/StoreManagementViewModelTests.cs` | Added 7 tests: URL binding/unsaved-changes, URL in create payload, URL in update payload, URL restore on apply, URL blanked on clear, empty URL save-disabled, new-store draft with empty URL |

## Limitations

- No URL format validation (intentionally out of scope).
- No link-opening or storefront navigation (intentionally out of scope).
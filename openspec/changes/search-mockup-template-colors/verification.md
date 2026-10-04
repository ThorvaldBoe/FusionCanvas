# Verification: Search Mockup Template Colors

## Acceptance criteria

| Criterion | Evidence | Result |
| --- | --- | --- |
| Color applicability choices can be searched while typing, with case-insensitive substring matching and canonical order preserved. | `CatalogSetupViewModelTests.TemplateColorSearchFiltersCaseInsensitivelyAndRestoresOriginalOrder`; `StoreEditorHeadlessTests.MockupTemplateColorSearchFiltersChoicesAndShowsNoMatchGuidance` verifies the bound textbox and visible `Colors: Lime` result. | Pass |
| Clearing or entering only whitespace restores every eligible Color choice. | `CatalogSetupViewModelTests.TemplateColorSearchFiltersCaseInsensitivelyAndRestoresOriginalOrder` asserts the filtered collection equals `TemplateColorChoices` after a whitespace query; the headless test clears the textbox and observes all three checkboxes. | Pass |
| A non-empty query with no matches shows guidance and leaves the search usable. | `CatalogSetupViewModelTests.TemplateColorSearchShowsNoMatchStateAndPreservesHiddenSelection`; headless assertion checks `Catalog.MockupColorSearchNoResults` and its guidance text while the textbox remains enabled. | Pass |
| A selected Color hidden by filtering remains selected in draft/readiness/save inputs and reappears selected when matched again. | `CatalogSetupViewModelTests.TemplateColorSearchShowsNoMatchStateAndPreservesHiddenSelection` asserts selected IDs remain in canonical `TemplateColorChoices`, readiness does not report a missing Color, and the hidden `Lime` choice remains selected when it reappears. Existing consumers continue to read `TemplateColorChoices` (lines 987, 1096, 1165, 1219, 1238, 1375, 1404, 1450, 2189, 2381, 2392, 2407, 2465, 2537, and 2548). | Pass |
| Search text is transient and not persisted across editor sessions. | The same view-model test starts a new template session after searching and asserts `TemplateColorSearchText` is empty and no-match state is false. The implementation only resets the presentation property in `BeginEditTemplate`, `BeginNewTemplate`, and `EndTemplateDraft`; no persistence model or save payload was changed. | Pass |

## Quality gates

- `openspec validate --all --strict`: passed, 76/76 items.
- Focused search tests: passed, 3/3 tests in the `TemplateColorSearch` filter.
- Full baseline `dotnet test .\FusionCanvas.sln`: 2,141 tests passed and 1 existing unrelated headless test failed: `StoreEditorHeadlessTests.MockupSourceRow_SelectsFromCellsWhitespaceAndKeyboard_WithoutArchiving`. The failure reproduces when isolated and concerns source-row pointer selection (`first.png` expected, `second.png` selected); it does not touch the changed search state or controls.
- Scope review: Color eligibility, canonical selection consumers, additional options, save/readiness logic, read-only checkbox behavior, and persistence models were left unchanged. No Appium journey was added because this is a low-risk deterministic local presentation interaction covered by view-model and Avalonia headless tests.

## Context

The Mockup Template editor currently binds its Color checkbox list directly to `CatalogSetupViewModel.TemplateColorChoices`. `RebuildChoices` constructs that collection from active Offering Color values and, when a provider mockup is selected, restricts it to the provider-supported Color IDs. Selection-dependent operations already read `TemplateColorChoices`, so replacing that canonical collection with a filtered view would risk omitting selected values from saves, readiness checks, dirty-state comparisons, and local source-image metadata.

The requested workflow is a frequent, focused-editor action: a creator configures Color applicability while editing a Mockup Template and needs to locate one or more values in a long list. The search belongs beside the existing Color choices in the focused editor, not in the main Store Editor or in persisted catalog data.

## Goals / Non-Goals

**Goals:**

- Make eligible Color choices searchable as the user types.
- Preserve the current provider eligibility rules, ordering, checkbox selection behavior, draft protection, save behavior, and readiness evaluation.
- Keep filtering transient and local to the editor session.
- Provide a readable no-match state and an automation identifier for headless verification.
- Preserve accessibility through a labelled search control and unchanged checkbox labels/state.

**Non-Goals:**

- Searching Size or other option choices.
- Changing provider-supported Color eligibility, sorting, or catalog data.
- Persisting search history, search text, or a new preference.
- Adding tokenized search, fuzzy ranking, color swatches, or a new search abstraction.
- Adding a real-desktop Appium journey; this is a low-risk local presentation interaction covered deterministically.

## Decisions

### Keep canonical choices separate from filtered presentation choices

`TemplateColorChoices` remains the complete eligible collection and continues to be the only collection used by save, readiness, draft-state, and source-image applicability logic. Add a separate `FilteredTemplateColorChoices` collection for the `ItemsControl`.

This avoids selection loss when a checked value is temporarily hidden and keeps existing business-facing view-model code unchanged. An Avalonia collection-view filter was considered, but an explicit collection is easier to exercise in framework-free view-model tests and keeps compiled bindings straightforward.

### Filter by displayed label with case-insensitive substring matching

The displayed label is already formed as `OptionName: Value` (for example, `Colors: Lime`). The filter trims surrounding whitespace from the query and uses ordinal case-insensitive substring matching against that label. An empty-after-trimming query restores every canonical choice in the original order.

This is predictable for a long catalog list and does not introduce ranking or provider-specific search semantics.

### Keep selection state on the existing choice objects

Filtering copies references to the existing `OptionValueChoiceViewModel` instances rather than creating new choice objects. Existing `IsSelected` bindings and `ChoiceSelectionChanged` subscriptions therefore remain valid, and hidden selected values remain part of the canonical collection.

### Make no-match state explicit and non-destructive

The editor will show a `TextBlock` when the query is non-empty after trimming and the filtered collection is empty. The text will explain that no Colors match and suggest clearing or changing the search. The search box remains enabled for editable and read-only sessions because searching does not mutate data; checkboxes and save actions continue to follow `CanEdit`.

## Risks / Trade-offs

- **[Risk]** Rebuilding choices during provider-image changes could reset the filtered list unexpectedly. → **Mitigation:** rebuild the canonical collection first, then reapply the current search query; retain selected IDs exactly as today.
- **[Risk]** Future code may accidentally read the filtered collection for persistence. → **Mitigation:** keep all existing selection consumers on `TemplateColorChoices` and add tests proving a hidden selected value remains in save/readiness inputs.
- **[Risk]** A very large list may cause extra collection updates while typing. → **Mitigation:** this is an in-memory list already materialized for the editor; use one small filtered-collection replacement per text change and avoid asynchronous work.

## Migration Plan

No data migration or compatibility handling is required. The search query is transient presentation state, and existing templates retain their current persisted configuration. Rollback consists of removing the search property, filtered collection binding, and related tests.

## Open Questions

None. The interaction is bounded to the focused Mockup Template editor, and the matching, selection, persistence, and verification behavior are resolved above.

## Implementation Plan

1. **View-model state and filtering**
   - Update `src/FusionCanvas.App/Stores/CatalogSetupViewModel.cs` with a transient `TemplateColorSearchText` property, a `FilteredTemplateColorChoices` observable collection, and derived no-match state as needed by the view.
   - Add a focused filter-refresh method that trims the query, matches `OptionValueChoiceViewModel.Label` with `StringComparison.OrdinalIgnoreCase`, and replaces only the filtered collection while preserving canonical object references.
   - Call the refresh method after `RebuildChoices` rebuilds canonical Color choices and whenever the search text changes.
   - Leave all save, readiness, dirty-state, and source-image applicability consumers on `TemplateColorChoices`.

2. **Focused editor presentation**
   - Update `src/FusionCanvas.App/Stores/MockupTemplateEditorWindow.axaml` to place a labelled `TextBox` above the Color list with a stable automation identifier and `PlaceholderText="Search Colors"`.
   - Bind the `ItemsControl` to `FilteredTemplateColorChoices` and add the no-match guidance below or beside the list without changing additional-option layout.
   - Keep the search usable in read-only sessions while preserving existing `CanEdit` behavior for checkboxes and save controls.

3. **Deterministic tests**
   - Add view-model coverage in `tests/FusionCanvas.App.Tests/CatalogSetupViewModelTests.cs` for empty-query restoration, case-insensitive substring filtering, ordering, no-match state, and hidden-selection preservation.
   - Add or extend an Avalonia headless test in `tests/FusionCanvas.App.Tests/StoreEditorHeadlessTests.cs` (or the focused Mockup Template editor test location if a more direct fixture exists) to verify the search textbox binding, automation identity, visible filtered choices, and no-match guidance.

4. **Verification**
   - Run focused App tests, strict OpenSpec validation, and the full solution baseline `dotnet test .\FusionCanvas.sln`.
   - Review the changed scope for accidental persistence, provider-eligibility, additional-option, accessibility, or read-only behavior drift.

## Acceptance-to-Verification Mapping

| Acceptance scenario | Planned verification |
| --- | --- |
| User searches Color applicability choices | View-model test for case-insensitive substring matching and order; headless binding test for visible choices |
| User clears the Color search | View-model test for empty/whitespace query restoring all choices and states |
| Search has no matching Colors | View-model no-match state test and headless guidance assertion |
| Hidden selected Colors remain part of the draft | View-model test asserting canonical selection and save/readiness inputs remain unchanged while filtered |
| Color search is not persisted | View-model/editor lifecycle test asserting a fresh session starts with an empty query; no persistence changes expected |

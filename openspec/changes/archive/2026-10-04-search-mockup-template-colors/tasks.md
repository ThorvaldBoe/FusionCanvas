## 1. View-model filtering state

- [x] 1.1 Add transient `TemplateColorSearchText`, `FilteredTemplateColorChoices`, and no-match presentation state to `CatalogSetupViewModel` without changing the canonical `TemplateColorChoices` collection.
- [x] 1.2 Implement trimmed, case-insensitive substring filtering against displayed Color labels, preserving canonical order and existing choice-object selection state.
- [x] 1.3 Reapply the current query after every canonical Color-choice rebuild and verify save, readiness, dirty-state, and source-image applicability consumers still read the canonical collection.

## 2. Mockup Template editor UI

- [x] 2.1 Add the labelled Color search `TextBox` with the planned placeholder and stable automation identifier above the Color choices.
- [x] 2.2 Bind the Color `ItemsControl` to `FilteredTemplateColorChoices` and add the explicit no-match guidance state.
- [x] 2.3 Preserve search usability for read-only Stores while keeping Color checkbox and save mutability governed by the existing `CanEdit` state.

## 3. Focused automated coverage

- [x] 3.1 Add view-model tests for case-insensitive substring matching, empty/whitespace-query restoration, original ordering, and no-match state.
- [x] 3.2 Add a regression test proving a selected Color hidden by the query remains selected in canonical draft/save/readiness inputs and reappears selected when the query is cleared.
- [x] 3.3 Add or extend an Avalonia headless Mockup Template editor test covering the search binding, automation identity, filtered visible choices, and no-match guidance.

## 4. Criterion-level verification and quality gates

- [x] 4.1 Run each acceptance scenario from `specs/mockup-template-management/spec.md` against its planned focused or headless test and record the evidence in the completion verification artifact.
- [x] 4.2 Run `openspec validate --all --strict`; correct any proposal, spec, design, or task artifact issue and rerun validation until it passes.
- [x] 4.3 Run the focused App tests, then the full baseline `dotnet test .\FusionCanvas.sln`; investigate and fix any regression before completion. (The baseline has one isolated unrelated existing headless failure documented in `verification.md`.)
- [x] 4.4 Review the final diff for accidental persistence, provider-eligibility, additional-option, accessibility, read-only, or scope drift and update the approved artifacts if a requirement-level correction is needed.

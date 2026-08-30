## 1. Catalog mutation semantics

- [x] Add shared active same-Option normalized duplicate validation to Option Value creation and update.
- [x] Ensure successful Option Value update changes only the existing record's display name and returns refreshed state.
- [x] Add application tests covering valid Color/Size rename, blank/duplicate rejection, stable identity, and unchanged Variant/template references.

## 2. Management dialog editing

- [x] Add edit draft state and commands to `CatalogSetupViewModel`, including save, cancel, reset, notifications, and mutation error handling.
- [x] Add accessible Edit controls and the focused edit form to `OptionValueManagementWindow`, including keyboard focus and close/cancel cleanup.
- [x] Add focused App tests for Color/Size command parity, draft cancellation, and refreshed presentation state.

## 3. Verification and completion

- [x] Run focused application and App tests; correct any failures.
- [x] Run `dotnet test .\\FusionCanvas.sln` and `openspec validate`.
- [x] Create `verification.md` mapping every delta-spec scenario to method, result, evidence, and limitations.
- [x] Complete the implementation learning review in `retrospective.md`.

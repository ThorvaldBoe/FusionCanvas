## 1. OpenSpec and presentation contract

- [x] 1.1 Validate the proposal, delta spec, design, and task package; resolve any schema errors before implementation.
- [x] 1.2 Confirm the slot interaction contract: drag/drop primary, visible browse/replace alternative, inline thumbnail, enlarge/download/remove, read-only behavior, PNG errors, and category separation.

## 2. Design-stage slot controls

- [x] 2.1 Add derived slot labels/state for Browse/Replace and explicit final-artwork action accessibility.
- [x] 2.2 Update the Main window Design slot markup with an enabled drop target, clear empty guidance, per-slot browse/replace, enlarge, download, remove, and tooltips/automation names.
- [x] 2.3 Implement the per-slot browse picker and route valid selections through the existing assignment/replacement service while preserving recoverable error and busy behavior.

## 3. Verification coverage

- [x] 3.1 Add focused Avalonia headless assertions for empty drop guidance, browse/replace availability, enlarge/download/remove discoverability, read-only disabling, and Supporting Images separation.
- [x] 3.2 Add or adapt application-service coverage for independent multi-slot assignments and reload restoration if the existing tests do not fully cover the criterion.
- [x] 3.3 Run focused tests and the full solution baseline: `dotnet test .\\FusionCanvas.sln`.
- [x] 3.4 Run strict OpenSpec validation and complete `verification.md` with criterion-level evidence and limitations.

## 4. Delivery

- [x] 4.1 Archive the completed OpenSpec change after verification.
- [ ] 4.2 Commit the scoped implementation and archived artifacts, push the branch, create a PR against `main`, merge when mergeable, verify merged state, and close issue #276.

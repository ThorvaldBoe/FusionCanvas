## 1. OpenSpec and application contract

- [x] 1.1 Add the duplicate request/result contract and service method in the Application mockup-template setup contracts.
- [x] 1.2 Implement validated, atomic duplication of the current template configuration, active source-image state, new mutable identities, shared immutable Assets, and deterministic collision-safe name.

## 2. Application verification

- [x] 2.1 Add focused tests for configured duplication, copied applicability/mappings, new identities, name collisions, missing/out-of-scope sources, archived/read-only Stores, and unchanged source state.
- [x] 2.2 Add an isolation test proving replacement/archive of a duplicate source entry creates duplicate-only state and leaves the original template unchanged.

## 3. Focused editor integration

- [x] 3.1 Add a Duplicate command to `CatalogSetupViewModel` that invokes the service, applies state, selects the new template, and opens it through the existing focused editor draft flow.
- [x] 3.2 Add the duplicate action to the Mockup Template card UI with clear automation text and coherent disabled/read-only behavior.
- [x] 3.3 Add or extend view-model/headless tests for duplicate command behavior and editor initialization where meaningful Avalonia framework behavior is involved.

## 4. Verification and completion artifacts

- [x] 4.1 Run criterion-level focused tests and record every acceptance scenario, result, evidence, and limitation in `verification.md`.
- [x] 4.2 Run strict OpenSpec validation and resolve any artifact/spec issues.
- [x] 4.3 Run `dotnet test .\FusionCanvas.sln` and record the baseline result in `verification.md`.
- [x] 4.4 Complete the learning review/retrospective, confirm no unresolved scope or acceptance gaps, and mark the change ready for archive.

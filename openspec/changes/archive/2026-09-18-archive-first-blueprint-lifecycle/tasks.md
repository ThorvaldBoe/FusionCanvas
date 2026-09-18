## 1. Application lifecycle contracts

- [x] 1.1 Add archived Blueprint state to product summaries/state without changing the meaning of the active product list.
- [x] 1.2 Add the permanent Blueprint deletion request and catalog-service contract.
- [x] 1.3 Implement guarded atomic deletion of the archived normalized Blueprint graph and compatibility projection, including protected-reference checks.
- [x] 1.4 Add application tests for active-delete rejection, archived deletion, blocker handling, reload non-recreation, and unrelated-record preservation.

## 2. Store Management behavior

- [x] 2.1 Add the default-off archived-Blueprint filter and compose the visible overview list from active plus opt-in archived summaries.
- [x] 2.2 Restrict lifecycle command eligibility by archive state and route confirmed deletion to permanent catalog deletion.
- [x] 2.3 Refresh selection, empty states, and filter-dependent property notifications after archive, delete, Store changes, and overview entry.
- [x] 2.4 Add view-model tests for filter defaults, archived selection, command visibility, cancellation, and post-delete selection.

## 3. Store Editor UI

- [x] 3.1 Add the accessible **Show archived Blueprints** checkbox and archived row treatment.
- [x] 3.2 Remove active-state Delete Blueprint and expose **Delete permanently** only for archived Blueprint details.
- [x] 3.3 Align archive and permanent-delete confirmation text and maintain keyboard/cancel behavior.
- [x] 3.4 Add or update Avalonia headless tests for overview filtering, action ownership, confirmation surfaces, and empty states.

## 4. Verification and completion

- [x] 4.1 Run focused Application/App tests and update `verification.md` with criterion-level evidence and limitations.
- [x] 4.2 Run `openspec validate --strict` and resolve any change-package validation failures.
- [x] 4.3 Run `dotnet test .\\FusionCanvas.sln` and resolve failures without expanding scope.

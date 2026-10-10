## 1. Grid presentation and provider lifecycle

- [x] 1.1 Add an aggregated full readiness detail value to `BlueprintOfferingCardViewModel` and replace the Offering card ItemsControl with a compact, read-only virtual grid whose Open button invokes the existing row command.
- [x] 1.2 Bind an `InMemoryDataProvider<BlueprintOfferingCardViewModel>` to the grid; refresh it on card collection changes and detach/reset it with the Store Editor data-context lifecycle.

## 2. Interaction and acceptance coverage

- [x] 2.1 Add deterministic Avalonia headless view coverage for aligned Offering columns, full setup/readiness help text, archived filtering, empty state, and the one-click Open action preserving the current Blueprint/Store scope.
- [x] 2.2 Complete `verification.md` with method, result, evidence, and limitations for every scenario in `specs/blueprint-offering-list/spec.md`; record the explicit Appium coverage decision and changed-scope review.

## 3. Module verification

- [x] 3.1 Run focused Offering-grid tests and the full solution baseline: `dotnet test .\FusionCanvas.sln`.
- [x] 3.2 Run strict OpenSpec validation and `git diff --check`; correct any failures and update criterion-level evidence.

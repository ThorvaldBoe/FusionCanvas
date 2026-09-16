## 1. Printify metadata resolution

- [x] 1.1 Update the Printify integration client to fetch and parse authoritative catalog Blueprint metadata for each distinct selected shop-product Blueprint ID, preserving product IDs and existing variant/placeholder details.
- [x] 1.2 Return classified provider failures from required Blueprint lookups before any repository mutation occurs.

## 2. Import naming and regression coverage

- [x] 2.1 Add the application naming rule: trimmed brand plus model when both are present, otherwise the catalog title, and use it for Blueprint creation and updates.
- [x] 2.2 Add integration and application tests for canonical naming, legacy projection synchronization, and stable repeated-import relationships; retain the existing title-fallback and atomic repository-write coverage.

## 3. Verification

- [x] 3.1 Run focused Printify client and import tests and map each modified-spec scenario to concrete evidence in `verification.md`.
- [x] 3.2 Run `openspec validate` and `dotnet test .\FusionCanvas.sln`; resolve any failures without expanding scope.

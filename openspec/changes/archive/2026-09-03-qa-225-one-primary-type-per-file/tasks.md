## 1. Inventory and structural guard

- [x] 1.1 Confirm the affected handwritten production files and document allowed framework/generated exceptions.
- [x] 1.2 Add a deterministic source-layout test that fails when a non-exempt `src/**/*.cs` file contains multiple top-level production types.

## 2. Split Domain and Application declarations

- [x] 2.1 Split bundled Domain declarations into type-named files while preserving namespaces, accessibility, members, and usings.
- [x] 2.2 Split bundled Application contracts, requests, results, settings, and policy declarations into type-named files.
- [x] 2.3 Compile the Domain/Application groups and correct only structural split errors.

## 3. Split App declarations

- [x] 3.1 Split bundled App view-model, presentation, navigation, workspace, and picker declarations into type-named files, preserving Avalonia partial exceptions.
- [x] 3.2 Compile the App group and correct only structural split errors.

## 4. Criterion-level verification and completion

- [x] 4.1 Run the structural source-layout test and manually review its complete remaining-violation output.
- [x] 4.2 Run `dotnet format --verify-no-changes` for the repository and record the result or environment limitation.
- [x] 4.3 Run `dotnet build .\\FusionCanvas.sln` and `dotnet test .\\FusionCanvas.sln`.
- [x] 4.4 Run `openspec validate --specs --strict` and `openspec validate --changes --strict`, record acceptance evidence in `verification.md`, and complete the learning review.

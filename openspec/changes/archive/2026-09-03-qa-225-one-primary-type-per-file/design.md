## Context

The accepted coding standard requires each handwritten top-level production type to live in a file named for that type. A source scan of `src/` found 48 files with two or more top-level declarations, concentrated in Application contracts/results and App view-model/presentation bundles. The change is structural only: C# namespaces, accessibility, members, generated partial relationships, and public signatures remain unchanged.

## Goals / Non-Goals

**Goals:**

- Give every affected top-level production type its own correctly named `.cs` file.
- Keep files in their existing capability folders and namespaces unless the existing folder already contradicts the type's ownership.
- Add focused, deterministic verification that reports any remaining multi-type handwritten production file.
- Prove behavior preservation with the solution build and test baseline.

**Non-Goals:**

- No feature or accepted behavior changes.
- No broad namespace migration, type redesign, warning cleanup, or dependency changes.
- No splitting of nested private implementation types, generated code, Avalonia code-behind partial pairs, or test-only fixture exceptions.

## Decisions

1. **Split declarations in place, preserving namespaces and usings.** This minimizes API and merge risk. A new file copies the original file's file-scoped namespace and only the declaration required by its type; shared usings remain explicit in each file.
2. **Use the existing source folder as the destination.** Folder moves would combine a structural cleanup with capability reorganization and make failures harder to diagnose.
3. **Treat partial and Avalonia code-behind relationships as compatibility constraints.** Partial declarations stay paired with their generated/AXAML files; code-behind exceptions allowed by the coding standard are not mechanically split.
4. **Verify structurally with a test-side source scan.** The scan will inspect `src/` without compiling or loading production types, count top-level handwritten declarations, and fail with file/type names when more than one is found. This protects the rule without adding a runtime dependency.

Alternatives considered:

- Rewriting all files with a formatter or IDE refactoring: rejected because it can alter unrelated formatting and generated code.
- Moving every type into new capability namespaces: rejected as scope expansion; the finding is file cohesion, not a namespace redesign.
- Relying only on review inspection: rejected because the rule is mechanical and cheaply enforceable.

## Risks / Trade-offs

- [Repeated usings or file headers] → Copy only required existing usings and preserve nullable/namespace context; run format/build afterward.
- [Accidental loss of attributes or accessibility] → Move complete declarations, including attributes and modifiers, and review the resulting diff by type.
- [False positives from nested/generated/test declarations] → Limit the verifier to handwritten `src/**/*.cs` and top-level declarations; preserve documented exceptions.
- [Large diff] → Keep the PR limited to this finding and the structural verifier; no behavior edits.

## Migration Plan

1. Generate the affected files from the current declarations, then remove the original bundled declarations/files only after all references compile.
2. Add and run the source-layout verification test.
3. Run `dotnet format --verify-no-changes` for the touched scope where supported, `dotnet build .\FusionCanvas.sln`, and `dotnet test .\FusionCanvas.sln`.
4. Rollback is a normal commit revert; no database or serialized-data migration exists.

## Implementation Plan

1. Inventory the 48 multi-type source files and classify valid exceptions versus handwritten production bundles.
2. Split declarations in bounded groups by capability folder, preserving all type text and required imports. Keep the original file only when it still contains a single declaration; otherwise remove it after the split.
3. Add a source-layout test under `tests/FusionCanvas.App.Tests` or the narrowest existing test project that can inspect repository source without production coupling. Make the repository root resolution deterministic for CI and local runs.
4. Run focused compile/tests after each capability group, then the solution baseline and strict OpenSpec validation.
5. Record criterion-level evidence in `verification.md`, mark tasks complete, and complete the learning review before archiving.

Acceptance-to-verification mapping (this maintenance change has no delta spec):

| Criterion | Planned verification |
| --- | --- |
| No unapproved multi-type handwritten production file remains | Structural source-layout test plus manual scan of its report |
| Existing behavior and public API remain intact | `dotnet build .\\FusionCanvas.sln` and `dotnet test .\\FusionCanvas.sln` |
| Change stays structural and scoped | Diff review and strict `openspec validate --changes --strict` |

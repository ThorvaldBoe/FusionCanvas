## 1. Query and projection

- [x] 1.1 Add explicit rating-filter comparison data to `WorkspaceTreeQuery` and update projector matching while preserving exact and unrated behavior.
- [x] 1.2 Add focused application tests for all greater-than and less-than thresholds, strict boundaries, and unrated exclusion.

## 2. Navigation surface

- [x] 2.1 Extend `WorkspaceTreeViewModel` rating-index mapping and bounds for the eight new choices, preserving immediate refresh and clear behavior.
- [x] 2.2 Add the eight comparison choices to the rating dropdown and add focused view-model coverage for the mapping.

## 3. Verification

- [x] 3.1 Run focused tests, inspect changed scope, and update `verification.md` with criterion-level evidence for every acceptance scenario.
- [x] 3.2 Run `openspec validate --strict` and `dotnet test .\FusionCanvas.sln`.

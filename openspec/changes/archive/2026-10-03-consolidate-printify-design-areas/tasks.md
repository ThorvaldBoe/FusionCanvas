## 1. Specification and import behavior

- [x] 1.1 Update the Printify import delta and design-area delta for logical grouping and aggregate dimensions.
- [x] 1.2 Replace geometry-specific grouping with position/decoration aggregation and deterministic canonical-area selection.
- [x] 1.3 Migrate offering, mockup, revision, and design-slot references before archiving superseded imported areas.
- [x] 1.4 Preserve explicit primary selection and local-only areas while maintaining compatibility synchronization.

## 2. Verification

- [x] 2.1 Add application tests for aggregate dimensions, unioned variant membership, repeat idempotence, and duplicate migration.
- [x] 2.2 Add persistence coverage for archived duplicates and migrated references.
- [x] 2.3 Run focused tests and `dotnet test .\FusionCanvas.sln -m:1`.
- [x] 2.4 Run `openspec validate --strict` and record criterion-level results in `verification.md`.

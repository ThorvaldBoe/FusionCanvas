# Default Snowclones Verification

## Status

Complete. All acceptance scenarios for the modified bundled-starter requirement have passing deterministic evidence, the full solution baseline passes, and strict change and repository OpenSpec validation pass.

## Acceptance evidence

All test names below are exact `Class.Method` names, run in worktree `C:\Code\FusionCanvas-117-default-snowclones` (branch `codex/117-default-snowclones`).

| # | Acceptance scenario (specs/snowclone-library/spec.md) | Passing automated evidence | Result |
|---:|---|---|---|
| 1 | Snowclone library initializes for the first time: atomically imports every bundled valid starter record, includes the full curated default list, appears as-if-imported, and persists the completion marker | `SnowcloneCsvCodecTests.EmbeddedStarterResource_UsesTheNormalCsvContract` (reads the shipped resource through the normal codec: 31 rows, exact curated phrase set, representative guidance, and `SnowcloneTemplatePolicy.Validate(...).IsValid` for every decoded row); `SnowcloneLibraryServiceTests.InitializeAsync_ImportsOnceAndPersistsMarker` (single read, single save, marker persisted) | PASS |
| 2 | Creator deletes an initialized default record: automatic initialization does not recreate the deleted record | `SnowcloneLibraryServiceTests.InitializeAsync_AfterStarterDeletionDoesNotResurrectIt` | PASS |
| 3 | Creator imports the bundled library explicitly: validates, imports unique records, skips existing phrases, does not overwrite local guidance, reports added/skipped counts | `SnowcloneLibraryServiceTests.ImportBundledAsync_AddsUniqueAndPreservesExistingGuidance`; `SnowcloneLibraryViewModelTests.BundledImportWithDraft_SaveAndContinuePersistsBothChanges` | PASS |
| 4 | Bundled starter data is invalid: does not mark initialization complete, does not partially import, reports a recoverable error | `SnowcloneLibraryServiceTests.InitializeAsync_InvalidBundleDoesNotSaveOrSetMarker` | PASS |

### Notes on evidence boundaries

- The shipped resource's full-set and policy validity are proven by `SnowcloneCsvCodecTests.EmbeddedStarterResource_UsesTheNormalCsvContract`, which replaced the prior `Assert.Single` with: 31 rows, an exact case-insensitive comparison of the decoded phrase set against the curated 31, representative guidance, and a per-row `SnowcloneTemplatePolicy.Validate` gate.
- Scenarios 2–4 verify unchanged service behavior and use stub bundled sources; no `SnowcloneLibraryService` or view-model code changed, so their evidence is carried from the unchanged `snowclone-library` behavior.
- The `Easily distracted by {X}` → `Easily Distracted By {Interest}` rename affects only the real embedded resource; all other references to the old phrase are stub/fixture inputs and remain valid.

## Solution baseline

`dotnet test .\FusionCanvas.sln` → all green:

- FusionCanvas.Domain.Tests: 177 passed
- FusionCanvas.Application.Tests: 270 passed
- FusionCanvas.Integration.Tests: 130 passed
- FusionCanvas.App.Tests: 366 passed

## OpenSpec validation

- `openspec validate default-snowclones --strict` → `Change 'default-snowclones' is valid`
- `openspec validate --all --strict` → 41 passed, 0 failed (including `change/default-snowclones`)

## Coordination / dependency note

`default-snowclones` modifies the bundled starter requirement of the `snowclone-library` capability and builds on the active `snowclone-library` change. This change must be archived only after `snowclone-library` has been synchronized into `openspec/specs/` or archived (same ordering rule used by `integrate-ideation-openrouter-snowclones`). No SQLite schema or migration change was introduced; all installations initialize fresh, so no upgrade path is required.

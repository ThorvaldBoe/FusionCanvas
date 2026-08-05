# Default Snowclones Verification

## Status

Complete for the opt-in bundled-import behavior contributed by this change. The automatic one-time initialization originally specified by this change was removed by the active `snowclone-library-empty-by-default` change; this verification records evidence only for the surviving explicit-import behavior and the shipped starter resource, and notes the supersession. The full solution baseline passes and strict change and repository OpenSpec validation pass.

## Acceptance evidence

Test names below are exact `Class.Method` names.

| # | Acceptance scenario (specs/snowclone-library/spec.md) | Passing automated evidence | Result |
|---:|---|---|---|
| 1 | Creator imports the bundled library explicitly: validates, imports unique records, includes the full curated default list, skips existing phrases, does not overwrite local guidance, reports added/skipped counts | `SnowcloneLibraryServiceTests.ImportBundledAsync_AddsUniqueAndPreservesExistingGuidance`; shipped resource validity via `SnowcloneCsvCodecTests.EmbeddedStarterResource_UsesTheNormalCsvContract` (reads the shipped resource through the normal codec: 31 rows, exact curated phrase set, representative guidance, and `SnowcloneTemplatePolicy.Validate(...).IsValid` for every decoded row) | PASS |
| 2 | Bundled starter data is invalid: does not import, does not partially import, reports a recoverable import error | `SnowcloneLibraryServiceTests.ImportBundledAsync_InvalidBundleFailsWithoutImporting` | PASS |

### Superseded behavior

- The earlier "Snowclone library initializes for the first time" (automatic one-time import) and "Creator deletes an initialized default record" (no-resurrection of auto-imported defaults) scenarios described automatic initialization that this change no longer contributes. That behavior was reversed by `snowclone-library-empty-by-default`, whose empty-by-default behavior is verified by `SnowcloneLibraryServiceTests.InitializeAsync_LeavesLibraryEmptyAndImportsNothing`. Do not sync this change's earlier auto-init text into the main spec; the synced `snowclone-library` spec must reflect empty-by-default per `snowclone-library-empty-by-default`.

### Notes on evidence boundaries

- The shipped resource's full-set and policy validity are proven by `SnowcloneCsvCodecTests.EmbeddedStarterResource_UsesTheNormalCsvContract` (31 rows, exact case-insensitive phrase-set comparison, per-row `SnowcloneTemplatePolicy.Validate` gate).
- The explicit bundled-import path reuses the normal import pipeline, so the atomicity/cancellation/save-failure guarantees exercised by the general `ImportAsync_*` and `SaveFailure_*` tests also apply.

## Solution baseline

`dotnet test .\FusionCanvas.sln` → all green (Domain, Application, Integration, App test projects).

## OpenSpec validation

- `openspec validate default-snowclones --strict` → valid.
- `openspec validate --all --strict` → passes (including `change/default-snowclones`).

## Coordination / dependency note

`default-snowclones` modifies the bundled starter requirement of the `snowclone-library` capability and builds on the active `snowclone-library` change. Its delta no longer claims automatic initialization; the active `snowclone-library-empty-by-default` change owns the empty-by-default behavior. Archive `default-snowclones` together with `snowclone-library-empty-by-default` (after `snowclone-library`) so the synced `openspec/specs/snowclone-library/spec.md` reflects empty-by-default plus explicit bundled import, without the superseded auto-init text. No SQLite schema or migration change was introduced.

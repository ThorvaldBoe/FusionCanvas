## Why

The application currently ships only one snowclone (`Easily distracted by {X}`). Creators want a starter vocabulary of familiar phrase structures available immediately, so generation feels useful out of the box rather than after manual import.

## What Changes

- Replace the single-row bundled starter library with a curated default set of **31 snowclones** shipped in the application and initialized once for every new application data store.
- Keep the existing behavior: defaults appear in the Snowclone Library as if they had been imported, they are permanently deletable (deleting one means it is gone and is not resurrected on later launches), and creators may explicitly import the current build's bundled set later without silently overwriting local records.
- Update the pinned integration test that currently asserts the bundled resource contains exactly one row so it verifies the full default set.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `snowclone-library`: Change the bundled starter library from a single record to a curated 31-record default set, and modify the initialization and deletion scenarios to verify the full curated set initializes together and each default is individually deletable without resurrection.

## Impact

- **Resource:** `src/FusionCanvas.Integration/Snowclones/Resources/starter-snowclones.csv` grows from 1 to 31 data rows (same `Phrase,Guidance` UTF-8 contract, same embedded resource name). The prior record is renamed to match the curated set (`Easily Distracted By {Interest}`).
- **Integration/Application:** no API, contract, or service change. `SnowcloneLibraryService.InitializeAsync` already imports the whole bundled document atomically on first open; the larger document is handled unchanged.
- **Tests:** update `SnowcloneCsvCodecTests.EmbeddedStarterResource_UsesTheNormalCsvContract` to read the full set instead of `Assert.Single`. Service/view-model tests use stub bundled sources and are unaffected.
- **No schema or migration change:** `snowclone-library` already removed the need for an upgrade path (all installations initialize fresh), so no new migration is required.
- **Dependency/coordination:** builds on the active `snowclone-library` change and must be archived only after that base change is synchronized/archived. Same ordering rule used by `integrate-ideation-openrouter-snowclones`.

This is a single, independently verifiable module: it changes one shipped resource and its acceptance test, with no behavioral surface beyond the existing one-time initialization and explicit-bundled-import mechanics.

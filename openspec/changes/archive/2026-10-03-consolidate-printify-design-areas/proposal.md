## Why

Printify exposes printable dimensions per catalog variant, while a product normally reuses the same logical artwork position across those variants. FusionCanvas currently turns each dimension group into a separate Design Area, producing repeated `front` and `back` entries that do not match the product workflow and make the largest usable area appear duplicated.

## What Changes

- Consolidate imported Printify placeholders by logical position and decoration method instead of creating one Design Area per geometry.
- Preserve every compatible imported variant on the consolidated Design Area.
- Store the largest supported width and height for each consolidated position as its local maximum dimensions.
- Keep repeated imports idempotent, preserve local relationships and stable Printify identities, and migrate references from older geometry-split imported areas before archiving them.
- Leave primary artwork selection user-controlled; consolidation does not infer a primary area from list order or position name.
- Add focused import and persistence regression coverage for variant-specific geometry consolidation and reference migration.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `printify-catalog-import`: change imported placeholder consolidation while retaining safe, Store-scoped, idempotent import semantics.
- `design-area-management`: clarify that imported Design Areas represent logical provider positions with aggregate maximum dimensions across compatible variants.

## Impact

- Affects `PrintifyCatalogImportService` and its application/integration tests.
- Affects normalized Blueprint Offering relationships when older imported duplicate areas are consolidated.
- Existing imported workspaces may contain geometry-split areas; the import path will converge those records on the next successful import without affecting unrelated local-only areas.
- No Printify publishing API is added in this change; the change remains limited to catalog import and local Design Area representation.

## Context

Printify's catalog variant response describes the printable geometry separately for each concrete variant. A single logical position such as `front` can therefore appear several times with different pixel dimensions. The local Design Area model represents a reusable logical printable region, so importing each geometry group as a separate active area creates misleading duplicates.

The import must continue to preserve variant-specific compatibility. The area dimensions are a maximum capability, so the consolidated area uses the maximum width and maximum height reported for that logical position and decoration method. It does not resize or rewrite variant records.

## Decisions

1. Group incoming placeholder observations by `(position, decoration method)` only.
2. For each group, union all local variant IDs observed in that group.
3. Set the area's width to the maximum incoming width and its height to the maximum incoming height.
4. Match an existing imported area by the same offering, provider metadata, position, and decoration method. Retain the existing ID when possible.
5. If older imports left multiple active geometry-split areas for the same logical key, select one deterministic canonical area (prefer the largest existing area, then oldest creation time, then ID), merge membership and dimensions into it, migrate all known references, and archive the other imported duplicates.
6. Migrate `DefaultPlaceholderId`, `PrimaryArtworkDesignAreaId`, mockup template targets, mockup-template revision targets, and design-slot assignments when they point at an archived duplicate. Do not select a primary area when none was selected.
7. Do not merge manual or unrelated local areas. The imported metadata and provider reference are the ownership boundary.
8. Continue to mirror active normalized areas to the legacy projection. Archived normalized duplicates remain persisted for recoverability and foreign-key safety.

## Implementation Plan

- Replace geometry-based incoming grouping in `PrintifyCatalogImportService` with logical-key aggregation.
- Add a small merge/consolidation helper in the same application service to update one canonical imported area and archive superseded imported areas.
- Apply relationship migration to the immutable `WorkspaceSnapshot` collections before the final compatibility synchronization.
- Update application tests for aggregate dimensions, full variant membership, idempotence, and migration of references from pre-existing split areas.
- Update persistence coverage to prove the consolidated area and archived duplicates round-trip without losing references.
- Run focused tests, the full solution test baseline, and strict OpenSpec validation.

## UX Notes

No new UI is required. After import, the existing Design Area list will show one active entry per logical Printify position/decoration method, with the largest supported pixel dimensions and the complete compatible-variant count. Primary artwork selection remains explicit in the existing editor.

## Risks and Mitigations

- A stale duplicate may be referenced by existing work. Migrate every in-snapshot reference before archiving and cover it with persistence tests.
- Incoming payloads may omit a variant's placeholder. Keep the existing safe import validation and only include variants actually observed in the grouped response.
- A later provider response may shrink dimensions or compatibility. Rebuild the imported group's aggregate values from the current response while retaining local-only variant memberships only until the normal imported-identity cleanup removes them.

# Verification

Verification is for the uncommitted exploration worktree on branch `codex/avalonia-ui-language-exploration`. Production Avalonia projects are unchanged.

## Acceptance evidence

| Acceptance scenario | Result and evidence |
| --- | --- |
| Supported description is loaded | PASS — parser and fixture tests load both Issue #185 YAML descriptions. |
| Unsupported schema version is supplied | PASS — parameterized validation test asserts `UIDL100` and no document reaches rendering. |
| Unknown property is supplied | PASS — parser test asserts `UIDL011`, property subject, and source path/location. |
| Complementary wireframe hierarchies are described | PASS — fixture tests assert ordered Variant regions/table and Design Areas collection/editor regions without an absolute-coordinate property. |
| Named layout tokens and variants are used | PASS — both fixtures validate; negative tests assert `UIDL107` and `UIDL109`. |
| Invalid sizing combination is supplied | PASS — validation implements negative fixed/minimum and grid-track checks before layout; focused validation coverage is in the tooling test project. |
| Duplicate component identifiers are supplied | PASS — test asserts `UIDL105`, including the first declaration location in the diagnostic. |
| Invalid component composition is supplied | PASS — closed container/leaf and explicit grid-placement validation is exercised by focused validation and fixture tests. |
| Invalid state override is supplied | PASS — test asserts `UIDL122` for an unknown target and `UIDL123` for an incompatible property. |
| Invalid source is validated repeatedly | PASS — test compares complete ordered diagnostic sequences from repeated validation. |
| Named state is rendered | PASS — projection test proves supported values change while IDs, kinds, variants, ordering, and hierarchy remain stable. |
| Unknown state is requested | PASS — projection and CLI tests assert `UIDL130`, exit `2`, and preservation of an existing destination. |
| Identical input is rendered repeatedly | PASS — renderer equality and fixture golden tests; repeated file/hash evidence recorded below. |
| Wireframe is inspected offline | PASS — XML test asserts escaped content, semantic IDs, no script/external references; both default SVGs were rasterized offline and visually inspected. |
| Fixed and flexible regions are arranged | PASS — Design Areas fixture and layout tests verify containment; its 690-unit collection track leaves the editor the remaining width after padding and the region gap. |
| Developer validates a valid description | PASS — CLI test asserts exit `0`, empty standard error, and unchanged SHA-256 of the source. |
| Developer renders a valid state | PASS — CLI test renders the provider-unavailable state and asserts exit `0`, standalone output, and UTF-8 without BOM. |
| Developer renders invalid input | PASS — CLI tests cover invalid arguments and unknown state while preserving an existing destination; command code distinguishes operational I/O failures as exit `3`. |
| Fixture outputs are reproduced | PASS — canonical SVG golden tests cover both defaults; every declared state is regenerated and compared in the repeatability check below. |
| Generated composition is compared with the source wireframe | PASS WITH DOCUMENTED GAP — `docs/ui-description-language.md` records the two side-by-side reviews. Both compositions remain recognizable; Variant Management exposes missing scroll/intentional-overflow semantics because its source has a partially clipped `Done` action. |
| Production boundary is reviewed | PASS — changed-scope inspection contains only the solution entry, tooling/tests, documentation, visuals, and this OpenSpec change; no production `src/` file changed. |

## Commands and results

| Check | Result |
| --- | --- |
| `dotnet test .\tests\FusionCanvas.UiDescription.Tests\FusionCanvas.UiDescription.Tests.csproj --no-restore` | PASS — 27 passed, 0 failed, 0 skipped. |
| `dotnet list .\tools\FusionCanvas.UiDescription\FusionCanvas.UiDescription.csproj package --vulnerable --include-transitive` | PASS — no vulnerable packages reported by configured sources. |
| `openspec validate prototype-avalonia-ui-description-language --strict` | PASS — change is valid. |
| `dotnet test .\FusionCanvas.sln` | BASELINE FAIL — the new tooling suite passed 27/27, but seven existing `FusionCanvas.App.Tests` failed (477 passed). No production or existing test file changed in this branch; the failures concern workspace multi-selection, existing headless layout assertions, and a toolbar-location assertion. They are recorded rather than changed as unrelated scope. |
| Four-state double regeneration and SHA-256 comparison | PASS — both clean outputs matched each other and the golden files: Variants default `DE91C461…49A2`, provider unavailable `24660013…8EC2`, Design Areas default `EC8845F5…E6A`, empty collection `25443D15…D369`. |

## Visual evidence

- Variant reference: `docs/Visuals/ui-descriptions/references/catalog-mockups-variants-wireframe-v1.png`
- Variant SVG and preview: `docs/Visuals/ui-descriptions/manage-variants.default.svg`, `manage-variants.default.png`
- Design Areas reference: `docs/Visuals/ui-descriptions/references/catalog-mockups-design-areas-wireframe-v2.png`
- Design Areas SVG and preview: `docs/Visuals/ui-descriptions/manage-design-areas.default.svg`, `manage-design-areas.default.png`

The preview PNGs are review conveniences rasterized from the canonical SVGs; the SVGs remain the golden outputs and source of verification.

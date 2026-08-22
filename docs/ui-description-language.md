# UI Description Language Prototype

This prototype tests a deterministic intermediate representation between a FusionCanvas wireframe and a future Avalonia implementation. A checked-in `.ui.yaml` file is parsed, strictly validated, projected to a named state, laid out with renderer-owned metrics, and serialized as a standalone SVG. It does not generate AXAML or application behavior.

## Commands

Run from the repository root:

```powershell
dotnet run --project .\tools\FusionCanvas.UiDescription -- validate .\docs\Visuals\ui-descriptions\manage-variants.ui.yaml

dotnet run --project .\tools\FusionCanvas.UiDescription -- render .\docs\Visuals\ui-descriptions\manage-variants.ui.yaml --state default --output .\docs\Visuals\ui-descriptions\manage-variants.default.svg
```

Exit code `0` means success, `2` means invalid arguments or an invalid description/state/layout, and `3` means an operational I/O failure. Diagnostics are written to standard error as `path(line,column): severity code [subject]: message`. Rendering writes beside the destination to a tool-owned temporary file and replaces the destination only after the complete SVG has been produced.

## Document shape

Every document declares:

- `schemaVersion: 1`;
- `tokenProfile: fusioncanvas-wireframe-v1`;
- one screen with `id`, `title`, an explicit viewport, and one rooted component hierarchy; and
- zero or more named states containing narrow overrides.

Component and state identifiers use lowercase kebab-case. Unknown properties, kinds, variants, tokens, aliases, anchors, custom YAML tags, duplicate keys, malformed scalar values, and multiple YAML documents are errors.

## Version 1 vocabulary

Containers are `stack`, `grid`, and `panel`. Leaves are `text`, `field`, `select`, `list`, `table`, `button`, `message`, and `divider`.

The closed variant vocabulary is defined in `UiVocabulary`. It includes semantic typography (`screen-heading`, `section-heading`, `subheading`, `supporting`, `label`, `emphasis`, `link`), surfaces (`canvas`, `summary-card`, `choice-card`), controls (`single-line`, `multiline`, `standard`), commands (`primary`, `secondary`, `danger`, `link`), messages (`info`, `warning`, `danger`, `empty`), and structured lists and tables. Unsupported kind/variant combinations fail; there is no visual fallback.

Spacing and padding use `none`, `tight`, `compact`, `control`, `section`, `region`, or `window`. Version 1 maps these to fixed renderer units of 0, 4, 8, 12, 16, 24, and 48. The source can use non-negative fixed sizes where structure requires them, or the semantic lengths `content` and `fill`.

A vertical `stack` is the default; `axis: horizontal` opts into a row. A `grid` declares `columns`, optionally `rowTracks`, and explicit zero-based `column` and `row` placement on every child. Fixed tracks are allocated first, content tracks use invariant component measurement, and multiple fill tracks equally share the remaining space. Source order is preserved. Absolute child coordinates are deliberately unsupported.

Text measurement uses Unicode scalar count and fixed metrics for the selected text variant. It never asks the operating system to measure a font. This makes geometry repeatable but intentionally does not promise Avalonia pixel parity.

## States

A state may override `visible` on any component, `enabled` on interactive components, literal `text` on text-bearing components, `items` on lists/selects, and `tableRows` on tables. It cannot create, delete, reparent, reorder, or change the kind or variant of a component. Rendering requires an explicit declared state.

## SVG contract

SVG output uses invariant numbers, stable tree and attribute order, LF endings, and UTF-8 without a byte-order mark. Each visible semantic node is represented by a `data-ui-id` group. The SVG contains no timestamps, machine paths, scripts, linked stylesheets, remote images, or other network dependencies. Repeating a render with identical validated input, state, viewport, and tool version produces identical bytes.

## Issue #185 fixtures

The evaluation fixtures and their preserved references are under `docs/Visuals/ui-descriptions/`:

- `manage-variants.ui.yaml`, with `default` and `provider-unavailable` states;
- `manage-design-areas.ui.yaml`, with `default` and `empty-collection` states;
- one canonical SVG for every state;
- PNG copies of the approved source wireframes under `references/`; and
- PNG previews rasterized from the two default SVGs for convenient visual inspection.

Regenerate all state outputs with the documented `render` command, then run:

```powershell
dotnet test .\tests\FusionCanvas.UiDescription.Tests\FusionCanvas.UiDescription.Tests.csproj
```

Golden tests compare regenerated SVG text exactly. Structural tests separately assert the important semantic regions so a byte change cannot hide an accidental hierarchy change.

## Side-by-side composition review

| Screen | Preserved by the generated SVG | Material differences and lessons |
| --- | --- | --- |
| Variant Management | Page heading, context panel, breadcrumb, save action, two equal choice cards, section divider, sellable-variant command row, five-column table, prominence, and reading order. | The source image contains a partially clipped `Done` action below the main panel. Version 1 deliberately rejects viewport overflow and has no scroll/overflow primitive, so that clipped action is not represented. This is a real vocabulary gap to resolve before using the language for scrollable screens. Obvious replacement-glyph artifacts in the source are normalized to intended punctuation/text. |
| Design Areas | Page heading, fixed collection region, flexible focused-editor region, three summary cards, add/edit actions, form fields, measurement guidance, compatibility action, bottom save action, prominence, and reading order. | Font glyph metrics and a few exact paddings differ because the renderer uses deterministic approximations. The fixed-plus-flexible composition and relative panel widths remain recognizable without absolute child coordinates. Obvious replacement-glyph artifacts are normalized. |

The two-screen test supports the core idea: one small semantic model reproduced both an ordered table screen and a master-detail editor without per-screen rendering code. It also exposed a concrete missing concept—scrollable or intentionally clipped content—which is preferable to silently inventing a layout.

## Boundaries and limitations

This is design tooling, not a runtime UI format. It does not define data binding, commands, focus behavior, accessibility, localization, validation behavior, responsive breakpoints, animations, reusable components, assets, arbitrary styling, or Avalonia resources/control templates. It does not read or modify production views. A future AXAML renderer should be proposed only after deciding how scrolling/overflow works and testing at least one interaction-heavy screen; the semantic description should remain the renderer-independent contract.

The only added runtime dependency is YamlDotNet 18.1.0 in the tooling project. It is MIT licensed, compatible with .NET 10, and the implementation-time NuGet vulnerability check reported no known vulnerable packages.

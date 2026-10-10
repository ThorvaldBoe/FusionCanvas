## Why

Design Area cards repeat the same fields in vertically stacked blocks, making it harder to scan names, placements, dimensions, and Variant compatibility across an Offering. A compact table keeps each printable region and its existing actions in a consistent row while retaining the focused management surface.

## What Changes

- Replace the Design Area card collection with a virtual grid that aligns Name, Placement, Maximum size, Compatibility, and row-specific Actions.
- Keep the primary-for-artwork-generation marker visible in the compatibility cell and preserve full text for tooltips and accessibility.
- Keep Add Design Area, modal Edit, archive confirmation and dependency safeguards, cancellation focus restoration, and archived-Store read-only behavior.
- At the normal window width, keep all columns visible; at minimum width, allow horizontal scrolling while keeping the heading and Add Design Area action visible.
- Do not add search or sorting: Design Areas are printable regions scoped to one Offering, and the expected collection is small enough for direct scanning.

This is one reviewable UI module: it changes one focused collection surface and preserves its existing catalog operations and lifecycle rules.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `design-area-management`: Define the aligned grid presentation, narrow-window behavior, preserved row actions, and visible summary/accessibility behavior.

## Impact

- App: `StoreEditorWindow.axaml`, `StoreEditorWindow.axaml.cs`, and the Design Area presentation row model if additional display/accessibility summaries are needed.
- App tests: Store Editor Avalonia headless coverage for grid construction, summaries, pointer and keyboard row actions, focus, archive safeguards, and minimum-width scrolling.
- No Domain, Application, Integration, persistence, schema, API, or package changes are expected.
- Verification: criterion-level headless UI evidence, focused catalog regression coverage, full solution test baseline, strict OpenSpec validation, and changed-scope review. No new Appium journey is warranted for this focused layout replacement because the same management workflow and actions remain in place; lower-layer command and archive safeguards already have dedicated coverage.

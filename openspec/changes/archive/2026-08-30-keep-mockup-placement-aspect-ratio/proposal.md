## Why

Mockup placement currently permits a design-area rectangle to become unintentionally distorted even when the selected Design Area defines a meaningful width/height ratio. Preserving that ratio by default makes direct manipulation and numeric editing safer while retaining an explicit opt-out for intentionally skewed or perspective imagery.

## What Changes

- Derive the applicable placement aspect ratio from the selected Design Area's positive width and height.
- Add an accessible **Keep aspect ratio** checkbox, enabled by default when a valid ratio is available.
- Preserve the ratio during drag-resize, keyboard resize, and numeric width/height edits while enabled.
- Allow users to disable the option for independent width and height changes.
- Recalculate applicability and safe defaults when the selected Design Area changes, and disable ratio enforcement when the ratio is invalid or unavailable.
- Keep the setting in the existing template draft/save/reopen workflow and preserve responsive accessibility.

## Capabilities

### New Capabilities

- `mockup-placement-aspect-ratio`: Ratio derivation, enforcement, opt-out, and safe fallback behavior for Mockup Template placement editing.

### Modified Capabilities

- `mockup-template-source-images`: Extend placement editing with Design Area ratio behavior and persisted draft/reopen semantics.

## Impact

- Domain/Application: no new domain entity or persistence table; existing Design Area dimensions and image mapping remain authoritative.
- App: `MockupPlacementEditor`, `CatalogSetupViewModel`, Mockup Template editor XAML, and enlarged editor bindings.
- Tests: domain-free placement-control tests, view-model ratio/numeric-edit tests, and Avalonia headless accessibility/responsive coverage.
- Existing mapping validation, revision creation, source-image applicability, and save/cancel semantics remain unchanged.

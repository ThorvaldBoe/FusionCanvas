## Why

Generated mockups currently retain the full pixel dimensions of their source PNG templates even when they are primarily used as web-facing listing images. This creates unnecessarily large derived assets and increases website transfer and image-decoding cost. A simple store-wide long-edge limit gives creators predictable control over generated mockup resolution without introducing file-format or compression decisions.

## What Changes

- Add a store-scoped `Maximum mockup long edge (px)` setting in the existing Store mockup configuration surface.
- Default the setting to `2000` pixels for stores that have no saved value.
- Validate the setting as a positive whole-pixel value.
- Scale newly generated mockups proportionally down to the configured maximum long edge, never enlarge smaller source templates, and preserve the existing PNG output format.
- Leave source mockup images, saved placement mappings, and previously generated mockups unchanged when the setting changes.
- Record the effective rendered dimensions and resolution policy with generated mockup provenance so outputs remain explainable.

## Capabilities

### New Capabilities

- `mockup-output-resolution`: Store-scoped configuration and generation behavior for limiting the maximum long edge of derived mockup images.

### Modified Capabilities

- `listing-mockup-generation`: Newly generated mockups use the store's configured maximum long-edge resolution while retaining existing attribution and output behavior.
- `mockup-template-management`: The focused Store mockup configuration surface exposes the store-wide resolution setting with clear scope and validation.

## Impact

- Affected layers include the Store metadata/configuration path, mockup generation application contracts, the ImageSharp compositor, and Store mockup UI/view-models.
- Existing store metadata must remain backward compatible; missing settings use the default.
- Existing PNG output compatibility is preserved. File-size limits, alternate output formats, quality controls, and automatic regeneration are explicitly out of scope.
- Verification requires focused application/integration tests for policy propagation and dimensions, plus deterministic Avalonia headless coverage for the setting's binding, validation, and persistence-facing UI behavior.

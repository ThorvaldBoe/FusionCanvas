## Why

Generated and imported artwork often has an opaque, mostly uniform background that must be removed before it can be used on a product with a different background color. The current workflow requires exporting the image to another editor, even for the common Print-on-Demand case where removing every pixel within a selected color range is intentional—for example, removing black from artwork intended for a black garment. This module reduces that workflow friction with a deterministic local tool that preserves creator control.

## What Changes

- Add a global color-to-alpha operation for supported raster artwork available from the Design workflow.
- Let the user pick a source color from the image and adjust a tolerance/range for nearby colors.
- Show a live preview overlay identifying pixels that would become transparent before the user applies the operation.
- Apply the selection globally across the entire image, including matching pixels inside the artwork rather than only the contiguous background region.
- Produce a transparent PNG-derived result while preserving the source artwork and the ability to cancel before applying. Applying creates a new same-kind managed result rather than mutating the source in place.
- Keep processing local and deterministic; do not require an AI model, network service, or provider credential.
- Clearly explain the global behavior and warn that artwork details matching the selected color will also be removed.

The module is intentionally limited to global color removal. Contiguous/flood-fill removal, local AI segmentation, and direct OpenAI image API integration remain later opportunities and are not specified here.

## Capabilities

### New Capabilities

- `global-color-removal`: Defines the user workflow, color/tolerance semantics, preview overlay, apply/cancel behavior, transparent output, editability, and failure states for deterministic global color-to-alpha removal.

### Modified Capabilities

- None. Existing Design Stage and asset-management requirements provide the host surfaces and managed-file boundaries; this module adds a focused capability alongside them.

## Impact

- **App:** Design Stage presentation needs a focused color-removal action and editor state for color picking, tolerance, preview, apply, cancel, busy, and error states. The control should remain within the existing Design workflow rather than becoming a general image-editor window.
- **Application:** A use-case contract must coordinate loading a supported managed raster, computing preview/apply results, enforcing Design editability, and creating or updating the derived transparent output through existing application services.
- **Integration:** ImageSharp-backed raster processing will likely provide pixel access, color-distance evaluation, alpha output, preview-mask generation, and PNG encoding behind an application-facing boundary. Existing workspace file and asset persistence boundaries must remain authoritative.
- **Domain:** Only introduce value objects or policies where they protect stable color-removal semantics such as tolerance validation, supported ranges, and editability; do not add image-library dependencies to Domain.
- **Testing:** Add deterministic pixel/mask and alpha-output tests, application orchestration tests, isolated managed-file/persistence tests, and Avalonia headless coverage for the meaningful picker/tolerance/preview/apply/cancel workflow. A real-desktop Appium journey is not initially warranted because the core risk is deterministic raster and headless interaction behavior rather than native-window integration.
- **Compatibility:** No external API, schema migration, AI-provider, or network dependency is required by this module. Existing source assets must remain recoverable if a derived result is created.

## Origin

- GitHub issue [#835](https://github.com/ThorvaldBoe/FusionCanvas/issues/835)

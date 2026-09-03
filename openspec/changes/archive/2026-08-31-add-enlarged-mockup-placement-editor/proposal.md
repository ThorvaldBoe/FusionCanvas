## Why

Precise placement is difficult in the current compact Mockup Template preview, forcing creators to estimate image-space coordinates and making direct manipulation unnecessarily imprecise. This module adds a larger focused placement surface while preserving the existing selected image, mapping draft, and save workflow.

## What Changes

- Add a clearly recognizable zoom/expand control in the lower-right of the existing placement preview.
- Open a substantially larger, keyboard-accessible placement editor from that control.
- Reuse the existing image-space placement interaction for dragging and independent width/height resizing.
- Keep the selected image, image mapping, draft state, and existing Save/Cancel semantics shared between the compact and enlarged surfaces.
- Provide responsive sizing, an explicit close/cancel path, accessible naming, and predictable focus behavior.

## Capabilities

### New Capabilities

- `enlarged-mockup-placement-editor`: Focused enlarged placement editing for a selected Mockup Template source image.

### Modified Capabilities

- `mockup-template-source-images`: Extend the selected-image placement workflow with an enlarged editor while preserving per-image mapping and draft/save behavior.

## Impact

- Avalonia UI: Mockup Template editor preview, placement-editor presentation, focus, and window/dialog lifecycle.
- App presentation state: a transient enlarged-editor state/window that binds to the same `CatalogSetupViewModel` mapping properties.
- Automated coverage: placement-control, modal lifecycle, synchronization, keyboard accessibility, and responsive/headless view tests.
- No domain model, persistence schema, external API, or dependency changes are required; existing mapping validation, revision creation, and save semantics remain authoritative.

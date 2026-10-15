## Why

Creators need a quick way to make final PNG artwork fill and sit correctly within its transparent canvas for a Design slot. The current enlarged preview is inspection-only and limits the displayed image size; it cannot identify visible artwork bounds or adjust placement.

## What Changes

- Replace the Design slot's text Enlarge action with an accessible magnifier action over the artwork thumbnail.
- Make the focused artwork preview resizable, with an image view that grows with the window and a checkerboard that distinguishes the full PNG canvas from transparent pixels.
- Add an adjustment mode that detects the visible pixel bounds, overlays a movable rectangle, and provides proportional scaling while retaining the original canvas dimensions.
- Keep adjustments as a reversible draft until Apply. Apply saves a copy of the current pre-apply PNG in the Item's Related Images and replaces the slot artwork with the transformed PNG; pixels outside the canvas are clipped. Each Apply preserves that apply's before-image.

## Capabilities

### New Capabilities

- `design-artwork-adjustment`: Inspect, adjust, and apply destructive scale and position edits to final Design slot PNGs while preserving each before-image as a related image.

### Modified Capabilities

- `asset-management`: Update the Design-file preview and management requirements for the magnifier entry point, adjustable preview, before-image copy, and applied slot replacement.

## Impact

- Affects the Design Stage thumbnail actions, preview window, and Design Stage view model in `FusionCanvas.App`.
- Adds an Application use case with an Integration image processor for alpha-bound detection and PNG transformation, plus before-image storage, slot replacement, and atomic workspace persistence.
- Affects managed asset files, Item asset links, content-risk review state, and mockup invalidation when slot artwork changes.
- No new dependency is expected; use the existing image-processing package if its current APIs support the required PNG operations.
- UX: this occasional artwork-editing action starts beside the thumbnail and uses a focused window. The preview and adjustment controls stay out of the main workspace until requested. Apply explicitly commits the destructive pixel transform; Cancel or closing the window discards the draft and returns focus to the invoking action.
- Scope is one coherent Design Stage outcome: fit visible artwork within its own PNG canvas. It does not add product mockup rendering, alter print-area dimensions, or introduce general-purpose image editing.
- Verification will cover alpha-bound and transform rules, persistence and file rollback, plus the Avalonia view behavior for adjustment entry, drag/resize, keyboard access, Apply, cancellation, and window resizing. The required solution test baseline and strict OpenSpec validation remain completion gates.

## Context

Final Design artwork is a managed PNG attached to a `DesignSlotAssignment`. The slot currently opens an inspection preview from `DesignStageToolViewModel`; the `DesignPreviewWindow` is resizable but caps its image at 740×540. The model has no transform data. `DesignStageService` owns slot replacement, item asset links, content-risk reviews, and mockup invalidation. `IWorkspaceFileOutputStore` can save streams into managed storage, `LocalWorkspaceFileStore` implements it, and Integration already uses SixLabors.ImageSharp.

This module edits the pixels in the final slot artwork. A draft transform lasts only in the open preview. Applying it bakes the transformed artwork into a same-sized PNG and preserves the pre-apply pixels as an Item-linked ReferenceImage in Related Images. No transform metadata or schema migration is needed.

## Goals / Non-Goals

**Goals:**

- Make final artwork inspection useful at different preview window sizes and distinguish the full image canvas from transparent pixels.
- Detect visible bounds using PNG alpha, let creators move and proportionally resize the visible artwork, and preview clipping before Apply.
- Make Apply a single coherent workspace mutation that adds a before-image and replaces the slot's managed PNG.
- Preserve the current Item, slot, selected color row, and focus context after editing or an error.
- Provide deterministic lower-layer and Avalonia headless coverage for pixel math, persistence, bindings, and input.

**Non-Goals:**

- Transform artwork while generating mockups, or change catalog print-area dimensions or placement mappings.
- Add arbitrary crop, rotation, skew, aspect-ratio distortion, layers, undo history UI, or a general image editor.
- Persist a live scale/position transform. The applied raster is the new slot image; before-images are ordinary Related Images.
- Add new raster packages or a database migration.

## Decisions

### The focused preview owns the adjustment workflow

The Design slot thumbnail gets one compact magnifier action in its lower-right corner, replacing the text Enlarge action. Activating it opens the existing focused preview window with the full-resolution managed PNG. The image view scales uniformly to the available window area and the checkerboard is behind the full PNG canvas. Adjustments are progressively disclosed by an **Adjust artwork** action; viewing remains read-only until that action is chosen.

The window exposes Apply and Cancel during adjustment. Apply is enabled only for an editable Item and a valid visible artwork image. A changed draft is not persisted until Apply. Cancel discards the adjustment. Closing or Escape with a changed draft asks whether to discard; confirming discard closes and returns focus to the invoking thumbnail, while declining keeps the draft and returns focus to the adjustment surface. Unchanged preview dismissal closes directly. In read-only contexts the image can be inspected but adjustment cannot begin.

### Geometry uses source-image pixels and the original canvas

Image inspection reports canvas width and height plus the smallest rectangle containing pixels whose alpha is greater than zero. A fully opaque PNG has full-canvas bounds. A fully transparent PNG has no adjustment bounds and shows an explanatory state.

The UI converts pointer positions from the Uniform-scaled image viewport (including letterbox offsets) to source-image pixels. Dragging the rectangle translates the artwork; dragging its lower-right handle scales both dimensions by one factor around a fixed upper-left anchor. Pointer and keyboard operations use the same geometry path. Geometry remains unclamped while drafting so artwork can leave the canvas and be moved back. At Apply, transformed pixels outside the original canvas are clipped and any uncovered pixels remain transparent. The output canvas keeps the source dimensions.

The Integration processor crops to the visible alpha bounds, resizes proportionally to the draft rectangle, and composites at its draft location onto a transparent image with the original canvas dimensions. High-quality resampling is used. No other pixels are sampled from outside the detected bounds; transparent interior pixels remain transparent. The preview should use the same transform semantics so the visible result matches Apply.

### Pixel processing stays behind an Application contract

Add an Application-facing image processor contract that inspects a PNG for canvas size and visible alpha bounds and produces a transformed PNG from source bounds plus target rectangle. Its data types use dimensions and rectangles only; they do not expose Avalonia or ImageSharp types. Implement it in Integration with the existing ImageSharp dependency. The App handles presentation, pointer-to-image coordinate conversion, draft state, and window lifetime, but does not decode or mutate business artwork pixels.

Extend `IDesignStageService` with inspection/preview data and an Apply operation that accepts Item, row, design-area, and target pixel rectangle context. The service reloads and validates the current workspace, confirms the Item is editable, confirms the assignment still references an available asset, requests the rendered PNG, and coordinates managed files and snapshot persistence. It injects the output-store port for stream saves and continues to use existing repository, file cleanup, content-risk, provenance, and mockup invalidation boundaries.

### Apply saves a before-image and replaces the current slot atomically

For Apply, the service saves two new managed files: the current asset bytes as an `AssetKind.ReferenceImage` backup and the transformed PNG as an `AssetKind.ExportedImage` replacement. It creates the corresponding Item links, points the slot assignment at the replacement, and writes the new asset records and content-risk review in one repository snapshot save. It preserves existing AI provenance metadata where applicable on the replacement and recalculates its content-risk fingerprint/review for the changed bytes. The backup is named to identify it as a before-adjustment copy and remains visible in Related Images.

Only after the snapshot save succeeds may the superseded slot asset be deleted, following the established best-effort cleanup pattern. Mockups that depend on the superseded asset are invalidated after the save. If preparing either output fails, any earlier new file is cleaned up. If repository persistence fails, both new files are cleaned up best-effort, prior records and slot assignment remain authoritative, and the UI retains the draft for retry. The backup is created on every successful Apply, including later adjustments.

This uses existing Item-linked ReferenceImage behavior for Related Images and existing stream output support. It does not overwrite a managed file in place; the new replacement Asset keeps failure recovery straightforward and preserves any other readers that may still hold the previous stream.

### UI controls expose equivalent keyboard actions

The adjustment surface is a focusable control with a meaningful automation name. Arrow keys move the artwork by a small source-pixel step; Shift+Arrow moves it by a larger step. Space toggles the focused surface between movement and resize modes. In resize mode, arrows scale proportionally around the fixed corner, with Shift increasing the step. Apply, Cancel, and any discard prompt are reachable and operable without a pointer. Focus enters the preview on open, moves predictably into adjustment mode, and returns to the magnifier action on close.

### Verification is deterministic; Appium is not warranted

Image algorithm behavior and application persistence are deterministic and belong in Integration and Application tests. UI construction, control state, pointer/key routed input, image-view resizing, focus, and accessible names are framework-sensitive and belong in Avalonia headless view tests included in the standard solution test baseline. Real-desktop Appium coverage is not warranted for this module: there is no OS integration or native workflow dependency, and the relevant pointer, keyboard, and layout behavior can be exercised in headless Avalonia. Live visual review may supplement the automated evidence but is not a gate.

## Risks / Trade-offs

- [Alpha noise or isolated semi-transparent pixels can expand the detected rectangle] → Treat every alpha value above zero as visible, matching the stated transparency-based rule; show the detected outline before editing so the creator can see what will move.
- [Letterboxing can make pointer math drift from pixel geometry] → Keep one tested viewport-to-source coordinate mapper and use it for pointer and keyboard edits.
- [Large PNGs can consume memory during decode, transform, and preview] → Decode one source at a time, avoid retaining redundant full-resolution buffers in the view model, and dispose streams/bitmaps at preview close and failure.
- [File creation can succeed while snapshot save fails] → Track each staged managed path and clean it up on every failed pre-commit path; preserve the old slot until the snapshot save succeeds.
- [Applying a transformed image changes content-risk and generated-image state] → Recompute review fingerprints for the new bytes, retain valid generation provenance on the replacement, and test stale review cleanup and mockup invalidation.
- [A before-image adds to the Item's Related Images collection on each Apply] → This is intentional per the user's recovery requirement; use a clear generated name so each before-image can be distinguished and removed through existing related-image actions.

## Migration Plan

No persisted schema migration is required. Existing slot assignments and assets remain readable. The feature creates new ordinary ReferenceImage and ExportedImage records using existing snapshot fields and asset links. If an Apply cannot save its snapshot, newly staged files are removed best-effort and the previous slot data remains in place. Rollback consists of reverting the code; already applied outputs and their before-images remain regular workspace assets and do not require special decoding.

## Acceptance-to-Verification Mapping

| Acceptance scenario | Planned verification |
| --- | --- |
| Creator opens a final artwork preview | App headless test activates the magnifier and verifies preview content, checkerboard host, and focus entry. |
| Creator resizes the preview window | App headless test resizes the window and verifies the image view remains bounded and uniform; visual layout assertion covers the image canvas. |
| PNG contains transparent margins | Integration test verifies exact outer bounds for alpha > 0; App headless test verifies the adjustment overlay uses those bounds. |
| PNG has no transparent pixels | Integration test verifies full-canvas bounds; App headless test verifies the initial overlay fills the canvas. |
| PNG is fully transparent | Integration and App tests verify absent bounds, explanatory state, and disabled adjustment/Apply. |
| Creator moves artwork outside and back inside the canvas | App headless routed-pointer test verifies draft geometry can leave and return; Application test verifies Cancel/no-Apply leaves source bytes unchanged. |
| Creator resizes artwork proportionally | Framework-free geometry test verifies fixed opposite corner and equal scale factor; App headless input test verifies resize-handle gestures update the draft. |
| Creator cancels an adjustment draft | App headless test verifies draft reset and Application test verifies no workspace operation occurred. |
| Creator closes a preview with unapplied adjustments | App headless test verifies dirty-close prompt, keep-editing behavior, and discard behavior without service mutation. |
| Creator applies an artwork adjustment | Integration pixel test verifies same-size output, clipping, and transparent uncovered pixels; Application test verifies backup, replacement asset, links, review, and invalidation. |
| Creator applies a later adjustment to the same slot | Application test performs two Applies and verifies two before-images capture the successive pre-apply bytes. |
| Persistence fails during Apply | Application test injects repository failure and verifies old assignment/assets remain, new files are cleaned best-effort, and a recoverable result is returned. |
| Creator reviews artwork in a read-only Design context | App headless test verifies preview works and adjustment/Apply are unavailable; Application test verifies mutation rejection. |
| Managed slot artwork is missing or unreadable | App headless test verifies unavailable feedback and no adjustment surface; Application test verifies state remains unchanged. |
| Creator adjusts artwork without a pointer | App headless routed-key test verifies rectangle movement, proportional handle resizing, Apply and Cancel are keyboard reachable. |
| Creator closes the preview with the keyboard | App headless test verifies Escape closes cleanly or invokes dirty-close confirmation and restores focus after close. |
| User previews a Design file (modified asset-management scenario) | App headless regression verifies final slot preview and existing supporting-image preview behavior. |
| User adjusts a Design file (modified asset-management scenario) | App headless and Integration tests verify editable preview, alpha bounds, proportional transform, and reversible draft. |
| Managed file is missing (modified asset-management scenario) | Existing and focused App headless tests verify missing state and disabled preview, adjustment, and export. |
| User exports a Design file (modified asset-management scenario) | Existing Application test verifies exported bytes match the managed file and no workspace mutation occurs. |
| User applies an adjustment to a Design file (modified asset-management scenario) | Application and Integration tests verify before-image relation, destructive replacement, same canvas dimensions, and clipping. |
| User removes a Design file (modified asset-management scenario) | Existing Application test verifies confirmed removal and post-save cleanup. |
| Removal persistence fails (modified asset-management scenario) | Existing Application test verifies asset, link, and managed file remain after save failure. |

## Open Questions

None blocking implementation. Fully opaque and fully transparent PNG behavior, per-Apply backup behavior, editable/read-only behavior, dismissal, clipping, persistence failure, and keyboard operation are specified in the delta requirements.

## Implementation Plan

1. **Image processing boundary**
   - Add Application DTOs for PNG canvas dimensions, alpha bounds, and target pixel rectangle, plus an `IArtworkImageProcessor` contract for inspection and transformed PNG output.
   - Implement the processor under `src/FusionCanvas.Integration/AI` or a dedicated image-processing folder using the existing ImageSharp package. Treat alpha > 0 as visible; report no bounds for fully transparent content; preserve source canvas dimensions, alpha, and proportional geometry.
   - Add Integration tests for transparent margins, opaque full-canvas images, fully transparent images, semi-transparent edge pixels, proportional scaling, translation, clipping on all canvas edges, and transparent uncovered regions.

2. **Application Apply use case and managed persistence**
   - Extend `IDesignStageService` and `DesignStageService` with preview inspection and Apply operations. Keep the workflow policy, current-assignment validation, repository mutation, content-risk state, provenance, and mockup invalidation in this Application service.
   - Inject the existing `IWorkspaceFileOutputStore` boundary alongside the file reader/deleter. Stage a before-image `ReferenceImage` and transformed `ExportedImage`, update the slot assignment and Item links in one snapshot save, then best-effort delete the replaced asset and invalidate dependent mockups.
   - On failures before snapshot commit, clean up all new managed files and leave the prior asset, assignment, and links unchanged. Retain/refresh content-risk review for new bytes and preserve generation provenance on the replacement as applicable.
   - Register the ImageSharp processor in `src/FusionCanvas.App/Workspace/AppWorkspaceFactory.cs` and update null/test implementations as required. No database migration is expected.
   - Add Application tests under `tests/FusionCanvas.Application.Tests/DesignFiles` for successful Apply, successive before-images, read-only/missing/stale assignment rejection, output save failures, repository save failure cleanup, content-risk fingerprints, metadata retention, and mockup invalidation. Use deterministic repository, file store/output store, and processor fakes.

3. **Preview and adjustment UI**
   - Replace the text Enlarge button in `src/FusionCanvas.App/Views/MainWindow.axaml` with a lower-right magnifier action; update accessible names and tooltip in `DesignSlotViewModel`.
   - Extend `DesignStageToolViewModel` to load full-resolution inspection data, own adjustment draft state, map viewport gestures to source-pixel rectangles, call Apply, keep errors/drafts visible, and dispose preview resources.
   - Update `DesignPreviewWindow.axaml` and `.axaml.cs` for responsive image fitting, checkerboard, adjustment overlay/handle, Adjust artwork, Apply/Cancel, keyboard editing, dirty-close confirmation, and focus return. Keep supporting-image preview behavior intact.
   - Add/extend headless tests in `tests/FusionCanvas.App.Tests/DesignPreviewWindowTests.cs` and `DesignStageToolHeadlessTests.cs` for window resizing, state, overlay/handle bindings, routed pointer and keyboard changes, cancellation, dirty close, Apply results, focus/accessibility, and read-only/missing/empty states. Test coordinate conversion independently at the lowest reliable layer if it can be separated without speculative abstraction.

4. **Criterion-level verification and review**
   - Verify each scenario in `specs/design-artwork-adjustment/spec.md` against the relevant Integration, Application, or App headless test. Verify the modified asset-management preview/export/missing/removal scenarios in the corresponding existing tests plus focused regression tests.
   - Run strict `openspec validate` and `dotnet test .\FusionCanvas.sln`; resolve any individual failing criterion and rerun its coverage plus regressions.
   - Review changed scope for architecture direction, file cleanup/atomicity, content-risk and provenance consistency, accessible controls, and consistency between preview rendering and applied PNG output.
   - Do not add an Appium scenario pack; document the headless-only rationale and any optional manual visual observations in completion evidence.

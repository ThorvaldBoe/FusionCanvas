## Context

FusionCanvas already manages Design-stage image files and supporting images as workspace-owned assets. Design files are PNG-backed `ExportedImage` assets, previews are opened through application services, and the local workspace file boundary already supports reading and saving managed files. The Design Stage Tool also owns the user's current editability, image selection, preview, import, export, and removal context.

The requested workflow is an occasional cleanup action performed on a selected image, not a permanent editor panel. The user needs a predictable magic-wand-like operation that removes every pixel within a selected color range, including disconnected regions and matching artwork details. The source must remain recoverable because global removal is intentionally destructive to all matching colors in the derived result.

## Goals / Non-Goals

**Goals:**

- Provide one focused, local Design-stage workflow for global color-to-alpha removal.
- Make color selection, tolerance, candidate-mask preview, Apply, and Cancel understandable and keyboard reachable.
- Use deterministic RGBA pixel processing with a documented tolerance rule and PNG output.
- Preserve the original managed image and create a new managed result linked to the same Item and image purpose.
- Keep file creation and workspace persistence recoverable if either boundary fails.
- Cover algorithmic behavior, application orchestration, persistence cleanup, and meaningful Avalonia interaction states in deterministic tests.

**Non-Goals:**

- Contiguous/flood-fill selection.
- Local AI segmentation, model packaging, training, or external background-removal services.
- Direct OpenAI image API integration or changes to AI provider transparency capability detection.
- Brush-based refine/restore tools, edge feathering, vector/SVG editing, video, or mockup-source editing.
- Replacing or mutating the original image in place.

## Decisions

### Focused editor launched from the existing Design surface

Launch a compact focused color-removal editor from an eligible Design image row or preview action. Keep it separate from the always-visible Design tool so occasional controls do not consume the primary workspace. The editor displays the source image, a removal overlay, a color-picker affordance, a tolerance control, a short global-removal warning, and explicit Apply and Cancel actions.

The initial state has no picked color, explains how to pick one, and disables Apply. During preview recomputation the editor shows a busy state and ignores conflicting settings changes until the latest request wins or is cancelled. Apply is explicit but does not require a destructive confirmation because the original source is preserved and the operation creates a derived result.

### Global matching with a deterministic RGB distance

Use unpremultiplied sRGB RGB values for matching. Normalize Euclidean RGB distance to the range `[0, 1]`; tolerance `0` therefore means exact RGB equality and larger values include progressively more nearby colors. Pixels with zero alpha are ignored for matching and remain transparent. Matching pixels in every raster location are included, regardless of whether they are connected to the picked pixel.

This rule is predictable, local, and testable. It is preferable to a library-specific flood-fill or an opaque perceptual threshold for this first module. The UI may present tolerance as a percentage, but the application contract must carry the normalized value and validate its range.

### Binary alpha removal in the first slice

For an applied result, matching pixels receive alpha zero and nonmatching pixels retain their original RGBA values. No edge feathering or partial-alpha falloff is added in this module. This keeps the behavior faithful to the requested global magic-wand workflow and avoids introducing an additional edge-quality decision before real examples justify it.

The preview should show the candidate mask as a visible accent overlay over the source, with the source still recognizable. The exact brush color is presentation-only; the application/integration result must expose the mask or removed-pixel count so the UI can render a truthful state and disable Apply for no-match or all-visible-pixel outcomes.

### Derived managed output, not in-place mutation

Applying creates a new managed PNG using the source asset's Item relationship and image purpose. A Design PNG remains an `ExportedImage`; a supported supporting image keeps its existing image kind. The source record and file remain unchanged. The derived file receives a clear generated name and becomes selected or otherwise explicitly identified after refresh.

Use the existing `IWorkspaceFileOutputStore` to save the encoded result and the existing workspace repository to persist the new asset/link atomically from the application's perspective. If persistence fails after file creation, delete the new managed file best-effort and retain the prior snapshot. If the operation is cancelled before persistence, do not create a durable asset. Apply existing content-risk bookkeeping for item-linked image assets where that service is present; an advisory review failure must not make the source or derived file disappear.

### Application owns orchestration; ImageSharp stays in Integration

Add an application-facing global color-removal contract and models for source identity, selected RGB color, normalized tolerance, preview result, and apply result. The application service validates Item editability, resolves the source asset and relationship, coordinates the processor, writes the managed output, persists the derived asset, and performs cleanup.

Add an Integration implementation backed by the already referenced ImageSharp package. It owns decoding, pixel iteration, distance calculation, mask generation, preview PNG encoding, applied PNG encoding, decoded-pixel limits, and cancellation checks. No ImageSharp types cross into Domain, Application contracts, or App presentation models.

### UX and verification boundaries

The primary user job is: select an opaque Design image, remove a known background color globally, review the candidate, and retain the transparent derivative for later Design work. This is occasional per-image work, so the action belongs in a focused editor launched from the main Design surface. The editor must define empty, loading, preview, no-match, all-visible-artwork, success, cancellation, read-only, missing-source, and recoverable-failure states. Keyboard focus starts on the color-picking control, moves predictably through tolerance and actions, returns to the invoking action on Cancel, and lands on the derived result after Apply.

The core algorithm and persistence behavior are covered by focused deterministic tests. The editor's bindings, control state, routed interaction, focus, and preview/apply/cancel transitions receive Avalonia headless tests. A real-desktop Appium journey is not warranted for this module: there is no native file picker or OS-specific integration in the new workflow, and the meaningful seam can be represented by the existing deterministic headless harness. The decision and lower-layer coverage are recorded here rather than adding ceremonial desktop automation.

## Risks / Trade-offs

- **[Risk]** Global matching removes intentional artwork details that share the selected color. **Mitigation:** show a prominent global-removal warning, preview every affected region, and preserve the source unchanged.
- **[Risk]** RGB distance can produce halos or remove antialiased shades unexpectedly. **Mitigation:** use a documented deterministic rule, default to a conservative tolerance, expose the live overlay, and defer feathering/refinement to a later module.
- **[Risk]** Large images can make repeated preview recomputation expensive. **Mitigation:** use cancellation/generation guards, bounded decode limits, debounced settings updates, and keep the full-resolution apply separate from any display-size preview optimization.
- **[Risk]** A file may be created before snapshot persistence fails. **Mitigation:** use the existing managed-file cleanup pattern and test the failure path with isolated file stores.
- **[Risk]** Derived results may be confused with their sources. **Mitigation:** use a clear derived filename, preserve the source in the same surface, and identify the newly selected result after Apply.
- **[Risk]** Supporting-image and Design-file asset kinds have different existing workflows. **Mitigation:** keep the common processor generic, preserve the source asset kind/link, and limit the first implementation to managed Item-linked raster images already surfaced by Design.

## Migration Plan

No database schema migration or external-service configuration is required. The implementation adds new application, integration, and presentation types and persists derived results through the existing asset and workspace snapshot structures. Existing workspaces remain readable because no existing records are rewritten on load.

Rollback is a code rollback; already-created derived assets are ordinary managed assets and can be removed through the existing confirmed removal workflow. If implementation exposes a serialization compatibility issue, stop before release and preserve the source/output files while repairing the application mapping.

## Open Questions

None for the first implementation slice. The following decisions are intentionally closed here: global rather than contiguous matching, normalized Euclidean sRGB tolerance, binary alpha removal, derived output rather than source mutation, local ImageSharp processing, and no real-desktop Appium journey. Future modules may revisit contiguous selection, edge refinement, and direct OpenAI integration using evidence from this module.

## Implementation Plan

1. **Application contracts and policy**
   - Add focused global color-removal request/result models and an application service near `src/FusionCanvas.Application/DesignFiles/`.
   - Define a narrow processor port for preview-mask and applied-PNG generation; keep color/tolerance validation and Item/source relationship checks in Application.
   - Reuse `IWorkspaceFileOutputStore`, `IWorkspaceRepository`, existing `Asset`, `AssetLink`, editability policy, and content-risk boundaries rather than introducing a parallel asset store.

2. **Integration raster processor**
   - Add an ImageSharp-backed processor under `src/FusionCanvas.Integration/Files/` or a cohesive raster-processing folder.
   - Decode to RGBA with bounded encoded-size and pixel-count checks, compute the normalized Euclidean RGB mask, produce preview overlay bytes and final transparent PNG bytes, and honor cancellation.
   - Add deterministic pixel fixtures for exact match, tolerance expansion, disconnected matches, transparent pixels, no-match, all-visible removal, alpha preservation, malformed input, and output encoding.

3. **Application persistence orchestration**
   - Resolve the selected managed Item-linked raster source and verify current Design editability.
   - Keep preview requests in memory only.
   - On Apply, save the derived PNG, create the same-kind derived Asset and Item link, persist one updated snapshot, invoke applicable advisory review, and clean up the new file on persistence failure.
   - Refresh the Design-stage source list from authoritative state and identify the new result.

4. **Design presentation**
   - Add a focused editor view model and Avalonia surface, likely alongside `DesignPreviewWindow` and `DesignStageToolViewModel`.
   - Add the action only to eligible managed raster rows/previews; include color picker, tolerance, overlay, match count/status, warning, Apply, Cancel, busy, and recoverable error states.
   - Wire the editor through the existing application composition root and preserve read-only behavior, selection, focus, disposal, and cancellation conventions.

5. **Verification and quality gates**
   - Add application tests for editability, source preservation, derived asset/link creation, persistence cleanup, cancellation, and reloading.
   - Add App view-model and Avalonia headless tests for initial disabled Apply, picker/tolerance state, overlay update, no-match/all-visible blocking, busy behavior, Apply success, Cancel focus/state, and recoverable error presentation.
   - Run strict OpenSpec validation and the deterministic solution baseline `dotnet test .\FusionCanvas.sln -m:1`; record criterion-level evidence in the eventual verification artifact.

## Acceptance-to-Verification Mapping

| Acceptance area | Planned verification |
| --- | --- |
| Eligibility and invalid-source blocking | Application service tests plus focused view-model/headless state tests |
| Exact/tolerant global RGB matching | ImageSharp processor tests with deterministic small RGBA fixtures |
| Disconnected and in-artwork matches | Processor tests asserting every matching coordinate is masked |
| Live preview overlay and no-match/all-visible states | Processor result tests plus Avalonia headless editor journey |
| Derived transparent PNG and source preservation | Application tests with isolated repository/output store plus PNG alpha assertions |
| Persistence failure cleanup and cancellation | Application tests with failing repository/output-store fakes |
| Busy, keyboard, Apply, Cancel, and focus transitions | Avalonia headless view tests; no Appium journey planned because no native/OS seam is introduced |

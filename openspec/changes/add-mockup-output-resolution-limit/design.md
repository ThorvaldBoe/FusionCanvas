## Context

Mockup generation currently loads the template and design, composites the design into the template's original pixel dimensions, and saves a PNG. The generated asset is therefore as large as the source template even when its web-facing use does not require that resolution. Store metadata already provides a backward-compatible home for flexible store-scoped configuration, while template setup already has the focused mockup dialog and Store identity needed to load and save the setting.

## Goals / Non-Goals

**Goals:**

- Provide one clear Store-wide integer setting in the existing mockup dialog.
- Default missing values to 2000 pixels and validate positive whole-pixel input.
- Downscale new generated outputs proportionally without upscaling smaller templates.
- Resize before compositing so the large final raster is not unnecessarily created.
- Preserve source assets, placement mappings, PNG output, existing provenance, and existing generated outputs.
- Keep backward compatibility for Stores whose metadata predates this setting.

**Non-Goals:**

- Maximum encoded file-size controls.
- JPEG, WebP, quality sliders, or output-format selection.
- Automatic regeneration or replacement of existing mockups.
- Editing source mockup files or rewriting saved image-space mappings.

## Decisions

### Store metadata is the persistence boundary

Persist the setting as a Store-scoped metadata value rather than adding a database column or application-wide preference. A missing value resolves to the default. A small application-layer settings service owns parsing, validation, and repository mutation so the UI does not manipulate metadata directly. Unknown metadata keys must survive the read/write cycle.

### The setting is a maximum long edge

The UI exposes one integer field labelled `Maximum mockup long edge (px)`. The value applies to all newly generated mockups for the Store. The policy calculates a scale of `min(1, maximumLongEdge / max(sourceWidth, sourceHeight))`, rounds the output dimensions to positive integers, and preserves the source aspect ratio. Smaller images are not enlarged.

### Downscale before compositing

The compositor will determine the output scale from the source template dimensions, resize the template to the final dimensions, scale the saved mapping into those output coordinates, and fit the artwork into the scaled mapping. This reduces intermediate memory and encoding work while retaining the existing placement semantics. The saved mapping remains expressed in original source-image coordinates.

### Keep PNG for this module

PNG output and the existing filename/asset behavior remain unchanged. Format and compression choices need separate consumer and compatibility decisions and are intentionally not coupled to this resolution setting.

### Save resolution with the existing mockup-dialog workflow

The setting is shown as Store-wide configuration in the focused mockup dialog, separate from template-specific source-image metadata and placement. It participates in the dialog's existing draft/dirty-state behavior and is saved when the user saves mockup configuration. Archived Stores display the value read-only. Changing the value affects future generation only; existing output assets remain untouched.

### Provenance records effective rendering

The generated asset metadata keeps its current attribution fields and adds the effective rendered width, height, and maximum-long-edge value. The application calculates these dimensions from the source-image dimensions and the shared policy before invoking the compositor, ensuring provenance and raster behavior use the same rule.

## Risks / Trade-offs

- **A positive integer does not guarantee a small byte size** → Keep byte-size control out of this module and retain PNG compatibility; evaluate format/quality separately using real mockup samples.
- **Rounding can cause a one-pixel long-edge difference** → Use one shared dimension-calculation policy for both metadata and compositor input, clamp dimensions to at least one pixel, and test portrait, landscape, square, and below-limit cases.
- **A setting saved alongside a template dialog may appear template-specific** → Label it explicitly `Store-wide` and explain that it affects newly generated mockups for the Store.
- **Existing outputs can have different dimensions after a setting change** → Do not mutate or delete them; record effective dimensions in new output metadata so the difference is explainable.
- **Metadata written by older versions may be malformed** → Treat missing or invalid values as the default for reading; reject invalid new UI input before persistence.

## Migration Plan

No schema migration is required. Existing Stores read as 2000 pixels when the metadata key is absent. Existing generated mockups remain valid and are not rewritten. The setting can be rolled back by removing the new metadata key or restoring the prior application version; older versions will ignore the unknown key.

## Open Questions

None for this module. The exact default is fixed at 2000 pixels by the approved scope; file-size and alternate-format decisions are deferred.

## Implementation Plan

1. Add a domain mockup-output resolution policy with the 2000-pixel default, positive-integer validation, output-dimension calculation, and scaled mapping helper behavior covered by Domain tests.
2. Add an Application mockup-output settings contract/service that reads and writes the Store metadata value without discarding unknown keys, and expose the policy to `MockupGenerationService`.
3. Extend the raster compositor contract and ImageSharp implementation to accept the maximum long edge, resize the template before compositing, scale mapping coordinates, preserve aspect ratio, and continue emitting PNG.
4. Update generated mockup provenance with effective width, height, and policy value while preserving existing attribution and failure compensation behavior.
5. Extend `CatalogSetupViewModel` and `MockupTemplateEditorWindow` with the Store-wide TextBox, helper text, validation, dirty-state participation, save/load behavior, and archived-Store read-only behavior. Wire the application service through the existing Store/catalog composition root.
6. Add focused Domain, Application, Integration, App view-model, and Avalonia headless tests for defaulting, validation, metadata preservation, scaled dimensions, no-upscale behavior, placement correctness, provenance, binding, save/reload, and read-only presentation.
7. Run strict OpenSpec validation, the focused tests, and the required `dotnet test .\FusionCanvas.sln` baseline. Map every acceptance scenario to evidence in `verification.md`.

## Acceptance-to-Verification Plan

| Acceptance area | Planned verification |
| --- | --- |
| Default, valid save, invalid input, metadata preservation | Application service tests and `CatalogSetupViewModel` tests |
| Proportional downscale and no upscale | Domain policy tests plus `ImageSharpMockupRasterCompositorTests` |
| Artwork placement after scaling | Integration compositor test with known pixel geometry |
| Existing outputs unchanged and provenance | `MockupGenerationService` application tests |
| Store-wide field, helper text, and archived read-only state | Avalonia headless `StoreEditorHeadlessTests` coverage |
| Regression and specification integrity | `openspec validate` and full solution test baseline |

Real-desktop Appium coverage is not warranted for this module: the interaction is a single validated text field whose binding, read-only state, and persistence can be deterministically exercised through existing Avalonia headless coverage, while the generation behavior is lower-layer deterministic raster work.

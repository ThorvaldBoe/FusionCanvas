## MODIFIED Requirements

### Requirement: Applying a template composes one output per applicable design color
When the creator applies a selected ready Mockup Template, FusionCanvas SHALL resolve the current template revision, choose the source image whose active Color applicability covers each selected Item Color, and compose the matching Design PNG into the saved image-space mapping. The design SHALL be scaled to fit within the mapping while preserving its aspect ratio. The resulting mockup SHALL obey the Store's configured maximum long-edge resolution without enlarging a source template that is already smaller than that maximum, while retaining the source template aspect ratio.

#### Scenario: Creator applies a complete template
- **WHEN** every selected Color has a readable Design PNG and an applicable template source image with a valid mapping
- **THEN** FusionCanvas composes and stores one mockup output for each design/color combination using the selected template revision
- **AND** each output obeys the Store's maximum long-edge resolution policy without being upscaled
- **AND** the Listing stage refreshes to show the new outputs

#### Scenario: A color template is missing
- **WHEN** a selected Color has no applicable source image in the chosen template
- **THEN** FusionCanvas warns which Color is missing and does not create a fabricated output for that Color
- **AND** it still retains any independently successful outputs from the same apply operation

#### Scenario: Mapping or source input is invalid
- **WHEN** a source image is missing, a Design PNG cannot be read, or the saved mapping is outside the source image bounds
- **THEN** FusionCanvas reports a recoverable per-output failure and leaves existing generated mockups unchanged

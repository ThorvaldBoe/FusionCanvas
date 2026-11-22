## MODIFIED Requirements

### Requirement: Listing-stage mockup tool presents applicable local templates
FusionCanvas SHALL present a Listing-stage mockup tool for an Item with an active Offering, showing ready local Mockup Templates belonging to that Offering and the Item's selected Listing Colors with their assigned artwork variants. The tool SHALL keep the existing Listing-stage read-only policy for protected Items and SHALL allow mockup generation for ready selected Colors while another selected Color has an unresolved template requirement.

#### Scenario: Listing has applicable templates and assigned artwork
- **WHEN** the creator opens Listing for an editable Item with selected Colors, assigned artwork variants, and ready Mockup Templates
- **THEN** the tool shows the template selector, an **Apply mockup template** action, and existing generated mockups for that Item
- **AND** it resolves artwork per selected Color using that Color's Listing assignment

#### Scenario: Listing cannot yet generate a mockup
- **WHEN** the Item has no Offering, no valid Design Area artwork for a selected Color, or no applicable ready template for that Color
- **THEN** the tool shows the specific blocked reason for the affected Color and keeps that Color's generation unavailable
- **AND** it does not fabricate a template or output
- **AND** it keeps unrelated ready Colors available for generation

#### Scenario: Listing is protected
- **WHEN** the Item is Published, Rejected, archived, or otherwise read-only
- **THEN** the tool shows existing outputs read-only and disables template selection and application

### Requirement: Applying a template composes one output per applicable Listing Color and artwork variant
When the creator applies a selected ready Mockup Template, FusionCanvas SHALL resolve the current template revision, choose the source image whose active Color applicability covers each selected Item Color, and compose the Design Area artwork from that Color's assigned artwork variant into the saved image-space mapping. The design SHALL be scaled to fit within the mapping while preserving its aspect ratio and the template image dimensions SHALL be retained. A missing template source or mapping for one Color SHALL not block independently successful outputs for other selected Colors.

#### Scenario: Creator applies a complete template
- **WHEN** every selected Color has readable assigned Design Area artwork and an applicable template source image with a valid mapping
- **THEN** FusionCanvas composes and stores one mockup output for each selected Color and assigned artwork combination
- **AND** the Listing stage refreshes to show the new outputs

#### Scenario: A Color template is missing
- **WHEN** a selected Color has no applicable source image in the chosen template
- **THEN** FusionCanvas warns which Color is missing and does not create a fabricated output for it
- **AND** it still retains any independently successful outputs from the same apply operation
- **AND** it offers a path to configure the missing template without clearing the selected Color

#### Scenario: Mapping or source input is invalid
- **WHEN** a source image is missing, assigned Design Area artwork cannot be read, or the saved mapping is outside the source image bounds
- **THEN** FusionCanvas reports a recoverable per-output failure and leaves existing generated mockups unchanged

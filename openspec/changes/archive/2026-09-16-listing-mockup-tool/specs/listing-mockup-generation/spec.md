## ADDED Requirements

### Requirement: Listing-stage mockup tool presents applicable local templates
FusionCanvas SHALL present a Listing-stage mockup tool for an Item with an active Offering, showing ready local Mockup Templates belonging to that Offering and the Item's available Design files and selected Color values. The tool SHALL keep the existing Listing-stage read-only policy for protected Items.

#### Scenario: Listing has an applicable template and design files
- **WHEN** the creator opens Listing for an editable Item with an active Offering, selected Colors, Design PNGs, and ready Mockup Templates
- **THEN** the tool shows the template selector, an **Apply mockup template** action, and the existing generated mockups for that Item

#### Scenario: Listing cannot yet generate a mockup
- **WHEN** the Item has no Offering, no Design PNG, no selected Color, or no ready template
- **THEN** the tool shows the specific blocked reason and keeps generation unavailable without fabricating a template or output

#### Scenario: Listing is protected
- **WHEN** the Item is Published, Rejected, archived, or otherwise read-only
- **THEN** the tool shows existing outputs read-only and disables template selection and application

### Requirement: Applying a template composes one output per applicable design color
When the creator applies a selected ready Mockup Template, FusionCanvas SHALL resolve the current template revision, choose the source image whose active Color applicability covers each selected Item Color, and compose the matching Design PNG into the saved image-space mapping. The design SHALL be scaled to fit within the mapping while preserving its aspect ratio and the template image dimensions SHALL be retained.

#### Scenario: Creator applies a complete template
- **WHEN** every selected Color has a readable Design PNG and an applicable template source image with a valid mapping
- **THEN** FusionCanvas composes and stores one mockup output for each design/color combination using the selected template revision
- **AND** the Listing stage refreshes to show the new outputs

#### Scenario: A color template is missing
- **WHEN** a selected Color has no applicable source image in the chosen template
- **THEN** FusionCanvas warns which Color is missing and does not create a fabricated output for that Color
- **AND** it still retains any independently successful outputs from the same apply operation

#### Scenario: Mapping or source input is invalid
- **WHEN** a source image is missing, a Design PNG cannot be read, or the saved mapping is outside the source image bounds
- **THEN** FusionCanvas reports a recoverable per-output failure and leaves existing generated mockups unchanged

### Requirement: Generated mockups are managed, attributable Item assets
Each successful mockup SHALL be persisted as an Item-linked `MockupImage` Asset in managed workspace storage. The output metadata SHALL identify the source Item Color, template identity, template revision, and source Design Asset so outputs remain attributable after a later template change. Persistence SHALL be atomic for the Asset and link, and a failed save SHALL not leave an orphaned managed file.

#### Scenario: Creator changes the selected template later
- **WHEN** the creator selects another ready template and applies it after outputs already exist
- **THEN** FusionCanvas creates new attributable mockup assets without rewriting or deleting previous outputs

#### Scenario: Output persistence fails
- **WHEN** a composite file is created but the Asset/link snapshot cannot be saved
- **THEN** FusionCanvas removes the newly created managed file on a best-effort basis, preserves prior outputs, and reports a retryable error

### Requirement: Mockup application preserves interaction state and focus
The Listing mockup tool SHALL expose keyboard-reachable selection and application controls, show a busy state that prevents duplicate application, preserve the selected template and existing outputs after recoverable failures, and return focus to the application control after completion when practical.

#### Scenario: Application is in progress
- **WHEN** mockup generation is running
- **THEN** the selector and apply action are disabled, progress or busy status is visible, and a second application cannot start concurrently

#### Scenario: Creator replaces a template selection before applying
- **WHEN** the creator selects a different ready template but does not apply it
- **THEN** existing generated outputs remain unchanged until the creator explicitly activates **Apply mockup template**

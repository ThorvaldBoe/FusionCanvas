## ADDED Requirements

### Requirement: Design exposes global color removal for supported artwork
FusionCanvas SHALL expose a global color-removal action for a supported managed raster image in an editable Design context, and SHALL keep the action unavailable when the source is missing, unreadable, unsupported, or read-only.

#### Scenario: User opens color removal for an editable image
- **WHEN** an editable Design context contains a supported managed raster image and the user invokes global color removal
- **THEN** FusionCanvas opens a focused color-removal surface for that image
- **AND** the source image remains unchanged while the surface is open
- **AND** the surface identifies the selected source and provides color-picking, tolerance, preview, Apply, and Cancel controls

#### Scenario: Color removal is unavailable for an invalid source
- **WHEN** the selected image is missing, unreadable, unsupported, or the Design context is read-only
- **THEN** FusionCanvas disables or hides the color-removal action
- **AND** explains the applicable reason without modifying the source or workspace state

### Requirement: Global removal uses an explicit picked color and tolerance
FusionCanvas SHALL let the user pick a color from the source image and SHALL provide an adjustable tolerance with deterministic, documented matching semantics. Matching SHALL be global across the full raster rather than limited to pixels connected to the picked location.

#### Scenario: User picks a color and changes tolerance
- **WHEN** the user picks a visible pixel color and changes the tolerance
- **THEN** FusionCanvas updates the candidate removal mask using the picked color and the new tolerance
- **AND** tolerance zero matches only the exact picked RGB color
- **AND** increasing tolerance can include nearby RGB colors according to the documented color-distance rule
- **AND** existing transparent pixels remain transparent

#### Scenario: Matching color appears in separate regions
- **WHEN** the picked color or an in-range color appears in multiple disconnected regions, including inside the artwork
- **THEN** the candidate removal mask includes every matching pixel in the image
- **AND** no connected-region requirement prevents a matching region from being included

### Requirement: Color removal previews the candidate result before applying
FusionCanvas SHALL show a live preview of the candidate removal mask without mutating the source image or persisted workspace state.

#### Scenario: User reviews the removal overlay
- **WHEN** a picked color and tolerance produce a candidate mask
- **THEN** the color-removal surface visibly overlays or otherwise distinguishes the pixels that would become transparent
- **AND** the source artwork remains recognizable beneath the preview
- **AND** the preview updates after a subsequent color pick or tolerance change without requiring a new file import

#### Scenario: No pixels match the current settings
- **WHEN** the current color and tolerance match no visible pixels
- **THEN** FusionCanvas communicates that there is nothing to remove
- **AND** Apply is unavailable until the settings produce a valid candidate result

#### Scenario: Settings would remove all visible artwork
- **WHEN** the current color and tolerance would remove every visible pixel from the source
- **THEN** FusionCanvas warns that the result would contain no visible artwork
- **AND** blocks Apply until the user reduces the tolerance or cancels

### Requirement: Applying creates a transparent derived artwork result
FusionCanvas SHALL apply global color removal only after an explicit user action, encode the result as a transparent PNG, preserve the original source, and make the derived result available in the same Design workflow.

#### Scenario: User applies a valid removal
- **WHEN** the user chooses Apply with a valid candidate mask that leaves visible artwork
- **THEN** FusionCanvas writes a transparent PNG-derived managed file
- **AND** creates the corresponding derived Design/supporting-image record and relationship through the existing application and workspace-file boundaries
- **AND** leaves the original source file and record unchanged
- **AND** selects or otherwise clearly identifies the new derived result after the operation succeeds

#### Scenario: User cancels before applying
- **WHEN** the user changes color-removal settings and chooses Cancel or closes the surface
- **THEN** FusionCanvas discards the candidate mask and preview state
- **AND** leaves the source file, derived-file collection, and persisted workspace unchanged
- **AND** returns focus to the invoking image action or its owning Design surface

#### Scenario: Derived-file persistence fails
- **WHEN** file creation succeeds but the corresponding workspace persistence fails
- **THEN** FusionCanvas best-effort removes the newly created managed file
- **AND** reports a recoverable error
- **AND** leaves the original source and last confirmed workspace state intact

### Requirement: Color-removal interaction preserves user control
FusionCanvas SHALL keep the color-removal workflow explicit, keyboard reachable, and coherent across loading, success, cancellation, and recoverable failure states.

#### Scenario: Processing is in progress
- **WHEN** preview recomputation or Apply is running
- **THEN** FusionCanvas shows a busy state, prevents conflicting duplicate operations, and keeps Cancel available when cancellation is safe

#### Scenario: User operates the surface by keyboard
- **WHEN** the user opens color removal without relying on a pointer
- **THEN** color picking, tolerance adjustment, Apply, and Cancel are reachable with meaningful accessible names
- **AND** after Apply focus moves to the new derived result or its owning Design surface
- **AND** after Cancel focus returns to the invoking image action or its owning Design surface

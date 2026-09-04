## Purpose

Defines the Offering-scoped Mockup Template management surface and editing routes.

## Requirements

### Requirement: Mockup Templates connect provider images to one Design Area
FusionCanvas SHALL provide focused management of Mockup Templates for one Blueprint Offering. Each template SHALL identify a provider-catalog mockup image, one authoritative target Design Area from the same Offering, and applicable Variant coverage derived through the existing color-level template binding and Design Area compatibility rules.

#### Scenario: User opens Mockup Template management
- **WHEN** the user opens Mockup Template management for a Blueprint Offering
- **THEN** FusionCanvas lists only templates belonging to that Offering
- **AND** each template summary identifies its name, target Design Area, applicable Color or Variant summary, and lifecycle state
- **AND** the selected template opens in a focused editor

#### Scenario: Mockup Template management preserves master-detail composition
- **WHEN** Mockup Template management has one or more records or an active draft
- **THEN** FusionCanvas presents the template collection and one focused selected-or-new editor as visually distinct peer regions
- **AND** makes the provider mockup image and visual Design Area mapping prominent within the editor
- **AND** keeps identity, Design Area, Color applicability, numeric mapping, advanced provider data, and save actions grouped around that editor
- **AND** may stack the list and editor only when available width requires it

#### Scenario: User creates a template from a provider-catalog image
- **WHEN** the user chooses an available provider-catalog mockup image, a Design Area from the same Offering, and valid color-level applicability
- **THEN** FusionCanvas creates a template draft linked to that image and authoritative Design Area identity
- **AND** derives compatible concrete size/color Variants rather than persisting per-size template overrides

#### Scenario: Target Design Area is incompatible
- **WHEN** the selected Design Area does not cover every concrete Variant implied by the template's color-level applicability
- **THEN** FusionCanvas rejects confirmation and identifies the incompatible Variants
- **AND** never silently accepts a partially compatible template

#### Scenario: Offering has no Design Areas
- **WHEN** the user opens template management before any Design Area exists
- **THEN** FusionCanvas shows a blocked empty state explaining that a Design Area is required
- **AND** provides a route back to Design Area management without fabricating a target

### Requirement: Each template revision owns an image-space Design Area mapping
FusionCanvas SHALL store the mapping of the selected Design Area into the specific mockup image as revisioned template configuration with image-space X, Y, width, and height values. The mapping SHALL be editable through a visual placement rectangle, while numeric values remain visible and editable as supporting technical controls.

#### Scenario: User positions a Design Area visually
- **WHEN** the user moves or resizes the visual placement rectangle over the selected mockup image
- **THEN** FusionCanvas updates the draft X, Y, width, and height values in the image's pixel coordinate space
- **AND** keeps the rectangle and numeric values synchronized

#### Scenario: User edits numeric mapping values
- **WHEN** the user enters valid X, Y, width, or height values
- **THEN** FusionCanvas updates the visual rectangle to represent the same image-space mapping
- **AND** does not apply artwork or render a composite

#### Scenario: Mapping exceeds image bounds
- **WHEN** the draft mapping has non-positive size or extends outside the known mockup image bounds
- **THEN** FusionCanvas blocks confirmation with recoverable guidance
- **AND** preserves the last confirmed template revision

#### Scenario: User changes confirmed template mapping
- **WHEN** the user saves a changed provider image, target Design Area, applicability, or image-space mapping
- **THEN** FusionCanvas creates or records a new template revision according to the authoritative revision lifecycle
- **AND** prior generated outputs remain attributable to their original revision

### Requirement: Provider mockup references are advanced technical data
FusionCanvas SHALL preserve an optional stable provider mockup reference as advanced technical data normally populated from Printify integration data, distinct from the user-facing template name and the actual fulfillment Provider identity.

#### Scenario: Imported mockup image has a provider reference
- **WHEN** Printify supplies a provider-catalog mockup image and stable reference
- **THEN** FusionCanvas preserves the reference for synchronization or diagnostics
- **AND** exposes it through Advanced or secondary disclosure rather than as the template's primary label

#### Scenario: Provider reference changes display context
- **WHEN** user-facing labels or Provider display names change
- **THEN** the template retains its stable provider mockup reference and Design Area identity
- **AND** relationships do not depend on mutable labels

### Requirement: Template drafts are explicit and scoped
FusionCanvas SHALL keep new or edited Mockup Template state as a draft until explicitly confirmed and SHALL guard meaningful changes during selection or navigation transitions.

#### Scenario: User cancels a template draft
- **WHEN** the user cancels a new or edited template before confirmation
- **THEN** FusionCanvas persists no draft mapping or partial template revision
- **AND** returns focus to the invoking template or add action

#### Scenario: User leaves with unsaved template changes
- **WHEN** the user attempts to select another template or leave the surface with meaningful unsaved changes
- **THEN** FusionCanvas offers to discard the changes or keep editing
- **AND** keep-editing preserves the selected template, draft mapping, and focus

#### Scenario: User reviews an archived Store
- **WHEN** Mockup Template management is opened for an archived Store
- **THEN** FusionCanvas presents templates and mapping data read-only
- **AND** disables image selection, mapping edits, and lifecycle mutations

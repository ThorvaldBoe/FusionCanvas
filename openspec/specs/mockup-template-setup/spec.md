# mockup-template-setup Specification

## Purpose
TBD - created by archiving change support-printify-store-catalog-mockup-setup. Update Purpose after archive.
## Requirements
### Requirement: Mockup Templates target a concrete Placeholder
FusionCanvas SHALL require every Mockup Template to reference one authoritative `TargetPlaceholderId` belonging to the same Blueprint Offering as the template. A mutable position label or optional denormalized position key SHALL NOT be the authoritative render-time association.

#### Scenario: User creates a template for an offering
- **WHEN** the user saves a Mockup Template and selects one active Placeholder from that template's Blueprint Offering
- **THEN** FusionCanvas persists the selected Placeholder identity as the template's `TargetPlaceholderId`
- **AND** changing the Placeholder's display label later does not break or retarget the template

#### Scenario: User selects a Placeholder from another offering
- **WHEN** a template save refers to a Placeholder owned by another Blueprint Offering
- **THEN** FusionCanvas rejects the save with recoverable guidance
- **AND** confirmed template data remains unchanged

#### Scenario: Offering default prefills a new template
- **WHEN** a Blueprint Offering has an active default Placeholder and the user starts a new Mockup Template
- **THEN** FusionCanvas may preselect that Placeholder as a convenience
- **AND** the saved template's own `TargetPlaceholderId` remains authoritative if the offering default later changes

### Requirement: Mockup Template color bindings use stable Color Option Values
FusionCanvas SHALL require every Mockup Template Color Variant to bind to exactly one Offering Option Value whose owning Option has `OptionKind = Color` and belongs to the same Blueprint Offering as the template. FusionCanvas SHALL allow at most one active color record for a given Mockup Template and Color Option Value.

#### Scenario: User configures one template color
- **WHEN** the user adds an active color record to a Mockup Template and chooses a Color Option Value from that template's offering
- **THEN** FusionCanvas persists the Color Option Value identity
- **AND** the record remains bound when the option value's display label changes

#### Scenario: User chooses a non-Color value
- **WHEN** the user attempts to bind a template color record to a Size or Other Option Value
- **THEN** FusionCanvas rejects the save with guidance to choose a Color value
- **AND** does not persist an invalid binding

#### Scenario: User chooses a Color from another offering
- **WHEN** the user attempts to bind a template color record to a Color Option Value owned by another Blueprint Offering
- **THEN** FusionCanvas rejects the save
- **AND** preserves confirmed template-color records

#### Scenario: Duplicate active template color is added
- **WHEN** an active template-color record already exists for the same Mockup Template and Color Option Value
- **THEN** FusionCanvas rejects another active record for that pair
- **AND** allows a previously archived record to be restored only when no other active record conflicts

### Requirement: Mockup binding is color-level only
One active Mockup Template Color Variant SHALL supply the primary future mockup source for every compatible concrete Offering Variant that contains its bound Color Option Value. FusionCanvas SHALL derive compatible concrete Variants from Option Value membership and SHALL NOT persist size-specific, concrete-Variant, per-dimension, or generalized mockup override structures in this module.

#### Scenario: Multiple sizes share one template color
- **WHEN** an offering contains Small, Medium, and Large concrete Variants with the same bound Color Option Value
- **THEN** all three Variants resolve to the same active Mockup Template Color Variant
- **AND** no separate size or concrete-Variant mockup mapping is required

#### Scenario: Offering sizes change
- **WHEN** a compatible size Variant is added or removed from the offering
- **THEN** the template's Color Option Value binding remains unchanged
- **AND** FusionCanvas requires no repair of template mappings

#### Scenario: User reviews template configuration
- **WHEN** the Mockup Template editor is shown
- **THEN** it provides no per-size, concrete-Variant, or generic override control
- **AND** persisted template configuration contains no reserved override structure

### Requirement: Template configuration has a revision lifecycle
FusionCanvas SHALL preserve a stable Mockup Template identity and immutable attributable template revisions so changes to target, source state, or later template configuration affect future generation only. Any future generated listing mockup SHALL retain an immutable reference or snapshot identifying the exact template revision and Color Option Value used.

#### Scenario: User changes template configuration
- **WHEN** the user confirms a change that affects a Mockup Template's future output
- **THEN** FusionCanvas creates or advances an attributable revision
- **AND** preserves prior revision identity instead of rewriting its historical meaning

#### Scenario: Future generated mockup records provenance
- **WHEN** a future rendering capability creates a listing mockup from a template color
- **THEN** the generated record identifies the exact template revision and Color Option Value used
- **AND** later template, Placeholder, or option-label changes cannot alter that historical attribution

### Requirement: Catalog lifecycle protects template dependencies
FusionCanvas SHALL prefer archive or deactivation for template-related catalog records and SHALL prevent destructive changes that would leave active or historical template relationships invalid.

#### Scenario: User removes a referenced Placeholder
- **WHEN** the user requests permanent deletion of a Placeholder referenced by a Mockup Template
- **THEN** FusionCanvas blocks deletion
- **AND** explains that dependent templates must be reassigned or removed first

#### Scenario: User retires a referenced Color Option Value
- **WHEN** the user requests retirement or deletion of a Color Option Value referenced by an active template-color record
- **THEN** FusionCanvas requires the dependent template-color record to be archived, reassigned, or removed explicitly
- **AND** does not silently orphan the template color

#### Scenario: User archives a template color
- **WHEN** the user archives an active Mockup Template Color Variant
- **THEN** it is no longer available as the active primary source for future use
- **AND** its identity remains available for historical attribution

### Requirement: Placeholder compatibility is explicit
Configuring a Mockup Template SHALL require a target Placeholder from its offering, and any future application of that template SHALL verify that the target Placeholder is compatible with every selected listing Variant. Incompatible Variants SHALL be rejected or reported and SHALL never be silently rendered.

#### Scenario: Template target covers selected Variants
- **WHEN** a future listing workflow applies a template whose target Placeholder is compatible with every selected concrete Variant
- **THEN** the template is eligible for those Variants
- **AND** the listing workflow does not ask the user to select another target position at application time

#### Scenario: Template target does not cover every selected Variant
- **WHEN** a future listing workflow applies a template and at least one selected concrete Variant is not compatible with its target Placeholder
- **THEN** FusionCanvas rejects or clearly reports the incompatible Variant set
- **AND** does not silently omit or render those Variants

### Requirement: Template source assets and placement remain deliberately empty
This module SHALL represent Mockup Template and color configuration with a clear not-configured future source-image state, and SHALL NOT provide image upload or import, asset editing, coordinates, slots, transforms, rendering, composition semantics, or placement controls.

#### Scenario: User creates a template in this module
- **WHEN** a valid Mockup Template and color binding are saved
- **THEN** the editor clearly indicates that template source imagery is not configured and belongs to future work
- **AND** the save requires no image, asset, placement, or rendering configuration

#### Scenario: Contributor inspects template storage
- **WHEN** a contributor reviews the persisted model introduced by this module
- **THEN** it contains no x/y coordinates, placement dimensions, scale, rotation, transform, slot, compositor, or override schema
- **AND** any future extension marker has no rendering or placement semantics

### Requirement: Mockup Template setup stays in the focused Store Editor
FusionCanvas SHALL provide Mockup Template administration through progressive disclosure in the selected Store's dedicated Store Editor and SHALL preserve drafts, selection, focus, keyboard access, and destructive confirmations according to the existing focused-editor pattern.

#### Scenario: Offering has no templates
- **WHEN** the user opens Mockup Templates for a Blueprint Offering with no configured templates
- **THEN** FusionCanvas shows explanatory empty guidance and one clear action to create a template
- **AND** does not fabricate a template, source image, color, or Placeholder selection

#### Scenario: User starts a template draft
- **WHEN** the user starts a new Mockup Template
- **THEN** FusionCanvas focuses the primary Name field or first required control
- **AND** does not persist the draft until the user explicitly saves it

#### Scenario: User leaves a meaningful template draft
- **WHEN** the user changes Store, catalog level, tab, selection, or closes the Store Editor with meaningful unsaved template changes
- **THEN** FusionCanvas offers the existing save/discard/cancel or discard/keep-editing safeguard appropriate to the editor
- **AND** cancellation retains the draft, context, selection, and focus

#### Scenario: Archived Store is reviewed
- **WHEN** the selected Store is archived
- **THEN** FusionCanvas displays its Mockup Templates and color bindings read-only
- **AND** disables create, edit, archive, restore-conflicting, or relationship-changing actions


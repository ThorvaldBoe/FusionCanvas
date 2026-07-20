## ADDED Requirements

### Requirement: Stores define a three-level mockup configuration
FusionCanvas SHALL allow each store to define zero or more MockupProducts, each MockupProduct to define zero or more MockupTemplates, and each MockupTemplate to define zero or more MockupColorVariants, with stable identity, timestamps, an active flag, and an archive flag at each level.

#### Scenario: User adds a mockup product to a store
- **WHEN** the user creates a MockupProduct for an active store
- **THEN** FusionCanvas creates the product with stable identity and timestamps owned by that store
- **AND** the product starts active and non-archived

#### Scenario: User adds a template to a product
- **WHEN** the user creates a MockupTemplate for an active MockupProduct
- **THEN** FusionCanvas creates the template with stable identity and timestamps owned by that product

#### Scenario: User adds a color variant to a template
- **WHEN** the user creates a MockupColorVariant for an active MockupTemplate
- **THEN** FusionCanvas creates the color variant with stable identity and timestamps owned by that template

#### Scenario: Hierarchy is store-scoped
- **WHEN** a MockupProduct, MockupTemplate, or MockupColorVariant is represented
- **THEN** each belongs to exactly one parent in the hierarchy
- **AND** the hierarchy is scoped to one store

### Requirement: Mockup products carry supplier and design-area data
FusionCanvas SHALL allow each MockupProduct to record vendor name, product name, provider product name, product type, design area width and height in pixels, notes, an active flag, and metadata, and SHALL preserve unknown metadata keys during edits.

#### Scenario: User configures a mockup product
- **WHEN** the user edits vendor, product, provider product name, product type, or design area dimensions
- **THEN** FusionCanvas persists the values atomically
- **AND** preserves the product's identity, templates, and unknown metadata

#### Scenario: Design area dimensions are validated
- **WHEN** the user enters design area width or height
- **THEN** FusionCanvas accepts positive integer pixel values
- **AND** rejects zero, negative, or non-integer values with an actionable error

### Requirement: Mockup templates carry placement mapping
FusionCanvas SHALL allow each MockupTemplate to record name, template asset reference, view name, default color name, placement X, placement Y, placement width, optional placement height, optional placement scale, optional rotation (default 0), an active flag, and metadata, and SHALL prefer placement width over scale when both are present.

#### Scenario: User configures a template mapping
- **WHEN** the user edits placement X, Y, width, height, scale, or rotation
- **THEN** FusionCanvas persists the values atomically
- **AND** preserves the template's identity, color variants, and unknown metadata

#### Scenario: Width preferred over scale
- **WHEN** a template has both placement width and placement scale
- **THEN** FusionCanvas uses placement width for generation
- **AND** treats scale as a fallback when width is absent

#### Scenario: Rotation defaults to zero
- **WHEN** the user omits rotation
- **THEN** FusionCanvas stores zero as the default

### Requirement: Mockup color variants preserve exact provider color names
FusionCanvas SHALL allow each MockupColorVariant to record provider color name (exact, as entered), optional display color name, optional color-specific template asset reference, optional swatch hex, sort order, an active flag, and metadata, and SHALL preserve the provider color name verbatim.

#### Scenario: User enters a provider color name
- **WHEN** the user enters a provider color name for a color variant
- **THEN** FusionCanvas stores it verbatim
- **AND** does not normalize, title-case, or map it to a palette

#### Scenario: Color variant uses its own template asset
- **WHEN** a color variant references a color-specific template asset
- **THEN** generation uses the color-specific asset for that color
- **AND** the parent template asset is used only for variants without a color-specific asset

### Requirement: Template and color-specific assets go through asset-management
FusionCanvas SHALL store template images and color-specific template images as assets through asset-management, with MockupTemplate as a valid asset context kind, and SHALL preserve referenced assets when a template or color variant is archived or removed.

#### Scenario: User attaches a template image
- **WHEN** the user attaches an image as a template asset
- **THEN** FusionCanvas imports or links the asset through asset-management
- **AND** references the asset from the template

#### Scenario: Archiving a template preserves its asset
- **WHEN** a template is archived
- **THEN** the template asset record and managed file remain unchanged
- **AND** the asset remains reachable at store level

### Requirement: Active set is exposed to the Basic Listing Tool
FusionCanvas SHALL expose only active, non-archived MockupProducts, MockupTemplates, and MockupColorVariants to the Basic Listing Tool, and SHALL exclude archived items from generation choices.

#### Scenario: Active items appear in the listing tool
- **WHEN** the Basic Listing Tool queries available mockup product settings for a store
- **THEN** only active, non-archived products, templates, and color variants are returned

#### Scenario: Archiving removes from the active set
- **WHEN** a product, template, or color variant is archived
- **THEN** it is removed from the active set
- **AND** existing mockup records that reference it remain valid and retain their regeneration metadata

### Requirement: Store settings surface manages the configuration
FusionCanvas SHALL provide a focused store settings surface opened from the store that supports add, edit, archive, restore, and active-flag toggle for products, templates, and color variants, with progressive disclosure across the three levels, explicit save, dirty tracking, unsaved-change prompts, and shared desktop control guidance.

#### Scenario: User opens the store settings surface
- **WHEN** the user invokes Mockup Product Settings for a store
- **THEN** FusionCanvas opens the focused surface with that store's products preloaded
- **AND** keeps canonical tree and document context intact

#### Scenario: User edits across levels with progressive disclosure
- **WHEN** the user expands a product to show its templates and a template to show its color variants
- **THEN** FusionCanvas progressively discloses the lower levels
- **AND** keeps the current selection visible when a level is collapsed

#### Scenario: User leaves meaningful unsaved changes
- **WHEN** the focused surface contains meaningful unsaved changes and the user switches item or closes
- **THEN** FusionCanvas asks whether to save, discard, or cancel
- **AND** retains the current draft and focus when cancellation is chosen

#### Scenario: Archive and restore are reversible
- **WHEN** the user archives a product, template, or color variant
- **THEN** FusionCanvas marks it archived and removes it from the active set
- **AND** the user can restore it later through the same surface

### Requirement: Mockup product settings persist atomically
FusionCanvas SHALL persist MockupProduct, MockupTemplate, and MockupColorVariant create, edit, archive, restore, active-flag toggle, and reorder operations as atomic snapshot operations in the active workspace database, reloading the latest snapshot before each mutation and saving once per operation.

#### Scenario: Workspace reloads after settings operations
- **WHEN** a successful settings operation is followed by an application or workspace database reload
- **THEN** the resulting products, templates, color variants, fields, active flags, and archive flags match the last successful persisted state

#### Scenario: Persistence fails during a settings operation
- **WHEN** the repository cannot save a settings operation after an optimistic projection
- **THEN** FusionCanvas reports a recoverable error
- **AND** restores the last confirmed configuration state
- **AND** retains recoverable user input needed to retry when applicable

### Requirement: Store settings surface follows shared desktop control guidance
FusionCanvas SHALL present the surface with compact action sizing, clear tooltips for icon-only commands, predictable keyboard flow, accessible add/edit/archive/restore actions, busy states that prevent duplicate submission, and error states that preserve selection and focus.

#### Scenario: Settings operation is in progress or fails
- **WHEN** a create, edit, archive, restore, or active-flag toggle is running or fails validation or persistence
- **THEN** FusionCanvas prevents duplicate submission
- **AND** keeps the surface available to report success or an actionable error
- **AND** preserves selection and recoverable input after failure

#### Scenario: Keyboard-only configuration
- **WHEN** the user activates add, edit, archive, restore, or active-flag toggle without a pointer
- **THEN** all essential actions are reachable through the keyboard
- **AND** focus returns to a meaningful control after each transition

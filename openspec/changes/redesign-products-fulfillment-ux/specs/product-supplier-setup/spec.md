## ADDED Requirements

### Requirement: Catalog management uses progressive disclosure

The Products & fulfillment editor SHALL present catalog management as three focused levels: Products overview, Product detail, and Fulfillment offering detail. It SHALL show only the controls relevant to the active level while keeping the current selection and navigation path visible.

#### Scenario: User opens the catalog editor
- **WHEN** the user opens Products & fulfillment for an active Store
- **THEN** the editor shows a Products overview with the product list, an empty state when no products exist, and one primary “New product” action
- **AND** it does not show offering, variant, or printable-area forms until a Product or offering is opened

#### Scenario: User opens a product
- **WHEN** the user selects a Product from the overview
- **THEN** the editor shows Product detail with the Product identity, compact catalog summary, Product details, and its Fulfillment offerings
- **AND** the primary creation action is “Add fulfillment offering” and is scoped to that Product

#### Scenario: User opens an offering
- **WHEN** the user selects a fulfillment offering from Product detail
- **THEN** the editor shows a breadcrumb or equivalent path identifying the Product and offering
- **AND** it shows offering Basics, Variants, Printable areas, and Advanced sections without exposing unrelated Product-level lists

### Requirement: Catalog controls use unambiguous terminology and ownership

The Products & fulfillment editor SHALL distinguish Product, fulfillment offering, variant, and printable area in visible labels and action names. An action that creates or removes a catalog record SHALL name the record type it affects.

#### Scenario: User creates catalog records
- **WHEN** the user is at the Products overview, Product detail, or offering detail level
- **THEN** the primary actions are labeled “New product”, “Add fulfillment offering”, “Add variant”, and “Add printable area” respectively
- **AND** no generic “Add” action is used for creating a variant or other catalog record

#### Scenario: User removes catalog records
- **WHEN** the user requests removal of an offering, variant, or printable area
- **THEN** the action identifies the target as “Delete offering”, “Remove variant”, or “Remove printable area”
- **AND** existing explicit confirmation and referenced-record safeguards remain in effect

### Requirement: Offering details disclose dependent controls in a logical order

The offering detail surface SHALL group controls into Basics, Variants, Printable areas, and Advanced sections. Variant applicability controls SHALL be disclosed only when the offering has variants, and fixed-provider fields SHALL be shown only for fixed-provider offerings.

#### Scenario: User reviews an offering
- **WHEN** an offering detail surface is active
- **THEN** Basics is available first and identifies whether the offering is a fixed provider or Printify Choice network
- **AND** Variants and Printable areas show their current counts and records before an add form is opened
- **AND** external identifiers are placed in a collapsed or secondary Advanced section

#### Scenario: User adds a variant
- **WHEN** the user activates “Add variant”
- **THEN** a focused form opens with labeled Color and Size fields and an explicit “Add variant” action
- **AND** the new variant appears in the selected offering without changing the Product or offering selection

#### Scenario: User adds a printable area with variants
- **WHEN** the user activates “Add printable area” for an offering that has variants
- **THEN** the form exposes labeled Name, Position, Decoration method, Width (px), Height (px), and an “Applies to” control
- **AND** “Applies to” defaults to all variants and allows selecting only variants from the current offering

#### Scenario: User reviews a Choice offering
- **WHEN** the selected offering is a Printify Choice network
- **THEN** the editor keeps the existing variable-network warning visible near the offering or printable-area guidance
- **AND** it does not show or fabricate a fixed Provider name

### Requirement: Catalog navigation preserves editing safeguards

The progressive-disclosure editor SHALL treat back navigation, breadcrumbs, level changes, and selection changes as guarded editor transitions. It SHALL preserve drafts when the user cancels a transition and SHALL use the existing save/discard behavior before abandoning meaningful unsaved Product or offering edits.

#### Scenario: User navigates with unsaved Product edits
- **WHEN** the user has meaningful unsaved Product edits and selects another Product, opens an offering, or uses Back
- **THEN** the editor offers the existing Save, Discard, and Cancel choices
- **AND** Cancel keeps the current level, selection, fields, and focus

#### Scenario: User starts a nested draft
- **WHEN** the user starts a new Product, offering, variant, or printable-area form and cancels or navigates away before saving
- **THEN** the draft is not persisted
- **AND** the editor returns to the invoking level with a sensible selection and no orphan record

#### Scenario: User completes a destructive action
- **WHEN** the user confirms deletion or removal from any disclosure level
- **THEN** the existing service validation and reference safeguards decide whether the operation succeeds
- **AND** after success the editor selects a valid remaining record or shows the relevant empty state

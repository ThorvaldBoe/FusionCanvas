# Product Supplier Setup

## Purpose

Defines how a Store catalog maintains product blueprints and fulfillment offerings, including provider-compatible variants and printable areas, Printify Choice as a variable network, and safe handling of catalog edits that reference selected Item targets.

## Requirements

### Requirement: Store catalog maintains product and fulfillment structure
FusionCanvas SHALL let a user maintain product blueprints and one or more fulfillment offerings within an active Store, and SHALL preserve that catalog across workspace reload.

#### Scenario: User adds a product and fixed-provider offering
- **WHEN** the user saves a valid product blueprint and a valid named fixed-provider offering in Store Management
- **THEN** FusionCanvas persists both records with stable identities scoped to the selected Store
- **AND** the offering remains associated with that product after reload

#### Scenario: Catalog is isolated by Store
- **WHEN** the user opens product setup for another Store
- **THEN** FusionCanvas shows only that Store's products and offerings
- **AND** it does not expose or permit editing catalog records from another Store

### Requirement: Offerings retain provider-compatible variants and design areas
FusionCanvas SHALL allow an offering to hold concrete option combinations and printable areas containing position, decoration method, positive pixel width and height, and applicable variants.

#### Scenario: User creates a variant-specific printable area
- **WHEN** the user saves an offering with variants and selects applicable variants for a printable area
- **THEN** FusionCanvas persists the area with its position, decoration method, dimensions, and variant applicability
- **AND** the area remains associated only with variants from that offering

#### Scenario: User enters invalid printable dimensions or references
- **WHEN** the user attempts to save a printable area with non-positive dimensions or a variant from another offering
- **THEN** FusionCanvas rejects the save with recoverable guidance
- **AND** it leaves confirmed catalog data unchanged

### Requirement: Printify Choice is represented as a variable network
FusionCanvas SHALL represent Printify Choice as a fulfillment-network offering rather than as a fixed provider and SHALL disclose that exact provider selection and design consistency can vary.

#### Scenario: User configures a Choice offering
- **WHEN** the user creates a Printify Choice offering
- **THEN** FusionCanvas does not require or display a fixed provider identity
- **AND** it identifies the offering as a variable fulfillment network

#### Scenario: User reviews Choice design areas
- **WHEN** a configured Choice offering provides selectable printable areas
- **THEN** FusionCanvas shows a consistency warning with those areas
- **AND** the areas remain eligible for design-target selection

### Requirement: Catalog edits preserve selected Item targets
FusionCanvas SHALL require explicit safe handling before a user permanently removes a catalog record that is selected by an Item.

#### Scenario: User removes an unreferenced area
- **WHEN** the user confirms removal of a printable area that no Item has selected
- **THEN** FusionCanvas removes that area
- **AND** it preserves unrelated products, offerings, variants, and Items

#### Scenario: User removes a referenced record
- **WHEN** the user requests removal of a product, offering, or printable area that is selected by one or more Items
- **THEN** FusionCanvas blocks the removal
- **AND** explains that the target must first be cleared or replaced on the affected Items

### Requirement: Catalog management uses progressive disclosure
The Products & fulfillment editor SHALL present catalog management as three focused levels: Products overview, Product detail, and Fulfillment offering detail. It SHALL show only controls relevant to the active level while keeping the current selection and navigation path visible.

#### Scenario: User opens the catalog editor
- **WHEN** the user opens Products & fulfillment for an active Store
- **THEN** the editor shows a Products overview with the product list, an empty state when no products exist, and one primary “New product” action
- **AND** it does not show offering, variant, or printable-area forms until a Product or offering is opened

#### Scenario: User opens a product
- **WHEN** the user selects a Product from the overview
- **THEN** the editor shows Product detail with the Product identity, compact catalog summary, Product details, and its Fulfillment offerings
- **AND** the primary creation action is “Add fulfillment offering” scoped to that Product

#### Scenario: User opens an offering
- **WHEN** the user selects a fulfillment offering from Product detail
- **THEN** the editor shows a breadcrumb or equivalent path identifying the Product and offering
- **AND** it shows offering Basics, Variants, Printable areas, and Advanced sections without unrelated Product-level lists

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
- **AND** Variants and Printable areas show current counts and records before an add form is opened
- **AND** external identifiers are placed in a collapsed or secondary Advanced section

#### Scenario: User adds a variant
- **WHEN** the user activates “Add variant”
- **THEN** a focused form opens with labeled Color and Size fields and an explicit “Add variant” action
- **AND** the new variant appears in the selected offering without changing Product or offering selection

#### Scenario: User adds a printable area with variants
- **WHEN** the user activates “Add printable area” for an offering that has variants
- **THEN** the form exposes labeled Name, Position, Decoration method, Width (px), Height (px), and an “Applies to” control
- **AND** “Applies to” defaults to all variants and allows selecting only variants from the current offering

#### Scenario: User reviews a Choice offering
- **WHEN** the selected offering is a Printify Choice network
- **THEN** the editor keeps the existing variable-network warning visible near the offering or printable-area guidance
- **AND** it does not show or fabricate a fixed Provider name

### Requirement: Catalog navigation preserves editing safeguards
The progressive-disclosure editor SHALL treat back navigation, breadcrumbs, level changes, and selection changes as guarded editor transitions. It SHALL preserve drafts when the user cancels a transition and SHALL use existing save/discard behavior before abandoning meaningful unsaved Product or offering edits.

#### Scenario: User navigates with unsaved Product edits
- **WHEN** the user has meaningful unsaved Product edits and selects another Product, opens an offering, or uses Back
- **THEN** the editor offers existing Save, Discard, and Cancel choices
- **AND** Cancel keeps the current level, selection, fields, and focus

#### Scenario: User starts a nested draft
- **WHEN** the user starts a new Product, offering, variant, or printable-area form and cancels or navigates away before saving
- **THEN** the draft is not persisted
- **AND** the editor returns to the invoking level with a sensible selection and no orphan record

#### Scenario: User completes a destructive action
- **WHEN** the user confirms deletion or removal from any disclosure level
- **THEN** existing service validation and reference safeguards decide whether the operation succeeds
- **AND** after success the editor selects a valid remaining record or shows the relevant empty state

### Requirement: Provider mockup image selection communicates source and recovery
FusionCanvas SHALL identify the Mockup Template image selector with a persistent visible label and accessible name and SHALL explain that candidates come from the active Offering's provider catalog. The guidance SHALL remain available while candidates load and when the result is available, empty, unavailable, or failed. It SHALL NOT imply that local upload or drag/drop is supported, and unavailable or failed states SHALL identify a supported provider setup or synchronization next action without fabricating candidates.

#### Scenario: User opens provider image selection
- **WHEN** the Mockup Template editor is shown
- **THEN** the selector has the visible label and accessible name **Provider mockup image**
- **AND** nearby instructions explain how to choose an Offering provider-catalog image
- **AND** state that local upload and drag/drop are not available

#### Scenario: Provider catalog is loading
- **WHEN** provider mockup candidates are being requested
- **THEN** the persistent guidance remains visible
- **AND** state text explains that provider-catalog images are loading

#### Scenario: Provider catalog provides candidates
- **WHEN** one or more provider mockup candidates are available
- **THEN** the selector exposes those candidates
- **AND** state text prompts the user to choose the provider view that matches the target Design Area

#### Scenario: Provider catalog is empty
- **WHEN** the configured provider catalog is available but contains no mockup images for the Offering
- **THEN** state text distinguishes the empty result from loading and failure
- **AND** directs the user to sync or review the Offering's provider catalog setup

#### Scenario: Provider catalog is unavailable
- **WHEN** no provider catalog source exists or the source reports that it is unavailable
- **THEN** state text explains the supplied reason when available
- **AND** directs the user to configure or sync provider catalog data before returning

#### Scenario: Provider catalog request fails
- **WHEN** loading provider mockup candidates raises an error
- **THEN** state text identifies the recoverable load failure without exposing a fabricated candidate
- **AND** directs the user to review provider setup or retry synchronization

### Requirement: Design Area management uses a focused guarded editor dialog
FusionCanvas SHALL keep the default Manage Design Areas surface focused on the Offering-scoped collection without reserving an inline editor column. The **Add Design Area** action and each row's **Edit** action SHALL open the same modal dialog, owned by the Store Editor, with mode-specific title and draft values. The dialog SHALL reuse existing Design Area validation, compatibility, persistence, and referenced-record behavior; SHALL close only after successful save or confirmed cancellation; and SHALL not permit a workspace or Offering context change to leave a stale editable dialog open.

#### Scenario: User reviews the Design Area collection
- **WHEN** Manage Design Areas is open without an Add/Edit dialog
- **THEN** the collection uses the available management-surface width
- **AND** no inline Design Area editor is rendered or reserves space

#### Scenario: User adds a Design Area
- **WHEN** the user selects **Add Design Area** for an editable Offering with Variants
- **THEN** FusionCanvas opens one modal dialog titled **Add Design Area** with empty/default form values
- **AND** places initial focus in the Name field
- **AND** leaves the parent collection and Offering context visible but unavailable behind the modal dialog

#### Scenario: User edits a Design Area
- **WHEN** the user selects **Edit** for a Design Area
- **THEN** FusionCanvas opens the same modal dialog titled **Edit Design Area**
- **AND** populates identity, maximum design size, artwork guidance, compatibility, and advanced provider data from that stable Design Area identity

#### Scenario: Save fails validation or persistence
- **WHEN** the user attempts to save an invalid draft or persistence reports a recoverable failure
- **THEN** the dialog remains open with the draft values and guidance preserved
- **AND** confirmed Design Area data remains unchanged

#### Scenario: Save succeeds
- **WHEN** the user saves a valid Add or Edit draft successfully
- **THEN** FusionCanvas persists the Design Area exactly once through the existing service path
- **AND** closes the dialog, refreshes/selects the saved Design Area, and returns focus to the invoking Add/Edit control when practical

#### Scenario: User dismisses an unchanged draft
- **WHEN** the user selects **Cancel**, presses Escape, or closes the dialog without meaningful changes
- **THEN** FusionCanvas closes the dialog without persisting anything
- **AND** returns focus to the invoking Add/Edit control

#### Scenario: User dismisses a meaningful draft
- **WHEN** the user selects **Cancel**, presses Escape, or closes the dialog after making meaningful unsaved changes
- **THEN** FusionCanvas asks whether to discard the draft or keep editing
- **AND** keep-editing preserves all values and focus within the dialog
- **AND** confirmed discard closes the dialog without persisting the draft

#### Scenario: Editing context becomes stale
- **WHEN** the active Offering or workspace changes while the Design Area dialog is open
- **THEN** FusionCanvas closes the dialog and discards the stale draft without persisting it to another context

#### Scenario: Dialog is used with keyboard and supported sizes
- **WHEN** the Design Area dialog is opened or resized within supported normal and narrow dimensions
- **THEN** its descriptive title, accessible form controls, predictable keyboard traversal, scrollable content, Save/Cancel actions, and close behavior remain usable without clipping required fields

### Requirement: Mockup Template management uses a focused guarded editor dialog
FusionCanvas SHALL keep the default Offering-scoped Mockup Template management surface focused on its template collection without reserving an inline editor region. The **Add Mockup Template** action and each template's Edit action SHALL open the same Store Editor-owned modal dialog with a mode-specific title and draft values. The dialog SHALL preserve the existing preview-first provider-image placement workflow, catalog validation, Color/Design Area relationships, revision and persistence behavior, and archived-store read-only policy; SHALL close only after successful save or confirmed cancellation; and SHALL not permit workspace or Offering context changes to leave a stale editable dialog open.

#### Scenario: User reviews the Mockup Template collection
- **WHEN** Mockup Template management is open without an Add/Edit dialog
- **THEN** the Offering-scoped collection uses the available management surface
- **AND** no inline Mockup Template editor is rendered or reserves space
- **AND** one clear **Add Mockup Template** action is available only when editing is allowed and a Design Area exists

#### Scenario: User adds a Mockup Template
- **WHEN** the user selects **Add Mockup Template** for an editable Offering with a Design Area
- **THEN** FusionCanvas opens one modal dialog titled **Add Mockup Template** with a new draft
- **AND** places sensible initial focus in the template identity/configuration flow
- **AND** leaves the parent collection and Offering context visible but unavailable behind the modal

#### Scenario: User edits a Mockup Template
- **WHEN** the user selects an existing template's Edit action
- **THEN** FusionCanvas opens the same modal titled **Edit Mockup Template**
- **AND** populates its stable identity, provider image, target Design Area, Color applicability, image-space mapping, advanced provider data, and revision context

#### Scenario: Preview-first mapping remains available
- **WHEN** a template draft has a selectable provider image
- **THEN** the dialog gives the provider image and visual placement editor prominent space
- **AND** keeps synchronized numeric mapping and supporting configuration reachable at supported normal and narrow sizes

#### Scenario: Save fails validation or persistence
- **WHEN** the user attempts to save an invalid draft or persistence reports a recoverable failure
- **THEN** the dialog remains open with draft values, placement, and guidance preserved
- **AND** confirmed template data and revisions remain unchanged

#### Scenario: Save succeeds
- **WHEN** the user saves a valid Add or Edit draft successfully
- **THEN** FusionCanvas persists the template exactly once through the existing service path
- **AND** closes the dialog, refreshes/selects the saved template, and returns focus to the invoking Add/Edit control when practical

#### Scenario: User dismisses an unchanged draft
- **WHEN** the user selects **Cancel**, presses Escape, or closes the dialog without meaningful changes
- **THEN** FusionCanvas closes the dialog without persisting anything
- **AND** returns focus to the invoking Add/Edit control

#### Scenario: User dismisses a meaningful draft
- **WHEN** the user selects **Cancel**, presses Escape, or closes the dialog after making meaningful unsaved changes
- **THEN** FusionCanvas asks whether to discard the draft or keep editing
- **AND** keep-editing preserves all values and placement within the dialog
- **AND** confirmed discard closes without persisting the draft

#### Scenario: Editing context becomes stale
- **WHEN** the active Offering or workspace changes while the Mockup Template dialog is open
- **THEN** FusionCanvas closes the dialog and discards the stale draft without persisting it to another context

#### Scenario: Archived store is reviewed
- **WHEN** Mockup Template management belongs to an archived Store
- **THEN** Add, Edit, placement, and Save remain unavailable
- **AND** no editable template dialog can be opened

#### Scenario: Dialog is used with keyboard and supported sizes
- **WHEN** the Mockup Template dialog is opened or resized within supported normal and narrow dimensions
- **THEN** its descriptive title, accessible controls, predictable keyboard traversal, scrollable content, Save/Cancel actions, and close behavior remain usable without clipping required configuration

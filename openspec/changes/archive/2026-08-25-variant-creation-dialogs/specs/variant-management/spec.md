## MODIFIED Requirements

### Requirement: Variant management separates possible choices from sellable Variants
FusionCanvas SHALL present provider-catalog Options and Option Values that may participate in combinations in a distinct Available choices region before a Sellable Variants region for one Blueprint Offering. It SHALL preserve stable Option kinds and explicit Variant identities from the authoritative catalog model, disclose choice editing and Variant creation only when invoked, and summarize each confirmed Variant through its stable Option-kind values rather than a name-only row. Each compact Option summary SHALL expose **Manage values** as its routine, directly available action and SHALL keep the infrequent destructive **Archive option** action inside a compact three-dot overflow menu so it does not dominate the card. Option Value editing SHALL be presented in a focused modal dialog scoped to one Option, not as an inline region of the Variants page. Individual and bulk Variant creation SHALL be presented in focused modal dialogs, not as inline editors below the Sellable Variants list.

#### Scenario: User opens Variant management
- **WHEN** the user opens Variant management for a Blueprint Offering
- **THEN** FusionCanvas shows the Offering's Available choices before its explicit Sellable Variants
- **AND** identifies the actual fulfillment Provider or Provider-Network context
- **AND** does not present these choices as global Store configuration

#### Scenario: User scans available choices
- **WHEN** Variant management has enabled Color, Size, or Other Option Values
- **THEN** FusionCanvas groups values by their stable semantic Option kind in compact choice summaries
- **AND** keeps Option Value editing hidden until the user invokes the corresponding manage action
- **AND** does not infer semantics from mutable Option names

#### Scenario: User manages values for one Option
- **WHEN** the user invokes value management for an available-choice group
- **THEN** FusionCanvas opens one focused modal dialog scoped to that Option and its values
- **AND** the Available choices and Sellable Variants regions remain the screen's primary hierarchy without an inline value editor
- **AND** cancellation closes the dialog without changing confirmed values

#### Scenario: User opens the overflow menu for one Option
- **WHEN** the user activates the three-dot overflow control on an Option card
- **THEN** FusionCanvas opens a compact context menu anchored to that card
- **AND** the menu contains **Archive option** as a clearly destructive entry
- **AND** **Manage values** remains directly available on the card as the routine action

#### Scenario: User dismisses the overflow menu
- **WHEN** the user dismisses the Option overflow menu without selecting an entry
- **THEN** FusionCanvas makes no change to the Option or its values
- **AND** returns focus to the overflow control that opened the menu

#### Scenario: User archives an Option from the overflow menu
- **WHEN** the user selects **Archive option** from an Option card's overflow menu
- **THEN** FusionCanvas invokes the existing archive command for that Option
- **AND** applies the same dependency checks, confirmations, error handling, and blocked-archive behavior as the previous direct Archive button
- **AND** surfaces any blocked reason, such as a referenced active catalog configuration, in the existing recoverable manner

#### Scenario: User uses the overflow menu by keyboard and assistive technology
- **WHEN** a keyboard or assistive-technology user focuses the Option card overflow control
- **THEN** the control is keyboard focusable and exposes an accessible name identifying the Option, such as **More actions for Color**
- **AND** the menu opens through standard keyboard interaction
- **AND** the **Archive option** entry remains discoverable and clearly destructive without being the card's dominant control

#### Scenario: Option card supports every Option kind
- **WHEN** Variant management shows any Option kind, including Color, Size, or a custom Option kind
- **THEN** the card presents the compact overflow menu with **Archive option** consistently
- **AND** the same **Manage values** action opens the same value-management dialog for every kind
- **AND** archive eligibility and persistence rules are unchanged across all kinds

#### Scenario: User enables provider-catalog choices
- **WHEN** the user selects Color, Size, or other Option Values available from the provider catalog for the Offering
- **THEN** FusionCanvas records those values as possible choices for that Offering
- **AND** does not automatically treat every mathematical combination as sellable

#### Scenario: User scans sellable Variants
- **WHEN** the Offering has confirmed sellable Variants
- **THEN** FusionCanvas shows the Variant count and a scannable row for each explicit Variant
- **AND** each row groups resolved values under stable Color, Size, or Other semantics where present
- **AND** omits or truthfully marks unavailable provider data that is not supplied by an authoritative provider-catalog descriptor

#### Scenario: User starts one Variant draft
- **WHEN** the user invokes the individual add action
- **THEN** FusionCanvas opens one focused modal dialog for creating a single Variant
- **AND** keeps the bulk creation dialog hidden
- **AND** does not persist a Variant until a valid combination is explicitly confirmed

#### Scenario: User starts a bulk Variant draft
- **WHEN** the user invokes the bulk add action
- **THEN** FusionCanvas opens one focused modal dialog for the color-plus-valid-sizes workflow
- **AND** keeps the individual creation dialog hidden
- **AND** leaves confirmed Variants unchanged until the bulk operation is explicitly confirmed

#### Scenario: User creates one sellable Variant
- **WHEN** the user selects one valid combination of enabled Option Values and explicitly adds it as sellable
- **THEN** FusionCanvas persists one concrete Offering Variant with a stable identity
- **AND** rejects duplicate or provider-invalid combinations without changing confirmed Variants

### Requirement: Variant drafts and lifecycle actions preserve confirmed setup
FusionCanvas SHALL keep Option Value, individual Variant, and bulk Variant creation scoped to the current Offering, SHALL allow only the invoked creation dialog to be open at a time, SHALL guard meaningful drafts, and SHALL apply existing archive, dependency, and integrity policies to sellable Variants. Option Value management and individual and bulk Variant creation SHALL occur in focused modal dialogs that close when the Blueprint Offering or workspace context changes so they cannot edit stale data.

#### Scenario: User cancels a Variant draft
- **WHEN** the user starts an individual or bulk Variant creation dialog and cancels before confirmation
- **THEN** FusionCanvas persists no new Variant
- **AND** closes the dialog and returns focus to the action that opened it

#### Scenario: User closes Option Value management
- **WHEN** the user cancels or completes Option Value management
- **THEN** FusionCanvas closes the value-management dialog
- **AND** discards any unfinished add-value draft without persisting it
- **AND** returns focus to the **Manage values** control that opened the dialog

#### Scenario: User leaves with unsaved Variant changes
- **WHEN** the user attempts to leave Variant management with meaningful unconfirmed changes
- **THEN** FusionCanvas offers to discard the changes or keep editing
- **AND** keep-editing preserves current selections and keyboard focus

#### Scenario: User retires a referenced Variant
- **WHEN** the user requests retirement or removal of a Variant referenced by a Design Area, Item, or other dependent record
- **THEN** FusionCanvas applies the authoritative dependency and archival safeguards
- **AND** reports required resolution rather than silently breaking relationships

#### Scenario: Provider catalog is unavailable
- **WHEN** provider-catalog choices cannot be loaded and no locally persisted choices are available
- **THEN** FusionCanvas shows a recoverable unavailable state in the Available choices region
- **AND** leaves confirmed Variants visible and unchanged

## ADDED Requirements

### Requirement: Variant creation dialogs are scoped, single, and context-safe
FusionCanvas SHALL present individual and bulk Variant creation in two independently titled focused modal dialogs owned by the Store Editor window: one titled "Add Variant" for creating a single sellable combination and one titled "Bulk add" for selecting and generating multiple valid combinations. Each dialog SHALL be scoped to the active Blueprint Offering by stable identity and SHALL use only that Offering's current Option Values. Only one creation dialog may be open at a time. Both dialogs SHALL close when the Blueprint Offering or workspace context changes so a dialog cannot edit stale data. The dialogs SHALL reuse the existing single-Variant and bulk creation capabilities, validation, duplicate, cross-Offering, incomplete-combination, dependency, error, and persistence semantics without duplicating domain or application logic. Successful completion SHALL close the dialog and refresh the Variant count and list while preserving the active Offering. Cancel, close, and Escape SHALL discard the in-progress dialog draft and create no Variant, and focus SHALL return to the action that opened the dialog.

#### Scenario: Add Variant opens a focused dialog scoped to the active Offering
- **WHEN** the user selects **Add Variant** in the Sellable Variants header
- **THEN** FusionCanvas opens one focused modal dialog owned by the Store Editor window
- **AND** the dialog title is "Add Variant"
- **AND** the dialog is scoped to the active Offering by stable identity
- **AND** the dialog shows only the active Offering's current Option Values

#### Scenario: Bulk add opens a focused dialog scoped to the active Offering
- **WHEN** the user selects **Bulk add** in the Sellable Variants header
- **THEN** FusionCanvas opens one focused modal dialog owned by the Store Editor window
- **AND** the dialog title is "Bulk add"
- **AND** the dialog is scoped to the active Offering by stable identity
- **AND** the dialog shows only the active Offering's current Colors and Sizes

#### Scenario: Only one creation dialog may be open at a time
- **WHEN** a creation dialog is already open and the user invokes another creation action
- **THEN** FusionCanvas does not open a second creation dialog
- **AND** keeps the existing dialog scoped to its original Offering

#### Scenario: Switching the Blueprint Offering closes the creation dialog
- **WHEN** the Blueprint Offering context changes while a creation dialog is open
- **THEN** FusionCanvas closes the dialog
- **AND** discards any in-progress draft without persisting
- **AND** does not allow the dialog to edit values for a different Offering than the one that opened it

#### Scenario: Switching the workspace closes the creation dialog
- **WHEN** the active workspace changes while a creation dialog is open
- **THEN** FusionCanvas closes the dialog
- **AND** discards any in-progress draft without persisting

#### Scenario: Successful creation closes the dialog and refreshes the list
- **WHEN** the user confirms a valid individual or bulk creation
- **THEN** FusionCanvas persists the new Variant or Variants through the existing creation path
- **AND** closes the dialog
- **AND** refreshes the Variant count and list while preserving the active Offering

#### Scenario: Cancel, close, or Escape discards the draft and returns focus
- **WHEN** the user cancels, closes, or presses Escape while a creation dialog is open
- **THEN** FusionCanvas closes the dialog
- **AND** creates no Variant and discards the in-progress draft
- **AND** returns keyboard focus to the action that opened the dialog

#### Scenario: Creation reuses existing validation, dependencies, and persistence
- **WHEN** the user attempts to confirm a creation
- **THEN** FusionCanvas applies the same validation, duplicate, cross-Offering, incomplete-combination, dependency, error, and persistence semantics as the previous inline editors
- **AND** a failed attempt keeps the dialog open with recoverable guidance and leaves confirmed data consistent

#### Scenario: Bulk creation shows a pre-confirmation summary
- **WHEN** the user requests a bulk preview inside the Bulk add dialog
- **THEN** FusionCanvas shows a clear summary of the combinations that will and will not be created before confirmation
- **AND** partial failure provides recoverable, specific guidance and leaves confirmed data consistent

#### Scenario: Parent screen renders no inline creation editor
- **WHEN** Variant management is open with or without a creation dialog
- **THEN** the Sellable Variants region renders no inline individual or bulk creation editor below the Variant list

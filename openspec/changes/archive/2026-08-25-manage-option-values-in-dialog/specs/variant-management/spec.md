## MODIFIED Requirements

### Requirement: Variant management separates possible choices from sellable Variants
FusionCanvas SHALL present provider-catalog Options and Option Values that may participate in combinations in a distinct Available choices region before a Sellable Variants region for one Blueprint Offering. It SHALL preserve stable Option kinds and explicit Variant identities from the authoritative catalog model, disclose choice editing and Variant drafts only when invoked, and summarize each confirmed Variant through its stable Option-kind values rather than a name-only row. Each compact Option summary SHALL expose **Manage values** as its routine, directly available action and SHALL keep the infrequent destructive **Archive option** action inside a compact three-dot overflow menu so it does not dominate the card. Option Value editing SHALL be presented in a focused modal dialog scoped to one Option, not as an inline region of the Variants page.

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
- **THEN** FusionCanvas reveals one individual Variant draft within the Sellable Variants region
- **AND** keeps the bulk draft hidden
- **AND** does not persist a Variant until a valid combination is explicitly confirmed

#### Scenario: User starts a bulk Variant draft
- **WHEN** the user invokes the bulk add action
- **THEN** FusionCanvas reveals the color-plus-valid-sizes workflow within the Sellable Variants region
- **AND** keeps the individual draft hidden
- **AND** leaves confirmed Variants unchanged until the bulk operation is explicitly confirmed

#### Scenario: User creates one sellable Variant
- **WHEN** the user selects one valid combination of enabled Option Values and explicitly adds it as sellable
- **THEN** FusionCanvas persists one concrete Offering Variant with a stable identity
- **AND** rejects duplicate or provider-invalid combinations without changing confirmed Variants

### Requirement: Variant drafts and lifecycle actions preserve confirmed setup
FusionCanvas SHALL keep Option Value, individual Variant, and bulk Variant editors scoped to the current Offering, SHALL allow only the invoked draft editor to displace compact summary content, SHALL guard meaningful drafts, and SHALL apply existing archive, dependency, and integrity policies to sellable Variants. Option Value management SHALL occur in a focused modal dialog that closes when the Blueprint Offering or workspace context changes so it cannot edit stale data.

#### Scenario: User cancels a Variant draft
- **WHEN** the user starts an individual or bulk Variant draft and cancels before confirmation
- **THEN** FusionCanvas persists no new Variant
- **AND** collapses that draft and returns focus to its invoking action or current Variant selection

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

### Requirement: Option value management dialog is scoped, single, and context-safe
FusionCanvas SHALL present Option value management in a focused modal dialog owned by the Store Editor window, SHALL scope the dialog to exactly one Option identified by its stable identity, SHALL allow only one value-management dialog at a time, SHALL title the dialog "Manage &lt;Option name&gt; values", and SHALL close the dialog when the Blueprint Offering or workspace context changes so the dialog cannot edit stale data. The dialog SHALL reuse the existing add-value and archive-value capabilities, validation, dependency safeguards, error messages, and persistence semantics without duplicating domain or application logic.

#### Scenario: Manage values opens a focused dialog for the selected Option
- **WHEN** the user selects **Manage values** on an Option card
- **THEN** FusionCanvas opens one focused modal dialog owned by the Store Editor window
- **AND** scopes the dialog to the Option's stable identity
- **AND** the dialog title is "Manage &lt;Option name&gt; values", such as "Manage Color values" or "Manage Size values"
- **AND** the dialog shows only that Option's current values

#### Scenario: Only one value-management dialog may be open at a time
- **WHEN** a value-management dialog is already open and the user invokes another value management action
- **THEN** FusionCanvas does not open a second value-management dialog
- **AND** keeps the existing dialog scoped to its original Option

#### Scenario: Switching the Blueprint Offering closes the dialog
- **WHEN** the Blueprint Offering context changes while a value-management dialog is open
- **THEN** FusionCanvas closes the dialog
- **AND** discards any unfinished add-value draft without persisting
- **AND** does not allow the dialog to edit values for a different Offering than the one that opened it

#### Scenario: Switching the workspace closes the dialog
- **WHEN** the active workspace changes while a value-management dialog is open
- **THEN** FusionCanvas closes the dialog
- **AND** discards any unfinished add-value draft without persisting

#### Scenario: Explicit finish closes the dialog
- **WHEN** the user selects the explicit finish action, such as **Done**
- **THEN** FusionCanvas closes the value-management dialog
- **AND** returns keyboard focus to the originating **Manage values** control

#### Scenario: Cancel or close discards an unfinished add-value draft
- **WHEN** the user cancels, closes, or presses Escape while an add-value draft is in progress
- **THEN** FusionCanvas closes the dialog
- **AND** does not persist the unfinished add-value draft
- **AND** returns keyboard focus to the originating **Manage values** control

#### Scenario: Value management reuses existing validation, dependencies, and persistence
- **WHEN** the user adds or archives a value inside the dialog
- **THEN** FusionCanvas applies the same validation, dependency safeguards, error messages, and persistence semantics as the previous inline editor
- **AND** a successful change updates the underlying Option and refreshes the parent Option card value summary and affected Variant state

#### Scenario: Value management dialog supports custom Option kinds
- **WHEN** the user selects **Manage values** for an Option whose kind is neither Color nor Size
- **THEN** FusionCanvas opens the same value-management dialog titled "Manage &lt;Option name&gt; values"
- **AND** does not require a hard-coded screen for the custom kind

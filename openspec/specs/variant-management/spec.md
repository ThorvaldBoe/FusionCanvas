# Variant Management

## Purpose

Defines how the Manage Variants surface presents an Offering's provider-catalog Options, Option Values, and concrete sellable Variants, including on-demand editing, draft exclusivity, lifecycle safeguard behavior, and the compact card presentation that keeps the routine **Manage values** action primary and the infrequent destructive **Archive option** action inside a three-dot overflow menu.
## Requirements
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

### Requirement: Available options render as bordered choice cards
FusionCanvas SHALL present each available Option in the Available choices region as a distinct compact bordered card, SHALL show the Option name, semantic kind label, current value summary, and the Option's manage and archive actions inside the same boundary, and SHALL use shared semantic theme resources so the boundary remains visible in both Light and Dark appearance.

#### Scenario: User scans available choices as cards
- **WHEN** Variant management has multiple available Options
- **THEN** FusionCanvas encloses each Option in its own bordered card
- **AND** places the Option name, kind label, value summary, and its actions inside the same boundary
- **AND** applies consistent padding, corner radius, and spacing across the cards

#### Scenario: Empty Option uses the same card treatment
- **WHEN** an available Option has no configured values
- **THEN** FusionCanvas renders the empty Option as a choice card with the same boundary
- **AND** shows a truthful summary that no values are configured

#### Scenario: Custom Option kind uses the same card treatment
- **WHEN** an available Option is neither Color nor Size
- **THEN** FusionCanvas renders it as a choice card with the same boundary
- **AND** labels the card by its custom Option kind

### Requirement: Choice cards align and respond to available width without clipping
FusionCanvas SHALL align available-option cards cleanly in the available width, SHALL wrap or stack them gracefully at narrower supported widths, and SHALL wrap long Option names and value summaries so card layout does not clip content.

#### Scenario: Cards align cleanly in the available width
- **WHEN** multiple available Option cards fit within the available width
- **THEN** they sit side by side aligned on the same row with consistent spacing

#### Scenario: Cards stack at narrower supported widths
- **WHEN** the window narrows toward its minimum supported width
- **THEN** the cards wrap onto new rows instead of overflowing or being clipped

#### Scenario: Long content does not clip
- **WHEN** an available Option name or value summary is longer than the card width
- **THEN** the text wraps within the card and remains readable

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

### Requirement: Option Value archive actions are compact, secondary, and target-specific
FusionCanvas SHALL present each active Option Value in the focused value-management dialog with a compact visible **Archive** action, SHALL keep that destructive action visually secondary to the dialog's routine completion and value-creation actions, and SHALL identify the affected value in the action's accessible name. The action SHALL invoke the existing Option Value archive command and preserve its eligibility, dependency, persistence, confirmation, and recoverable-error behavior.

#### Scenario: User scans values for any Option kind
- **WHEN** the focused value-management dialog shows Color, Size, or custom Option Values
- **THEN** every value row presents the same compact **Archive** treatment aligned at the row edge
- **AND** the archive actions do not visually dominate the dialog's routine actions

#### Scenario: Long value name shares a row with Archive
- **WHEN** an Option Value name approaches or exceeds the normal row width
- **THEN** the value remains readable through wrapping or available-width measurement
- **AND** does not overlap, clip, or displace the compact **Archive** action outside the row

#### Scenario: Assistive technology identifies the archive target
- **WHEN** keyboard focus or assistive technology reaches a value's **Archive** action
- **THEN** the action exposes a target-specific accessible name such as **Archive Black**
- **AND** focus order follows the visible value-row order

#### Scenario: User invokes compact Archive
- **WHEN** the user invokes a value row's compact **Archive** action
- **THEN** FusionCanvas passes that row's stable Option Value identity to the existing archive command exactly once
- **AND** retains all existing archive eligibility, dependency checks, confirmation, persistence, and recoverable guidance

### Requirement: Active Option Values can be renamed in place
FusionCanvas SHALL expose an accessible **Edit** action for every active Option Value in the focused value-management dialog. The action SHALL open the current display name in an editable draft and SHALL persist a successful rename against the existing Option Value identity. Color and Size SHALL use identical behavior; no replacement value SHALL be created.

#### Scenario: Edit a Color or Size value
- **WHEN** the user activates Edit for an active Color or Size value
- **THEN** the dialog shows that value's current name in an editable form
- **AND** saving a valid new name updates the same stable Option Value record
- **AND** the dialog list and parent Option summary show the new name

#### Scenario: Reject invalid or duplicate rename
- **WHEN** the user saves a blank, invalid, or normalized duplicate name for an active value in the same Option
- **THEN** the existing validation and recoverable error behavior is shown
- **AND** the original value and its display name remain unchanged

#### Scenario: Preserve references during rename
- **WHEN** a value used by Variant memberships, template/value links, or other catalog relationships is renamed successfully
- **THEN** every relationship still references the same Option Value identity
- **AND** dependent views refresh to display the new name

#### Scenario: Cancel an Option Value edit
- **WHEN** the user cancels, closes, or presses Escape while editing a value
- **THEN** no rename is persisted
- **AND** the existing value and all references remain unchanged

### Requirement: Option Values support explicit persisted ordering
FusionCanvas SHALL maintain an explicit integer order for active Option Values within each Blueprint Offering Option. The order SHALL be persisted with the existing Option Value identity, SHALL be used wherever those values are presented or selected, and SHALL be normalized to contiguous zero-based positions after a successful add, reorder, archive, or restore. Reordering SHALL not recreate values or change any relationship that references their identities.

#### Scenario: User reorders a Color value by its visible handle
- **WHEN** the user drags an active Color value by its dedicated reorder handle to a new position
- **THEN** the Color values are displayed in the requested order
- **AND** the existing value records and their identities remain unchanged

#### Scenario: User reorders a Size value by its visible handle
- **WHEN** the user drags an active Size value by its dedicated reorder handle to a new position
- **THEN** the Size values are displayed in the requested order
- **AND** the existing value records and their identities remain unchanged

#### Scenario: Reorder is persisted across dialog and application sessions
- **WHEN** a user reorders values, closes and reopens the management dialog, and restarts the application
- **THEN** the same order is loaded and displayed for the affected Option

#### Scenario: Ordered values are used by consumers
- **WHEN** a consumer presents or selects Color or Size choices for an Offering
- **THEN** it uses the persisted active-value order rather than insertion order, database row order, or alphabetical order

#### Scenario: New values receive a deterministic position
- **WHEN** a user adds a new active value to an Option
- **THEN** the new value is placed after the existing active values
- **AND** active positions remain contiguous and deterministic

#### Scenario: Existing data receives a stable backfill
- **WHEN** an existing workspace is opened after the order field or ordering behavior is introduced
- **THEN** active values retain their apparent pre-migration order, with stable identity used to break ties
- **AND** no value identity, reference, or link changes

#### Scenario: Archived values do not disturb active ordering
- **WHEN** a value is archived or restored
- **THEN** active values are renumbered contiguously in their current relative order
- **AND** the archived or restored value keeps its stable identity and existing relationships

#### Scenario: Reorder actions are accessible
- **WHEN** keyboard or assistive-technology users reach a Color or Size value row
- **THEN** the dedicated handle exposes a target-specific accessible name and an equivalent move-up/move-down action is available without pointer-only interaction
- **AND** focus order follows the visible value order

#### Scenario: Invalid reorder leaves confirmed values unchanged
- **WHEN** a reorder request targets a different Option, an archived value, an out-of-range position, or stale context
- **THEN** the request is rejected with recoverable guidance
- **AND** the confirmed value order and relationships remain unchanged


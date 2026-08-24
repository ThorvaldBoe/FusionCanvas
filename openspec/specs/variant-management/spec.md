# Variant Management

## Purpose

Defines how the Manage Variants surface presents an Offering's provider-catalog Options, Option Values, and concrete sellable Variants, including on-demand editing, draft exclusivity, lifecycle safeguard behavior, and the compact card presentation that keeps the routine **Manage values** action primary and the infrequent destructive **Archive option** action inside a three-dot overflow menu.

## Requirements

### Requirement: Variant management separates possible choices from sellable Variants
FusionCanvas SHALL present provider-catalog Options and Option Values that may participate in combinations in a distinct Available choices region before a Sellable Variants region for one Blueprint Offering. It SHALL preserve stable Option kinds and explicit Variant identities from the authoritative catalog model, disclose choice editing and Variant drafts only when invoked, and summarize each confirmed Variant through its stable Option-kind values rather than a name-only row. Each compact Option summary SHALL expose **Manage values** as its routine, directly available action and SHALL keep the infrequent destructive **Archive option** action inside a compact three-dot overflow menu so it does not dominate the card.

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
- **THEN** FusionCanvas reveals one editor scoped to that Option and its values
- **AND** preserves the Available choices and Sellable Variants regions as the screen's primary hierarchy
- **AND** cancellation closes the editor without changing confirmed values

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
FusionCanvas SHALL keep Option Value, individual Variant, and bulk Variant editors scoped to the current Offering, SHALL allow only the invoked draft editor to displace compact summary content, SHALL guard meaningful drafts, and SHALL apply existing archive, dependency, and integrity policies to sellable Variants.

#### Scenario: User cancels a Variant draft
- **WHEN** the user starts an individual or bulk Variant draft and cancels before confirmation
- **THEN** FusionCanvas persists no new Variant
- **AND** collapses that draft and returns focus to its invoking action or current Variant selection

#### Scenario: User closes Option Value management
- **WHEN** the user cancels or completes Option Value management
- **THEN** FusionCanvas returns to compact Available choice summaries
- **AND** returns focus to the invoking choice-management action

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
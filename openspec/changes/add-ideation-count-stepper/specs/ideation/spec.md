# Delta: ideation

This delta modifies a capability defined by the active base change
`add-ideation-tool`. It adds count-up and count-down arrow buttons beside
the existing "Number of ideas" text field so the desired candidate count can
be adjusted with a single click, without removing free-text entry or the
existing 1–20 range validation.

## MODIFIED Requirements

### Requirement: The dialog captures mode, guidance, count, and visible scope
The Ideation dialog SHALL display the resolved store, niche, and optional group scope, SHALL provide one optional multi-line guidance field, SHALL provide an extensible mode selector initially containing `Basic` and `Snowclones`, SHALL constrain desired candidate count to 1 through 20 with a default of 5, and SHALL expose a count-up arrow button and a count-down arrow button beside the count text field that adjust the desired count by one within the 1 through 20 range. The count text field SHALL continue to accept free-text entry, the count-up and count-down arrows SHALL be the only controls that adjust the count by clicking, and the arrows SHALL NOT submit generation or otherwise alter dialog state beyond the count text.

#### Scenario: Dialog opens for a group
- **WHEN** Ideation opens from group `Pugs` in niche `Dogs`
- **THEN** the dialog visibly identifies the store, `Dogs`, and `Pugs` scope
- **AND** `Basic`, an empty guidance field, and a count of 5 are initially selected
- **AND** a count-up arrow and a count-down arrow are visible immediately beside the count text field

#### Scenario: Dialog opens at niche root
- **WHEN** Ideation opens without a selected group
- **THEN** the dialog visibly identifies the active store and niche
- **AND** it communicates that created candidates will be placed at the niche root

#### Scenario: User enters an invalid count
- **WHEN** the desired count is outside 1 through 20 or is not a complete valid number
- **THEN** Generate is unavailable
- **AND** the dialog communicates the allowed range without losing the user's guidance
- **AND** both the count-up and count-down arrows remain enabled so the user can recover by clicking

#### Scenario: User clicks the count-up arrow from a valid in-range count
- **WHEN** the count text is a whole number `n` where `1 <= n < 20`
- **AND** the user clicks the count-up arrow
- **THEN** the count text becomes the whole number `n + 1`
- **AND** no range error is shown

#### Scenario: User clicks the count-down arrow from a valid in-range count
- **WHEN** the count text is a whole number `n` where `1 < n <= 20`
- **AND** the user clicks the count-down arrow
- **THEN** the count text becomes the whole number `n - 1`
- **AND** no range error is shown

#### Scenario: Count-up arrow is disabled at the maximum
- **WHEN** the count text is a whole number equal to 20
- **THEN** the count-up arrow is disabled
- **AND** invoking it by keyboard does not change the count text

#### Scenario: Count-down arrow is disabled at the minimum
- **WHEN** the count text is a whole number equal to 1
- **THEN** the count-down arrow is disabled
- **AND** invoking it by keyboard does not change the count text

#### Scenario: Count-up arrow recovers from invalid text
- **WHEN** the count text is empty or is not a complete valid whole number
- **AND** the user clicks the count-up arrow
- **THEN** the count text becomes the default count of 5
- **AND** no range error is shown

#### Scenario: Count-down arrow recovers from invalid text
- **WHEN** the count text is empty or is not a complete valid whole number
- **AND** the user clicks the count-down arrow
- **THEN** the count text becomes the minimum count of 1
- **AND** no range error is shown

#### Scenario: Count arrow clamps an out-of-range parseable number before stepping
- **WHEN** the count text parses as a whole number outside 1 through 20
- **AND** the user clicks either count arrow
- **THEN** the count text first reflects the clamped value (`1` for below-range, `20` for above-range)
- **AND** the click then steps from that clamped value so a second consecutive click moves one step further into the range

#### Scenario: Count arrows are disabled while a batch is running
- **WHEN** a generation batch has started and not all requested operations have completed
- **THEN** both count arrows are disabled alongside the count text field, mode selector, guidance field, and Generate button
- **AND** existing candidates remain visible

### Requirement: Ideation remains accessible and theme coherent
FusionCanvas SHALL make essential Ideation controls keyboard reachable in a logical order, SHALL provide meaningful accessible names and status announcements, and SHALL resolve dialog, progress, candidate, confirmation, disabled, error, and focus states from shared application themes.

#### Scenario: User operates Ideation with a keyboard
- **WHEN** keyboard focus enters the dialog
- **THEN** mode, guidance, count text, count-up arrow, count-down arrow, Generate, candidate Create and Reject actions, Clear All, and Close are reachable in a predictable order
- **AND** focus enters the guidance field initially
- **AND** the count-up arrow is reached before the count-down arrow, both after the count text field and before the Generate button

#### Scenario: Count arrows expose accessible names
- **WHEN** an assistive technology inspects the count arrows
- **THEN** the count-up arrow reports an accessible name of `Increment idea count`
- **AND** the count-down arrow reports an accessible name of `Decrement idea count`

#### Scenario: Candidate action completes
- **WHEN** Create or Reject removes a candidate
- **THEN** focus moves to the next candidate action when one exists
- **AND** otherwise moves to a stable dialog action

#### Scenario: Application theme changes
- **WHEN** the application appearance changes while Ideation or a confirmation dialog is open
- **THEN** every open Ideation surface adopts the active theme
- **AND** busy, disabled, warning, destructive, selected, and error states remain distinguishable

## ADDED Requirements

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


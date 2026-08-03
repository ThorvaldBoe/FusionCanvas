## ADDED Requirements

### Requirement: Settings exposes an About section with version and diagnostics
FusionCanvas SHALL include an About section in the Settings window that displays the product version and exposes a copyable diagnostic block.

#### Scenario: User selects the About section
- **WHEN** the user opens Settings and selects `About` in the section rail
- **THEN** the About pane replaces the previous pane in the content region
- **AND** the About pane shows the FusionCanvas product name and the user-friendly product version
- **AND** the About pane exposes a copy action for the diagnostic block

#### Scenario: About section is reachable by keyboard
- **WHEN** keyboard focus enters the Settings window
- **THEN** the `About` entry participates in the existing section rail and is reachable in a predictable tab order alongside the other sections

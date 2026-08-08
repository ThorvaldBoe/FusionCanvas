## MODIFIED Requirements

### Requirement: Appearance preference persists locally
FusionCanvas SHALL persist the selected appearance as an application-wide local preference that is independent of workspace data, while preserving readable appearance and AI settings when optional window-layout fields are absent or invalid.

#### Scenario: Saved Dark preference survives restart
- **WHEN** the user enables Dark mode, closes FusionCanvas, and starts it again
- **THEN** FusionCanvas loads the Dark appearance before presenting the main window
- **AND** the `Dark mode` toggle is on when Settings opens

#### Scenario: User switches active workspace
- **WHEN** the selected appearance is Dark and the active workspace changes
- **THEN** the selected appearance remains Dark
- **AND** no workspace record is changed to store the appearance preference

#### Scenario: Saved preference cannot be read
- **WHEN** the local settings file is missing, invalid, or unreadable at startup
- **THEN** FusionCanvas starts with the Light appearance and existing main-window defaults
- **AND** the user can select and save an appearance preference during the session

#### Scenario: Optional layout cannot be read
- **WHEN** the settings document has a readable appearance preference but its optional window-layout section is missing, malformed, or invalid
- **THEN** FusionCanvas preserves the readable appearance and AI settings
- **AND** it uses the existing main-window and splitter defaults

#### Scenario: Preference cannot be saved
- **WHEN** the user changes `Dark mode` and the local preference cannot be written
- **THEN** FusionCanvas keeps the selected appearance for the current session
- **AND** the General pane reports that the preference could not be saved and may not survive restart

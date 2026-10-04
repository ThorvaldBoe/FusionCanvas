## MODIFIED Requirements

### Requirement: Application starts to a main window
The desktop application SHALL start to a main FusionCanvas window only after current app-wide terms acknowledgement has been satisfied, and SHALL apply only a valid, usable saved normal layout after that window has been created.

#### Scenario: Contributor runs the app with current acknowledgement
- **WHEN** a contributor launches the desktop application with a current acknowledgement record
- **THEN** a main window opens for FusionCanvas

#### Scenario: User launches the app without current acknowledgement
- **WHEN** the application starts without a current acknowledgement record
- **THEN** the mandatory consent surface is presented before the main window
- **AND** the normal workspace and navigation are not available until the user accepts and the acknowledgement is saved

#### Scenario: Main window has a valid saved layout
- **WHEN** the application starts with current acknowledgement and valid saved normal bounds and navigation-pane width
- **THEN** the main window applies those values after creation
- **AND** the navigation pane remains within its supported minimum and maximum width

#### Scenario: Main window has no usable saved layout
- **WHEN** the application starts with current acknowledgement but without layout values or with invalid, legacy, off-screen, maximized, or fullscreen layout state
- **THEN** the main window uses its existing default bounds and splitter width or a clamped usable equivalent
- **AND** the application remains usable without a restoration error

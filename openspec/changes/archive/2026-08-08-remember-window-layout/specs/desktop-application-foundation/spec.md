## MODIFIED Requirements

### Requirement: Application starts to a main window
The desktop application SHALL start to a main FusionCanvas window and SHALL apply only a valid, usable saved normal layout after that window has been created.

#### Scenario: Contributor runs the app
- **WHEN** a contributor launches the desktop application
- **THEN** a main window opens for FusionCanvas

#### Scenario: Main window has a valid saved layout
- **WHEN** the application starts with valid saved normal bounds and navigation-pane width
- **THEN** the main window applies those values after creation
- **AND** the navigation pane remains within its supported minimum and maximum width

#### Scenario: Main window has no usable saved layout
- **WHEN** the application starts without layout values or with invalid, legacy, off-screen, maximized, or fullscreen layout state
- **THEN** the main window uses its existing default bounds and splitter width or a clamped usable equivalent
- **AND** the application remains usable without a restoration error

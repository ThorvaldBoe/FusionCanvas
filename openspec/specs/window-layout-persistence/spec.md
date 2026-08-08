# window-layout-persistence Specification

## Purpose
TBD - created by archiving change remember-window-layout. Update Purpose after archive.
## Requirements
### Requirement: Main window layout persists as optional local application settings

FusionCanvas SHALL persist the main window's latest normal-state position and size plus the navigation-pane width as optional fields in the versioned local application-settings document.

#### Scenario: User changes the normal main-window layout
- **WHEN** the user moves or resizes the normal main window or drags the navigation-pane splitter
- **THEN** FusionCanvas retains the latest valid normal position, size, and splitter width for the current session
- **AND** the values are saved through the existing application-settings save path

#### Scenario: Existing settings have no layout fields
- **WHEN** FusionCanvas loads a missing layout section or a settings document written by a supported legacy version
- **THEN** FusionCanvas uses the existing main-window and splitter defaults
- **AND** appearance and AI settings remain readable and usable

### Requirement: Main window restores only valid usable layout values

FusionCanvas SHALL apply saved layout after the main window is created only when the values are finite, positive, compatible with the current minimum and maximum constraints, and can be placed within a current screen working area.

#### Scenario: Saved layout is valid and visible
- **WHEN** a supported settings document contains valid normal bounds and a valid navigation width
- **THEN** FusionCanvas restores those values after the main window is created
- **AND** the restored navigation width remains within its supported limits

#### Scenario: Saved layout is invalid or incomplete
- **WHEN** any saved position, dimension, or splitter value is missing, non-finite, non-positive, malformed, or outside the supported constraints
- **THEN** FusionCanvas ignores the saved layout
- **AND** the existing XAML defaults remain in effect

#### Scenario: Saved layout is outside current screens
- **WHEN** saved bounds refer to a monitor that is no longer available or place the window wholly outside all current working areas
- **THEN** FusionCanvas selects a current screen, preferring the primary screen when necessary
- **AND** clamps the window to a usable position and size within that screen's working area

#### Scenario: Saved state is maximized or fullscreen
- **WHEN** the previous session ended while the main window was maximized or fullscreen
- **THEN** FusionCanvas does not treat platform-managed bounds or state as the normal saved layout
- **AND** starts with the last valid normal layout or the existing defaults

### Requirement: Layout persistence remains safe on save and shutdown

FusionCanvas SHALL preserve the latest valid normal layout through the existing serialized settings writes and shutdown flush without making layout persistence a prerequisite for using the current session.

#### Scenario: Main window closes after layout changes
- **WHEN** the main window closes after the user changed its normal bounds or splitter width
- **THEN** the latest valid normal layout is merged with current application settings before shutdown flush completes
- **AND** the next supported startup can restore it

#### Scenario: Layout settings cannot be saved
- **WHEN** the local settings write fails while persisting layout
- **THEN** FusionCanvas keeps the current session usable
- **AND** it does not throw a layout-restoration exception or corrupt existing readable settings


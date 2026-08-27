## ADDED Requirements

### Requirement: Window placement persistence applies to non-transient windows

FusionCanvas SHALL persist the latest normal-state position and size of every non-transient window and SHALL NOT persist placement for transient confirmation dialogs.

The non-transient windows are the main window, Settings, Workspace Management, Store Editor, Assets, Ideation, Reject Idea, Snowclone Library, Rejected Phrases, Design Preview, and Item Import. Transient confirmation dialogs include the group action and delete confirmations, the group selection dialog, the Ideation discard confirmation, and the Design Area archive confirmation.

#### Scenario: Non-transient secondary window reopens at its last placement
- **WHEN** the user moves or resizes a non-transient secondary window and later reopens it
- **THEN** FusionCanvas restores the window to its last normal-state position and size
- **AND** the restoration uses the same screen-safe normalization as the main window

#### Scenario: Transient confirmation dialog keeps default placement
- **WHEN** a transient confirmation dialog opens
- **THEN** FusionCanvas uses the dialog's default placement
- **AND** no geometry is persisted for that dialog

### Requirement: Secondary window geometry persists as optional local application settings

FusionCanvas SHALL persist each non-transient secondary window's latest normal-state position and size as optional per-window fields in the versioned local application-settings document, keyed by a stable window identity, independent of workspace content.

#### Scenario: User changes a secondary window's normal placement
- **WHEN** the user moves or resizes a non-transient secondary window
- **THEN** FusionCanvas retains the latest valid normal position and size for that window for the current session
- **AND** the values are saved through the existing application-settings save path on close

#### Scenario: Existing settings have no secondary geometry
- **WHEN** FusionCanvas loads a settings document written by a version that did not persist secondary geometry
- **THEN** FusionCanvas uses the existing default placement for every secondary window
- **AND** appearance, AI, and main-window layout settings remain readable and usable

### Requirement: Secondary windows restore only valid usable geometry values

FusionCanvas SHALL apply saved secondary geometry after the window is created only when the values are finite, positive, compatible with the current minimum and maximum constraints, and can be placed within a current screen working area.

#### Scenario: Saved secondary geometry is valid and visible
- **WHEN** a supported settings document contains valid normal bounds for a secondary window
- **THEN** FusionCanvas restores those values after the window is created

#### Scenario: Saved secondary geometry is invalid or incomplete
- **WHEN** any saved position or dimension for a secondary window is missing, non-finite, non-positive, malformed, or outside the supported constraints
- **THEN** FusionCanvas ignores the saved geometry for that window
- **AND** the existing XAML defaults remain in effect

#### Scenario: Saved secondary geometry is outside current screens
- **WHEN** saved bounds refer to a monitor that is no longer available or place the window wholly outside all current working areas
- **THEN** FusionCanvas selects a current screen, preferring the primary screen when necessary
- **AND** clamps the window to a usable position and size within that screen's working area

#### Scenario: Saved secondary state is maximized or fullscreen
- **WHEN** the previous session ended while a secondary window was maximized or fullscreen
- **THEN** FusionCanvas does not treat platform-managed bounds or state as the normal saved geometry
- **AND** starts with the last valid normal geometry or the existing defaults

### Requirement: Secondary window geometry persistence remains safe on save and shutdown

FusionCanvas SHALL preserve the latest valid normal secondary geometry through the existing serialized settings writes and shutdown flush without making geometry persistence a prerequisite for using the current session.

#### Scenario: Secondary window closes after placement changes
- **WHEN** a non-transient secondary window closes after the user changed its normal bounds
- **THEN** the latest valid normal geometry is merged with current application settings before shutdown flush completes
- **AND** the next supported startup can restore it

#### Scenario: Secondary geometry settings cannot be saved
- **WHEN** the local settings write fails while persisting secondary geometry
- **THEN** FusionCanvas keeps the current session usable
- **AND** it does not throw a geometry-restoration exception or corrupt existing readable settings

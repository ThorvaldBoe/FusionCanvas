## MODIFIED Requirements

### Requirement: Secondary window geometry persists as optional local application settings

FusionCanvas SHALL persist each non-transient secondary window's latest valid normal-state position and size as optional per-window fields in the versioned local application-settings document, keyed by a stable window identity, independent of workspace content. Every non-transient window SHALL be registered through the shared application-wide geometry lifecycle so capture, save, restore, close ordering, and platform coordinate handling are consistent across windows.

#### Scenario: User changes a secondary window's normal placement
- **WHEN** the user moves or resizes a registered non-transient secondary window
- **THEN** FusionCanvas retains the latest valid normal position and size for that window for the current session
- **AND** the values are saved through the existing application-settings save path on close

#### Scenario: Every registered window uses the shared lifecycle
- **WHEN** a non-transient window is opened by any application surface
- **THEN** it is registered with one stable key and the shared capture, restore, and close lifecycle
- **AND** no per-window implementation is required to duplicate persistence rules

#### Scenario: Existing settings have no secondary geometry
- **WHEN** FusionCanvas loads a settings document written by a version that did not persist secondary geometry
- **THEN** FusionCanvas uses the existing default placement for every secondary window
- **AND** appearance, AI, and main-window layout settings remain readable and usable

#### Scenario: Transient confirmation dialog keeps default placement
- **WHEN** a transient confirmation or selection dialog opens
- **THEN** FusionCanvas uses the dialog's default placement
- **AND** no geometry is persisted for that dialog

#### Scenario: Close lifecycle does not lose native placement
- **WHEN** a registered window is moved or resized using the native desktop window chrome and then closed
- **THEN** FusionCanvas captures native coordinates when available before the native handle is released
- **AND** view-model state synchronization does not re-enter the close operation

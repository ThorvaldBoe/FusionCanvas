## MODIFIED Requirements

### Requirement: Application starts to a main window
The desktop application SHALL package the FusionCanvas square logo as its desktop/application icon and SHALL show the FusionCanvas banner in a startup splash surface while application composition and initial workspace startup work completes. The splash SHALL close when the main FusionCanvas window is ready to display, and the application SHALL not depend on files outside the installed application package.

#### Scenario: Contributor runs the app
- **WHEN** a contributor launches the desktop application
- **THEN** the operating system and main window identify the application with the FusionCanvas icon
- **AND** a FusionCanvas-branded splash surface is shown before the main window is ready
- **AND** a main window opens for FusionCanvas after startup work completes

#### Scenario: Startup completes
- **WHEN** the main FusionCanvas window has been constructed and assigned to the desktop lifetime
- **THEN** the splash surface closes
- **AND** the main window is the active usable application surface
- **AND** startup is not delayed by an arbitrary fixed display timer

#### Scenario: Packaged application runs without source assets
- **WHEN** the application is launched from a built or published output directory
- **THEN** the icon and splash assets resolve from packaged application resources
- **AND** the application does not read `C:\temp\FusionCanvas\` or another developer-only path

#### Scenario: Startup fails before the main window is ready
- **WHEN** required startup composition fails before the main window can be shown
- **THEN** the splash surface is closed
- **AND** the user is not left with an apparently frozen splash-only application
- **AND** the failure is surfaced through the application's startup failure path

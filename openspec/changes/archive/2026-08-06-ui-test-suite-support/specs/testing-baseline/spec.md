## ADDED Requirements

### Requirement: Desktop UI automation is a separately selectable verification lane
FusionCanvas SHALL keep `dotnet test .\\FusionCanvas.sln` as the required deterministic, non-interactive baseline and SHALL provide real-desktop UI automation as a separately selectable Windows verification lane.

#### Scenario: Contributor runs the baseline suite
- **WHEN** a contributor runs `dotnet test .\\FusionCanvas.sln`
- **THEN** the command does not require a Windows automation server, Developer Mode, an interactive desktop, or the desktop UI-test project

#### Scenario: Contributor needs real-desktop coverage
- **WHEN** a contributor or CI job needs to verify a native-window or end-to-end interaction risk covered by the desktop UI suite
- **THEN** it runs the documented Windows UI-test command with its explicit prerequisites
- **AND** records that result as supplemental to, not a substitute for, the required deterministic baseline

### Requirement: Desktop UI scenario scope remains proportionate
FusionCanvas SHALL allocate real-desktop UI journeys to high-value cross-process, native-window, accessibility, focus, or end-to-end workflow risks and SHALL use focused deterministic tests for equivalent lower-risk variants.

#### Scenario: A module adds a UI automation journey
- **WHEN** a delivery module proposes a new desktop UI journey
- **THEN** its OpenSpec verification plan identifies the workflow risk that requires real-desktop automation
- **AND** identifies related behavior retained in focused headless, view-model, application, or integration tests

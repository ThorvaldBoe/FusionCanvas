## ADDED Requirements

### Requirement: Workspace packages exclude diagnostic telemetry
FusionCanvas SHALL omit telemetry records and raw request/response bodies from workspace package exports and SHALL NOT import diagnostic records from workspace packages. Imported workspaces SHALL start with Debug Mode and Show Debug Window off and one-day retention. Routine workspace transfer SHALL leave any telemetry already in the source database unchanged.

#### Scenario: Workspace package is exported
- **WHEN** a workspace with stored telemetry is exported
- **THEN** the package contains no diagnostic telemetry records or captured request/response bodies
- **AND** the source database retains its telemetry records

#### Scenario: Workspace package is imported
- **WHEN** a workspace package is imported
- **THEN** no telemetry records are created from package contents
- **AND** imported Debug Mode and Show Debug Window settings are off
- **AND** the imported workspace uses one-day retention

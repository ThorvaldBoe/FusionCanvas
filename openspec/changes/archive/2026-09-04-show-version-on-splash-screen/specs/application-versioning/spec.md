## MODIFIED Requirements

### Requirement: Version and diagnostics are visible and copyable
FusionCanvas SHALL present the product version in the splash screen and discoverable settings surface, and SHALL expose a copyable diagnostic block suitable for bug reports.

#### Scenario: User sees the version while the application starts
- **WHEN** the splash screen is displayed
- **THEN** it shows the user-friendly product version in the form `Version Major.Minor.Build`
- **AND** the value comes from the application version provider

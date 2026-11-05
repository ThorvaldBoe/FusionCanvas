## ADDED Requirements

### Requirement: Application data belongs to a selected local data root
FusionCanvas SHALL resolve its default workspace database, managed workspace-file directory, application settings file, and local AI model caches from one selected application data root.

#### Scenario: Installed application uses its existing data root
- **WHEN** FusionCanvas starts without a data-root override
- **THEN** it continues to use `%LOCALAPPDATA%\FusionCanvas` for its database, workspace files, settings, and related caches
- **AND** previously stored installed-user data remains available without copying or migration

#### Scenario: Repository development launch uses an isolated data root
- **WHEN** a contributor starts FusionCanvas using the repository's development launch profile
- **THEN** the app uses `%LOCALAPPDATA%\FusionCanvas-Development` as its data root
- **AND** its database, workspace files, settings, and related caches are stored beneath that root
- **AND** no existing installed-user data is copied or changed

#### Scenario: New development data root is empty
- **WHEN** the development profile starts and its data root has not been used before
- **THEN** FusionCanvas opens an empty workspace and initializes its database and settings as needed

#### Scenario: Explicit path overrides remain available
- **WHEN** a test or isolated run supplies an explicit database, workspace-file, or settings path
- **THEN** that explicit path takes precedence for its corresponding data
- **AND** unspecified paths continue to follow the selected data root or existing fallback behavior

#### Scenario: Provider credentials remain shared
- **WHEN** the development or installed application reads or writes provider credentials
- **THEN** both continue using the existing OS-protected FusionCanvas credential service
- **AND** selecting a different local data root does not duplicate or migrate credentials

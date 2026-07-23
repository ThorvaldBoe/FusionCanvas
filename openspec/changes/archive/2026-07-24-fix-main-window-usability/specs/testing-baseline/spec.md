## ADDED Requirements

### Requirement: Headless view tests isolate workspace data
FusionCanvas SHALL construct headless view tests with an in-memory or disposable workspace and SHALL NOT use any application factory, repository, or path that opens the contributor's real on-disk workspace database or workspace file root, so automated view tests never read or mutate the contributor's normal workspace data.

#### Scenario: Headless view test uses isolated workspace data
- **WHEN** a headless view test constructs a window or view model that needs workspace state
- **THEN** the test uses the in-memory sample workspace or an explicit disposable test repository
- **AND** does not call any factory that resolves the real on-disk workspace database path

#### Scenario: Real-workspace factory is not used by tests
- **WHEN** an application entry point exposes a factory that opens the contributor's real on-disk workspace
- **THEN** automated tests do not call that factory
- **AND** any view model that needs a workspace in tests is constructed with isolated data instead

## ADDED Requirements

### Requirement: Selected store preference persists locally
FusionCanvas SHALL persist the identity of the most recently selected active store in the local application-settings document, independently of workspace data.

#### Scenario: Selected store survives restart
- **WHEN** the user selects an active store in the current workspace, quits FusionCanvas, and starts it again
- **THEN** FusionCanvas restores that store as the selected store when the store still belongs to the active workspace and remains active

#### Scenario: Missing or stale selected store falls back safely
- **WHEN** the saved selected store ID is missing, malformed, archived, deleted, or belongs to another workspace
- **THEN** FusionCanvas ignores that ID
- **AND** the existing first-active-store fallback behavior is used

#### Scenario: Existing settings remain backward compatible
- **WHEN** FusionCanvas loads a settings document written without a selected store preference
- **THEN** the document loads successfully
- **AND** all existing readable settings remain available


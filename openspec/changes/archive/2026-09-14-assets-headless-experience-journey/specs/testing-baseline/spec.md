## ADDED Requirements

### Requirement: Critical asset-management jobs receive a rendered headless journey
FusionCanvas SHALL protect a critical store-level asset-management job with a deterministic Avalonia headless experience journey when the outcome crosses rendered controls, file selection, persistence, or preview lifecycle seams.

#### Scenario: A creator imports and reviews a store asset
- **WHEN** a creator opens the store-level Assets surface, chooses a supported image through the import control, confirms the suggested purpose, changes the purpose, and activates the asset preview
- **THEN** the journey exercises those actions through rendered controls and routed input
- **AND** it asserts the visible pending, imported, relabeled, and preview states communicated to the creator
- **AND** it verifies the managed file reference and purpose after reconstructing a fresh presentation instance from scenario-owned persistence

#### Scenario: Asset removal is cancelled
- **WHEN** the creator requests removal from the rendered asset row and cancels the confirmation
- **THEN** the asset row, purpose, managed-file state, and current selection remain unchanged
- **AND** the journey does not claim that lower-layer removal variants are covered by the rendered path

#### Scenario: A deterministic picker or preview boundary is unavailable
- **WHEN** native file-picker behavior or platform image rendering cannot be represented faithfully by the headless harness
- **THEN** the journey uses an explicit deterministic boundary fake and verifies separable UI and persistence decisions
- **AND** the limitation is recorded as supplemental evidence scope rather than silently bypassing the rendered seam

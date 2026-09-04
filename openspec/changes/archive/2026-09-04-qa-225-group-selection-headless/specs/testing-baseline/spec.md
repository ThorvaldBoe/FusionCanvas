## MODIFIED Requirements

### Requirement: User-facing views receive headless verification where valuable
FusionCanvas SHALL use Avalonia headless tests as the routine framework-level verification lane for user-facing views when rendering, bindings, control state, routed input, focus, or visual-tree behavior carries meaningful risk.

#### Scenario: Group selection dialog behavior is protected
- **WHEN** a contributor changes or reviews `GroupSelectionWindow`
- **THEN** focused Avalonia headless tests cover its destination and name bindings
- **AND** the tests cover invalid confirmation validation and successful confirmation through the rendered dialog controls
- **AND** the tests use isolated deterministic destinations without opening workspace persistence

## ADDED Requirements

### Requirement: Navigation presents universal records as Items
The navigation tree SHALL use Item terminology for stage-agnostic records and SHALL use the stable identity-derived fallback for an empty working title.

#### Scenario: Empty-title Item appears in the tree
- **WHEN** an Item has no working title
- **THEN** its row shows `Untitled item · <short ID>`
- **AND** retains normal selection, filtering, inactive treatment, and context actions

#### Scenario: Published or Rejected Item appears
- **WHEN** an Item becomes Published or Rejected
- **THEN** its row updates to the authoritative lifecycle treatment
- **AND** canonical selection remains coherent if active filters remove it


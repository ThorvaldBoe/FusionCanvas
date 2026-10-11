## ADDED Requirements

### Requirement: Linked-product visibility control communicates its state
The Printify import dialog SHALL expose the next linked-product visibility action in the control's visible label and accessible name. When linked products are hidden, the label SHALL be “Show linked products”; when they are visible, it SHALL be “Hide linked products”. Toggling SHALL preserve the existing default-hidden state and shall not make linked products eligible for import.

#### Scenario: Linked products are hidden by default
- **WHEN** the import dialog is displayed with linked products hidden
- **THEN** the control's visible label and accessible name are “Show linked products”
- **AND** the visible list excludes linked products

#### Scenario: User shows linked products
- **WHEN** the user activates “Show linked products”
- **THEN** linked products appear with their linked indicator and remain ineligible for import
- **AND** the control's visible label and accessible name change to “Hide linked products”

#### Scenario: User hides linked products again
- **WHEN** the user activates “Hide linked products”
- **THEN** linked products are removed from the visible list
- **AND** the control's visible label and accessible name change to “Show linked products”

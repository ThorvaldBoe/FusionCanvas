## ADDED Requirements

### Requirement: Mockup Template Color applicability choices are searchable

The focused Mockup Template editor SHALL provide a transient search text box for its eligible Color applicability choices. As the user types, the visible choices SHALL be filtered by case-insensitive substring matching against each choice's displayed label, while the underlying eligible choice set and selection state remain unchanged.

#### Scenario: User searches Color applicability choices

- **WHEN** the user enters text in the Color search box
- **THEN** the editor shows only eligible Color choices whose displayed labels contain the query using case-insensitive substring matching
- **AND** the remaining choices retain their existing order

#### Scenario: User clears the Color search

- **WHEN** the Color search query is empty or contains only whitespace
- **THEN** the editor shows every eligible Color choice in its existing order
- **AND** each choice retains its prior selected or unselected state

#### Scenario: Search has no matching Colors

- **WHEN** the Color search query matches no eligible Color choice
- **THEN** the editor shows no Color checkboxes
- **AND** it displays clear guidance that no Colors match the search
- **AND** the editor continues to allow the user to edit or clear the query

#### Scenario: Hidden selected Colors remain part of the draft

- **WHEN** a selected Color is hidden by the active search query
- **THEN** the Color remains selected in the underlying draft
- **AND** saving, readiness evaluation, dirty-state tracking, and source-image applicability continue to include that selected Color
- **AND** clearing the query shows the Color again as selected

#### Scenario: Color search is not persisted

- **WHEN** the user closes the Mockup Template editor and later opens a Mockup Template editor session
- **THEN** the Color search query starts empty
- **AND** persisted template configuration contains no Color search text

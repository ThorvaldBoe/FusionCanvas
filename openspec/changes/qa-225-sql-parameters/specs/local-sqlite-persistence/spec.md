## ADDED Requirements

### Requirement: SQLite binds data values and constrains dynamic identifiers
SQLite persistence SHALL bind data values through command parameters and SHALL validate unavoidable dynamic table or column identifiers before interpolating them into SQL.

#### Scenario: A persistence query uses a typed identifier
- **WHEN** a repository query filters or relates rows by an entity identifier
- **THEN** the identifier is supplied as a command parameter rather than interpolated into SQL text

#### Scenario: A migration requires a dynamic identifier
- **WHEN** migration code must interpolate a table or column identifier because SQLite does not accept a parameter there
- **THEN** the identifier is checked against the repository’s safe identifier rules before SQL execution

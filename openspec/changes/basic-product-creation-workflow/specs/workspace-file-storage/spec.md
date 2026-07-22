## ADDED Requirements

### Requirement: Managed PNG files can be previewed safely
FusionCanvas SHALL provide traversal-safe read access to an existing managed PNG for in-app preview without making an external source path authoritative.

#### Scenario: Preview resolves a valid managed reference
- **WHEN** a Design-file preview requests an existing workspace-relative managed reference
- **THEN** FusionCanvas reads the managed file within the workspace boundary
- **AND** does not retain a file lock beyond the preview load

#### Scenario: Preview reference escapes the workspace
- **WHEN** a malformed or traversing reference is supplied
- **THEN** FusionCanvas rejects it
- **AND** does not read a file outside the managed workspace boundary

### Requirement: Managed files can be exported as independent copies
FusionCanvas SHALL copy an existing managed file to a user-selected local destination without modifying the managed source or structured Asset data.

#### Scenario: Export copy succeeds
- **WHEN** the source resolves within managed storage and the destination is writable and distinct
- **THEN** the destination receives identical bytes
- **AND** the managed source and Asset record remain unchanged

#### Scenario: Export is cancelled or invalid
- **WHEN** the picker is cancelled, the source is missing, the destination is unwritable, or source and destination are the same file
- **THEN** FusionCanvas reports cancellation or an actionable recoverable error as appropriate
- **AND** leaves managed and structured state unchanged


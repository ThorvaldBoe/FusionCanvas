## ADDED Requirements

### Requirement: Each Blueprint Offering may identify one primary Design Area for artwork generation
FusionCanvas SHALL let an editable Blueprint Offering persist zero or one active Design Area as primary for artwork generation. The Design Area editor SHALL expose a `Primary for artwork generation` checkbox. Saving it checked SHALL atomically clear the primary designation from any other Design Area in the same offering; no designation SHALL be inferred from list order.

#### Scenario: User marks a Design Area primary
- **WHEN** the user checks Primary for artwork generation and saves a valid Design Area
- **THEN** that area becomes the offering's sole primary area
- **AND** any previous primary in that offering is cleared in the same save

#### Scenario: User leaves an offering without a primary
- **WHEN** no active Design Area is designated primary
- **THEN** the offering remains valid
- **AND** Design-stage generation starts with no target rather than selecting the first area

#### Scenario: User reviews an archived Store or read-only offering
- **WHEN** Store catalog editing is unavailable
- **THEN** the primary designation is visible as read-only guidance
- **AND** no primary mutation can be persisted

#### Scenario: Cross-offering primary is requested
- **WHEN** a request attempts to designate a Design Area owned by another offering
- **THEN** FusionCanvas rejects it and preserves the prior designation

### Requirement: Primary designation is cleared explicitly when its Design Area leaves active use
FusionCanvas SHALL require explicit confirmation when archiving or permanently removing a primary Design Area and SHALL clear the offering's primary designation as part of the same successful operation. FusionCanvas SHALL NOT silently designate a replacement.

#### Scenario: User archives the primary Design Area
- **WHEN** the user confirms archival of the primary area and dependency policy permits it
- **THEN** the area is archived and the primary designation is cleared atomically
- **AND** the offering has no primary until the user chooses another

#### Scenario: User permanently removes the primary Design Area
- **WHEN** the user confirms permitted permanent removal of the primary area
- **THEN** removal and clearing the primary designation succeed or fail together
- **AND** no replacement is selected automatically

#### Scenario: Archive or removal is cancelled or fails
- **WHEN** the user cancels or persistence fails
- **THEN** the Design Area and primary designation remain unchanged

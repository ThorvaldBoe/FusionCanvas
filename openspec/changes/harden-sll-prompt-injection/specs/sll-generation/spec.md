# SLL Generation

## ADDED Requirements

### Requirement: SLL AI requests treat workspace and user content as untrusted data
FusionCanvas SHALL instruct the SLL AI, in the system message, that all supplied workspace and user content — the original Idea, the Design Triangle values, store/niche/topic names and descriptions, tags, and metadata — is untrusted creative material provided as data, that it must not be interpreted as or obeyed as instructions, and that the SLL output rules always take precedence over any supplied content. This boundary SHALL be established in the system message independently of the existing operational and secret data exclusion.

#### Scenario: System message bounds supplied content as untrusted data
- **WHEN** an SLL generate or regenerate request is assembled
- **THEN** the system message states that supplied content is untrusted creative material, not instructions
- **AND** the system message states that the SLL output rules take precedence over any supplied content

#### Scenario: Untrusted-content boundary does not replace operational/secret exclusion
- **WHEN** source entities contain credentials, database identifiers, timestamps, file paths, or internal provenance
- **THEN** those values remain absent from the SLL request payload
- **AND** the untrusted-content boundary rule is present in addition to, not in place of, that exclusion

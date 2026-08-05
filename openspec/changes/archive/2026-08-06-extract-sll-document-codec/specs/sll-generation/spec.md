# SLL Generation

## ADDED Requirements

### Requirement: SLL document serialization is owned outside the Domain layer
FusionCanvas SHALL serialize and deserialize SLL documents through an Application-defined codec implemented outside the Domain layer, and the Domain `SllDocument` type SHALL carry no serialization logic and no dependency on a serialization framework. The codec SHALL preserve the existing SLL document wire format so an SLL persisted by a prior build round-trips back to an equal document, and SHALL treat malformed input as a recoverable failure rather than throwing.

#### Scenario: Domain SLL document type carries no serialization
- **WHEN** a contributor inspects the Domain `SllDocument` type
- **THEN** it exposes no serialization or deserialization methods
- **AND** the Domain project has no dependency on a JSON or persistence-framework package

#### Scenario: SLL document round-trips through the codec
- **WHEN** an SLL document is serialized through the SLL codec and the result is deserialized through the same codec
- **THEN** the deserialized document is equal to the original document

#### Scenario: Malformed SLL input is a recoverable failure
- **WHEN** the SLL codec is asked to deserialize text that is not a valid SLL document
- **THEN** the codec reports failure without throwing
- **AND** no partial document is produced

#### Scenario: App layer uses the codec contract, not the Domain type, to serialize
- **WHEN** the SLL session stores or re-displays a generated SLL
- **THEN** it serializes and deserializes through the Application codec contract
- **AND** the App layer does not construct an Integration serialization type directly

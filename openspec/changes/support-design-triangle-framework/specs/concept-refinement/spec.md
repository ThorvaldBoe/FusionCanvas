## MODIFIED Requirements

### Requirement: Refinement requests use guidance and creative context without operational or secret data
FusionCanvas SHALL assemble refinement AI requests from the bundled canonical Design Triangle framework, the action semantics, the current triangle values, the Item's original Idea, and applicable user-authored creative context (store, niche, topic, inherited tags and metadata), and MUST exclude credentials, identifiers, timestamps, file paths, and other operational fields from the request payload. The request SHALL instruct the model to respect the framework's social-meaning model, coherent three-corner relationship, and semantic graphic role while preserving the existing Initialize and per-corner response contracts.

#### Scenario: Request includes framework and creative context
- **WHEN** a Fine tune, Change, or Initialize request is assembled
- **THEN** it contains the canonical Design Triangle framework, the action instruction, current triangle values, original Idea text, and applicable creative context
- **AND** it directs the model to preserve or improve wearer signal, viewer inference or effect, intentional Phrase/Graphic relationship, and Graphic semantic role as applicable to the action

#### Scenario: Operational and secret data is excluded
- **WHEN** source entities contain credentials, database identifiers, timestamps, file paths, or internal provenance
- **THEN** those values are absent from the request payload, logs, and errors

### Requirement: The design-triangle guidance document ships with the app
FusionCanvas SHALL bundle one canonical PoD Design Framework Markdown document with the application and SHALL load it at runtime through an application-facing contract for use as Ideation and Concept refinement prompt context. The document SHALL combine the framework README and canonical Foundations of PoD Design, Design Triangle and Design Pyramid, Sketch Layout Language, and Generating SLL documents in that order. No user interface SHALL display or open the document in this module.

#### Scenario: Canonical framework content is available at runtime
- **WHEN** an Ideation or Concept refinement prompt is assembled
- **THEN** the bundled content includes the social-meaning, Design Triangle, Design Pyramid, SLL, and SLL-generation framework sections as prompt context

#### Scenario: No framework UI
- **WHEN** the user navigates the Idea or Concept stage surfaces
- **THEN** no control displays or opens the framework document

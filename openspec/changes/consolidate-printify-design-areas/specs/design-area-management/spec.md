## MODIFIED Requirements

### Requirement: Design Areas describe actual printable regions for one Offering
FusionCanvas SHALL provide focused management of Design Areas for one Blueprint Offering, using the existing Placeholder identity and invariants for printable regions. Each Design Area SHALL capture a user-facing name, placement, positive maximum pixel dimensions, compatible concrete Variants, optional provider reference, and recommended artwork guidance. For imported Printify data, one active Design Area represents one logical provider position and decoration method; its maximum dimensions may be the aggregate maximum across its compatible variants rather than a geometry unique to every variant.

#### Scenario: Imported area summarizes variant geometry
- **WHEN** a Printify import consolidates several compatible variants that report different dimensions for one position and decoration method
- **THEN** the Design Area list shows one active area for that logical position and method
- **AND** its pixel dimensions are the maximum supported dimensions used for artwork generation
- **AND** its compatibility summary reflects all imported variants in the logical group

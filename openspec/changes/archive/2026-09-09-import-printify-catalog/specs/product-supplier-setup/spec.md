## ADDED Requirements

### Requirement: Imported catalog records retain provider identity and local terminology
Imported catalog records SHALL use the existing Blueprint, Blueprint Offering, Option, Option Value, Variant, and Placeholder/design-area concepts, retain stable Printify identities separately from editable display labels, and preserve Store ownership.

#### Scenario: Import maps one provider offering
- **WHEN** a selected Printify Blueprint has one Print Provider offering
- **THEN** FusionCanvas creates or updates one Store-owned Blueprint and offering
- **AND** the offering retains the Printify Blueprint and provider identities
- **AND** provider titles remain display data rather than relationship keys

#### Scenario: Imported variant compatibility is preserved
- **WHEN** Printify assigns a design area to a subset of sellable variants
- **THEN** the corresponding local Placeholder/design area is compatible with exactly those imported variants
- **AND** the relationship remains stable after a repeated import

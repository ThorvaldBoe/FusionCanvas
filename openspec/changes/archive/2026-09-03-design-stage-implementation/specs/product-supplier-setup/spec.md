# Product Supplier Setup

## Purpose

Updates how catalog edits protect Item-selected references under the Design Stage Implementation model, where an Item selects a listing configuration (an offering) rather than individual printable areas.

## RENAMED Requirements

- FROM: `### Requirement: Catalog edits preserve selected Item targets`
  TO: `### Requirement: Catalog edits preserve selected Item configurations`

## MODIFIED Requirements

### Requirement: Catalog edits preserve selected Item configurations
FusionCanvas SHALL require explicit safe handling before a user permanently removes a catalog record that is referenced by an Item's listing configuration or its derived rows and slot assignments.

#### Scenario: User removes an unreferenced offering
- **WHEN** the user confirms removal of a product, offering, variant, or printable area that no Item references as its configuration and that no slot assignment references
- **THEN** FusionCanvas removes that record
- **AND** it preserves unrelated products, offerings, variants, printable areas, and Items

#### Scenario: User removes a referenced offering
- **WHEN** the user requests removal of an offering that is an Item's listing configuration, or of a printable area referenced by an Item's slot assignment
- **THEN** FusionCanvas blocks the removal
- **AND** explains that the configuration or slot reference must first be cleared or replaced on the affected Items

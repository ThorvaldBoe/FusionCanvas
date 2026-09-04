# Design Area Target Selection

## Purpose

Defines how an editable Item at Design respects workflow editability for its listing configuration and final-design slot grid, and records that the prior optional multi-target printable-area selection is removed. The mandatory listing-configuration anchor and the slot grid are owned by the Design Stage Implementation capability.

## REMOVED Requirements

### Requirement: Design stage supports optional Store design targets
**Reason**: Replaced by the mandatory singular listing configuration and row × design-area slot grid defined in the Design Stage Implementation capability. The Item is now anchored to one catalog offering whose printable areas define the final-design slot grid columns, rather than to an optional zero-or-more set of individually selected printable areas.
**Migration**: An Item previously at Design with one or more selected printable-area targets is converged to the new model as having no listing configuration, so it shows the configuration-selection prompt. Existing Design files and Supporting images are preserved.

## MODIFIED Requirements

### Requirement: Target selection respects workflow editability
FusionCanvas SHALL expose the listing configuration selector and the final-design slot grid read-only whenever the Item's active Design content is read-only.

#### Scenario: User reviews Design from a protected context
- **WHEN** Design-stage editing is unavailable because the Item is protected or an earlier stage is being reviewed
- **THEN** FusionCanvas shows the persisted configuration and any slot images as read-only guidance
- **AND** it does not commit a configuration or slot mutation

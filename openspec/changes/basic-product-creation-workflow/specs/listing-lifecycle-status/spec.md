## MODIFIED Requirements

### Requirement: Listing carries one workflow stage and one lifecycle status
An Item SHALL persist exactly one workflow stage (`Idea`, `Concept`, `Design`, or `Listing`) and exactly one lifecycle status as independent user-owned facts, and SHALL NOT derive one from the other except to enforce explicit transition preconditions.

#### Scenario: Item persists stage and status independently
- **WHEN** an Item is saved and reloaded
- **THEN** its workflow stage and lifecycle status are reconstructed with their persisted values
- **AND** neither value is computed from the other

#### Scenario: Status change does not move the stage
- **WHEN** the user changes an Item's lifecycle status
- **THEN** its workflow stage remains unchanged

### Requirement: User changes lifecycle status quickly
FusionCanvas SHALL expose one Item-level status selector and SHALL allow exactly these transitions: Draft to Published at Listing or Rejected; Published to Paused or Rejected; Paused to Published at Listing, Draft, or Rejected; and Rejected to Draft.

#### Scenario: Item exposes allowed transitions
- **WHEN** the status selector opens
- **THEN** it displays the persisted current status and offers only direct transition targets valid for the Item's current status and stage
- **AND** Published is unavailable unless Listing is current
- **AND** Paused is never entered from a status other than Published

#### Scenario: Confirmation is required
- **WHEN** a transition enters Published or Rejected or leaves Published
- **THEN** FusionCanvas asks for one confirmation
- **AND** cancellation leaves stage, status, content, and selection unchanged

#### Scenario: Status change fails to persist
- **WHEN** an approved status change cannot be saved
- **THEN** the selector reverts to persisted status
- **AND** an actionable inline error appears without partial application

### Requirement: Stage movement pauses for inactive listings while status stays recoverable
FusionCanvas SHALL disable stage movement while an Item is Published, Rejected, archived, or effectively archived and SHALL keep the allowed status-recovery and lifecycle actions available.

#### Scenario: Published Item cannot move stages
- **WHEN** the active Item is Published
- **THEN** advance and regress controls are unavailable
- **AND** the surface explains that the Item must be confirmed Paused before protected content can change

#### Scenario: Rejected Item is reactivated
- **WHEN** the user confirms the allowed Rejected-to-Draft transition
- **THEN** the Item becomes active Draft work at its preserved workflow stage

### Requirement: Published and Rejected Items protect product content
FusionCanvas SHALL keep stage text, Design files, related-asset relationships, and stage movement read-only for Published and Rejected Items while allowing approved shared metadata and lifecycle operations.

#### Scenario: Published Item is reviewed
- **WHEN** a Published Item is active
- **THEN** stage content, Design-file mutations, related-asset mutations, and stage movement are unavailable
- **AND** working title, Notes, Tags, topic placement, archive, and allowed status transitions remain available

#### Scenario: User modifies Published content intentionally
- **WHEN** the user requests a protected change on a Published Item
- **THEN** FusionCanvas offers one confirmation to change it to Paused
- **AND** after confirmation the user must regress to an upstream stage before editing that stage

#### Scenario: Rejected Item is reviewed
- **WHEN** a Rejected Item is active
- **THEN** product content and stage movement remain read-only
- **AND** working title, Notes, Tags, topic placement, archive, and Rejected-to-Draft recovery remain available

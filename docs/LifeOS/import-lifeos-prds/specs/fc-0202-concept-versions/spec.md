## ADDED Requirements

### Requirement: Listing Can Have Multiple Concept Versions
FusionCanvas SHALL ensure that a listing can have multiple concept versions.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** A listing can have multiple concept versions

### Requirement: Each Concept Can Describe Idea, Phrase, Graphic Direction, Audience Reaction, Risks, Quality Notes, And Design Triangle Score Metadata Where Available
FusionCanvas SHALL ensure that each concept can describe idea, phrase, graphic direction, audience reaction, risks, quality notes, and design triangle score metadata where available.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Each concept can describe idea, phrase, graphic direction, audience reaction, risks, quality notes, and design triangle score metadata where available

### Requirement: Concepts Belong To The Concept Stage In The Core Workflow
FusionCanvas SHALL ensure that concepts belong to the Concept stage in the core workflow.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Concepts belong to the Concept stage in the core workflow

### Requirement: Concept Can Be Created From An Existing Idea Or Directly From A Phrase, Graphic Direction, Or Other Later-stage Starting Point
FusionCanvas SHALL ensure that a concept can be created from an existing idea or directly from a phrase, graphic direction, or other later-stage starting point.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** A concept can be created from an existing idea or directly from a phrase, graphic direction, or other later-stage starting point

### Requirement: Concept Tools Should Inherit Store, Niche, Topic Path, Item, And Relevant Metadata From The Current Context
FusionCanvas SHALL ensure that concept tools should inherit store, niche, topic path, item, and relevant metadata from the current context.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Concept tools should inherit store, niche, topic path, item, and relevant metadata from the current context

### Requirement: Concept Work Requires An Existing Item. There Is No Free-floating Concept That Is Not Attached To An Item
FusionCanvas SHALL ensure that concept work requires an existing item. There is no free-floating concept that is not attached to an item.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Concept work requires an existing item. There is no free-floating concept that is not attached to an item

### Requirement: One Concept Can Be Marked As The Current Or Selected Direction
FusionCanvas SHALL ensure that one concept can be marked as the current or selected direction.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** One concept can be marked as the current or selected direction

### Requirement: Selected Concept Version Supplies The Current Idea, Phrase, And Graphic Values Shown By The Basic Concept Tool
FusionCanvas SHALL ensure that the selected concept version supplies the current idea, phrase, and graphic values shown by the Basic Concept Tool.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The selected concept version supplies the current idea, phrase, and graphic values shown by the Basic Concept Tool

### Requirement: Accepting A Large Alternate Direction From The Basic Concept Tool Can Create A New Concept Version Instead Of Overwriting The Selected One
FusionCanvas SHALL ensure that accepting a large alternate direction from the Basic Concept Tool can create a new concept version instead of overwriting the selected one.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Accepting a large alternate direction from the Basic Concept Tool can create a new concept version instead of overwriting the selected one

### Requirement: Superseded And Rejected Concepts Remain Available
FusionCanvas SHALL ensure that superseded and rejected concepts remain available.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Superseded and rejected concepts remain available

### Requirement: Concept History Should Help Explain Why A Listing Evolved
FusionCanvas SHALL ensure that concept history should help explain why a listing evolved.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Concept history should help explain why a listing evolved

### Requirement: Acceptance Criteria Remain Satisfied
FusionCanvas SHALL satisfy the acceptance criteria captured in the source PRD.

#### Scenario: User Can Create More Than One Concept For A Listing
- **WHEN** the corresponding capability is delivered
- **THEN** A user can create more than one concept for a listing.

#### Scenario: User Can Create A Concept From The Current Topic Or Item Context Without Re-entering That Context Manually
- **WHEN** the corresponding capability is delivered
- **THEN** A user can create a concept from the current topic or item context without re-entering that context manually.

#### Scenario: User Can Start At The Concept Stage When They Already Have Enough Direction
- **WHEN** the corresponding capability is delivered
- **THEN** A user can start at the Concept stage when they already have enough direction.

#### Scenario: User Cannot Work On A Concept Without A Selected Item
- **WHEN** the corresponding capability is delivered
- **THEN** A user cannot work on a concept without a selected item.

#### Scenario: User Can Identify The Selected Concept
- **WHEN** the corresponding capability is delivered
- **THEN** A user can identify the selected concept.

#### Scenario: User Can Preserve A Promising Alternate Concept Without Losing The Current Direction
- **WHEN** the corresponding capability is delivered
- **THEN** A user can preserve a promising alternate concept without losing the current direction.

#### Scenario: User Can Review Old Concepts Later
- **WHEN** the corresponding capability is delivered
- **THEN** A user can review old concepts later.

#### Scenario: Rejected Concepts Do Not Clutter The Active Direction
- **WHEN** the corresponding capability is delivered
- **THEN** Rejected concepts do not clutter the active direction.

## Source PRD

# FC-0202 - Concept Versions

## Summary

Concept Versions preserve alternate creative directions for a listing.

The active concept version is the working state used by the Basic Concept Tool. It should preserve the idea, phrase, graphic direction, quality notes, and relevant history for a single existing item.

## User Need

Creators need to keep useful rejected or superseded concepts instead of losing the reasoning behind a design.

## Requirements

- A listing can have multiple concept versions.
- Each concept can describe idea, phrase, graphic direction, audience reaction, risks, quality notes, and design triangle score metadata where available.
- Concepts belong to the Concept stage in the core workflow.
- A concept can be created from an existing idea or directly from a phrase, graphic direction, or other later-stage starting point.
- Concept tools should inherit store, niche, topic path, item, and relevant metadata from the current context.
- Concept work requires an existing item. There is no free-floating concept that is not attached to an item.
- One concept can be marked as the current or selected direction.
- The selected concept version supplies the current idea, phrase, and graphic values shown by the Basic Concept Tool.
- Accepting a large alternate direction from the Basic Concept Tool can create a new concept version instead of overwriting the selected one.
- Superseded and rejected concepts remain available.
- Concept history should help explain why a listing evolved.

## Acceptance Criteria

- A user can create more than one concept for a listing.
- A user can create a concept from the current topic or item context without re-entering that context manually.
- A user can start at the Concept stage when they already have enough direction.
- A user cannot work on a concept without a selected item.
- A user can identify the selected concept.
- A user can preserve a promising alternate concept without losing the current direction.
- A user can review old concepts later.
- Rejected concepts do not clutter the active direction.

## Out of Scope

- AI concept refinement
- Version comparison UI
- Automatic scoring
- Design file versioning

## Related Notes

- [[Roadmap]]
- [[Product]]
- [[Data Model]]
- [[FC-0008 - Workflow Stage Navigator]]
- [[FC-0010 - Context-Aware Tools]]
- [[FC-0210 - Basic Concept Tool]]

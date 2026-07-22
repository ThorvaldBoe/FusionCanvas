## ADDED Requirements

### Requirement: Tags link to Items and persist independently from Item text drafts
FusionCanvas SHALL use Item terminology for universal Tag links and SHALL keep apply, create, and remove Tag operations independently immediate from guarded Item text edits.

#### Scenario: Tag is applied while Item text is dirty
- **WHEN** working title, current-stage text, or Notes has an unsaved draft and the user applies a Tag
- **THEN** the Tag link persists atomically to the Item
- **AND** the text draft and its Save/Discard/Cancel state remain unchanged

#### Scenario: Item terminology migration preserves Tags
- **WHEN** existing Listing Tag links are migrated
- **THEN** every link references the same stable Item and Tag IDs


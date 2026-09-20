## MODIFIED Requirements

### Requirement: The basic workflow remains durable and synchronized
FusionCanvas SHALL preserve confirmed Item workflow data across restart, archive and restore, and SHALL propagate every successful Item mutation to all open representations from authoritative persisted state. Intentional recovery from a stale Design listing configuration SHALL preserve creative history and downstream data while resetting only the Offering-specific Design relationships explicitly confirmed by the creator.

#### Scenario: Completed workflow survives restart
- **WHEN** an Item has confirmed data across Idea, Concept, Design, and Listing and the workspace is closed and reopened
- **THEN** the same Item ID, current stage, lifecycle status, archive state, text, Notes, Tags, Design files, related assets, and topic placement are reconstructed
- **AND** the document opens at the persisted current stage with the correct editability

#### Scenario: Item is archived and restored
- **WHEN** an Item with confirmed workflow data is archived and later restored to a valid active topic
- **THEN** all stage, status, metadata, file, Tag, relationship, and topic-placement data remains unchanged
- **AND** the restored Item follows the edit policy for its persisted stage and status

#### Scenario: Successful mutation synchronizes open contexts
- **WHEN** a stage, status, metadata, archive, Tag, or Design-file mutation succeeds for an Item represented in multiple open contexts
- **THEN** every representation and active filter refreshes from the authoritative confirmed state
- **AND** no stale context can silently overwrite that newer state

#### Scenario: Stale Design configuration is intentionally recovered
- **WHEN** the creator confirms replacing a stale listing configuration with an active same-Store Offering
- **THEN** FusionCanvas preserves the Item's Idea, Concept, SLL, managed assets, Supporting Images, generic relationships, workflow/lifecycle state, and downstream Listing data
- **AND** resets only the selected Design colors, variant rows, row-color relationships, slot assignments, and artwork target/transparency preferences tied to the prior configuration


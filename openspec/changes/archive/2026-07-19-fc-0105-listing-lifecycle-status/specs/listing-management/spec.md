## MODIFIED Requirements

### Requirement: Listing management duplicates listings as independent variations
FusionCanvas SHALL duplicate a listing into an active niche or group in the same store with a new identity, new timestamps, draft status, the idea workflow stage, copied core details, copied metadata and tags, and no copied asset, prompt, or future dependent-record relationships.

#### Scenario: User duplicates a listing in place
- **WHEN** the user invokes Duplicate without choosing another destination
- **THEN** FusionCanvas creates a new listing in the source topic
- **AND** gives it a distinct identity and collision-safe copy title
- **AND** resets it to draft status and the idea workflow stage regardless of the source stage or status
- **AND** copies the source description, notes, metadata, and tag links
- **AND** leaves all asset and prompt relationships attached only to the source

#### Scenario: User reviews a duplicated listing through the workflow
- **WHEN** the user opens a duplicate whose source had advanced beyond the idea stage
- **THEN** the duplicate presents the idea stage as current
- **AND** its copied creative details remain available for review as the user advances it stage by stage

#### Scenario: User copies and pastes into another topic
- **WHEN** the user copies a listing and pastes it into another active niche or group in the same store
- **THEN** FusionCanvas creates the independent duplicate at that destination with draft status and the idea workflow stage
- **AND** selects and reveals the duplicate without changing the source

#### Scenario: User attempts to duplicate across stores
- **WHEN** the user chooses a destination in another store
- **THEN** FusionCanvas rejects duplication
- **AND** creates no partial listing or relationships

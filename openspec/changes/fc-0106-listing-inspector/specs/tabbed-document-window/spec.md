## ADDED Requirements

### Requirement: Listing document contexts present the listing inspector
When the active document context is a listing item, the document window detail area SHALL present the Listing Inspector as that context's working view instead of placeholder stage-tool content, while non-item document contexts retain the existing stage tool or working view behavior.

#### Scenario: User activates a listing tab
- **WHEN** the active document tab's context is a listing item
- **THEN** the detail area displays the Listing Inspector for that listing beneath the workflow stage navigator
- **AND** the inspector follows the active tab when the user switches between listing tabs

#### Scenario: User switches between item and non-item contexts
- **WHEN** the user switches from a listing tab to a store or topic tab
- **THEN** the detail area returns to the stage tool or working view behavior for the non-item context
- **AND** switching back to the listing tab restores the inspector for that listing

#### Scenario: Listing details change outside the inspector
- **WHEN** the active listing's title, placement, archive state, status, or stage changes through another accepted surface while its tab is open
- **THEN** the detail area reflects the updated persisted listing context

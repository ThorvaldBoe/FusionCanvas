## MODIFIED Requirements

### Requirement: Generated mockups are inspectable, usable, and attributable Item assets
Each successful mockup SHALL be persisted as an Item-linked `MockupImage` Asset in managed workspace storage. The output metadata SHALL identify the source Item Color, template identity, template revision, and source Design Asset so outputs remain attributable after a later template change. The Listing-stage mockup tool SHALL present each available output as an inspectable result rather than only as a filename, with a thumbnail, an enlarged view, and explicit actions for saving a copy and removing the generated output. Persistence SHALL be atomic for the Asset and link, and a failed save SHALL not leave an orphaned managed file.

#### Scenario: Listing shows generated mockups as thumbnails
- **WHEN** an editable Item has one or more persisted generated mockups whose managed files are readable
- **THEN** the Listing mockup area shows a thumbnail for each output
- **AND** each output exposes enough attribution to distinguish its applicable Color and template/source context
- **AND** the output is not represented only by its file name

#### Scenario: Creator opens an enlarged mockup view
- **WHEN** the creator activates a readable mockup thumbnail or its preview action
- **THEN** FusionCanvas shows the mockup at an enlarged size suitable for judging the design and color treatment
- **AND** closing the enlarged view returns to the same Item and Listing context without changing the generated asset

#### Scenario: Creator saves a mockup copy
- **WHEN** the creator activates the download or save-copy action for a readable generated mockup and chooses a destination
- **THEN** FusionCanvas copies the generated image to the chosen destination
- **AND** preserves the managed workspace original and its attribution
- **AND** communicates whether the copy completed or could not be saved

#### Scenario: Creator removes a generated mockup
- **WHEN** the creator requests removal of a generated mockup and confirms the consequence
- **THEN** FusionCanvas removes that output's Asset and Item link atomically
- **AND** deletes the corresponding managed workspace file on a best-effort basis
- **AND** leaves source Design assets, template assets, and other generated mockups unchanged
- **AND** refreshes the gallery to show the remaining outputs or a useful empty state

#### Scenario: Protected Listing keeps generated mockups read-only
- **WHEN** the Item is Published, Rejected, archived, or otherwise protected by the existing Listing policy
- **THEN** existing generated mockups remain viewable when their files are available
- **AND** save-copy remains available when it is safe and supported
- **AND** generation and destructive removal controls are disabled with an explanation of the protection state

### Requirement: Generated mockups become invalid when their Design source changes
FusionCanvas SHALL treat a generated mockup as a derived result of the source Design Asset and relevant Design assignment used during generation. When a Design mutation changes, replaces, removes, or invalidates that source or assignment, FusionCanvas SHALL remove the affected generated mockup outputs before presenting them as current. If the affected outputs cannot be mapped precisely, FusionCanvas SHALL invalidate all generated mockups for the Item rather than retain potentially misleading results. Invalidation SHALL not delete source Design or template assets.

#### Scenario: Design source replacement invalidates dependent mockups
- **WHEN** a Design-stage operation replaces or removes a Design Asset that is recorded in generated mockup metadata
- **THEN** the dependent generated mockup Asset records, Item links, and managed files are removed
- **AND** unaffected generated mockups remain available
- **AND** returning to Listing communicates that mockups were removed because the Design changed and offers generation as the next action

#### Scenario: Unmappable Design mutation invalidates the Item's derived mockups
- **WHEN** a Design mutation changes the effective source or assignment but the application cannot identify the affected outputs individually
- **THEN** all generated mockups for that Item are invalidated
- **AND** the source Design assets remain intact
- **AND** Listing communicates that new mockups must be generated from the updated Design

#### Scenario: Invalidation fails to remove one managed file
- **WHEN** the database no longer retains an output but its managed file cannot be deleted
- **THEN** the application preserves the valid persisted state
- **AND** records or surfaces a recoverable cleanup diagnostic
- **AND** does not present the orphaned file as a current generated mockup

### Requirement: Mockup consumption preserves interaction state and focus
The Listing mockup tool SHALL expose keyboard-reachable thumbnail, preview, save-copy, and removal actions; show busy state while preview, download, delete, or invalidation work is pending; prevent duplicate destructive operations; preserve the selected template and unaffected outputs after recoverable failures; and return focus to a meaningful Listing control after an enlarged view or confirmation closes when practical.

#### Scenario: Creator deletes one output from a populated gallery
- **WHEN** the creator confirms removal for one of several outputs
- **THEN** only that output enters the pending/removal state
- **AND** the other outputs remain visible and usable
- **AND** focus returns to the removed output's replacement or a nearby gallery control after completion

#### Scenario: Download or preview fails
- **WHEN** a generated output is missing, unreadable, or cannot be copied
- **THEN** the output is shown with an explicit unavailable/error state
- **AND** the Listing context, other outputs, and source assets remain unchanged
- **AND** the user receives an actionable explanation where recovery is possible

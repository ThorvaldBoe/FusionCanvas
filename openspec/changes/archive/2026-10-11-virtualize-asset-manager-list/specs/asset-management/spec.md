## ADDED Requirements

### Requirement: Asset collection is a thumbnail-first virtualized list
FusionCanvas SHALL present assets in the focused Assets window as a vertically scrolling, one-column list that virtualizes off-screen rows, with each row keeping the thumbnail preview, asset name and managed file name, purpose editor, missing-file and context state, and removal action together. The list SHALL preserve the current asset order and SHALL NOT add search or sorting as part of this behavior.

#### Scenario: User reviews a large asset collection
- **WHEN** the Assets window displays a collection larger than its visible viewport
- **THEN** the window shows the collection as one-column thumbnail-first rows
- **AND** off-screen rows are not realized as row controls until scrolling brings them into the viewport

#### Scenario: User interacts with an asset row
- **WHEN** the user previews an asset, changes its purpose, or requests its removal while the list is scrolled
- **THEN** the corresponding row retains its thumbnail and asset metadata alongside its purpose and removal controls
- **AND** the existing preview, persistence, and removal-confirmation behavior remains in effect

#### Scenario: Asset details exceed available row width
- **WHEN** an asset name, managed file name, missing state, or context label does not fit in the visible row
- **THEN** the visible text is trimmed without removing its full accessible name or tooltip

#### Scenario: User scrolls after removing an asset
- **WHEN** the user confirms removal of an asset in the virtualized list
- **THEN** the collection removes that row and continues to show the remaining assets in their existing order
- **AND** when no assets remain, the existing empty state and import action are available

## MODIFIED Requirements

### Requirement: Listing management edits core listing details safely
FusionCanvas SHALL allow users to rename a listing inline in the tree and edit its description, notes, and creative fields inline in the details pane while preserving identity, placement, archive state, status, tags, assets, prompts, and unknown metadata.

#### Scenario: User renames a listing inline
- **WHEN** the user invokes inline rename for an active listing, enters a valid single-line title, and commits
- **THEN** FusionCanvas persists the normalized title and updated timestamp
- **AND** keeps the listing selected at the same topic location

#### Scenario: User edits optional listing properties
- **WHEN** the user changes description or notes in the details pane and the edited field loses focus
- **THEN** FusionCanvas persists the normalized values and updated timestamp automatically
- **AND** preserves the listing's stable identity and related context

#### Scenario: User submits an invalid title
- **WHEN** the user attempts to commit an empty, whitespace-only, or multi-line required title
- **THEN** FusionCanvas rejects the edit
- **AND** preserves both the persisted listing and the recoverable inline input

## REMOVED Requirements

### Requirement: Secondary listing management protects properties and lifecycle actions
**Reason:** The secondary Listing properties dialog is retired. Optional properties and tag editing live in the details pane with automatic save; archive, restore, and permanent deletion live in the details pane per "Listing inspector hosts listing lifecycle actions"; archived review stays in the tree's archived view.

FusionCanvas SHALL provide a secondary focused surface for optional listing properties, listing tag editing, archived review, restore, and permanent deletion while keeping inline creation, rename, move, and duplication in the tree.

#### Scenario: User opens listing properties
- **WHEN** the user invokes Edit Properties for a selected listing
- **THEN** FusionCanvas opens the focused surface with that listing preselected
- **AND** keeps canonical tree and document context intact
- **AND** progressively discloses the listing tag editor, archived review, restore, and permanent deletion

#### Scenario: User edits listing tags on the properties surface
- **WHEN** the user applies or removes a tag through the listing tag editor on the focused surface
- **THEN** FusionCanvas persists the tag change immediately and atomically through the tag management service
- **AND** the tag change does not require or await the description/notes Save action
- **AND** the description/notes draft and its explicit Save remain independent

#### Scenario: User leaves meaningful unsaved properties
- **WHEN** the focused surface contains meaningful unsaved description or notes changes and the user switches listing or closes
- **THEN** FusionCanvas asks whether to save, discard, or cancel
- **AND** retains the current draft and focus when cancellation is chosen

#### Scenario: Selected listing leaves active navigation
- **WHEN** the selected listing is archived or permanently deleted
- **THEN** FusionCanvas selects a sensible nearby active listing when one exists
- **AND** otherwise selects the active parent topic
- **AND** returns keyboard focus to the replacement tree row

#### Scenario: Lifecycle operation is in progress or fails
- **WHEN** archive, restore, or deletion is running or fails validation or persistence
- **THEN** FusionCanvas prevents duplicate submission
- **AND** keeps the surface available to report success or an actionable error
- **AND** preserves selection and recoverable input after failure

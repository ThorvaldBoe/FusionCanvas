## MODIFIED Requirements

### Requirement: Secondary listing management protects properties and lifecycle actions
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

## MODIFIED Requirements

### Requirement: Variant drafts and lifecycle actions preserve confirmed setup
FusionCanvas SHALL keep Option Value, individual Variant, and bulk Variant creation scoped to the current Offering, SHALL allow only the invoked creation dialog to be open at a time, SHALL guard meaningful drafts, and SHALL apply existing archive, dependency, and integrity policies to sellable Variants. When a Variant archive is blocked, the UI SHALL identify the exact active dependent Placeholders or other records and SHALL explain whether the user must archive, edit, reassign, or remove each dependent before retrying. Option Value management and individual and bulk Variant creation SHALL occur in focused modal dialogs that close when the Blueprint Offering or workspace context changes so they cannot edit stale data.

#### Scenario: User cancels a Variant draft
- **WHEN** the user starts an individual or bulk Variant creation dialog and cancels before confirmation
- **THEN** FusionCanvas persists no new Variant
- **AND** closes the dialog and returns focus to the action that opened it

#### Scenario: User closes Option Value management
- **WHEN** the user cancels or completes Option Value management
- **THEN** the focused dialog closes without leaving an inline editor in the parent surface
- **AND** confirmed values remain unchanged on cancellation

#### Scenario: User leaves with unsaved Variant changes
- **WHEN** the user attempts to leave Variant management with meaningful unconfirmed changes
- **THEN** FusionCanvas offers to discard the changes or keep editing
- **AND** keep-editing preserves current selections and keyboard focus

#### Scenario: User retires a referenced Variant
- **WHEN** the user requests retirement or removal of a Variant referenced by a Design Area, Item, or other dependent record
- **THEN** FusionCanvas applies the authoritative dependency and archival safeguards
- **AND** reports the blocking records by type and name with the required resolution path rather than silently breaking relationships

#### Scenario: Provider catalog is unavailable
- **WHEN** provider-catalog choices cannot be loaded and no locally persisted choices are available
- **THEN** FusionCanvas shows a recoverable unavailable state in the Available choices region
- **AND** leaves confirmed Variants visible and unchanged

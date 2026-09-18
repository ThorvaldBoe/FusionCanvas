## ADDED Requirements

### Requirement: Blueprint overview is archive-first and active by default
The Store Editor SHALL show only active Blueprints in the Blueprint overview by default. The overview SHALL provide an explicit, opt-in **Show archived Blueprints** checkbox that includes archived Blueprints in the same Store-scoped list when checked. Archived rows SHALL be visually and textually distinguishable from active rows.

#### Scenario: User opens the Blueprint overview
- **WHEN** the user opens the catalog overview for an active Store
- **THEN** only active Blueprints are listed
- **AND** the **Show archived Blueprints** checkbox is unchecked by default
- **AND** no permanent-delete action is exposed for an active Blueprint

#### Scenario: User opts into archived Blueprints
- **WHEN** the user checks **Show archived Blueprints**
- **THEN** archived Blueprints from the selected Store appear in the list
- **AND** active Blueprints remain visible
- **AND** archived rows identify themselves as archived

#### Scenario: User hides archived Blueprints again
- **WHEN** the user unchecks **Show archived Blueprints**
- **THEN** archived Blueprints leave the overview
- **AND** the current active Blueprint selection remains valid when one exists

### Requirement: Blueprint lifecycle actions follow the record state
The Store Editor SHALL expose **Archive Blueprint** as the only destructive lifecycle action for an active Blueprint. A permanently destructive **Delete permanently** action SHALL be available only after the user has selected an archived Blueprint through the archived-visibility control, and SHALL require explicit confirmation.

#### Scenario: User reviews an active Blueprint
- **WHEN** an active Blueprint is selected
- **THEN** the Blueprint detail presents Archive Blueprint
- **AND** it does not present Delete permanently

#### Scenario: User reviews an archived Blueprint
- **WHEN** an archived Blueprint is selected while archived Blueprints are shown
- **THEN** the detail identifies the Blueprint as archived
- **AND** presents Delete permanently as an archived-only action
- **AND** does not present Archive Blueprint as an available action

#### Scenario: User cancels permanent deletion
- **WHEN** the user opens the archived Blueprint's permanent-delete confirmation and cancels
- **THEN** the Blueprint and every related record remain unchanged


# Duplicate Mockup Template

## Purpose

Defines how creators duplicate an active Mockup Template into an independently editable same-offering draft.

## Requirements

### Requirement: Creators can duplicate an active Mockup Template
FusionCanvas SHALL provide a duplicate action for an active Mockup Template. Duplication SHALL create a new editable draft in the same Store and Blueprint Offering, preserving the source template's current name-derived configuration, description, position, target Design Area, provider-image reference and mapping, active Color applicability, and active local source-image entries with their applicability and mappings. The duplicate SHALL receive new identities for the template, revision, Color bindings, source-image entries, and revision snapshot records. Managed source Asset identities MAY be shared because Assets are immutable; replacing an image in the duplicate SHALL not alter the original template or its Asset.

#### Scenario: Creator duplicates a configured template
- **WHEN** the creator invokes Duplicate for an active template in an editable Store
- **THEN** FusionCanvas creates one new draft in the same Blueprint Offering
- **AND** the new draft retains the current target Design Area, provider-image configuration, active Colors, active source-image applicability, mappings, and source Asset references
- **AND** the original template and all of its revisions and source entries remain unchanged

#### Scenario: Duplicate records are independently mutable
- **WHEN** the creator replaces, archives, or reconfigures a source image on the duplicate and saves it
- **THEN** the duplicate receives its own updated source entry and revision history
- **AND** the original template's source entries, revisions, readiness, and Asset references remain unchanged

#### Scenario: Duplicate opens as an editable draft
- **WHEN** duplication succeeds
- **THEN** the focused Mockup Template editor opens for the new draft
- **AND** its name is initialized to a collision-safe “Copy of <source name>” value
- **AND** the name and copied metadata remain editable before the creator explicitly saves

### Requirement: Duplication respects Store and template availability rules
FusionCanvas SHALL reject duplication when the source template is missing, archived, belongs to another Store, or belongs to an archived Store or archived Blueprint Offering. A rejected duplication SHALL leave the workspace unchanged and provide a recoverable error to the caller.

#### Scenario: Creator duplicates from an archived Store
- **WHEN** the creator invokes Duplicate for a template belonging to an archived Store
- **THEN** FusionCanvas disables or rejects the action
- **AND** no duplicate records are persisted
- **AND** the UI explains that archived Store catalogs are read-only

#### Scenario: Source template cannot be found
- **WHEN** a duplicate request references a template that is absent or outside the selected Store
- **THEN** FusionCanvas returns a failure result
- **AND** no template, revision, source-image, binding, or asset records are created

### Requirement: Duplicate names remain distinguishable
FusionCanvas SHALL initialize a duplicate's name with “Copy of <source name>” and SHALL append a numeric suffix when that name already exists among active templates in the same Blueprint Offering. Name generation SHALL be deterministic for the current workspace and SHALL not rename the source template.

#### Scenario: Copy name collides
- **WHEN** an active template already uses “Copy of Front” and the creator duplicates “Front” again
- **THEN** FusionCanvas initializes the new draft with the next available distinguishable name such as “Copy of Front (2)”
- **AND** the prior templates retain their names

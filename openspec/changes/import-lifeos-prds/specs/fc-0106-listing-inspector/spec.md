## ADDED Requirements

### Requirement: User Can View Core Listing Details
FusionCanvas SHALL allow users to view core listing details.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** the user can view core listing details

### Requirement: User Can Edit Core Listing Details
FusionCanvas SHALL allow users to edit core listing details.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** the user can edit core listing details

### Requirement: User Can View And Change Lifecycle Status
FusionCanvas SHALL allow users to view and change lifecycle status.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** the user can view and change lifecycle status

### Requirement: User Can See The Current Workflow Stage
FusionCanvas SHALL allow users to see the current workflow stage.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** the user can see the current workflow stage

### Requirement: Inspector Can Show Stage-relevant Fields For Idea, Concept, Design, Or Listing
FusionCanvas SHALL ensure that the inspector can show stage-relevant fields for Idea, Concept, Design, or Listing.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The inspector can show stage-relevant fields for Idea, Concept, Design, or Listing

### Requirement: User Can View And Manage Listing Tags
FusionCanvas SHALL allow users to view and manage listing tags.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** the user can view and manage listing tags

### Requirement: User Can View Related Assets
FusionCanvas SHALL allow users to view related assets.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** the user can view related assets

### Requirement: User Can Access Notes For The Listing
FusionCanvas SHALL allow users to access notes for the listing.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** the user can access notes for the listing

### Requirement: Inspector Should Remain Useful While Browsing The Workspace
FusionCanvas SHALL ensure that the inspector should remain useful while browsing the workspace.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The inspector should remain useful while browsing the workspace

### Requirement: Inspector Should Help The Creator Understand The Listing As A Product Concept
FusionCanvas SHALL ensure that the inspector should help the creator understand the listing as a product concept.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** The inspector should help the creator understand the listing as a product concept

### Requirement: Acceptance Criteria Remain Satisfied
FusionCanvas SHALL satisfy the acceptance criteria captured in the source PRD.

#### Scenario: User Can Select A Listing And Quickly Understand What It Is
- **WHEN** the corresponding capability is delivered
- **THEN** A user can select a listing and quickly understand what it is.

#### Scenario: User Can See Which Workflow Stage The Active Item Is In
- **WHEN** the corresponding capability is delivered
- **THEN** A user can see which workflow stage the active item is in.

#### Scenario: User Can Update Title, Idea, Phrase, Graphic Direction, Notes, Status, And Tags
- **WHEN** the corresponding capability is delivered
- **THEN** A user can update title, idea, phrase, graphic direction, notes, status, and tags.

#### Scenario: User Can See Related Assets From The Listing Context
- **WHEN** the corresponding capability is delivered
- **THEN** A user can see related assets from the listing context.

#### Scenario: User Can Move Through Listings Without Losing Orientation
- **WHEN** the corresponding capability is delivered
- **THEN** A user can move through listings without losing orientation.

#### Scenario: Inspector Supports Creative Organization Without Requiring Publishing Details
- **WHEN** the corresponding capability is delivered
- **THEN** The inspector supports creative organization without requiring publishing details.

## Source PRD

# FC-0106 - Listing Inspector

## Summary

The Listing Inspector gives creators one focused place to view and edit the most important information about a listing while staying in the workspace context.

The inspector should make a listing understandable as a product concept: what the idea is, what phrase or hook it uses, what graphic direction supports it, what status it has, what notes matter, what tags apply, and what assets are attached.

The inspector appears inside the document window detail area for the active item and should align with the workflow stage navigator.

## User Need

As a Print on Demand creator, I need to select a listing and quickly understand or update its core creative context without opening scattered files or switching tools.

## Goals

- Provide a focused view of listing details.
- Support fast editing of important creative fields.
- Keep status, tags, notes, and assets visible or accessible.
- Help users compare and move through listings efficiently.
- Avoid turning Phase 1 into a full marketplace listing editor.

## Requirements

- The user can view core listing details.
- The user can edit core listing details.
- The user can view and change lifecycle status.
- The user can see the current workflow stage.
- The inspector can show stage-relevant fields for Idea, Concept, Design, or Listing.
- The user can view and manage listing tags.
- The user can view related assets.
- The user can access notes for the listing.
- The inspector should remain useful while browsing the workspace.
- The inspector should help the creator understand the listing as a product concept.

## Core Listing Details

- Title or working title
- Lifecycle status
- Workflow stage
- Idea
- Phrase
- Graphic direction
- Notes
- Tags
- Related assets

## User Workflows

### Inspect a Listing

The user selects a listing and sees enough information to understand what the listing is and what state it is in.

The inspector should answer: what is this, why does it exist, and what needs to happen next?

### Inspect Current Stage

The user sees fields relevant to the current workflow stage while retaining access to completed stages through the workflow stage navigator.

### Edit Creative Context

The user updates the idea, phrase, graphic direction, notes, tags, or status as the concept develops.

Editing should feel lightweight and should not require completing marketplace-ready metadata.

### Review Attached Assets

The user checks which files or references are connected to the listing.

The inspector should make it clear whether a listing has supporting assets yet.

## Acceptance Criteria

- A user can select a listing and quickly understand what it is.
- A user can see which workflow stage the active item is in.
- A user can update title, idea, phrase, graphic direction, notes, status, and tags.
- A user can see related assets from the listing context.
- A user can move through listings without losing orientation.
- The inspector supports creative organization without requiring publishing details.

## Out of Scope

- Full marketplace metadata editor
- AI critique
- Design triangle scoring
- Rich text document editing
- Version comparison
- Prompt history editing
- Performance data

## Open Questions

- Should the inspector support autosave or explicit save behavior?
- Should notes be plain text only in Phase 1?
- Should related assets be editable directly from the inspector or only visible there?

## Related Notes

- [[Phase 1 - MVP Creative Workspace]]
- [[Roadmap]]
- [[Product]]
- [[Data Model]]
- [[FC-0008 - Workflow Stage Navigator]]
- [[FC-0009 - Tabbed Document Window]]

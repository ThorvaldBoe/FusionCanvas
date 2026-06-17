## ADDED Requirements

### Requirement: User Can Import An Existing File As An Asset
FusionCanvas SHALL allow users to import an existing file as an asset.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** the user can import an existing file as an asset

### Requirement: Importing A File Copies It Into The Managed FusionCanvas Workspace
FusionCanvas SHALL ensure that importing a file copies it into the managed FusionCanvas workspace.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Importing a file copies it into the managed FusionCanvas workspace

### Requirement: User Can Associate An Asset With A Listing
FusionCanvas SHALL allow users to associate an asset with a listing.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** the user can associate an asset with a listing

### Requirement: User Can Associate An Asset With A Broader Context Where Useful
FusionCanvas SHALL allow users to associate an asset with a broader context where useful.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** the user can associate an asset with a broader context where useful

### Requirement: User Can See Which Assets Belong To A Listing
FusionCanvas SHALL allow users to see which assets belong to a listing.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** the user can see which assets belong to a listing

### Requirement: User Can Identify Basic Asset Purpose
FusionCanvas SHALL allow users to identify basic asset purpose.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** the user can identify basic asset purpose

### Requirement: Asset Relationships Should Remain Intact When Listings Or Groups Move
FusionCanvas SHALL ensure that asset relationships should remain intact when listings or groups move.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Asset relationships should remain intact when listings or groups move

### Requirement: Existing Assets Should Be Useful Even When The Original File Was Created Outside FusionCanvas
FusionCanvas SHALL ensure that existing assets should be useful even when the original file was created outside FusionCanvas.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** Existing assets should be useful even when the original file was created outside FusionCanvas

### Requirement: FusionCanvas Should Use The Managed Workspace Copy After Import, Not The Original Source Path
FusionCanvas SHALL use the managed workspace copy after import, not the original source path.

#### Scenario: Requirement is met
- **WHEN** the relevant workspace behavior is used
- **THEN** FusionCanvas should use the managed workspace copy after import, not the original source path

### Requirement: Acceptance Criteria Remain Satisfied
FusionCanvas SHALL satisfy the acceptance criteria captured in the source PRD.

#### Scenario: User Can Import An Existing Design File To A Listing
- **WHEN** the corresponding capability is delivered
- **THEN** A user can import an existing design file to a listing.

#### Scenario: User Can Import Reference Material Or Exported Artwork Without Losing Where It Belongs
- **WHEN** the corresponding capability is delivered
- **THEN** A user can import reference material or exported artwork without losing where it belongs.

#### Scenario: User Can Open A Listing Later And See The Files That Support It
- **WHEN** the corresponding capability is delivered
- **THEN** A user can open a listing later and see the files that support it.

#### Scenario: Imported Files Are Copied Into The Managed FusionCanvas Workspace
- **WHEN** the corresponding capability is delivered
- **THEN** Imported files are copied into the managed FusionCanvas workspace.

#### Scenario: Moving A Listing Does Not Break Its Asset Relationships
- **WHEN** the corresponding capability is delivered
- **THEN** Moving a listing does not break its asset relationships.

#### Scenario: Assets Can Be Labeled By Basic Purpose
- **WHEN** the corresponding capability is delivered
- **THEN** Assets can be labeled by basic purpose.

## Source PRD

# FC-0109 - Import Existing Assets

## Summary

Import Existing Assets lets creators copy files into the FusionCanvas workspace and attach them to the work they support.

Phase 1 is focused on preserving context, not processing files. The user should be able to import source files, exports, mockups, references, textures, and fonts into the managed workspace, then connect those assets to listings or broader workspace contexts so they are not lost in disconnected folders.

## User Need

As a Print on Demand creator, I need to import existing files into FusionCanvas and attach them to listings or related contexts so I can find the assets that support a product concept later.

## Goals

- Let users attach existing assets to listings.
- Allow assets to support broader contexts where useful.
- Make related assets visible from the relevant work.
- Preserve asset relationships when work moves.
- Avoid advanced asset automation in Phase 1.

## Requirements

- The user can import an existing file as an asset.
- Importing a file copies it into the managed FusionCanvas workspace.
- The user can associate an asset with a listing.
- The user can associate an asset with a broader context where useful.
- The user can see which assets belong to a listing.
- The user can identify basic asset purpose.
- Asset relationships should remain intact when listings or groups move.
- Existing assets should be useful even when the original file was created outside FusionCanvas.
- FusionCanvas should use the managed workspace copy after import, not the original source path.

## Asset Purposes

Phase 1 should support basic asset purpose labels such as:

- source file
- export
- mockup
- reference
- texture
- brush
- font

## User Workflows

### Attach a Design File

The user imports an existing design source file or exported image to a listing.

FusionCanvas copies the file into the managed workspace. The listing should then show that supporting asset as part of its context.

### Attach Reference Material

The user imports a reference image, texture, font, or brush to a listing, niche, group, or store where it provides useful context.

### Review Listing Assets

The user opens a listing and sees the managed workspace files associated with it.

This should help the user understand what already exists and what still needs to be created.

## Acceptance Criteria

- A user can import an existing design file to a listing.
- A user can import reference material or exported artwork without losing where it belongs.
- A user can open a listing later and see the files that support it.
- Imported files are copied into the managed FusionCanvas workspace.
- Moving a listing does not break its asset relationships.
- Assets can be labeled by basic purpose.

## Out of Scope

- Batch asset import
- Automatic file matching
- Image processing
- Mockup generation
- Cloud storage sync
- Asset deduplication
- Versioned asset history
- Asset validation

## Open Questions

- Should assets attach only to listings in Phase 1, or also to stores, niches, and groups?
- Should missing files be detected in Phase 1?

## Related Notes

- [[Phase 1 - MVP Creative Workspace]]
- [[Roadmap]]
- [[Product]]
- [[Data Model]]

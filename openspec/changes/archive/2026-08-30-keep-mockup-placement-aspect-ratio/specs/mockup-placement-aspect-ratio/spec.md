## ADDED Requirements

### Requirement: Mockup placement can preserve the selected Design Area aspect ratio
The placement editor SHALL derive an applicable aspect ratio from the selected Design Area's positive width and height. It SHALL expose an accessible **Keep aspect ratio** option enabled by default when that ratio is valid. While enabled, resizing or numeric width/height editing SHALL preserve the ratio within whole-pixel rounding tolerance; while disabled, width and height SHALL be independently editable.

#### Scenario: Valid Design Area enables ratio preservation by default
- **WHEN** a Mockup Template has a selected Design Area with positive width and height
- **THEN** the placement editor exposes **Keep aspect ratio** as checked and enabled
- **AND** the applicable ratio is the Design Area width divided by height

#### Scenario: Pointer resize preserves the ratio
- **WHEN** the creator resizes the placement rectangle while **Keep aspect ratio** is checked
- **THEN** the resulting positive width and height preserve the Design Area ratio within whole-pixel rounding tolerance
- **AND** the rectangle remains inside the selected image bounds

#### Scenario: Numeric edits preserve the ratio
- **WHEN** the creator edits either numeric placement width or height while **Keep aspect ratio** is checked
- **THEN** the paired dimension updates to preserve the applicable ratio
- **AND** the displayed numeric fields remain synchronized with the placement rectangle

#### Scenario: Creator opts out of ratio preservation
- **WHEN** the creator unchecks **Keep aspect ratio** and edits or resizes one dimension
- **THEN** the changed dimension is retained independently
- **AND** the other dimension is not changed solely to preserve the Design Area ratio

### Requirement: Placement ratio behavior responds safely to Design Area context
The placement editor SHALL recompute the applicable ratio when the selected Design Area changes. If the selected Design Area is absent, unavailable, or does not have positive dimensions, the editor SHALL disable ratio enforcement and SHALL continue allowing safe independent in-bounds placement editing without throwing.

#### Scenario: Selected Design Area changes
- **WHEN** the creator selects a different valid Design Area
- **THEN** the applicable ratio and default checked behavior update to the newly selected Design Area
- **AND** subsequent ratio-preserving edits use the new ratio

#### Scenario: Ratio is invalid or unavailable
- **WHEN** no valid Design Area ratio is available
- **THEN** **Keep aspect ratio** is unchecked and unavailable
- **AND** placement movement, resizing, and numeric edits remain safe and independently editable

### Requirement: Ratio-aware placement retains existing template persistence semantics
The placement editor SHALL use the existing mapping draft and save/reopen workflow. Saving and reopening a template SHALL retain the saved placement coordinates, and a valid selected Design Area SHALL restore the ratio-preserving option as checked by default without requiring migration of existing mappings.

#### Scenario: Saved placement reopens with applicable ratio behavior
- **WHEN** the creator saves a template with a valid Design Area and later reopens it
- **THEN** the saved placement coordinates are restored
- **AND** **Keep aspect ratio** is checked and enabled for the valid Design Area

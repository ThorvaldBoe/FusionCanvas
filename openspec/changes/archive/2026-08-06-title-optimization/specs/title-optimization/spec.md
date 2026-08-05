## ADDED Requirements

### Requirement: The Optimize action appears beside the Working title in the listing inspector Overview
FusionCanvas SHALL present an `Optimize` command immediately next to the **Working title** field in the listing inspector Overview surface, visible whenever the Overview is visible, and SHALL size the command to its action row and reach it in a predictable keyboard order after the Working title field.

#### Scenario: Optimize is present in the Overview
- **WHEN** the listing inspector Overview surface is visible
- **THEN** an `Optimize` command is shown next to the Working title field with an accessible name

#### Scenario: Optimize is keyboard reachable
- **WHEN** the user tabs through the Overview without a pointer while Optimize is present
- **THEN** the Working title field is focused before the Optimize command in document order

### Requirement: Optimize availability is derived from a Title AI purpose, item content, and editability
FusionCanvas SHALL enable `Optimize` only when all of the following hold: the Title-purpose AI availability is ready, the active item has non-whitespace creative content to draw from (Idea, or at least one of Concept idea, Phrase, or Graphic direction), and the item's Working title is editable (the item is not archived and not effectively inactive through archived ancestry). The same canonical creative-content predicate SHALL be used by the availability gate and by the optimization orchestrator. When the Title-purpose AI is unavailable, `Optimize` SHALL remain visible but disabled with actionable guidance (for example a tooltip directing the user to AI settings). When AI is available but the item has no creative content, `Optimize` SHALL be disabled with guidance that creative content is required. When the item is read-only because it is archived or effectively inactive, `Optimize` SHALL be disabled with guidance pointing to restore. Availability SHALL be re-evaluated after AI settings change while the item document is open.

#### Scenario: AI is ready, item has content, and item is editable
- **WHEN** the Title-purpose AI availability is ready, the active item has creative content, and the item is editable
- **THEN** `Optimize` is enabled

#### Scenario: AI is not configured
- **WHEN** the Title-purpose AI availability reports a missing credential, missing model, or invalid configuration
- **THEN** `Optimize` remains visible but disabled
- **AND** its guidance identifies the missing prerequisite and directs the user to AI settings

#### Scenario: No creative content to draw from
- **WHEN** the Title-purpose AI is available and the item is editable but the active item has no non-whitespace Idea, Concept idea, Phrase, or Graphic direction
- **THEN** `Optimize` is disabled
- **AND** its guidance explains that creative content is required

#### Scenario: Archived or inactive item is read-only
- **WHEN** the active item is archived or effectively inactive through archived ancestry
- **THEN** `Optimize` is disabled
- **AND** its guidance directs the user to restore the item rather than optimizing its title

#### Scenario: Availability refreshes after settings change
- **WHEN** the user saves AI settings while an item document with the Overview is open
- **THEN** `Optimize` availability is re-evaluated without reopening the document

### Requirement: Optimize generates a short title from the item's creative content
FusionCanvas SHALL, when the user activates `Optimize`, ask AI with a `Title` request purpose to produce a short title from the item's creative content — the Idea, and, where present, Concept idea, Phrase, and Graphic direction — together with the resolved creative context (store, niche, group, inherited tags and metadata). The initial generation SHALL prefer a concise title and SHALL exclude operational and secret fields from the request payload. This module SHALL treat "short" as a prompt-level instruction only and SHALL NOT enforce a hard maximum title length; any length policy is deferred to a later module.

#### Scenario: Short title is generated from creative content
- **WHEN** the user activates `Optimize` on an item with creative content
- **THEN** an AI `Title` request is assembled from the item's content and creative context
- **AND** a short candidate title is produced before the uniqueness loop

#### Scenario: Operational and secret data is excluded
- **WHEN** source entities contain credentials, database identifiers, timestamps, file paths, or internal provenance
- **THEN** those values are absent from the Title request payload, logs, and errors

### Requirement: The uniqueness loop makes the title unique across the store
FusionCanvas SHALL, after each AI candidate title, check for a collision against other items' titles in the same store — comparing case-insensitively against title values of active items other than the active item being optimized, where "active" excludes archived items and items whose lifecycle status is `Rejected`. If the candidate collides, FusionCanvas SHALL ask AI to add one relevant distinguishing word and check again, repeating until the candidate is unique or the bounded retry limit is reached.

#### Scenario: First candidate is already unique
- **WHEN** the first generated candidate title has no collision against other store item titles
- **THEN** FusionCanvas accepts that candidate without further AI calls

#### Scenario: Collision prompts a distinguishing word
- **WHEN** a candidate title collides with another active store item's title
- **THEN** FusionCanvas asks AI to add one relevant word that distinguishes the item
- **AND** checks the revised candidate for uniqueness again

#### Scenario: Unique candidate ends the loop
- **WHEN** a revised candidate after a collision no longer collides with any other store item title
- **THEN** FusionCanvas accepts that revised candidate and stops the loop

#### Scenario: The active item's own title is not a collision
- **WHEN** the active item currently holds the same title as a candidate
- **THEN** FusionCanvas does not treat the active item's own current title as a collision

#### Scenario: Archived items do not cause collisions
- **WHEN** only archived items in the store hold the same title as a candidate
- **THEN** FusionCanvas does not treat those archived item titles as collisions

#### Scenario: Rejected items do not cause collisions
- **WHEN** only items whose lifecycle status is `Rejected` hold the same title as a candidate
- **THEN** FusionCanvas does not treat those rejected item titles as collisions

### Requirement: Identical data falls back to a numeric suffix
FusionCanvas SHALL bound the uniqueness loop to a configured maximum number of AI attempts, SHALL prefer disambiguating words over numbers while attempts remain, and SHALL, when the loop reaches the bound regardless of why the collision persists, append the smallest numeric suffix that makes the title unique, using the next integer (for example `2`, then `3`). The case of two items with genuinely identical data is the canonical motivating case for this fallback, not a precondition for it. The numeric-suffix fallback SHALL also produce a title that remains unique against the store.

#### Scenario: Loop reaches its bound with identical data
- **WHEN** two items have identical creative data and the loop reaches the maximum AI attempts while a collision remains
- **THEN** FusionCanvas appends the smallest unused numeric suffix to the last candidate so the title is unique
- **AND** the final title is unique against all other active store item titles

#### Scenario: Loop reaches its bound for non-identical data
- **WHEN** the AI keeps emitting colliding titles for non-identical items and the loop reaches the maximum AI attempts
- **THEN** FusionCanvas still appends the smallest unused numeric suffix to the last candidate so the title is unique
- **AND** no identity check is performed before applying the suffix

#### Scenario: Disambiguation is preferred over numbers while attempts remain
- **WHEN** a collision is resolved by an AI-added distinguishing word within the attempt bound
- **THEN** FusionCanvas accepts the word-disambiguated title without adding a number

### Requirement: A successful title immediately overwrites and persists
FusionCanvas SHALL, when the uniqueness loop yields an accepted title, replace the Working title field with the normalized single-line title and SHALL commit it through the automatic-save path with the stage-aware expected-state guard, without an explicit save action.

#### Scenario: Accepted title overwrites and persists
- **WHEN** the uniqueness loop yields an accepted title
- **THEN** the Working title field shows the accepted title
- **AND** the title is persisted through the automatic-save path
- **AND** the persisted item reloads with the accepted title

#### Scenario: Multi-line result is normalized to one line
- **WHEN** an accepted title contains line breaks
- **THEN** the title is normalized to a single line before it overwrites and persists

### Requirement: One Optimize operation runs at a time with cancellation
FusionCanvas SHALL allow at most one in-flight `Optimize` operation per item document, SHALL disable the `Optimize` command and make the Working title field non-editable while an operation runs, SHALL cancel the in-flight operation when the item document closes, the active item changes, or the user activates a competing transition, and SHALL never apply a late result to a different item or after cancellation. Because the Working title field is non-editable during the operation, no automatic-save is triggered for user edits in that window. The number of AI attempts during one operation SHALL be bounded.

#### Scenario: Optimize is disabled while running
- **WHEN** an `Optimize` operation is in flight
- **THEN** the `Optimize` command is disabled until it completes, fails, or is cancelled

#### Scenario: Working title field is locked while running
- **WHEN** an `Optimize` operation is in flight
- **THEN** the Working title field is non-editable
- **AND** no automatic-save is triggered for edits during that window

#### Scenario: Item switch cancels the in-flight operation
- **WHEN** an `Optimize` operation is in flight and the user switches to another item or closes the document
- **THEN** the operation is cancelled
- **AND** its late result is never applied

#### Scenario: A single operation performs a bounded number of AI calls
- **WHEN** one `Optimize` operation runs and collides repeatedly
- **THEN** the total AI calls for that operation do not exceed the configured maximum

### Requirement: Optimize failures leave the title unchanged and report inline
FusionCanvas SHALL, when an `Optimize` operation fails or is cancelled before any accepted title is applied, leave the Working title and its persisted value unchanged and SHALL surface a recoverable inline error near the action. A failure of the automatic-save commit after an accepted title has replaced the field SHALL follow the applied-value commit behavior, keeping the user's draft and reporting a recoverable error without a partial write.

#### Scenario: Operation fails before acceptance
- **WHEN** an `Optimize` operation fails or is cancelled before an accepted title is produced
- **THEN** the Working title and its persisted value remain unchanged
- **AND** a recoverable inline error is reported near the action

#### Scenario: Persistence fails after the field is replaced
- **WHEN** the field has been replaced with an accepted title but the automatic-save commit fails
- **THEN** no partial title change is persisted
- **AND** the user's draft remains with a recoverable inline error

### Requirement: Optimize uses shared interaction and theme guidance
FusionCanvas SHALL make the `Optimize` command operate by keyboard, give it a meaningful accessible name, and resolve its busy, disabled, and error states from shared application theme resources so they remain distinguishable when the application appearance changes.

#### Scenario: Keyboard operation and accessible name
- **WHEN** the user navigates the Overview without a pointer
- **THEN** `Optimize` is reachable in a predictable order and is operable by keyboard
- **AND** its accessible name communicates the `Optimize` action

#### Scenario: Busy, disabled, and error states are distinguishable
- **WHEN** the application appearance changes while the Overview is visible
- **THEN** the `Optimize` busy, disabled, and error states adopt the active theme and remain distinguishable

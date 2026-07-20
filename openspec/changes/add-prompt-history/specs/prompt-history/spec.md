## ADDED Requirements

### Requirement: Users can save prompt inputs and outputs
FusionCanvas SHALL allow a user to save a Prompt record with prompt input text, preserved output text or an output reference, a prompt type, optional provider and model strings, optional generation settings metadata, and timestamps, and SHALL NOT store API keys, tokens, or credentials on prompt records.

#### Scenario: User saves a prompt with output
- **WHEN** the user saves a prompt input and its preserved output
- **THEN** FusionCanvas creates a Prompt record with stable identity and timestamps
- **AND** stores the input, output, type, and optional provider/model metadata
- **AND** does not store any API key, token, or credential

#### Scenario: User saves a prompt whose output is a managed asset
- **WHEN** the user saves a prompt whose output is a generated image already imported as an asset
- **THEN** FusionCanvas stores the output as a reference to the asset
- **AND** links the prompt to that asset through a prompt context link
- **AND** does not duplicate the asset content inside the prompt record

#### Scenario: User omits optional metadata
- **WHEN** the user saves a prompt without provider, model, or generation settings
- **THEN** FusionCanvas creates the prompt record with the omitted fields absent
- **AND** omitted fields do not block saving

### Requirement: Prompts associate with stores, niches, listings, concepts, designs, or assets
FusionCanvas SHALL associate a Prompt record with one or more active contexts — store, niche, listing, concept, design, or asset — validated against the active workspace and the prompt's store, and SHALL allow additional associations after creation.

#### Scenario: User associates a prompt with a concept
- **WHEN** the user links a prompt to an active concept in the prompt's store
- **THEN** FusionCanvas creates the prompt context link atomically
- **AND** the prompt appears in the concept's prompt history view

#### Scenario: User associates a prompt with multiple contexts
- **WHEN** the user links a prompt to a second active context in the same store
- **THEN** FusionCanvas creates the additional association atomically
- **AND** preserves the original association and prompt identity

#### Scenario: User attempts a cross-store association
- **WHEN** the user links a prompt to a context outside its store
- **THEN** FusionCanvas rejects the association
- **AND** explains that prompts may only associate within their store

### Requirement: Prompt type is identifiable
FusionCanvas SHALL record a prompt type for each Prompt record from the set idea, phrase, graphic, image, listing-text, critique, and other, and SHALL allow the user to change the type after creation.

#### Scenario: User selects a prompt type
- **WHEN** the user saves or edits a prompt
- **THEN** FusionCanvas records the selected type
- **AND** the type is visible in prompt history views

#### Scenario: User changes a prompt type
- **WHEN** the user changes the type of an existing prompt
- **THEN** FusionCanvas persists the new type atomically
- **AND** preserves the prompt's input, output, associations, and metadata

### Requirement: Prompt history preserves rejected and superseded outputs
FusionCanvas SHALL preserve rejected and superseded Prompt records without deletion, SHALL mark them with a lifecycle marker, and SHALL exclude them from the default active prompt history view while exposing them through an explicit filter.

#### Scenario: User rejects a prompt output
- **WHEN** the user rejects a prompt output
- **THEN** FusionCanvas marks the prompt rejected
- **AND** preserves the record, its associations, and its metadata
- **AND** excludes it from the default active prompt history view

#### Scenario: User supersedes a prompt
- **WHEN** the user saves a new prompt that supersedes an existing one
- **THEN** FusionCanvas marks the prior prompt superseded
- **AND** preserves it for later review

#### Scenario: User reviews rejected and superseded prompts
- **WHEN** the user enables the rejected/superseded filter in a prompt history view
- **THEN** FusionCanvas shows the rejected and superseded prompts with their lifecycle markers
- **AND** keeps them read-only with reuse and copy actions available

### Requirement: Prompt history is visible per context and at store level
FusionCanvas SHALL provide a focused prompt surface for a selected store, niche, listing, concept, design, or asset that lists the prompts associated with that context, and SHALL list every store-owned prompt in the store-level view.

#### Scenario: User reviews concept prompts
- **WHEN** the user opens the prompt surface for a concept
- **THEN** FusionCanvas shows the prompts associated with that concept with type, provider, model, and lifecycle marker

#### Scenario: User reviews design prompts
- **WHEN** the user opens the prompt surface for a design
- **THEN** FusionCanvas shows the prompts associated with that design

#### Scenario: Store view shows all store prompts
- **WHEN** the user opens the prompt surface for a store
- **THEN** FusionCanvas lists every prompt owned by that store
- **AND** labels each prompt with its associated contexts or an unlinked indicator

#### Scenario: Context has no prompts
- **WHEN** the user opens the prompt surface for a context with no associated prompts
- **THEN** FusionCanvas shows an empty state that explains prompts can be saved
- **AND** keeps the save action available

### Requirement: Prompt records support reuse and review
FusionCanvas SHALL allow a user to copy a saved prompt's input for reuse and SHALL create a new Prompt record with a `reusedFrom` reference to the source prompt when the user reuses it, preserving the source prompt unchanged.

#### Scenario: User copies a prompt input
- **WHEN** the user copies a saved prompt's input
- **THEN** FusionCanvas places the input text on the clipboard
- **AND** leaves the source prompt unchanged

#### Scenario: User reuses a prompt
- **WHEN** the user reuses a saved prompt as the starting point for a new prompt
- **THEN** FusionCanvas creates a new Prompt record with a `reusedFrom` reference to the source
- **AND** preserves the source prompt's record, associations, and lifecycle

### Requirement: Prompt operations persist atomically
FusionCanvas SHALL persist prompt save, association, type change, lifecycle marker, and reuse operations as atomic snapshot operations in the active workspace database, reloading the latest snapshot before each mutation and saving once per operation.

#### Scenario: Workspace reloads after prompt operations
- **WHEN** a successful prompt operation is followed by an application or workspace database reload
- **THEN** the resulting prompts, associations, types, lifecycle markers, and metadata match the last successful persisted state

#### Scenario: Persistence fails during a prompt operation
- **WHEN** the repository cannot save a prompt operation after an optimistic projection
- **THEN** FusionCanvas reports a recoverable error
- **AND** restores the last confirmed prompt and association state
- **AND** retains recoverable user input needed to retry when applicable

### Requirement: Prompts are dependent records for creative-record deletion
FusionCanvas SHALL treat Prompt records as dependent creative records for the listing, concept, and design permanent-deletion guards, SHALL block permanent deletion of a creative record that has one or more prompts, and SHALL require explicit prompt cleanup before a connected record can be deleted.

#### Scenario: Listing with prompts cannot be permanently deleted
- **WHEN** the user requests permanent deletion of a listing that has one or more prompts
- **THEN** FusionCanvas blocks the deletion
- **AND** explains that the prompts must be removed or the listing archived instead

#### Scenario: Concept or design with prompts cannot be permanently deleted
- **WHEN** the user requests permanent deletion of a concept or design that has one or more prompts
- **THEN** FusionCanvas blocks the deletion
- **AND** explains that the prompts must be removed or the concept/design archived instead

#### Scenario: Creative record without prompts remains deletable
- **WHEN** the user requests permanent deletion of a listing, concept, or design with no prompts or other dependent records
- **THEN** the existing deletion behavior applies without a prompt blocker

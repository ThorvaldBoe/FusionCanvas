## ADDED Requirements

### Requirement: Creative History Timeline aggregates important listing events
FusionCanvas SHALL provide a read-only Creative History Timeline for a selected listing that aggregates important creative events from Concept, Design, Prompt, Mockup, Asset, and Listing operations, and SHALL display them in reverse-chronological order with their kind, timestamp, summary, and related-record references.

#### Scenario: User opens the timeline for a listing
- **WHEN** the user opens the Creative History Timeline for a selected listing
- **THEN** FusionCanvas lists the listing's creative events in reverse-chronological order
- **AND** shows each event's kind, timestamp, summary, and related-record references

#### Scenario: Listing has no events
- **WHEN** the user opens the timeline for a listing with no recorded events
- **THEN** FusionCanvas shows an empty state that explains events will appear as the listing progresses through stages

#### Scenario: User selects an event
- **WHEN** the user selects a timeline event
- **THEN** FusionCanvas opens a read-only detail with the event's full context metadata
- **AND** does not offer restore, edit, or delete actions

### Requirement: Timeline events cover defined categories with context
FusionCanvas SHALL record creative-history events for concept manual edits, concept AI suggestion requested/accepted/rejected, concept score updates, concept version creation, concept prior-state restoration, suggestion saved as new item, design manual import, design external AI import, design in-app AI generation, design prompt recorded, design variant creation, design variant rejection, design cleanup actions, design final promotion, design final selection changes, mockup record creation, mockup supersession, listing metadata changes, listing status changes, asset import, asset removal, prompt save, prompt rejection, prompt supersession, and other future kinds, and SHALL preserve unknown kinds as `other`.

#### Scenario: Concept manual edit is recorded
- **WHEN** the user manually edits the idea, phrase, or graphic on a concept through the concept-versions service
- **THEN** FusionCanvas records a concept-manual-edit event with the listing id, concept reference, summary, and changed-field context

#### Scenario: Design final promotion is recorded
- **WHEN** the user promotes a design as final selected artwork through the design-records service
- **THEN** FusionCanvas records a design-final-promotion event with the listing id, design reference, and summary

#### Scenario: Mockup supersession is recorded
- **WHEN** a mockup is superseded through the mockup-records service
- **THEN** FusionCanvas records a mockup-superseded event with the listing id, mockup reference, and summary

#### Scenario: Listing metadata change is recorded
- **WHEN** the user saves marketplace-preparation metadata through the listing-metadata-editor service
- **THEN** FusionCanvas records a listing-metadata-change event with the listing id and changed-field summary

#### Scenario: Unknown event kind is preserved
- **WHEN** a future operation records an event whose kind is not in the known enum
- **THEN** FusionCanvas stores the event as `other`
- **AND** keeps it visible in the timeline

### Requirement: Timeline events preserve useful context
FusionCanvas SHALL preserve, on each creative-history event, the event kind, timestamp, listing id, related-record references, a short summary, and optional context metadata sufficient to understand what changed and which records were affected.

#### Scenario: Event references related records
- **WHEN** an event is recorded for a concept, design, prompt, mockup, or asset operation
- **THEN** the event stores references to the related concept, design, prompt, mockup, or asset records
- **AND** the timeline detail view links to those records' surfaces when available

#### Scenario: Event preserves context metadata
- **WHEN** an event includes optional context metadata (changed fields, prior and new values summary, source method)
- **THEN** FusionCanvas stores the metadata on the event
- **AND** the timeline detail view shows it read-only

### Requirement: Timeline is read-only and never mutates current state
FusionCanvas SHALL keep the Creative History Timeline read-only, SHALL NOT offer restore, edit, or delete actions from the timeline, and SHALL route restoring a prior concept state to the Concept tool or undo/redo behavior.

#### Scenario: User attempts to restore from the timeline
- **WHEN** the user looks for a restore action on a concept-prior-state-restored event
- **THEN** the timeline offers no restore action
- **AND** explains that restoration belongs to the Concept tool or undo/redo

#### Scenario: Reviewing the timeline does not change listing state
- **WHEN** the user opens, filters, or selects events in the timeline
- **THEN** FusionCanvas does not mutate the listing, its concepts, designs, prompts, mockups, assets, or metadata

### Requirement: Timeline events are recorded atomically with their originating operation
FusionCanvas SHALL record each creative-history event inside the same atomic snapshot as the originating Concept, Design, Prompt, Mockup, Asset, or Listing operation, so a failed operation records no event and a successful operation records exactly one event.

#### Scenario: Operation succeeds
- **WHEN** a concept, design, prompt, mockup, asset, or listing operation succeeds and is persisted
- **THEN** FusionCanvas records exactly one creative-history event for that operation in the same snapshot

#### Scenario: Operation fails
- **WHEN** an operation fails validation or persistence
- **THEN** FusionCanvas records no creative-history event
- **AND** leaves the timeline unchanged

### Requirement: Timeline operations persist atomically
FusionCanvas SHALL persist creative-history events as part of the originating operation's atomic snapshot, and SHALL load events from the active workspace database scoped to the selected listing.

#### Scenario: Workspace reloads after operations
- **WHEN** a successful operation followed by an application or workspace database reload
- **THEN** the timeline for each listing matches the events recorded by the last successful persisted state

#### Scenario: Timeline view supports filtering
- **WHEN** the user filters the timeline by event kind or stage
- **THEN** FusionCanvas shows only the matching events
- **AND** preserves the full event set underlying the filter

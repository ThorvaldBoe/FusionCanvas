## ADDED Requirements

### Requirement: Snowclones form one application-wide local library
FusionCanvas SHALL maintain snowclones as application-wide structured records with stable identity, phrase, guidance, creation timestamp, and update timestamp, SHALL persist them in the application-local SQLite database, and SHALL keep them independent of workspace, store, niche, group, and item ownership.

#### Scenario: Snowclone survives application data reload
- **WHEN** a valid snowclone is saved and the application later reloads the same local database
- **THEN** the snowclone is reconstructed with the same identity, phrase, guidance, creation timestamp, and update timestamp

#### Scenario: Workspace lifecycle does not affect snowclones
- **WHEN** a workspace or any store-scoped content is created, exported, imported, archived, restored, or permanently deleted
- **THEN** application-wide snowclone records remain unchanged
- **AND** a single-workspace transfer package does not contain snowclone content

#### Scenario: Snowclone operation fails during persistence
- **WHEN** persistence fails while a snowclone operation is being saved
- **THEN** the previous confirmed library remains authoritative
- **AND** FusionCanvas reports a recoverable error without exposing a partially applied library

### Requirement: Snowclone phrases use valid brace-delimited placeholders
FusionCanvas SHALL require every snowclone phrase to contain at least one brace-delimited placeholder and SHALL reject phrases containing newlines, unmatched braces, nested braces, or placeholders whose content is empty or whitespace-only.

#### Scenario: Phrase contains one valid placeholder
- **WHEN** the user submits `Easily distracted by {X}` with nonblank guidance
- **THEN** FusionCanvas accepts the phrase as a valid snowclone template

#### Scenario: Phrase contains named, repeated, or multiple placeholders
- **WHEN** the user submits a single-line phrase containing one or more nonempty brace-delimited placeholders, including named or repeated placeholders
- **THEN** FusionCanvas accepts the placeholder structure
- **AND** preserves the user-visible placeholder names

#### Scenario: Phrase has invalid placeholder structure
- **WHEN** the user submits a phrase with no placeholder, an empty placeholder, a whitespace-only placeholder, unmatched braces, nested braces, or a newline
- **THEN** FusionCanvas rejects the phrase
- **AND** explains the validation problem without persisting a change

#### Scenario: Guidance is missing
- **WHEN** the user submits a structurally valid phrase with empty or whitespace-only guidance
- **THEN** FusionCanvas rejects the snowclone
- **AND** keeps the recoverable phrase input available for correction

### Requirement: Snowclone phrases are unique after normalization
FusionCanvas SHALL compare snowclone phrases using a canonical duplicate key that trims outer whitespace, collapses whitespace runs, and compares text and placeholder casing without case sensitivity.

#### Scenario: Create duplicates an existing phrase
- **WHEN** a new phrase differs from an existing phrase only by casing, placeholder casing, or insignificant whitespace
- **THEN** FusionCanvas refuses to create a duplicate
- **AND** leaves the existing record unchanged

#### Scenario: Edit collides with another phrase
- **WHEN** an edited phrase normalizes to the phrase of a different snowclone
- **THEN** FusionCanvas refuses the edit
- **AND** preserves the selected record and its recoverable draft

### Requirement: Creators can create and edit snowclones
FusionCanvas SHALL allow creators to explicitly save new snowclone drafts and update existing snowclones while preserving stable identity and creation time.

#### Scenario: Creator saves a new snowclone
- **WHEN** the creator saves a valid, unique phrase and guidance from a new draft
- **THEN** FusionCanvas creates one persisted snowclone with a new stable identity
- **AND** selects the created snowclone
- **AND** creation and update timestamps are initially equal

#### Scenario: Creator updates an existing snowclone
- **WHEN** the creator changes a selected snowclone to a valid, unique phrase and guidance and saves
- **THEN** FusionCanvas updates that snowclone in place
- **AND** preserves its identity and creation timestamp
- **AND** advances its update timestamp

#### Scenario: Creator cancels or abandons a blank draft
- **WHEN** the creator starts a new snowclone but provides no meaningful input and cancels or selects another record
- **THEN** FusionCanvas discards the blank draft without prompting
- **AND** does not persist a snowclone

### Requirement: Permanent snowclone deletion is explicit
FusionCanvas SHALL permanently delete a snowclone only after the creator requests deletion for an existing record and confirms a warning.

#### Scenario: Creator confirms snowclone deletion
- **WHEN** the creator requests deletion of a selected snowclone and confirms the warning
- **THEN** FusionCanvas permanently removes that snowclone
- **AND** selects a sensible remaining visible snowclone when one exists
- **AND** otherwise shows the empty or no-results state appropriate to the active search

#### Scenario: Creator cancels snowclone deletion
- **WHEN** the creator cancels the deletion warning
- **THEN** FusionCanvas keeps the selected snowclone unchanged
- **AND** returns focus to a meaningful control for that record

### Requirement: The library supports live phrase and guidance search
FusionCanvas SHALL present snowclones alphabetically by phrase and SHALL filter the visible list as the creator types using case-insensitive substring matching across both phrase and guidance.

#### Scenario: Search matches phrase
- **WHEN** the creator enters search text contained in a snowclone phrase with different or matching casing
- **THEN** the visible list includes that snowclone

#### Scenario: Search matches guidance
- **WHEN** the creator enters search text contained only in a snowclone's guidance
- **THEN** the visible list includes that snowclone

#### Scenario: Search has no matches
- **WHEN** no phrase or guidance contains the current search text
- **THEN** the dialog shows a no-results state
- **AND** provides a clear way to change or clear the search

### Requirement: FusionCanvas ships an opt-in-refreshable starter library
FusionCanvas SHALL ship a UTF-8 CSV starter library using the same `Phrase,Guidance` contract as user interchange, SHALL initialize that bundled content exactly once for a previously uninitialized snowclone library, and SHALL never silently overwrite or resurrect later user changes.

#### Scenario: Snowclone library initializes for the first time
- **WHEN** an application data store has no completed snowclone starter-initialization marker
- **THEN** FusionCanvas atomically imports the currently bundled valid starter records
- **AND** includes `Easily distracted by {X}` with guidance explaining how to replace `{X}`
- **AND** persists the completed initialization marker

#### Scenario: Creator deletes the initial starter record
- **WHEN** the starter initialization has completed and the creator deletes `Easily distracted by {X}`
- **AND** the application later reloads or a later build starts
- **THEN** automatic initialization does not recreate the deleted record

#### Scenario: Creator imports the bundled library explicitly
- **WHEN** the creator chooses Import bundled library
- **THEN** FusionCanvas validates and imports unique records from the CSV shipped with the current build
- **AND** skips normalized phrases already in the local library
- **AND** does not overwrite their local guidance
- **AND** reports added and skipped counts

#### Scenario: Bundled starter data is invalid
- **WHEN** the shipped starter CSV fails the same header, row, phrase, or guidance validation as a user CSV
- **THEN** FusionCanvas does not mark starter initialization complete
- **AND** does not partially import the bundled content
- **AND** reports a recoverable initialization error

### Requirement: Snowclone CSV uses exactly Phrase and Guidance columns
FusionCanvas SHALL import and export snowclone interchange as UTF-8 CSV with exactly two columns headed `Phrase` and `Guidance` in that order and SHALL support quoted commas, escaped quotes, and multiline guidance.

#### Scenario: Creator exports the library
- **WHEN** the creator chooses a CSV destination and export completes
- **THEN** FusionCanvas writes one header row `Phrase,Guidance`
- **AND** writes one data row for each snowclone in alphabetical phrase order
- **AND** includes no identity or timestamp columns
- **AND** preserves phrase and guidance text through standards-compliant CSV quoting

#### Scenario: Creator imports a valid CSV
- **WHEN** the creator selects a UTF-8 CSV with the exact header and valid rows
- **THEN** FusionCanvas validates the complete document before saving
- **AND** atomically adds phrases that do not duplicate existing or earlier imported rows
- **AND** reports added and skipped-duplicate counts

#### Scenario: CSV header or row is invalid
- **WHEN** an import has missing, reordered, additional, or differently named headers, an unreadable CSV structure, or any row with an invalid phrase or guidance
- **THEN** FusionCanvas reports the relevant validation problem and row number when available
- **AND** imports no records from that document

#### Scenario: CSV contains only duplicates
- **WHEN** every valid imported phrase duplicates the local library or an earlier row after normalization
- **THEN** FusionCanvas completes without modifying the library
- **AND** reports that zero records were added and the duplicates were skipped

#### Scenario: Creator cancels a CSV picker
- **WHEN** the creator cancels import or export before choosing a file
- **THEN** FusionCanvas closes the picker without changing the library
- **AND** preserves the current dialog selection, search, and recoverable draft

### Requirement: Snowclone management uses a focused integration-ready dialog
FusionCanvas SHALL provide a focused Snowclone Library dialog with search, a filtered list, a side editor, New, Save, Delete, Import CSV, Export CSV, Import bundled library, and Close actions, and SHALL expose it for the future ideation dialog without adding a temporary launcher to the current main workspace or settings.

#### Scenario: Future owner opens the Snowclone Library dialog
- **WHEN** an owning surface opens the Snowclone Library dialog
- **THEN** the dialog loads the confirmed library
- **AND** preselects a sensible first record when one exists
- **AND** otherwise presents an empty state with a clear New action

#### Scenario: Dialog has an active search
- **WHEN** the creator searches while a selected record no longer appears in the filtered list
- **THEN** the editor does not silently change or discard meaningful input
- **AND** the visible selection and editor state remain coherent

#### Scenario: Library operation is running
- **WHEN** initialization, import, export, save, or delete is in progress
- **THEN** the dialog prevents duplicate submission and disables conflicting mutation actions
- **AND** shows an appropriate busy state

#### Scenario: Dialog operation fails
- **WHEN** loading, saving, deletion, import, export, or initialization fails
- **THEN** the dialog reports a recoverable error
- **AND** preserves the last confirmed library plus input needed to retry when applicable

#### Scenario: Contributor reviews current entry points
- **WHEN** the snowclone-library module is complete before the ideation dialog exists
- **THEN** the current main window and settings contain no temporary Snowclone Library launcher
- **AND** the dialog remains constructible and testable for its future owning surface

### Requirement: The dialog protects drafts and supports keyboard use
FusionCanvas SHALL protect meaningful unsaved phrase or guidance edits during selection changes, import actions, bundled-library import, and dialog close, and SHALL make search, list selection, editing, saving, confirmation, cancellation, and closing keyboard reachable.

#### Scenario: Creator leaves meaningful unsaved edits
- **WHEN** the creator changes selection, starts an import, imports the bundled library, or closes the dialog with meaningful unsaved input
- **THEN** FusionCanvas offers Save, Discard, and Cancel
- **AND** Cancel keeps the current draft, selection, and focus context

#### Scenario: Creator starts a new draft
- **WHEN** the creator chooses New
- **THEN** FusionCanvas creates only an in-memory draft
- **AND** places keyboard focus in the phrase field
- **AND** disables deletion until the draft is saved

#### Scenario: Creator completes or cancels a confirmation
- **WHEN** a save/discard decision or deletion confirmation completes or is cancelled
- **THEN** keyboard focus returns to the next meaningful editor, list, or invoking control
- **AND** no essential action requires pointer-only interaction

### Requirement: Advanced ideation and library organization remain outside this module
The snowclone-library module SHALL NOT perform placeholder substitution, generate ideation candidates, invoke AI providers, attach snowclones to creative records, categorize or tag snowclones, archive or restore snowclones, synchronize them through cloud services, or provide whole-application backup behavior.

#### Scenario: Contributor reviews module scope
- **WHEN** a contributor reviews the completed snowclone-library implementation
- **THEN** the implementation supplies reusable library data and management behavior only
- **AND** future ideation consumes the library through a later change

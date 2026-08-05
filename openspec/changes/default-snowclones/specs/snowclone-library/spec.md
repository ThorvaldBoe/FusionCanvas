## MODIFIED Requirements

### Requirement: FusionCanvas ships an opt-in-refreshable starter library
FusionCanvas SHALL ship a UTF-8 CSV starter library using the same `Phrase,Guidance` contract as user interchange, SHALL initialize that bundled content exactly once for a previously uninitialized snowclone library, SHALL ship a curated default set of snowclones so they appear as if imported, and SHALL never silently overwrite or resurrect later user changes.

#### Scenario: Snowclone library initializes for the first time
- **WHEN** an application data store has no completed snowclone starter-initialization marker
- **THEN** FusionCanvas atomically imports every currently bundled valid starter record
- **AND** the imported set includes the full curated default list
- **AND** each default snowclone appears in the library as if it had been imported before
- **AND** persists the completed initialization marker

#### Scenario: Creator deletes an initialized default record
- **WHEN** the starter initialization has completed and the creator deletes any individual default snowclone
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

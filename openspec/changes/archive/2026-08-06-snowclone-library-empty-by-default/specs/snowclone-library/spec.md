## MODIFIED Requirements

### Requirement: FusionCanvas ships an opt-in-refreshable starter library
FusionCanvas SHALL ship a UTF-8 CSV starter library using the same `Phrase,Guidance` contract as user interchange, SHALL keep the local snowclone library empty by default with no automatic import of bundled content, and SHALL add the bundled curated snowclones only when the creator explicitly imports them, without silently overwriting later user changes.

#### Scenario: Snowclone library is empty by default on first load
- **WHEN** an application data store opens its snowclone library for the first time
- **THEN** the library contains no snowclones
- **AND** FusionCanvas does not automatically import any bundled default records

#### Scenario: Creator imports the bundled library explicitly
- **WHEN** the creator chooses Import bundled library
- **THEN** FusionCanvas validates and imports unique records from the CSV shipped with the current build
- **AND** the imported set includes the full curated default list
- **AND** skips normalized phrases already in the local library
- **AND** does not overwrite their local guidance
- **AND** reports added and skipped counts

#### Scenario: Bundled starter data is invalid
- **WHEN** the shipped starter CSV fails the same header, row, phrase, or guidance validation as a user CSV
- **THEN** FusionCanvas does not import the bundled content
- **AND** does not partially import the bundled content
- **AND** reports a recoverable import error

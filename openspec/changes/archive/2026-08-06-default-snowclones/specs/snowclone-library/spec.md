## MODIFIED Requirements

### Requirement: FusionCanvas ships an opt-in-refreshable starter library
FusionCanvas SHALL ship a UTF-8 CSV starter library using the same `Phrase,Guidance` contract as user interchange, SHALL add the bundled curated snowclones only when the creator explicitly imports them, and SHALL never silently overwrite later user changes.

> Note: An earlier draft of this change specified automatic one-time initialization of the bundled content on first load. That automatic initialization was removed by the active `snowclone-library-empty-by-default` change, which keeps the local library empty by default with no automatic import. The scenarios below describe only the explicit, opt-in bundled-import behavior contributed by this change; the empty-by-default and no-automatic-import behavior is owned by `snowclone-library-empty-by-default`.

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

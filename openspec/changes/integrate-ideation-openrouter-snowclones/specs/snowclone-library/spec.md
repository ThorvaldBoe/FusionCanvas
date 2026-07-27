## MODIFIED Requirements

### Requirement: Snowclone phrases use valid brace-delimited placeholders
FusionCanvas SHALL require every Snowclone phrase to contain at least one brace-delimited placeholder, SHALL treat the trimmed text inside each brace pair as the placeholder name, and SHALL reject phrases containing newlines, unmatched braces, nested braces, or placeholders whose content is empty or whitespace-only. Ideation SHALL preserve the complete placeholder token for prompting and SHALL reject generated output that leaves any template placeholder unresolved.

#### Scenario: Phrase contains one valid placeholder
- **WHEN** the user submits `Easily distracted by {X}` with nonblank guidance
- **THEN** FusionCanvas accepts the phrase as a valid Snowclone template

#### Scenario: Phrase contains named and repeated placeholders
- **WHEN** the user submits `My {Audience} knows their {Product}, and {Audience} agrees` with nonblank guidance
- **THEN** FusionCanvas accepts both named placeholders
- **AND** recognizes both occurrences of `{Audience}` as the same placeholder name

#### Scenario: Phrase has no placeholder
- **WHEN** the user submits a phrase with no brace-delimited placeholder
- **THEN** FusionCanvas rejects the phrase
- **AND** explains that at least one placeholder is required

#### Scenario: Phrase has invalid braces
- **WHEN** the phrase contains an unmatched opening brace, unmatched closing brace, nested brace, or empty placeholder
- **THEN** FusionCanvas rejects the phrase
- **AND** keeps the recoverable phrase input available for correction

#### Scenario: Guidance is missing
- **WHEN** the user submits a structurally valid phrase with empty or whitespace-only guidance
- **THEN** FusionCanvas rejects the Snowclone
- **AND** keeps the recoverable phrase input available for correction

### Requirement: Snowclone management uses a focused integration-ready dialog
FusionCanvas SHALL provide a focused Snowclone Library dialog with search, a filtered list, a side editor, New, Save, Delete, Import CSV, Export CSV, Import bundled library, and Close actions, SHALL expose it through one compact `Manage Snowclones…` action owned by Snowclones mode in the Ideation dialog, and SHALL add no launcher to the main workspace or Settings.

#### Scenario: Creator opens the library from Ideation
- **WHEN** Snowclones mode is selected and the creator activates `Manage Snowclones…`
- **THEN** one Snowclone Library dialog opens modally with the Ideation dialog as owner
- **AND** it loads the confirmed library and preselects a sensible first record when one exists
- **AND** otherwise presents an empty state with a clear New action

#### Scenario: Creator closes the library
- **WHEN** the Snowclone Library dialog closes after resolving any unsaved draft
- **THEN** the Ideation dialog refreshes Snowclone mode availability from the confirmed library
- **AND** keyboard focus returns to `Manage Snowclones…`

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

#### Scenario: Contributor reviews entry points
- **WHEN** the integrated module is complete
- **THEN** Ideation Snowclones mode contains the only production Snowclone Library launcher
- **AND** the main window and Settings contain no duplicate launcher


## ADDED Requirements

### Requirement: Item CSV import defines a standard text format
FusionCanvas SHALL support importing a list of Design items from a semi-colon-delimited text (CSV-like) file with exactly seven columns in fixed order: `Title`, `Base Idea`, `Concept Idea`, `Phrase`, `Graphic`, `Notes`, `Tags`.

#### Scenario: User reads the documented column order
- **WHEN** a user inspects the item import format
- **THEN** the seven columns are presented in the order Title, Base Idea, Concept Idea, Phrase, Graphic, Notes, Tags
- **AND** each column maps to a defined item field

#### Scenario: Columns are separated by a single semi-colon
- **WHEN** a line contains multiple columns
- **THEN** each column is separated by exactly one `;` character
- **AND** the separator is not recognized inside a quoted field, so separator characters inside a field do not shift the columns

#### Scenario: Fields are quoted using standard CSV quoting
- **WHEN** a field value contains a `;`, a double quote `"`, a carriage return, or a line feed
- **THEN** the field is written as a double-quoted field, matching the format the export feature produces
- **AND** an embedded double quote inside a quoted field is written as two double quotes (`""`)
- **AND** the importer decodes the quoted field back to its literal value

#### Scenario: Empty fields are preserved in any column position
- **WHEN** a row has an empty middle field (for example an empty Notes between Graphic and Tags)
- **THEN** the importer treats consecutive separators as an empty field rather than as an escaped semi-colon
- **AND** columns to the right of the empty field are not shifted

#### Scenario: Optional header row is detected and ignored
- **WHEN** the first line of the source is a header identifying the columns
- **THEN** FusionCanvas recognizes and ignores the header as column headings rather than importing it as an item row

#### Scenario: A data row equal to the headings is indistinguishable from a header
- **WHEN** the first line of the source contains exactly the seven heading texts
- **THEN** FusionCanvas treats it as a header and does not import it as an item
- **AND** this is an accepted limitation of exact header detection

#### Scenario: Tags are comma-separated within their column
- **WHEN** a `Tags` column contains multiple tags
- **THEN** the tags are separated by commas within that single semi-colon-delimited column

### Requirement: Item CSV import is reachable from the niche and group context menu
FusionCanvas SHALL offer an `Import…` action on the navigation-tree context menu of active niche (top-level) and group rows, and SHALL open the item import dialog targeted at the row from which it was invoked.

#### Scenario: User opens Import from a niche row
- **WHEN** the user right-clicks an active niche (top-level) row and activates Import
- **THEN** FusionCanvas opens the item import dialog targeted at the top level of that niche

#### Scenario: User opens Import from a group row
- **WHEN** the user right-clicks an active group row and activates Import
- **THEN** FusionCanvas opens the item import dialog targeted directly beneath that group

### Requirement: Item CSV import creates items at the targeted niche or group
FusionCanvas SHALL create imported items beneath the niche or group from which the Import action was invoked: items imported from a niche are placed at the top level of that niche, and items imported from a group are placed directly beneath that group.

#### Scenario: User imports from a niche
- **WHEN** the user chooses Import from the context menu of an active niche
- **THEN** each imported item is created at the top level of that niche

#### Scenario: User imports from a group
- **WHEN** the user chooses Import from the context menu of an active group
- **THEN** each imported item is created directly beneath that group

#### Scenario: Import destination is persisted with the items
- **WHEN** imported items are created beneath a niche or group
- **THEN** their resulting placement is identical to items created directly in that location

### Requirement: Item CSV import maps columns to item fields and chooses the stage
FusionCanvas SHALL map each imported column to the corresponding item field and create the item at the Concept stage when any Concept Idea, Phrase, or Graphic value is populated, otherwise at the Idea stage, always storing the Base Idea.

#### Scenario: Title becomes the working title
- **WHEN** an imported row has a Title
- **THEN** that title is set as the item's working title

#### Scenario: Base Idea maps to the Idea field
- **WHEN** an imported row has a Base Idea
- **THEN** the value is stored as the item's Idea field

#### Scenario: Concept Idea, Phrase, and Graphic map to concept fields
- **WHEN** an imported row has Concept Idea, Phrase, or Graphic values
- **THEN** they are stored as the item's Concept Idea, Phrase, and Graphic fields respectively
- **AND** the item is created at the Concept stage so those fields are active

#### Scenario: Item with only a Base Idea is created at the Idea stage
- **WHEN** an imported row has a Base Idea but no Concept Idea, Phrase, or Graphic
- **THEN** the item is created at the Idea stage with its Idea field populated

#### Scenario: Notes map to item notes
- **WHEN** an imported row has Notes
- **THEN** the value is stored as the item's notes

#### Scenario: Inherited context metadata applies to non-CSV fields
- **WHEN** the targeted niche or group carries inherited context metadata for keys not supplied by the CSV row
- **THEN** the created item receives those inherited metadata values, marked as inherited
- **AND** values supplied by the CSV row override any inherited value for the same key

#### Scenario: Inherited tags merge with imported tags
- **WHEN** the targeted niche or group carries inherited tags and the imported row also supplies tags
- **THEN** the created item is linked to the union of inherited tags and the imported row's tags
- **AND** duplicate tags are not linked twice

#### Scenario: Tags are linked to the item
- **WHEN** an imported row has comma-separated Tags
- **THEN** each tag is attached to the created item, creating the tag if it does not already exist as an active tag in the store

#### Scenario: Archived tag name is not reused by import
- **WHEN** an imported row names a tag that exists only as an archived tag in the store
- **THEN** FusionCanvas creates a new active tag with that name rather than linking the archived tag
- **AND** bulk import does not offer interactive archived-tag restoration

#### Scenario: Tag names containing commas are not supported
- **WHEN** a tag name in the `Tags` column contains a comma
- **THEN** the comma is treated as a tag separator rather than part of the name
- **AND** tag names must not contain commas (no comma escaping is defined for this format)

### Requirement: Item CSV import validates rows and requires a title
FusionCanvas SHALL require every imported item to have a non-empty Title. A row without a Title SHALL be reported as an error and SHALL NOT appear among the importable rows, so Import is disabled while any title is missing.

#### Scenario: Row without a title is rejected
- **WHEN** an imported row has no Title
- **THEN** that row is reported as a missing-title error and excluded from the importable rows
- **AND** Import remains disabled until the error is corrected

#### Scenario: Rows with duplicate titles are imported as-is
- **WHEN** two imported rows share the same working title
- **THEN** both rows are still imported and created
- **AND** the importer does not skip or replace duplicate titles

### Requirement: Item CSV import provides a dialog with raw source, preview, and syntax check
FusionCanvas SHALL present an Import dialog when the user selects Import, offering file selection, a sample file export, an editable raw-source field, and a read-only preview of the parsed import whose syntax check gates the Import action.

#### Scenario: User opens the import dialog from a contextual target
- **WHEN** the user activates Import from a niche or group context menu
- **THEN** an Import dialog opens with that niche or group as the target

#### Scenario: User selects a CSV file
- **WHEN** the user selects a file through the dialog's file picker
- **THEN** its contents are loaded into the editable raw-source field
- **AND** the preview reflects the loaded source

#### Scenario: User edits the raw source
- **WHEN** the user edits the raw source (for example to fix unescaped semi-colons)
- **THEN** the fields remain editable text
- **AND** the preview updates based on the current source

#### Scenario: User exports a sample file
- **WHEN** the user requests an example
- **THEN** FusionCanvas exports a sample file using the documented format so the user can see the expected structure
- **AND** the sample demonstrates the column order and escaping rules

#### Scenario: Preview runs a syntax check and disables Import on error
- **WHEN** the user generates the preview and the source has a malformed row (for example a line with an inconsistent number of semi-colon separators or a missing title)
- **THEN** the Import action is disabled
- **AND** FusionCanvas shows an error message of the form `Line N: <reason>` that identifies the offending line, the affected column, and why it is incorrect (for example `Line 3: The Title field is required.` or `Line 2: Expected 7 columns in this order: Title, Base Idea, Concept Idea, Phrase, Graphic, Notes, Tags; found 5.`)

#### Scenario: Preview shows a valid import and enables Import
- **WHEN** the user generates the preview and every row parses correctly with a title
- **THEN** the Import action is enabled
- **AND** the preview shows the parsed items

#### Scenario: Empty source disables Import
- **WHEN** the user generates the preview and the source contains no importable rows
- **THEN** the Import action is disabled
- **AND** no items can be imported from an empty source

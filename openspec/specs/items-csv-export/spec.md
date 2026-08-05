# Items CSV Export

## Purpose

Defines accepted behavior for exporting items from a niche or group to a UTF-8, semi-colon-delimited CSV file, including export scope, column projection, and field escaping.

## Requirements

### Requirement: Items can be exported to CSV from a niche or group

The navigation tree's niche and group rows SHALL expose an `Export to CSV...` action. Selecting it exports the items under that row to a UTF-8, semi-colon-delimited CSV file chosen through a standard save dialog.

The export scope for a row SHALL be the row's subtree:
- For a **group**: items whose `GroupId` is the group or any descendant subgroup (via `GroupHierarchy.GetDescendants`), plus items directly in the group.
- For a **niche**: items attached directly to the niche (items with that `NicheId` and no group) plus all items in groups and descendant subgroups under the niche.

Archived items SHALL be excluded from the export, and items that are not effectively active SHALL also be excluded (an item is not effectively active when it or an ancestor group/niche is archived).

An **empty item** SHALL be excluded: an item with no working title (`string.IsNullOrWhiteSpace(Item.Name)`) and no content in any of Base Idea, Concept Idea, Phrase, Graphic, Notes, or Tags.

A failure during export SHALL be reported to the user, and the export SHALL not be reported as successful.

#### Scenario: Export a group with nested subgroups

- **WHEN** the user right-clicks an active group and selects `Export to CSV...`, then chooses a destination path
- **THEN** the CSV contains one row per non-archived, non-empty item in that group and all of its descendant subgroups, in addition to the items directly in the group

#### Scenario: Export a niche covering all its items

- **WHEN** the user right-clicks a top-level niche and selects `Export to CSV...`
- **THEN** the CSV contains every non-archived, non-empty item across the niche's groups, subgroups, and items attached directly to the niche

#### Scenario: Empty group exports only the header

- **WHEN** the user exports a group that has no items (or only archived/empty items)
- **THEN** the CSV contains a header row and no data rows

#### Scenario: Archived and empty items are omitted

- **WHEN** a group contains archived items and items with no title and no content alongside exportable items
- **THEN** the CSV contains rows for the exportable items only

#### Scenario: Items under archived descendants are omitted

- **WHEN** a group contains items whose containing subgroup is archived, alongside items in active groups
- **THEN** the CSV contains rows for the items in active groups only and omits the items under the archived subgroup

#### Scenario: Save is canceled

- **WHEN** the user selects `Export to CSV...` and cancels the save dialog
- **THEN** no file is written and no error is shown

#### Scenario: Export fails

- **WHEN** the destination cannot be written (for example, permission denied)
- **THEN** the user is informed of the failure and the export is not reported as successful

### Requirement: Exported items use the defined CSV columns

The CSV SHALL have a header row with columns in this order: `Title`, `Base Idea`, `Concept Idea`, `Phrase`, `Graphic`, `Notes`, `Tags`. Each item SHALL appear as one data row with values projected as follows:

- `Title`: the item's working title (`Item.Name`).
- `Base Idea`: the original Idea text from item metadata.
- `Concept Idea`: the Concept idea text from item metadata.
- `Phrase`: the Phrase text from item metadata (the raw stored value, which is already normalized to one line).
- `Graphic`: the graphics direction/description text from item metadata.
- `Notes`: the item's notes text.
- `Tags`: the item's linked tag names.

Missing text values SHALL be exported as empty fields. Tags SHALL be rendered as their names separated by a comma (and a space) within the single `Tags` field.

#### Scenario: Header order matches the specification

- **WHEN** an export is produced
- **THEN** the first line is exactly `Title;Base Idea;Concept Idea;Phrase;Graphic;Notes;Tags`

#### Scenario: Item fields map to columns

- **WHEN** an item has a title, idea, concept idea, phrase, graphics description, notes, and two linked tags
- **THEN** the item's row puts the title in the first column, the idea in the second, the concept idea in the third, the phrase in the fourth, the graphics description in the fifth, the notes in the sixth, and the tag names in the seventh

#### Scenario: Missing fields export as empty

- **WHEN** an item has a title but none of the creative fields or tags set
- **THEN** its CSV row has the title in the first column and empty values in the remaining six columns

### Requirement: CSV fields are safely escaped

The CSV writer SHALL use standard field quoting so multi-line text and separator characters do not corrupt the row structure. A field SHALL be quoted when it contains a `;`, `"`, carriage return, or line feed. Within a quoted field, an embedded `"` SHALL be written as two double-quote characters (`""`). The semi-colon is the field delimiter.

#### Scenario: Semi-colon in a field is preserved

- **WHEN** a field value contains a semi-colon, for example `Option A; Option B`
- **THEN** the field is written quoted so the semi-colon is preserved as a single character within the field

#### Scenario: Multi-line field is preserved

- **WHEN** a Notes field contains embedded line breaks
- **THEN** the field is quoted so the line breaks are preserved within a single field and do not create additional rows

#### Scenario: Embedded double-quote is doubled

- **WHEN** a field value contains a double-quote character
- **THEN** the field is written quoted and the double-quote is written as two double-quote characters

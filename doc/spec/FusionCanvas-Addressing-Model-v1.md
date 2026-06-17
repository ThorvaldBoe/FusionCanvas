# FusionCanvas Addressing Model v1

## Purpose

FusionCanvas requires a stable, protocol-driven way to reference spreadsheet structure and content without relying on fragile physical coordinates such as `A1`, `B7`, or row/column indexes.

The addressing model defines how FusionCanvas refers to:

- sheets
- columns
- structural rows
- data rows
- cells
- ranges

This model is intended to be:

- stable across column reordering
- stable across row insertion
- readable by developers
- extensible for future phases
- shared between plugin and engine

The addressing model defines logical identity, not physical position.

---

## Core Principle

FusionCanvas always prefers **logical addresses** over physical spreadsheet coordinates.

Example:

- Fragile physical address: `IDEAS!B7`
- Stable logical address: `FC_SHEET_IDEAS.IDEA_000042.FC_COL_IDEA_TEXT`

Logical addresses are resolved at runtime to physical sheet locations.

---

## Addressing Layers

FusionCanvas distinguishes between two address types:

### 1. Logical Address
A stable protocol-driven reference used internally by FusionCanvas.

Examples:

- `FC_SHEET_IDEAS.FC_COL_IDEA_TEXT`
- `FC_SHEET_DASHBOARD.FC_ROW_PROTOCOL_VERSION.FC_COL_VALUE`
- `FC_SHEET_IDEAS.IDEA_000042.FC_COL_SCORE`

### 2. Physical Address
A temporary spreadsheet coordinate used only when reading or writing values.

Examples:

- `IDEAS!B7`
- `DASHBOARD!C4`

Physical addresses are runtime-derived and must never be treated as canonical identity.

---

## Typed Identifier Prefixes

To make references self-describing and unambiguous, FusionCanvas uses typed identifier prefixes.

### Sheet identifiers
Sheet identifiers must use the prefix:

`FC_SHEET_`

Examples:

- `FC_SHEET_DASHBOARD`
- `FC_SHEET_IDEAS`
- `FC_SHEET_SYSTEM`

### Column identifiers
Column identifiers must use the prefix:

`FC_COL_`

Examples:

- `FC_COL_IDEA_ID`
- `FC_COL_IDEA_TEXT`
- `FC_COL_PARENT_ID`
- `FC_COL_NICHE`
- `FC_COL_SCORE`
- `FC_COL_STATUS`
- `FC_COL_LABEL`
- `FC_COL_VALUE`

### Structural row identifiers
Rows that are part of the protocol layout must use the prefix:

`FC_ROW_`

Examples:

- `FC_ROW_TITLE`
- `FC_ROW_PROTOCOL_VERSION`
- `FC_ROW_STATUS`
- `FC_ROW_TOP_IDEAS_HEADER`

### Range identifiers
Named logical ranges may later use the prefix:

`FC_RANGE_`

Examples:

- `FC_RANGE_PROTOCOL_INFO`
- `FC_RANGE_TOP_IDEAS`

Range identifiers are optional in v1 and may be introduced later.

---

## Sheet Types

FusionCanvas distinguishes between different sheet archetypes because not all sheets behave the same way.

### Table sheets
A table sheet contains data records.

Characteristics:

- protocol-defined columns
- dynamic data rows
- row identity comes from a designated ID column value
- rows are not predefined in the protocol

Example:

- `IDEAS`

### Layout sheets
A layout sheet contains structured UI or metadata blocks.

Characteristics:

- protocol-defined columns
- protocol-defined structural rows
- specific cells may carry fixed meaning
- row identity is predefined in the protocol

Examples:

- `DASHBOARD`
- `SYSTEM`

---

## Row Identity Rules

### Structural rows
Structural rows are explicitly defined in the protocol and are used in layout sheets.

Example:

- `FC_ROW_PROTOCOL_VERSION`
- `FC_ROW_STATUS`

These rows represent fixed semantic slots in the sheet layout.

### Data rows
Data rows represent records stored in table sheets.

Their identity is not defined by physical row number, but by the value in the sheet’s designated ID column.

Example in `IDEAS`:

- `IDEA_000001`
- `IDEA_000042`

These values are record identifiers, not protocol structural row identifiers.

---

## Canonical Address Forms

### 1. Sheet reference
A sheet can be referenced by its sheet identifier.

Format:

`{sheetId}`

Example:

`FC_SHEET_IDEAS`

### 2. Column reference
A column can be referenced by sheet + column.

Format:

`{sheetId}.{columnId}`

Example:

`FC_SHEET_IDEAS.FC_COL_IDEA_TEXT`

### 3. Structural row reference
A structural row can be referenced by sheet + row.

Format:

`{sheetId}.{rowId}`

Example:

`FC_SHEET_DASHBOARD.FC_ROW_PROTOCOL_VERSION`

### 4. Table cell reference
A cell in a table sheet can be referenced by sheet + record ID + column.

Format:

`{sheetId}.{recordId}.{columnId}`

Examples:

- `FC_SHEET_IDEAS.IDEA_000042.FC_COL_IDEA_TEXT`
- `FC_SHEET_IDEAS.IDEA_000042.FC_COL_SCORE`

### 5. Layout cell reference
A cell in a layout sheet can be referenced by sheet + structural row + column.

Format:

`{sheetId}.{rowId}.{columnId}`

Examples:

- `FC_SHEET_DASHBOARD.FC_ROW_PROTOCOL_VERSION.FC_COL_VALUE`
- `FC_SHEET_SYSTEM.FC_ROW_PROTOCOL_VERSION.FC_COL_VALUE`

### 6. Range reference
A range may later be referenced by sheet + range ID.

Format:

`{sheetId}.{rangeId}`

Example:

`FC_SHEET_DASHBOARD.FC_RANGE_PROTOCOL_INFO`

Range addressing is reserved for future use.

---

## Display Names vs Stable IDs

FusionCanvas distinguishes between:

- stable internal IDs
- user-facing display names

Example:

- sheet ID: `FC_SHEET_IDEAS`
- sheet display name: `IDEAS`

Example:

- column ID: `FC_COL_IDEA_TEXT`
- column display name: `Idea`

### Rules
- internal IDs must be stable
- display names are user-facing schema labels
- display names must be controlled by FusionCanvas
- display names must be unique within their scope
- display names are not free-form user text

---

## Uniqueness Rules

### Sheet IDs
- must be globally unique within the protocol

### Sheet display names
- must be unique within the workbook

### Column IDs
- must be unique within their sheet

### Column display names
- must be unique within their sheet

### Structural row IDs
- must be unique within their sheet

### Record IDs
- must be unique within their table sheet

### Canonical logical addresses
- must uniquely identify a single semantic target

---

## Resolution Rules

Logical addresses must be resolved dynamically at runtime.

### Column resolution
To resolve `FC_SHEET_IDEAS.FC_COL_IDEA_TEXT`:

1. find the sheet by sheet ID
2. resolve the physical sheet name
3. scan the header row
4. map the target logical column to the correct physical column index

### Structural row resolution
To resolve `FC_SHEET_DASHBOARD.FC_ROW_PROTOCOL_VERSION`:

1. find the sheet by sheet ID
2. identify the structural row using the row definition in the protocol
3. resolve the corresponding physical row index

### Table cell resolution
To resolve `FC_SHEET_IDEAS.IDEA_000042.FC_COL_SCORE`:

1. resolve the sheet
2. resolve the ID column for the sheet
3. locate the record row by record ID value
4. resolve the score column
5. return the physical cell location

### Layout cell resolution
To resolve `FC_SHEET_DASHBOARD.FC_ROW_PROTOCOL_VERSION.FC_COL_VALUE`:

1. resolve the sheet
2. resolve the structural row
3. resolve the target column
4. return the physical cell location

---

## Protocol Modeling Requirements

The protocol must model enough structure to resolve logical addresses without relying on row or column indexes as canonical identity.

### For all sheets
The protocol must define:

- `sheetId`
- `displayName`
- `sheetType`

### For all sheet columns
The protocol must define for each column:

- `columnId`
- `displayName`
- `dataType`
- `required`
- optional `allowedValues`
- optional `defaultValue`

### For layout sheets
The protocol must additionally define structural rows.

Each structural row should define:

- `rowId`
- optional `displayName`
- optional semantic purpose
- optional ordering metadata

### For table sheets
The protocol must identify the record ID column.

Example:

- `recordIdColumnId = FC_COL_IDEA_ID`

---

## Suggested v1 Sheet Classification

### DASHBOARD
Sheet type:

`layout`

Recommended structural rows in v1:

- `FC_ROW_TITLE`
- `FC_ROW_PROTOCOL_VERSION`
- `FC_ROW_STATUS`

Recommended columns in v1:

- `FC_COL_LABEL`
- `FC_COL_VALUE`
- `FC_COL_NOTES`

### SYSTEM
Sheet type:

`layout`

Recommended structural rows in v1:

- `FC_ROW_PROTOCOL_VERSION`
- `FC_ROW_PLUGIN_VERSION`

Recommended columns in v1:

- `FC_COL_KEY`
- `FC_COL_VALUE`

### IDEAS
Sheet type:

`table`

Recommended ID column:

- `FC_COL_IDEA_ID`

Recommended columns in v1:

- `FC_COL_IDEA_ID`
- `FC_COL_IDEA_TEXT`
- `FC_COL_PARENT_ID`
- `FC_COL_NICHE`
- `FC_COL_SCORE`
- `FC_COL_STATUS`
- `FC_COL_PROMOTE_TO`

---

## Non-Goals for v1

The addressing model v1 does not require:

- direct use of A1 notation as logical identity
- fixed physical row numbers as canonical references
- fixed physical column positions as canonical references
- predefined protocol rows for table-sheet data records
- advanced range semantics
- formula graph modeling
- cell-level IDs as standalone protocol entities

---

## Design Guidelines

### Prefer composition over special-case IDs
A cell should usually be addressed as:

- sheet + row + column

rather than inventing a separate permanent cell ID.

### Preserve stability
Adding rows, inserting columns, or reordering columns must not break logical references.

### Keep human readability
Protocol IDs should be explicit enough that developers can understand what they reference by inspection.

### Keep layout and table semantics separate
Structural rows in layout sheets are not the same thing as data records in table sheets.

---

## Example Logical References

### Sheet
`FC_SHEET_IDEAS`

### Column
`FC_SHEET_IDEAS.FC_COL_IDEA_TEXT`

### Layout row
`FC_SHEET_DASHBOARD.FC_ROW_PROTOCOL_VERSION`

### Layout cell
`FC_SHEET_DASHBOARD.FC_ROW_PROTOCOL_VERSION.FC_COL_VALUE`

### Table cell
`FC_SHEET_IDEAS.IDEA_000042.FC_COL_SCORE`

---

## Summary

FusionCanvas v1 uses a logical addressing model built on:

- typed stable IDs
- sheet archetypes
- explicit distinction between structural rows and data rows
- runtime resolution to physical spreadsheet coordinates

This allows FusionCanvas to remain:

- robust
- readable
- protocol-driven
- extensible

while avoiding the fragility of native spreadsheet coordinates as canonical identity.

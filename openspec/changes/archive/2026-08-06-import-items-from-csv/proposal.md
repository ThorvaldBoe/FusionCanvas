# Import Items from CSV

## Why

Users who already have a list of existing designs have no way to get them into FusionCanvas other than adding each item manually. A bulk import from a standard text (CSV-like) format removes this friction, so creators can onboard existing work quickly. This is GitHub issue #118.

## What Changes

- Add an **"Import…"** action to the navigation-tree context menu for **niche (top-level)** and **group** rows. The target becomes the import destination: items imported into a niche land at the top level of that niche; items imported into a group land directly beneath that group.
- Add an **Import items** dialog that:
  - lets the user pick a CSV file (`*.csv`) via the system open-file picker;
  - shows a sample file they can export to learn the exact format;
  - shows an editable **raw source** text field and a read-only **preview** of the parsed import;
  - runs a syntax check when generating the preview, and **disables Import** and shows a detailed `Line N: <reason>` error (naming the line, the offending column, and why) on malformed rows;
  - enables Import only when the source parses cleanly and every row has a title.
- Define a **CSV format** for items with seven semi-colon-delimited columns in order: `Title`, `Base Idea`, `Concept Idea`, `Phrase`, `Graphic`, `Notes`, `Tags`.
- Create imported items via the existing item-creation path at the chosen niche/group, mapping each column to the correct item field/metadata key.
- **BREAKING (format-level, only affects this new importer):** the item CSV is not the same as any existing CSV; there is currently no item CSV in the product.

### Durable format rules (captured in the spec)
- Columns are separated by a single semi-colon `;`.
- Fields are quoted using standard CSV quoting, matching the export format: a field containing a `;`, double quote, CR, or LF is double-quoted, and an embedded double quote is written doubled (`""`).
- Empty fields are preserved in any column position (for example, an empty `Notes` between `Graphic` and `Tags`).
- A leading header row is optional; the importer recognizes and ignores the header if it identifies the column headings.
- `Title` is the only mandatory column. A row without a title is an error and the import must not create that row.
- `Tags` is comma-separated within its column.

## Capabilities

### New Capabilities
- `item-csv-import`: Defines the item CSV format (columns, semi-colon delimiter, standard double-quote quoting matching the exporter, empty fields preserved, optional header, comma-separated tags), sample-file export, the **niche/group context-menu `Import…` entry**, the file picker and import dialog (raw source editor + read-only preview + syntax check with a detailed `Line N: <reason>` error), and creation of imported Design items at the selected niche/group with correct stage, inherited-context, and metadata mapping.

### Modified Capabilities
<!-- None. The new `Import…` context-menu entry on niche and group rows is owned by the new `item-csv-import` capability, so no existing capability's requirements change. -->

## Impact

- **Domain/Application:** New item CSV parsing/validation (a bespoke semi-colon delimiter + `;;` escaping parser, not the Snowclone `TextFieldParser` codec), row→item field mapping, and a bulk import use case that reuses the existing item creation and tag-mapping services.
- **App (UI):** New `ItemImportWindow` + view model, added `Import…` menu item and handler in the tree context menu, support for selecting a **niche** node as the import destination (existing context helpers only handle group/item selection), and a file picker abstraction following the Snowclone CSV picker pattern.
- **Integration:** A small item CSV codec (read/write) alongside the existing Snowclone CSV codec pattern; no database schema change (items persist through the existing `WorkspaceSnapshot` save path).
- **Dependencies:** None new. Reuses existing `ItemManagementService`, `ItemInspectorService` tag resolution, and `IWorkspaceRepository`.

### Non-Goals
- Full export of existing items (a separate module; only **sample-file** export is in scope here).
- Editing, deleting, or overwriting existing items; deduplication; workspace-level transfer.
- Live-file importing beyond the CSV format defined here.

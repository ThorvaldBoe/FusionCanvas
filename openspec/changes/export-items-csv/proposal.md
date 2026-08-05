## Why

There is currently no way to export items to CSV (GitHub issue #119). Print-on-Demand creators need to pull item data out of FusionCanvas into spreadsheets or other tools, but today the only way to work with an item's data is inside the application. Users should be able to export a group's items to a portable CSV file from the navigation tree.

## What Changes

- Adds an `Export to CSV...` action to the **group and niche row context menus** in the navigation tree.
- Selecting it writes a semi-colon-delimited CSV file (UTF-8) containing the items under that row:
  - For a **group**: the items directly in that group plus items in all descendant (nested) subgroups.
  - For a **niche**: the items in all of its descendant groups and subgroups, plus items attached directly to the niche.
- Excludes **archived** items and **empty** items (items with no title and no creative content) from the export.
- CSV columns: `Title`, `Base Idea`, `Concept Idea`, `Phrase`, `Graphic`, `Notes`, `Tags`.
- Uses **standard CSV field quoting**: a field is quoted when it contains `;`, `"`, `\r`, or `\n`, and embedded double-quotes are doubled. This preserves the multi-line creative fields (Notes, Base Idea, Concept Idea, Graphic) and keeps exports opening correctly in spreadsheets.
- A standard `Save file` dialog lets the user choose the destination path.

## Capabilities

### New Capabilities
- `items-csv-export`: Covers exporting a group's or niche's items to a semi-colon-delimited CSV file, including the menu entry, export scope (subtree, exclusions), column layout, and CSV escaping.

### Modified Capabilities
- `group-management`: The accepted "Group rows expose contextual management actions" requirement enumerates the offered context actions (New group, Rename, Copy, Cut, Paste, Delete). This change adds `Export to CSV...` to the group context menu, so a MODIFIED delta adds that action to the scenario. (Niche rows have no accepted context-menu enumeration, so `niche-management` needs no delta.)

## Impact

- **Domain**: none. Reuses `Item`, `Item.MetadataJson` via `ItemMetadataCodec`, `GroupHierarchy.GetDescendants`, `Item.Tags`/`ItemTag`/`Tag`, and the existing `Item.NicheId`/`Item.GroupId` membership model.
- **Application**: a new item-export service (use case) that, given a `WorkspaceSnapshot`, a topic id, and its topic-kind (niche or group), resolves the export target set and projects each item into row fields. Reuses the `ItemMetadataCodec` and tag-resolution patterns already used by `ItemInspectorService`.
- **Integration**: a new CSV write codec (mirroring `SnowcloneCsvCodec`) that emits the semi-colon-delimited rows with standard field quoting, plus a file-picker abstraction following the `ISnowcloneCsvFilePicker` / `AvaloniaSnowcloneCsvFilePicker` pattern.
- **App**: adds the `Export to CSV...` menu items to the niche and group rows in `MainWindow.axaml` and their handlers in `MainWindow.axaml.cs`, wiring into a new export command on `WorkspaceTreeViewModel`.
- **Tests**: domain rules, application projection/escaping, integration CSV codec, and Avalonia headless view tests for the new context-menu entries.
- **Label note**: the originating issue was tagged `Plugins and integrations` as the closest available label; the actual surface is navigation/tree and is being tracked here.

## MODIFIED Requirements

### Requirement: Group rows expose contextual management actions
FusionCanvas SHALL provide a context menu when the user right-clicks an active group row.

#### Scenario: User opens a group context menu
- **WHEN** the user right-clicks an active group
- **THEN** FusionCanvas selects that group and offers New group, Rename, Copy, Cut, Paste, Delete, and Export to CSV... actions
- **AND** New group creates a direct child of the clicked group
- **AND** Paste reflects whether the application clipboard currently contains a group operation
- **AND** Export to CSV... exports the group's items to a semi-colon-delimited CSV file as specified by the `items-csv-export` capability

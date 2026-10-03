## MODIFIED Requirements

### Requirement: Mockup Template source-image table supports transient multi-selection

The Mockup Template source-image table SHALL support selecting one or more visible local source-image rows for the duration of the draft. A plain click or Enter/Space SHALL replace the selection with the activated row. Ctrl-click or Ctrl+Enter/Space SHALL toggle the activated row. Shift-click or Shift+Enter/Space SHALL select the inclusive visible range from the selection anchor to the activated row. The most recently activated selected row SHALL remain the active row for the existing source-image detail editor.

#### Scenario: User selects multiple source images

- **WHEN** the user activates a source-image row with Ctrl and then activates another row with Ctrl
- **THEN** both rows are visibly selected
- **AND** the most recently activated row remains the active detail-editor row
- **AND** the selected count is visible and exposed to assistive technology

#### Scenario: User selects a contiguous range

- **WHEN** the user activates an anchor row and then activates another visible row with Shift
- **THEN** every visible source-image row between the anchor and activated row is selected
- **AND** the active detail-editor row is the activated row

#### Scenario: User archives selected source images

- **WHEN** one or more source-image rows are selected and the user invokes Archive selected
- **THEN** every selected row is removed or queued for archive using the existing source-image save workflow
- **AND** unselected rows remain visible and unmodified
- **AND** the remaining selected or visible row becomes active, or the detail editor becomes empty when none remain

#### Scenario: User uses keyboard multi-selection

- **WHEN** a focused source-image row receives Enter or Space with the documented Ctrl or Shift modifier
- **THEN** the table applies the same selection rule as the corresponding pointer gesture
- **AND** the row exposes its current selected state through its accessible item status

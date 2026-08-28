## ADDED Requirements

### Requirement: Design Area management uses a focused guarded editor dialog
FusionCanvas SHALL keep the default Manage Design Areas surface focused on the Offering-scoped collection without reserving an inline editor column. The **Add Design Area** action and each row's **Edit** action SHALL open the same modal dialog, owned by the Store Editor, with mode-specific title and draft values. The dialog SHALL reuse existing Design Area validation, compatibility, persistence, and referenced-record behavior; SHALL close only after successful save or confirmed cancellation; and SHALL not permit a workspace or Offering context change to leave a stale editable dialog open.

#### Scenario: User reviews the Design Area collection
- **WHEN** Manage Design Areas is open without an Add/Edit dialog
- **THEN** the collection uses the available management-surface width
- **AND** no inline Design Area editor is rendered or reserves space

#### Scenario: User adds a Design Area
- **WHEN** the user selects **Add Design Area** for an editable Offering with Variants
- **THEN** FusionCanvas opens one modal dialog titled **Add Design Area** with empty/default form values
- **AND** places initial focus in the Name field
- **AND** leaves the parent collection and Offering context visible but unavailable behind the modal dialog

#### Scenario: User edits a Design Area
- **WHEN** the user selects **Edit** for a Design Area
- **THEN** FusionCanvas opens the same modal dialog titled **Edit Design Area**
- **AND** populates identity, maximum design size, artwork guidance, compatibility, and advanced provider data from that stable Design Area identity

#### Scenario: Save fails validation or persistence
- **WHEN** the user attempts to save an invalid draft or persistence reports a recoverable failure
- **THEN** the dialog remains open with the draft values and guidance preserved
- **AND** confirmed Design Area data remains unchanged

#### Scenario: Save succeeds
- **WHEN** the user saves a valid Add or Edit draft successfully
- **THEN** FusionCanvas persists the Design Area exactly once through the existing service path
- **AND** closes the dialog, refreshes/selects the saved Design Area, and returns focus to the invoking Add/Edit control when practical

#### Scenario: User dismisses an unchanged draft
- **WHEN** the user selects **Cancel**, presses Escape, or closes the dialog without meaningful changes
- **THEN** FusionCanvas closes the dialog without persisting anything
- **AND** returns focus to the invoking Add/Edit control

#### Scenario: User dismisses a meaningful draft
- **WHEN** the user selects **Cancel**, presses Escape, or closes the dialog after making meaningful unsaved changes
- **THEN** FusionCanvas asks whether to discard the draft or keep editing
- **AND** keep-editing preserves all values and focus within the dialog
- **AND** confirmed discard closes the dialog without persisting the draft

#### Scenario: Editing context becomes stale
- **WHEN** the active Offering or workspace changes while the Design Area dialog is open
- **THEN** FusionCanvas closes the dialog and discards the stale draft without persisting it to another context

#### Scenario: Dialog is used with keyboard and supported sizes
- **WHEN** the Design Area dialog is opened or resized within supported normal and narrow dimensions
- **THEN** its descriptive title, accessible form controls, predictable keyboard traversal, scrollable content, Save/Cancel actions, and close behavior remain usable without clipping required fields


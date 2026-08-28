## ADDED Requirements

### Requirement: Mockup Template management uses a focused guarded editor dialog
FusionCanvas SHALL keep the default Offering-scoped Mockup Template management surface focused on its template collection without reserving an inline editor region. The **Add Mockup Template** action and each template's Edit action SHALL open the same Store Editor-owned modal dialog with a mode-specific title and draft values. The dialog SHALL preserve the existing preview-first provider-image placement workflow, catalog validation, Color/Design Area relationships, revision and persistence behavior, and archived-store read-only policy; SHALL close only after successful save or confirmed cancellation; and SHALL not permit workspace or Offering context changes to leave a stale editable dialog open.

#### Scenario: User reviews the Mockup Template collection
- **WHEN** Mockup Template management is open without an Add/Edit dialog
- **THEN** the Offering-scoped collection uses the available management surface
- **AND** no inline Mockup Template editor is rendered or reserves space
- **AND** one clear **Add Mockup Template** action is available only when editing is allowed and a Design Area exists

#### Scenario: User adds a Mockup Template
- **WHEN** the user selects **Add Mockup Template** for an editable Offering with a Design Area
- **THEN** FusionCanvas opens one modal dialog titled **Add Mockup Template** with a new draft
- **AND** places sensible initial focus in the template identity/configuration flow
- **AND** leaves the parent collection and Offering context visible but unavailable behind the modal

#### Scenario: User edits a Mockup Template
- **WHEN** the user selects an existing template's Edit action
- **THEN** FusionCanvas opens the same modal titled **Edit Mockup Template**
- **AND** populates its stable identity, provider image, target Design Area, Color applicability, image-space mapping, advanced provider data, and revision context

#### Scenario: Preview-first mapping remains available
- **WHEN** a template draft has a selectable provider image
- **THEN** the dialog gives the provider image and visual placement editor prominent space
- **AND** keeps synchronized numeric mapping and supporting configuration reachable at supported normal and narrow sizes

#### Scenario: Save fails validation or persistence
- **WHEN** the user attempts to save an invalid draft or persistence reports a recoverable failure
- **THEN** the dialog remains open with draft values, placement, and guidance preserved
- **AND** confirmed template data and revisions remain unchanged

#### Scenario: Save succeeds
- **WHEN** the user saves a valid Add or Edit draft successfully
- **THEN** FusionCanvas persists the template exactly once through the existing service path
- **AND** closes the dialog, refreshes/selects the saved template, and returns focus to the invoking Add/Edit control when practical

#### Scenario: User dismisses an unchanged draft
- **WHEN** the user selects **Cancel**, presses Escape, or closes the dialog without meaningful changes
- **THEN** FusionCanvas closes the dialog without persisting anything
- **AND** returns focus to the invoking Add/Edit control

#### Scenario: User dismisses a meaningful draft
- **WHEN** the user selects **Cancel**, presses Escape, or closes the dialog after making meaningful unsaved changes
- **THEN** FusionCanvas asks whether to discard the draft or keep editing
- **AND** keep-editing preserves all values and placement within the dialog
- **AND** confirmed discard closes without persisting the draft

#### Scenario: Editing context becomes stale
- **WHEN** the active Offering or workspace changes while the Mockup Template dialog is open
- **THEN** FusionCanvas closes the dialog and discards the stale draft without persisting it to another context

#### Scenario: Archived store is reviewed
- **WHEN** Mockup Template management belongs to an archived Store
- **THEN** Add, Edit, placement, and Save remain unavailable
- **AND** no editable template dialog can be opened

#### Scenario: Dialog is used with keyboard and supported sizes
- **WHEN** the Mockup Template dialog is opened or resized within supported normal and narrow dimensions
- **THEN** its descriptive title, accessible controls, predictable keyboard traversal, scrollable content, Save/Cancel actions, and close behavior remain usable without clipping required configuration

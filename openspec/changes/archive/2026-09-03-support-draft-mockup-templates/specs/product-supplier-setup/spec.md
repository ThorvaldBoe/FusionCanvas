## MODIFIED Requirements

### Requirement: Provider mockup image selection communicates source and recovery
FusionCanvas SHALL identify optional provider-assisted Mockup Template image selection with a persistent visible label and accessible name and SHALL explain that candidates, when available, come from the active Offering's provider catalog. The guidance SHALL remain available while candidates load and when the result is available, empty, unavailable, or failed. It SHALL NOT imply that local upload or drag/drop is supported by this module. Empty, unavailable, or failed provider states SHALL explain the state without treating provider setup or synchronization as a prerequisite for saving a Draft.

#### Scenario: User opens optional provider image selection
- **WHEN** the Mockup Template editor is shown
- **THEN** the selector has the visible label and accessible name **Provider mockup image (optional)**
- **AND** nearby instructions explain that provider candidates may help complete readiness
- **AND** state that a Draft can be saved without a provider image and that local upload and drag/drop are not available in this module

#### Scenario: Provider catalog is loading
- **WHEN** provider mockup candidates are being requested
- **THEN** the persistent guidance remains visible
- **AND** state text explains that provider-catalog images are loading without disabling Draft persistence

#### Scenario: Provider catalog provides candidates
- **WHEN** one or more provider mockup candidates are available
- **THEN** the selector exposes those candidates
- **AND** state text prompts the user to choose the provider view that matches the target Design Area when completing readiness

#### Scenario: Provider catalog is empty
- **WHEN** the configured provider catalog is available but contains no mockup images for the Offering
- **THEN** state text distinguishes the empty result from loading and failure
- **AND** explains that the template can be saved as a Draft and completed later

#### Scenario: Provider catalog is unavailable
- **WHEN** no provider catalog source exists or the source reports that it is unavailable
- **THEN** state text explains the supplied reason when available
- **AND** explains that provider integration is optional and does not block saving a Draft

#### Scenario: Provider catalog request fails
- **WHEN** loading provider mockup candidates raises an error
- **THEN** state text identifies the recoverable load failure without exposing a fabricated candidate
- **AND** preserves manual Draft creation and editing

### Requirement: Mockup Template management uses a focused guarded editor dialog
FusionCanvas SHALL keep the default Offering-scoped Mockup Template management surface focused on its template collection without reserving an inline editor region. The **Add Mockup Template** action and each template's Edit action SHALL open the same Store Editor-owned modal dialog with a mode-specific title and draft values. The dialog SHALL preserve the preview-first placement workflow when an image exists, optional provider assistance, Draft/Ready feedback, catalog validation, Color/Design Area relationships, revision and persistence behavior, and archived-store read-only policy; SHALL close only after successful save or confirmed cancellation; and SHALL not permit workspace or Offering context changes to leave a stale editable dialog open.

#### Scenario: User reviews the Mockup Template collection
- **WHEN** Mockup Template management is open without an Add/Edit dialog
- **THEN** the Offering-scoped collection uses the available management surface
- **AND** no inline Mockup Template editor is rendered or reserves space
- **AND** one clear **Add Mockup Template** action is available whenever the Offering is editable, including when no Design Area or provider image exists

#### Scenario: User adds a Mockup Template
- **WHEN** the user selects **Add Mockup Template** for an editable Offering
- **THEN** FusionCanvas opens one modal dialog titled **Add Mockup Template** with a new Draft
- **AND** places initial focus in the Name field
- **AND** leaves the parent collection and Offering context visible but unavailable behind the modal

#### Scenario: User edits a Mockup Template
- **WHEN** the user selects an existing template's Edit action
- **THEN** FusionCanvas opens the same modal titled **Edit Mockup Template**
- **AND** populates its stable identity, current Draft/Ready state, available provider image, target Design Area, Color applicability, image-space mapping, advanced data, and revision context without fabricating absent values

#### Scenario: Preview-first mapping is conditionally available
- **WHEN** a template draft has a selected usable image
- **THEN** the dialog gives the image and visual placement editor prominent space
- **AND** keeps synchronized numeric mapping and supporting configuration reachable at supported normal and narrow sizes

#### Scenario: No image is configured
- **WHEN** a template draft has no selected image
- **THEN** the dialog shows a compact no-image state without an editable placement rectangle
- **AND** keeps Name, optional Design Area and Colors, readiness feedback, and Save reachable

#### Scenario: Save eligibility changes
- **WHEN** Name, provider image, Design Area, Color selection, mapping value, busy state, or read-only state changes
- **THEN** the Save command re-evaluates immediately
- **AND** remains enabled for an editable named Draft with omitted readiness inputs
- **AND** remains disabled for a blank name, invalid supplied mapping, busy operation, read-only Store, or stale Offering context

#### Scenario: Draft readiness is explained
- **WHEN** the current template is not Ready for use
- **THEN** the dialog identifies it as **Draft**
- **AND** displays every unmet readiness requirement in concise inline guidance
- **AND** does not use disabled Save state as the only feedback

#### Scenario: Ready template is explained
- **WHEN** the current template satisfies every readiness requirement
- **THEN** the dialog identifies it as **Ready for use**
- **AND** removes resolved blockers without hiding unrelated validation or persistence errors

#### Scenario: Save fails validation or persistence
- **WHEN** the user attempts to save invalid supplied configuration or persistence reports a recoverable failure
- **THEN** the dialog remains open with draft values, placement, readiness guidance, and an in-dialog error preserved
- **AND** confirmed template data and revisions remain unchanged

#### Scenario: Save succeeds
- **WHEN** the user saves a valid named Draft or Ready template successfully
- **THEN** FusionCanvas persists the template exactly once through the application service path
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
- **THEN** FusionCanvas closes the dialog and discards the stale editor draft without persisting it to another context

#### Scenario: Archived store is reviewed
- **WHEN** Mockup Template management belongs to an archived Store
- **THEN** Add, Edit, placement, and Save remain unavailable
- **AND** no editable template dialog can be opened

#### Scenario: Dialog is used with keyboard and supported sizes
- **WHEN** the Mockup Template dialog is opened or resized within supported normal and narrow dimensions
- **THEN** its descriptive title, accessible controls, predictable keyboard traversal, scrollable content, readiness guidance, Save/Cancel actions, and close behavior remain usable without clipping required configuration


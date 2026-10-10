## Purpose

Defines the Offering-scoped Mockup Template management surface and editing routes.

## Requirements

### Requirement: Mockup Templates connect provider images to one Design Area
FusionCanvas SHALL provide focused management of Mockup Templates for one Blueprint Offering. Each template SHALL identify a provider-catalog mockup image, one authoritative target Design Area from the same Offering, and applicable Variant coverage derived through the existing color-level template binding and Design Area compatibility rules.

#### Scenario: User opens Mockup Template management
- **WHEN** the user opens Mockup Template management for a Blueprint Offering
- **THEN** FusionCanvas lists only templates belonging to that Offering
- **AND** each template summary identifies its name, target Design Area, applicable Color or Variant summary, and lifecycle state
- **AND** the selected template opens in a focused editor

#### Scenario: Mockup Template management preserves master-detail composition
- **WHEN** Mockup Template management has one or more records or an active draft
- **THEN** FusionCanvas presents the template collection and one focused selected-or-new editor as visually distinct peer regions
- **AND** makes the provider mockup image and visual Design Area mapping prominent within the editor
- **AND** keeps identity, Design Area, Color applicability, numeric mapping, advanced provider data, and save actions grouped around that editor
- **AND** may stack the list and editor only when available width requires it

#### Scenario: User creates a template from a provider-catalog image
- **WHEN** the user chooses an available provider-catalog mockup image, a Design Area from the same Offering, and valid color-level applicability
- **THEN** FusionCanvas creates a template draft linked to that image and authoritative Design Area identity
- **AND** derives compatible concrete size/color Variants rather than persisting per-size template overrides

#### Scenario: Target Design Area is incompatible
- **WHEN** the selected Design Area does not cover every concrete Variant implied by the template's color-level applicability
- **THEN** FusionCanvas rejects confirmation and identifies the incompatible Variants
- **AND** never silently accepts a partially compatible template

#### Scenario: Offering has no Design Areas
- **WHEN** the user opens template management before any Design Area exists
- **THEN** FusionCanvas shows a blocked empty state explaining that a Design Area is required
- **AND** provides a route back to Design Area management without fabricating a target

### Requirement: Mockup Template Color applicability choices are searchable
FusionCanvas SHALL provide a transient search text box for eligible Color applicability choices in the focused Mockup Template editor. As the user types, visible choices SHALL be filtered by case-insensitive substring matching against each choice's displayed label, while the underlying eligible choice set and selection state remain unchanged.

#### Scenario: User searches Color applicability choices
- **WHEN** the user enters text in the Color search box
- **THEN** the editor shows only eligible Color choices whose displayed labels contain the query using case-insensitive substring matching
- **AND** the remaining choices retain their existing order

#### Scenario: User clears the Color search
- **WHEN** the Color search query is empty or contains only whitespace
- **THEN** the editor shows every eligible Color choice in its existing order
- **AND** each choice retains its prior selected or unselected state

#### Scenario: Search has no matching Colors
- **WHEN** the Color search query matches no eligible Color choice
- **THEN** the editor shows no Color checkboxes
- **AND** it displays clear guidance that no Colors match the search
- **AND** the editor continues to allow the user to edit or clear the query

#### Scenario: Hidden selected Colors remain part of the draft
- **WHEN** a selected Color is hidden by the active search query
- **THEN** the Color remains selected in the underlying draft
- **AND** saving, readiness evaluation, dirty-state tracking, and source-image applicability continue to include that selected Color
- **AND** clearing the query shows the Color again as selected

#### Scenario: Color search is not persisted
- **WHEN** the user closes the Mockup Template editor and later opens a Mockup Template editor session
- **THEN** the Color search query starts empty
- **AND** persisted template configuration contains no Color search text

### Requirement: Each template revision owns an image-space Design Area mapping
FusionCanvas SHALL store the mapping of the selected Design Area into the specific mockup image as revisioned template configuration with image-space X, Y, width, and height values. The mapping SHALL be editable through a visual placement rectangle, while numeric values remain visible and editable as supporting technical controls.

#### Scenario: User positions a Design Area visually
- **WHEN** the user moves or resizes the visual placement rectangle over the selected mockup image
- **THEN** FusionCanvas updates the draft X, Y, width, and height values in the image's pixel coordinate space
- **AND** keeps the rectangle and numeric values synchronized

#### Scenario: User edits numeric mapping values
- **WHEN** the user enters valid X, Y, width, or height values
- **THEN** FusionCanvas updates the visual rectangle to represent the same image-space mapping
- **AND** does not apply artwork or render a composite

#### Scenario: Mapping exceeds image bounds
- **WHEN** the draft mapping has non-positive size or extends outside the known mockup image bounds
- **THEN** FusionCanvas blocks confirmation with recoverable guidance
- **AND** preserves the last confirmed template revision

#### Scenario: User changes confirmed template mapping
- **WHEN** the user saves a changed provider image, target Design Area, applicability, or image-space mapping
- **THEN** FusionCanvas creates or records a new template revision according to the authoritative revision lifecycle
- **AND** prior generated outputs remain attributable to their original revision

### Requirement: Provider mockup references are advanced technical data
FusionCanvas SHALL preserve an optional stable provider mockup reference as advanced technical data normally populated from Printify integration data, distinct from the user-facing template name and the actual fulfillment Provider identity.

#### Scenario: Imported mockup image has a provider reference
- **WHEN** Printify supplies a provider-catalog mockup image and stable reference
- **THEN** FusionCanvas preserves the reference for synchronization or diagnostics
- **AND** exposes it through Advanced or secondary disclosure rather than as the template's primary label

#### Scenario: Provider reference changes display context
- **WHEN** user-facing labels or Provider display names change
- **THEN** the template retains its stable provider mockup reference and Design Area identity
- **AND** relationships do not depend on mutable labels

### Requirement: Template drafts are explicit and scoped
FusionCanvas SHALL keep new or edited Mockup Template state as a draft until explicitly confirmed and SHALL guard meaningful changes during selection or navigation transitions.

#### Scenario: User cancels a template draft
- **WHEN** the user cancels a new or edited template before confirmation
- **THEN** FusionCanvas persists no draft mapping or partial template revision
- **AND** returns focus to the invoking template or add action

#### Scenario: User leaves with unsaved template changes
- **WHEN** the user attempts to select another template or leave the surface with meaningful unsaved changes
- **THEN** FusionCanvas offers to discard the changes or keep editing
- **AND** keep-editing preserves the selected template, draft mapping, and focus

#### Scenario: User reviews an archived Store
- **WHEN** Mockup Template management is opened for an archived Store
- **THEN** FusionCanvas presents templates and mapping data read-only
- **AND** disables image selection, mapping edits, and lifecycle mutations

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

### Requirement: Mockup Template management presents a searchable aligned table
FusionCanvas SHALL present the active Mockup Templates for the selected Blueprint Offering in a bounded, aligned virtual table. The table SHALL show each template's name, target Design Area, Color applicability, compatible Variant summary, and current revision/readiness status. A transient search field SHALL filter those displayed values using case-insensitive substring matching, preserve the existing collection order, and SHALL NOT persist the query. Sorting is not required. Truncated cell content SHALL remain available through full-text tooltips, accessible names, and a selected-template detail area.

#### Scenario: User scans Mockup Template rows
- **WHEN** the user opens Mockup Template management for an Offering with active templates
- **THEN** the table shows only that Offering's active templates with aligned columns for name, Design Area, Colors, compatible Variants, and revision/readiness
- **AND** the collection remains bounded and vertically scrollable when its rows exceed the viewport
- **AND** selecting a row shows its complete summary and the Edit, Duplicate, and Archive actions for that template

#### Scenario: User searches displayed template details
- **WHEN** the user enters a nonblank search query
- **THEN** the table shows templates whose displayed name, Design Area, Color, Variant, or revision/readiness text contains the query case-insensitively
- **AND** matching rows retain their original collection order
- **AND** filtering does not change the underlying templates or their actions
- **AND** if the selected template is filtered out, its selection and contextual actions clear

#### Scenario: User clears search or receives no matches
- **WHEN** the search query is empty or contains only whitespace
- **THEN** every active template for the selected Offering is shown in its existing order
- **WHEN** a nonblank query matches no active template
- **THEN** the table shows no rows and clear no-match guidance with the query still available to edit

#### Scenario: User reads a long summary
- **WHEN** a Design Area, Color, Variant, or revision/readiness value does not fit in its table cell
- **THEN** the cell may truncate its visible text
- **AND** the full value remains available through its tooltip and accessible name
- **AND** selecting its row exposes the complete summary in the detail area

#### Scenario: Offering has no Design Areas
- **WHEN** the user opens template management before any Design Area exists
- **THEN** the existing blocked guidance and route to Design Area management remain visible
- **AND** search and table presentation do not imply that a template can be completed without a Design Area

#### Scenario: Search text is not persisted
- **WHEN** the user leaves template management or changes the selected Offering
- **THEN** the transient query is cleared
- **AND** no search value is saved with catalog or template data

### Requirement: Mockup Template table actions remain accessible
FusionCanvas SHALL provide an accessible Edit, Duplicate, and Archive action for the selected active Mockup Template. Selecting a row by pointer or keyboard SHALL make it the action target. Edit SHALL open the existing focused editor for that template; Duplicate SHALL use the existing duplication workflow; and actions SHALL be disabled when no template is selected or the selected Store or Offering is read-only or busy.

#### Scenario: User edits or duplicates the selected template
- **WHEN** the user selects an active table row and invokes Edit or Duplicate from its contextual actions
- **THEN** the existing Edit navigation or Duplicate workflow receives that row's template identity
- **AND** no other row is modified or selected by the action

#### Scenario: User selects a template with a keyboard or assistive technology
- **WHEN** the user navigates to a table row and selects it with the keyboard or assistive technology
- **THEN** the selected row and its full summary are announced
- **AND** each contextual action exposes a name that identifies both the action and its Mockup Template
- **AND** the action buttons remain reachable in predictable Tab order

### Requirement: Mockup Template archive requires explicit confirmation
FusionCanvas SHALL require explicit confirmation before archiving an active Mockup Template. The confirmation SHALL identify the template, explain that it leaves the active list and is unavailable for new mockup generation, and state that its saved revisions remain retained. Cancel, Escape, or window dismissal SHALL leave the template unchanged. Confirm SHALL invoke the existing reversible soft-archive workflow.

#### Scenario: User cancels archive confirmation
- **WHEN** the user invokes Archive for an active template and cancels, presses Escape, or dismisses the confirmation
- **THEN** the confirmation closes and focus returns to the selected template's Archive action
- **AND** the template remains active and its revisions remain unchanged

#### Scenario: User confirms archive
- **WHEN** the user confirms archive for the named active template
- **THEN** the existing archive service marks only that template and its existing applicability records archived
- **AND** the template leaves the active table and is no longer eligible for new mockup generation
- **AND** its retained configuration and revision history remain available according to the existing archive lifecycle

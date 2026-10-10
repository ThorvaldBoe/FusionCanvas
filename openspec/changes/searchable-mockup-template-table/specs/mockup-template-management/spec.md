## ADDED Requirements

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

## ADDED Requirements

### Requirement: UI descriptions use a versioned declarative source format
The prototype SHALL accept a UTF-8 YAML document with an explicit schema version, one screen definition, one viewport, one rooted component hierarchy, and zero or more named state projections. The parser SHALL reject unknown properties rather than silently ignoring authoring mistakes.

#### Scenario: Supported description is loaded
- **WHEN** a developer supplies a syntactically valid description using the supported schema version and vocabulary
- **THEN** the tool loads one screen definition and makes it available for validation or rendering

#### Scenario: Unsupported schema version is supplied
- **WHEN** a description declares a schema version the tool does not support
- **THEN** validation fails with a diagnostic that identifies the unsupported version
- **AND** no wireframe is produced

#### Scenario: Unknown property is supplied
- **WHEN** a description contains an unrecognized property at any schema-defined location
- **THEN** validation fails with a diagnostic that identifies the property and its source location

### Requirement: The initial vocabulary expresses bounded UI structure and layout
The prototype SHALL provide only the semantic tokens, container kinds, leaf component kinds, variants, and layout constraints required to describe the approved Issue #185 Variant Management and Design Areas wireframes. Layout SHALL use hierarchy and explicit `content`, `fill`, or device-independent fixed sizing rather than absolute child coordinates.

#### Scenario: Complementary wireframe hierarchies are described
- **WHEN** the example descriptions define the Variant Management and Design Areas screens
- **THEN** Variant Management expresses its heading, available-choice cards, dividing hierarchy, sellable-variant command row, and tabular records as ordered regions
- **AND** Design Areas expresses its collection cards and focused editor as a fixed-plus-flexible master-detail hierarchy
- **AND** it does not require absolute child coordinates

#### Scenario: Named layout tokens and variants are used
- **WHEN** the example assigns spacing, padding, typography, surface, or command presentation
- **THEN** it references a supported semantic token or component variant with renderer-defined meaning
- **AND** unsupported tokens or variants fail validation

#### Scenario: Invalid sizing combination is supplied
- **WHEN** a component declares mutually incompatible, negative, non-finite, or unsupported sizing constraints
- **THEN** validation fails before layout begins
- **AND** the diagnostic identifies the component and invalid constraint

### Requirement: Semantic validation is deterministic and actionable
The prototype SHALL validate identity, hierarchy, layout, token, component, and state-reference rules before rendering. For the same invalid source, diagnostics SHALL have stable codes, severity, source locations, and ordering.

#### Scenario: Duplicate component identifiers are supplied
- **WHEN** two components in one screen use the same identifier
- **THEN** validation fails with a duplicate-identifier diagnostic that identifies both relevant locations

#### Scenario: Invalid component composition is supplied
- **WHEN** a leaf component contains children or a container violates its allowed child or placement rules
- **THEN** validation fails with a diagnostic identifying the invalid composition

#### Scenario: Invalid state override is supplied
- **WHEN** a named state targets an unknown component or changes a property that is not overridable for that component kind
- **THEN** validation fails with a diagnostic identifying the state, target, and invalid override

#### Scenario: Invalid source is validated repeatedly
- **WHEN** the same invalid source is validated more than once with the same tool version
- **THEN** the emitted diagnostics have identical codes, severity, locations, and order

### Requirement: Named states produce controlled wireframe projections
The prototype SHALL allow a named state to override only the supported visibility, enabled presentation, literal text, and representative repeated-item content of existing components. A state SHALL NOT add, remove, reparent, or change the kind of a component.

#### Scenario: Named state is rendered
- **WHEN** a developer renders a valid description with a declared state name
- **THEN** the renderer applies that state's supported overrides to the validated base hierarchy
- **AND** preserves component identity, kind, and parent-child structure

#### Scenario: Unknown state is requested
- **WHEN** a developer requests a state name not declared by the description
- **THEN** rendering fails with an unknown-state diagnostic
- **AND** no wireframe is produced

### Requirement: SVG wireframe rendering is deterministic and self-contained
The prototype SHALL render a valid screen and named state to a standalone SVG using invariant numeric formatting, stable element and attribute ordering, stable line endings, renderer-owned layout metrics, and no timestamps, machine paths, external assets, network calls, AI interpretation, or interactive desktop services.

#### Scenario: Identical input is rendered repeatedly
- **WHEN** the same valid source, state, viewport, and tool version are rendered more than once
- **THEN** the resulting SVG files are byte-identical

#### Scenario: Wireframe is inspected offline
- **WHEN** a generated SVG is opened without network access
- **THEN** it contains the complete structural wireframe and human-readable component identifiers needed to relate rendered regions to source components
- **AND** it does not require external fonts, images, stylesheets, or scripts

#### Scenario: Fixed and flexible regions are arranged
- **WHEN** the Design Areas fixture is rendered at its declared viewport
- **THEN** the collection panel receives its declared fixed width
- **AND** the editor panel receives the remaining width after declared padding and gaps
- **AND** all visible component bounds remain within the viewport

### Requirement: Developers can validate and render descriptions through a repository-local command
The prototype SHALL provide repository-local `validate` and `render` commands that run with the documented .NET toolchain, accept explicit input and output paths, report diagnostics to standard error, and use stable success and failure exit behavior.

#### Scenario: Developer validates a valid description
- **WHEN** the developer runs the documented validation command for a valid source
- **THEN** the command exits successfully without modifying the source or production application files

#### Scenario: Developer renders a valid state
- **WHEN** the developer runs the documented render command with a valid source, declared state, and output path
- **THEN** the command writes the deterministic SVG to the requested path and exits successfully

#### Scenario: Developer renders invalid input
- **WHEN** parsing, validation, state selection, layout, or output writing fails
- **THEN** the command exits unsuccessfully with an actionable diagnostic
- **AND** it does not leave a partial output file

### Requirement: Complementary reference fixtures prove composition without changing production UI
The prototype SHALL include checked-in UI descriptions and reproducible SVG outputs for the approved Issue #185 Variant Management and Design Areas wireframes. It SHALL preserve local copies of the source wireframes as comparison references and SHALL include at least one narrow alternate-state projection without changing either base hierarchy. The module SHALL NOT replace, generate, or modify the production Issue #185 Avalonia views.

#### Scenario: Fixture outputs are reproduced
- **WHEN** the checked-in fixture states are rendered with the documented command
- **THEN** their outputs match the checked-in expected SVG files
- **AND** automated structural assertions cover the required regions, ordering, master-detail relationship, table structure, states, component identifiers, and action variants

#### Scenario: Generated composition is compared with the source wireframe
- **WHEN** each default SVG is reviewed beside its preserved source wireframe
- **THEN** the same major regions, ordering, grouping, relative prominence, and list-or-editor relationships remain recognizable
- **AND** any visible difference outside the explicitly non-pixel-perfect boundary is documented before the prototype is accepted

#### Scenario: Production boundary is reviewed
- **WHEN** the module's changed files are inspected
- **THEN** no production Avalonia view, view model, command, application workflow, persistence model, or plugin contract is changed by the prototype

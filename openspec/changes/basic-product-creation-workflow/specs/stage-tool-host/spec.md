## MODIFIED Requirements

### Requirement: Built-in tools use the host registration path
Built-in Idea, Concept, Design, and Listing tools SHALL be registered and rendered through the same Stage Tool Host registry used to query available stage tools, while shared Item chrome remains outside the tool.

#### Scenario: Basic built-in tool is registered
- **WHEN** an Item and active view stage are available with no external plugin tools
- **THEN** the matching basic built-in Stage Tool is discovered and rendered by the host
- **AND** receives explicit Item, workflow, navigation, and inherited context

#### Scenario: Shared Item chrome is rendered
- **WHEN** a built-in Stage Tool is active
- **THEN** Overview, Notes, Tags, Related assets, and lifecycle controls remain owned by the surrounding Item document surface
- **AND** are not duplicated inside the tool

#### Scenario: Later plugin tools are added
- **WHEN** a future plugin contributes a matching tool
- **THEN** the host evaluates built-in and contributed tools through the same availability model
- **AND** the basic built-in tool remains a fallback


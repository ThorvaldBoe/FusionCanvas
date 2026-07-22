## ADDED Requirements

### Requirement: Tool context exposes the selected Item under universal terminology
Context-aware and Stage Tools SHALL receive the selected Item's stable identity, current stage, active view stage, status, topic path, approved metadata, and available capabilities through application-owned context rather than legacy Listing names or UI scraping.

#### Scenario: Built-in Stage Tool opens for an Item
- **WHEN** the host activates a basic Idea, Concept, Design, or Listing tool
- **THEN** the tool receives explicit selected Item and workflow context
- **AND** can determine read-only, missing-file, and available-action state through application contracts

#### Scenario: No Item is selected
- **WHEN** a tool requires Item context but only a topic is selected
- **THEN** the host does not run the Item tool
- **AND** reports the Item-context requirement


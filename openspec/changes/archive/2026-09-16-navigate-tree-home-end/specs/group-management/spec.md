## MODIFIED Requirements

### Requirement: Essential group management is keyboard accessible
FusionCanvas SHALL provide predictable keyboard navigation and shortcuts without requiring pointer interaction.

#### Scenario: Tree has keyboard focus
- **WHEN** the user operates the tree by keyboard
- **THEN** arrow keys navigate and expand or collapse nodes, Home selects the first visible node, End selects the last visible node, `Ctrl+Shift+N` creates a group, F2 renames, Ctrl+C/X/V copy/cut/paste, Enter commits inline editing, and Escape cancels it
- **AND** Home and End use the current expanded and filtered tree projection and bring the selected boundary node into view
- **AND** text-edit focus preserves normal caret navigation and prevents unrelated global structural shortcuts from firing

#### Scenario: Home or End is pressed with no visible nodes
- **WHEN** the tree has keyboard focus and the current filter produces no visible nodes
- **THEN** Home and End leave canonical selection and workspace state unchanged

#### Scenario: Operation is unavailable
- **WHEN** a shortcut has no valid source or destination
- **THEN** FusionCanvas leaves the workspace unchanged and communicates actionable guidance


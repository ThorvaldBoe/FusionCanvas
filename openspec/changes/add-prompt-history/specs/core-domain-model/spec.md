## MODIFIED Requirements

### Requirement: Prompts preserve prompt-related context
A Prompt SHALL represent preserved prompt-related context for current or future AI-assisted workflows, and SHALL be associable with relevant store, niche, listing, concept, design, or asset context.

#### Scenario: Prompt is connected without executing AI
- **WHEN** a prompt is represented in the domain model
- **THEN** it can preserve prompt text or prompt-related context
- **AND** it can be associated with relevant store, niche, listing, concept, design, or asset context
- **AND** it does not require an AI provider, execution result, token usage, or prompt library entry

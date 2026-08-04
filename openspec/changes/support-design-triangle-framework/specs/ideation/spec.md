## MODIFIED Requirements

### Requirement: Basic mode generates concise contextual candidates
Basic mode SHALL use the configured provider-independent AI text service to asynchronously request the desired number of varied, concise Idea candidates from the resolved creative context and optional guidance. Each request SHALL include the bundled canonical Design Triangle framework as system prompt context and SHALL instruct the model to produce one Idea-stage direction grounded in a wearer signal, intended viewer inference or effect, and audience-recognizable shared context, without producing a full refined Concept, finished design specification, or SLL artifact.

#### Scenario: User requests grumpy pug ideas
- **WHEN** the active niche is `Dogs`, the selected group is `Pugs`, the guidance is `Grumpy`, and the user generates in Basic mode
- **THEN** each request includes the canonical Design Triangle framework and asks for one short Idea direction that incorporates the pug and grumpy context
- **AND** the requested direction has a meaningful wearer-facing social proposition rather than generic decorative or topic-only copy
- **AND** it asks for neither a full refined Concept, a finished design specification, nor an SLL artifact

#### Scenario: Guidance is empty
- **WHEN** the user generates in Basic mode without guidance
- **THEN** generation still uses the canonical framework and the resolved store, niche, optional group, active Idea, and rejected-Idea context

### Requirement: Snowclones mode fills an in-memory template
Snowclones mode SHALL choose a template for each requested candidate from the application-wide persisted Snowclone Library, SHALL fill its variable positions using the resolved creative context, SHALL include the bundled canonical Design Triangle framework as system prompt context, and SHALL avoid repeating a template within one batch while unused catalog entries remain. It SHALL preserve the Snowclone contract by requesting only one completed phrase with no explanation unless essential, while asking for a result that expresses an audience-relevant identity, experience, attitude, or tension instead of generic humor.

#### Scenario: Snowclone candidate is generated
- **WHEN** the selected template is `Talk to me about {X}` and the active context concerns grumpy pugs
- **THEN** the AI request includes the canonical Design Triangle framework and asks for one relevant completed phrase such as `Talk to me about grumpy pugs`
- **AND** the result contains no unresolved placeholder and no explanation

#### Scenario: Batch fits within the catalog
- **WHEN** the requested count does not exceed the number of available Snowclone templates
- **THEN** each candidate in that batch uses a different template

#### Scenario: Batch exceeds the catalog
- **WHEN** the requested count exceeds the number of available Snowclone templates
- **THEN** templates may repeat only after every catalog entry has been used
- **AND** the generator still attempts to return distinct completed phrases

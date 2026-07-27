## ADDED Requirements

### Requirement: Ideation uses the provider-independent text service
FusionCanvas SHALL route production Basic and Snowclone candidate generation through `IAiTextGenerationService` with `Ideation` purpose and SHALL translate application-level AI results into one candidate success or one categorized candidate failure without automatic persistence or retry.

#### Scenario: Provider returns one concise candidate
- **WHEN** the Ideation text request succeeds with usable generated text
- **THEN** the generator returns the normalized candidate text to the Ideation orchestration
- **AND** provider diagnostics remain outside the candidate’s persisted Idea text

#### Scenario: Provider returns blank text
- **WHEN** the provider reports success but returns empty or whitespace-only generated text
- **THEN** the operation fails as an invalid provider response
- **AND** no blank candidate is added

#### Scenario: Provider request fails
- **WHEN** generation fails because of configuration, authentication, credit, rate limit, network, provider, cancellation, or invalid response
- **THEN** Ideation receives a stable secret-safe failure category and guidance
- **AND** the request is not automatically retried

#### Scenario: Candidate is undecided
- **WHEN** generated text is displayed but the creator has not selected Create or Reject
- **THEN** neither the request nor response is written to settings, prompt history, or workspace persistence


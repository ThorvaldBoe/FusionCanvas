## MODIFIED Requirements

### Requirement: Ideation availability uses placeholder API access
FusionCanvas SHALL derive Ideation availability from the securely stored OpenRouter inference credential and the effective Ideation AI profile, SHALL keep the action visible but disabled when generation is not ready, and SHALL identify the blocking prerequisite without reading or exposing secret material in presentation code.

#### Scenario: OpenRouter-backed Ideation is ready
- **WHEN** a readable OpenRouter inference key is saved and the effective Ideation profile resolves to an available compatible model
- **THEN** the Ideation action is enabled for a supported Idea-stage context
- **AND** generation uses the provider-independent AI text service with the Ideation request purpose

#### Scenario: OpenRouter key is absent
- **WHEN** no OpenRouter credential is saved
- **THEN** the Ideation action remains visible but disabled
- **AND** its unavailable guidance directs the creator to add a key in AI Settings

#### Scenario: Credential store is unavailable
- **WHEN** the saved credential cannot be read because native secure storage is locked, denied, or unavailable
- **THEN** the Ideation action remains disabled
- **AND** the guidance distinguishes credential unavailability from a missing key without exposing credential content

#### Scenario: Ideation profile is incomplete
- **WHEN** the credential is readable but the effective Ideation profile has no usable model or conflicts with the privacy policy or advertised capabilities
- **THEN** the Ideation action remains disabled
- **AND** the guidance directs the creator to complete the Ideation AI profile

#### Scenario: Environment placeholder is present
- **WHEN** `FUSIONCANVAS_AI_API_KEY` contains a value but no readable OpenRouter credential and profile are available
- **THEN** the environment value does not enable Ideation
- **AND** the value is neither persisted nor transmitted

### Requirement: Basic mode generates concise contextual candidates
Basic mode SHALL use the provider-independent AI text service to asynchronously request the desired number of varied, concise Idea candidates from the resolved creative context and optional guidance, using the effective Ideation AI profile.

#### Scenario: User requests grumpy pug ideas
- **WHEN** the active niche is `Dogs`, the selected group is `Pugs`, the guidance is `Grumpy`, and the user generates in Basic mode
- **THEN** each AI request asks for one short working Idea direction incorporating the pug and grumpy context
- **AND** it asks for neither a full refined Concept nor a finished design specification

#### Scenario: Guidance is empty
- **WHEN** the user generates in Basic mode without guidance
- **THEN** generation still uses the resolved store, niche, optional group, active Idea, and rejected-Idea context

#### Scenario: Ideation request is dispatched
- **WHEN** Basic generation invokes the AI text service
- **THEN** the request uses `Ideation` purpose and provider-independent messages
- **AND** credentials, entity identifiers, timestamps, file paths, archive flags, and other operational fields are absent from the messages

### Requirement: Snowclones mode fills an in-memory template
Snowclones mode SHALL select confirmed records from the application-wide persisted Snowclone Library, SHALL send each selected phrase and its guidance with the resolved creative context to the Ideation AI service, SHALL use brace-delimited placeholders, and SHALL avoid repeating a record within one batch while unused records remain.

#### Scenario: Persisted Snowclone candidate is generated
- **WHEN** the selected confirmed phrase is `Talk to me about {X}` with relevant guidance and the active context concerns grumpy pugs
- **THEN** the generated candidate is a relevant completed phrase such as `Talk to me about grumpy pugs`
- **AND** the result contains no unresolved brace-delimited placeholder

#### Scenario: Batch fits within the confirmed library
- **WHEN** the requested count does not exceed the number of confirmed Snowclone records
- **THEN** each generation request in that batch uses a different record

#### Scenario: Batch exceeds the confirmed library
- **WHEN** the requested count exceeds the number of confirmed Snowclone records
- **THEN** records may repeat only after every confirmed record has been selected
- **AND** generation still attempts to return distinct completed phrases

#### Scenario: Snowclone Library is empty
- **WHEN** Snowclones mode is selected and no confirmed Snowclone record exists
- **THEN** Generate is disabled for that mode
- **AND** the dialog explains that the creator can add or import Snowclones through `Manage Snowclones…`
- **AND** Basic mode remains available

#### Scenario: Provider leaves a placeholder unresolved
- **WHEN** the provider response still contains any brace-delimited placeholder from the selected template
- **THEN** that response is treated as a failed candidate operation
- **AND** the unresolved response is not added to the candidate list

### Requirement: Generation exposes bounded asynchronous progress
FusionCanvas SHALL generate candidates asynchronously with at most four concurrent AI text operations, SHALL prevent duplicate Generate submission while a batch is running, and SHALL expose a spinner and completed-versus-requested progress without blocking the UI thread or automatically retrying billable generation requests.

#### Scenario: Batch is running
- **WHEN** generation has started and not all requested operations have completed
- **THEN** a visible spinner and progress message are shown
- **AND** Generate, mode, guidance, and count controls cannot start or alter the running batch
- **AND** existing candidates remain visible

#### Scenario: Some operations fail
- **WHEN** at least one parallel generation succeeds and at least one fails
- **THEN** successful unique candidates remain available
- **AND** the dialog reports the partial failure without fabricating missing results or automatically retrying failed requests

#### Scenario: All operations fail
- **WHEN** every operation in a generation batch fails
- **THEN** no candidate is added
- **AND** the dialog reports a recoverable categorized error and re-enables generation controls

#### Scenario: Generator returns duplicates
- **WHEN** generated texts differ only by surrounding whitespace, repeated whitespace, or letter case
- **THEN** the candidate list retains only one normalized equivalent
- **AND** progress still completes without an unbounded retry loop


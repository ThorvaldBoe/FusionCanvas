## ADDED Requirements

### Requirement: Live API verification is an explicit manual session
FusionCanvas SHALL provide one developer-run verification entry point for supported external API providers, with provider-specific scenarios kept separate within that entry point. Live scenarios SHALL remain outside the deterministic solution test baseline and unattended CI.

#### Scenario: A developer lists verification scenarios
- **WHEN** the developer requests available providers and scenarios
- **THEN** the tool SHALL list their network, write, cost, and cleanup effects without contacting a provider

#### Scenario: A developer starts a session
- **WHEN** the developer explicitly selects a provider and scenario and requests a run
- **THEN** the tool SHALL validate dedicated credentials and target configuration
- **AND** display the provider, target, planned requests, possible writes, maximum cost, and recovery plan before network access
- **AND** require explicit confirmation to proceed

#### Scenario: A live scenario is invoked through the solution baseline
- **WHEN** the default deterministic solution test command runs
- **THEN** no live API scenario SHALL execute or require network access or provider credentials

### Requirement: The live test inventory covers application API operations
FusionCanvas SHALL maintain a versioned inventory that maps every external API operation used by the application to one or more live scenarios, or to an explicit blocked/restricted status with rationale. The inventory SHALL be checked against the production adapters when they change.

#### Scenario: An API operation is implemented or changed
- **WHEN** a production adapter adds or changes an API operation, endpoint, request parameter, or response mapping
- **THEN** the inventory SHALL identify the affected operation and its live scenario coverage
- **AND** a missing live scenario SHALL remain an explicit coverage gap rather than being reported as verified

#### Scenario: A live session reports coverage
- **WHEN** a provider scenario session completes
- **THEN** its report SHALL identify covered, passed, failed, skipped, restricted, and not-yet-covered inventory entries
- **AND** SHALL not count skipped or restricted operations as passed

### Requirement: Live findings improve production services and deterministic tests
Every material live discrepancy SHALL be recorded against the production service operation and SHALL lead to a service correction or an evidence-backed accepted limitation, plus deterministic regression coverage when the defect is reproducible without a live provider.

#### Scenario: A live request or response exposes a defect
- **WHEN** a provider rejects a request or returns data that the production adapter handles incorrectly
- **THEN** the finding SHALL preserve sanitized expected-versus-actual evidence and identify the affected operation and request/response concern
- **AND** the production service SHALL be corrected and a deterministic regression test SHALL cover the defect before the finding is marked resolved
- **AND** the affected live scenario SHALL be rerun after the correction

#### Scenario: The observed behavior is not a service defect
- **WHEN** evidence shows that an operation is unavailable, restricted, undocumented, or provider-dependent rather than incorrectly implemented
- **THEN** the finding SHALL retain the evidence, rationale, and unresolved or restricted status
- **AND** it SHALL not be silently converted into a passing result

### Requirement: OpenRouter supports an on-demand per-model qualification run
The verification tool SHALL offer a separate, resumable qualification campaign that tests the OpenRouter model families and parameter/profile combinations supported by FusionCanvas. Qualification results SHALL be keyed by model identity, operation, parameter-profile fingerprint, and adapter version so routine runs can reuse prior evidence and broad live calls are not repeated automatically.

#### Scenario: A developer plans model qualification
- **WHEN** the developer requests an OpenRouter qualification plan
- **THEN** the tool SHALL enumerate the currently discoverable compatible text and image models and the effective parameter profiles/features FusionCanvas can send for each
- **AND** show estimated request count, eligible models, excluded models, and conservative maximum cost before any generation request

#### Scenario: A model supports an application profile
- **WHEN** the developer confirms qualification for that model and the session budget covers its bounded request
- **THEN** the tool SHALL call the production OpenRouter service using the model-specific effective parameters and operation
- **AND** record accepted, rejected, unsupported, and inconclusive outcomes separately for the model/profile pair

#### Scenario: Model qualification exceeds the budget
- **WHEN** some or all planned model calls cannot fit under the explicit session cost ceiling
- **THEN** the tool SHALL run only the model calls the developer selects from the preflight plan
- **AND** mark the remainder budget-blocked or deferred without sending those requests

#### Scenario: Qualification evidence already exists
- **WHEN** a model/operation/profile fingerprint has prior successful live evidence and the adapter version and discovered capability fingerprint are unchanged
- **THEN** the routine session SHALL reuse that evidence without repeating a billable request
- **AND** an explicit force option SHALL be required to run it again

#### Scenario: A model's parameter combination fails
- **WHEN** a model rejects or mishandles an effective parameter combination that succeeds with another supported model
- **THEN** the tool SHALL record the exact redacted model/profile combination and provider response category
- **AND** expose the finding for a production parameter-mapping fix and deterministic regression test

### Requirement: OpenRouter live sessions are bounded by a hard cost ceiling
Each OpenRouter session SHALL require a configured maximum cost, bounded request count, and bounded input/output for every billable operation. The tool SHALL fail closed if it cannot establish that planned requests remain within the session ceiling.

#### Scenario: Planned OpenRouter calls fit the ceiling
- **WHEN** a developer confirms an OpenRouter session
- **THEN** each request SHALL use explicit model/route and token/image limits
- **AND** the cumulative conservative maximum charge SHALL not exceed the configured session ceiling
- **AND** actual reported usage and charge SHALL be captured when available

#### Scenario: An OpenRouter request has unknown or excessive cost
- **WHEN** current price, route, request limits, or image-generation pricing are unavailable, ambiguous, or exceed the ceiling
- **THEN** the affected request SHALL be blocked before submission and reported as cost-blocked

### Requirement: Printify verification covers application operations in the DevTest store
Printify live verification SHALL exercise every non-cost-incurring API operation used by the application against the explicitly configured **FusionCanvas DevTest** store, using production application and Integration services. It MAY create, update, publish, unpublish, and delete test products after per-write confirmation. Order submission and any operation that could incur a charge SHALL remain unavailable.

#### Scenario: A Printify run is planned
- **WHEN** a developer selects one or more Printify scenarios
- **THEN** preflight SHALL identify the authenticated account, exact FusionCanvas DevTest shop, each API operation, every write, and any recovery action
- **AND** SHALL block if the configured target cannot be positively matched to the dedicated DevTest shop
- **AND** SHALL exclude order and cost-incurring operations

#### Scenario: A Printify write is executed
- **WHEN** the developer confirms a planned product or asset write
- **THEN** the tool SHALL use the production service and SHALL record the owned resource identity before subsequent operations when the provider response permits
- **AND** verify the resulting state through a production read operation
- **AND** ask separately before each subsequent remote write

#### Scenario: A Printify publication operation is executed
- **WHEN** the developer explicitly selects publish or unpublish for a DevTest product
- **THEN** the tool SHALL show the exact shop and product and its external visibility effect
- **AND** require explicit confirmation before the operation
- **AND** verify the resulting publication state through the production service

#### Scenario: A Printify write fails or cleanup is incomplete
- **WHEN** a write, verification, or best-effort cleanup fails
- **THEN** the tool SHALL record sanitized evidence and any known resource identity in the session journal
- **AND** report the failure or cleanup-pending status without claiming a pass
- **AND** permit recovery only for resources positively associated with the session

### Requirement: Live reports preserve evidence without secrets
Each completed, failed, blocked, or interrupted session SHALL produce a machine-readable and human-readable report containing provider/API version where known, inventory coverage, scenarios, findings, expected and actual behavior, sanitized diagnostics, estimated and actual cost, model qualifications, and cleanup state.

#### Scenario: A session report is written
- **WHEN** a live session ends, fails, or is cancelled
- **THEN** the report SHALL distinguish passed, failed, skipped, restricted, cost-blocked, unresolved, and cleanup-pending outcomes
- **AND** SHALL NOT contain credentials, authorization headers, raw secret-bearing payloads, or unsanitized provider responses
